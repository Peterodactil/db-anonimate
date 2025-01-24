using AnonimizarDados.Dtos;
using AnonimizarDados.Enums;
using Dapper;
using Microsoft.Data.SqlClient;
using Npgsql;
using SqlKata;
using SqlKata.Compilers;
using SqlKata.Execution;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AnonimizarDados.Servicos
{
    public interface IDataBaseService
    {
        bool TestarConexaoComBanco();

        Task<bool> VerificarExistenciaTabela(
            DefinicaoTabela tabela,
            CancellationToken cancellationToken);

        Task AtualizarAsync(
            IEnumerable<InstrucaoAtualizacaoPorRegra> parametrosParaAtualizar,
            CancellationToken cancellationToken);

        Task AtualizarAsync(
            IEnumerable<InstrucaoAtualizacaoPorValor> parametrosParaAtualizar,
            CancellationToken cancellationToken);

        Task ExcluirAsync(
            IEnumerable<InstrucaoExclusao> parametrosParaExcluir,
            CancellationToken cancellationToken);

        Task TruncarAsync(
            IEnumerable<InstrucaoExclusao> parametrosParaExcluir,
            CancellationToken cancellationToken);
    }

    public class PostgresService(string connectionString)
        : TemplateService(
            new NpgsqlConnection(connectionString),
            new PostgresCompiler())
    {
    }

    public class SqlServerService(string connectionString)
        : TemplateService(
            new SqlConnection(connectionString),
            new SqlServerCompiler())
    {
    }

    public abstract class TemplateService(IDbConnection dbConnection, Compiler compiler) : IDataBaseService
    {
        protected readonly QueryFactory _queryFactory = new(dbConnection, compiler);

        public bool TestarConexaoComBanco()
        {
            try
            {
                dbConnection.Open();

                if (dbConnection.Database.EndsWith("_RANDOM", StringComparison.InvariantCultureIgnoreCase))
                    return true;

                Console.WriteLine("Banco de dados inválido. Aponte para um banco de dados com final '_RANDOM'.");

                return false;
            }
            catch (SqlException ex)
            {
                Console.WriteLine("Falha ao conectar no banco de dados.");
                Console.WriteLine(ex.Message);

                return false;
            }
            finally
            {
                dbConnection.Close();
            }
        }

        public virtual async Task<bool> VerificarExistenciaTabela(
            DefinicaoTabela tabela,
            CancellationToken cancellationToken)
        {
            var query = $@"
                IF EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_TYPE='BASE TABLE'
                    AND TABLE_SCHEMA = '{tabela.Esquema}'
                    AND TABLE_NAME = '{tabela.Tabela}')
                SELECT 1
                ELSE
                SELECT 0";

            query = $@"
                SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_TYPE='BASE TABLE'
                AND TABLE_SCHEMA = '{tabela.Esquema}'
                AND TABLE_NAME = '{tabela.Tabela}'";

            var tabelaExiste = await dbConnection.ExecuteScalarAsync<bool>(query, cancellationToken);

            if (tabelaExiste) return true;

            LogService.Info($"Tabela não encontrada: {tabela.NomeCompletoTabela}");
            return false;
        }

        public async Task AtualizarAsync(
            IEnumerable<InstrucaoAtualizacaoPorRegra> parametrosParaAtualizar,
            CancellationToken cancellationToken)
        {
            foreach (var parametroParaAtualizar in parametrosParaAtualizar)
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (!await VerificarExistenciaTabela(parametroParaAtualizar.Tabela, cancellationToken)) continue;

                LogService.Info($"Anonimizando: {parametroParaAtualizar.Tabela.NomeCompletoTabela}");

                var quantidadeDeRegistros = await _queryFactory
                    .Query(parametroParaAtualizar.Tabela.NomeCompletoTabela)
                    .CountAsync<int>(cancellationToken: cancellationToken);

                var colunasRegras = parametroParaAtualizar.ColunaRegra.ToArray();
                var controle = new Controle(quantidadeDeRegistros);

                var sbCommand = new StringBuilder();
                var sbUpdate = new StringBuilder();

                while (controle.Resto > 0)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    var ids = await _queryFactory.Query(parametroParaAtualizar.Tabela.NomeCompletoTabela)
                        .Select($"{parametroParaAtualizar.ColunaId}")
                        .OrderBy($"{parametroParaAtualizar.ColunaId}")
                        .Limit(controle.Quantidade)
                        .Skip(controle.Pular)
                        .GetAsync<int>(cancellationToken: cancellationToken);

                    var builder = CriarBuilder(colunasRegras);
                    var dataEnumerator = builder.Criar().Generate(controle.Resto).GetEnumerator();
                    var idsEnumerator = ids.GetEnumerator();

                    for (var i = 0; i < controle.Resto; i++)
                    {
                        dataEnumerator.MoveNext();
                        idsEnumerator.MoveNext();

                        if (cancellationToken.IsCancellationRequested) return;

                        var entidade = dataEnumerator.Current;
                        var id = idsEnumerator.Current;

                        sbUpdate.Clear();

                        //sbUpdate.Append($"UPDATE {parametroParaAtualizar.Tabela.NomeCompletoTabela} SET ");

                        //var sets = string.Join(", ", colunasRegras.Select(s => $"{s.Key} = '{RegraService.ObterValor(entidade, s.Value)}'"));
                        //sbUpdate.Append(sets);

                        //sbUpdate.Append($" WHERE {parametroParaAtualizar.ColunaId} = {id}");

                        var q = new Query(parametroParaAtualizar.Tabela.NomeCompletoTabela)
                            .Where(parametroParaAtualizar.ColunaId, id)
                            .AsUpdate(colunasRegras.Select(s =>
                                new KeyValuePair<string, object>(
                                    s.Key,
                                    RegraService.ObterValor(entidade, s.Value))
                                )
                            );

                        sbCommand.AppendLine(compiler.Compile(q).RawSql);
                        _queryFactory.Execute(q);
                        //sbCommand.AppendLine(sbUpdate.ToString());
                    }

                    //await dbConnection.ExecuteAsync(
                    //    sbCommand.ToString(),
                    //    cancellationToken);

                    controle.AtualizarControle();

                    sbCommand.Clear();
                }
            }
        }

        private static BuilderEntidadeFicticia CriarBuilder(IEnumerable<KeyValuePair<string, RegrasEnum>> colunas)
        {
            var regrasDistintas = colunas.Select(c => c.Value).Distinct();

            var builder = new BuilderEntidadeFicticia();

            foreach (var regra in regrasDistintas)
            {
                RegraService.GerarBuilder(builder, regra);
            }

            return builder;
        }

        public async Task AtualizarAsync(
            IEnumerable<InstrucaoAtualizacaoPorValor> parametrosParaAtualizar,
            CancellationToken cancellationToken)
        {
            foreach (var parametroParaAtualizar in parametrosParaAtualizar)
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (!await VerificarExistenciaTabela(parametroParaAtualizar.Tabela, cancellationToken)) continue;

                LogService.Info($"Atualizando: {parametroParaAtualizar.Tabela.NomeCompletoTabela}");

                await _queryFactory.Query(parametroParaAtualizar.Tabela.NomeCompletoTabela)
                    .UpdateAsync(
                        parametroParaAtualizar.ColunaValor,
                        cancellationToken: cancellationToken);
            }
        }

        public virtual async Task ExcluirAsync(
            IEnumerable<InstrucaoExclusao> parametrosParaExcluir,
            CancellationToken cancellationToken)
        {
            foreach (var parametroParaExcluir in parametrosParaExcluir.OrderBy(o => o.Prioridade))
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (!await VerificarExistenciaTabela(parametroParaExcluir, cancellationToken)) continue;

                LogService.Info($"Excluindo: {parametroParaExcluir.NomeCompletoTabela}");

                await _queryFactory.Query(parametroParaExcluir.NomeCompletoTabela)
                    .DeleteAsync(cancellationToken: cancellationToken);
            }
        }

        public virtual async Task TruncarAsync(
            IEnumerable<InstrucaoExclusao> parametrosParaExcluir,
            CancellationToken cancellationToken)
        {
            foreach (var parametroParaExcluir in parametrosParaExcluir.OrderBy(o => o.Prioridade))
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (!await VerificarExistenciaTabela(parametroParaExcluir, cancellationToken)) continue;

                LogService.Info($"Truncando: {parametroParaExcluir.NomeCompletoTabela}");

                await dbConnection.ExecuteAsync($"TRUNCATE TABLE {parametroParaExcluir.NomeCompletoTabela}", cancellationToken);
            }
        }
    }
}
