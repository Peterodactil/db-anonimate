using AnonimizarDados.Dtos;
using AnonimizarDados.Enums;
using AnonimizarDados.Servicos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AnonimizarDados;

internal static class Program
{
    private static readonly CancellationTokenSource TokenSource = new();

    private static async Task Main()
    {
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            Console.WriteLine("Operação cancelada.");
            TokenSource.Cancel();
            eventArgs.Cancel = true;
        };

        var configuracao = LerArquivoDeConfiguracao();

        var conexoes = configuracao.GetSection("ConnectionStrings").Get<Conexoes>();

        if (conexoes is null || string.IsNullOrEmpty(conexoes.ObterStringConexao()))
        {
            Console.WriteLine("Nenhuma string de conexão definida.");
            return;
        }

        var parametros = configuracao.GetSection(nameof(Instrucoes)).Get<Instrucoes>();

        if (parametros is null)
        {
            Console.WriteLine("Nenhum parâmetro registrado.");
            return;
        }

        var stringConexao = conexoes.ObterStringConexao();

        IDataBaseService servico = conexoes.TipoBanco() switch
        {
            TipoServicoBanco.Postgres => new PostgresService(stringConexao),
            _ => new SqlServerService(stringConexao),
        };

        if (!servico.TestarConexaoComBanco()) return;

        if (parametros.AtualizarPorValor.Any())
        {
            var dadosMapeados = parametros.AtualizarPorValor.GroupBy(
                keySelector: p => p,
                elementSelector: p => new KeyValuePair<string, object?>(p.Coluna, p.Valor.Equals("null") ? null : p.Valor),
                resultSelector: (g, p) => new InstrucaoAtualizacaoPorValor(g, p),
                comparer: new ParametrosAtualizacaoDeValorEqualityComparer())
            .ToList();

            await servico.AtualizarAsync(
                dadosMapeados,
                TokenSource.Token);
        }

        if (parametros.AtualizarPorRegra.Any())
        {
            var dadosMapeados = parametros.AtualizarPorRegra.GroupBy(
                keySelector: p => p,
                elementSelector: p => new KeyValuePair<string, RegrasEnum>(p.Coluna, p.Regra),
                resultSelector: (g, p) => new InstrucaoAtualizacaoPorRegra(g, g.ColunaId, p),
                comparer: new ParametrosAtualizacaoDeRegraEqualityComparer())
            .ToList();

            await servico.AtualizarAsync(
                dadosMapeados,
                TokenSource.Token);
        }

        if (parametros.Excluir.Any())
        {
            await servico.ExcluirAsync(
                parametros.Excluir,
                TokenSource.Token);
        }

        if (parametros.Truncar.Any())
        {
            await servico.TruncarAsync(
                parametros.Truncar,
                TokenSource.Token);
        }

        Console.WriteLine("Operação finalizada. Pressione qualquer tecla para encerrar.");
        Console.ReadKey();
    }

    private static IConfiguration LerArquivoDeConfiguracao()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false, true);

        return configurationBuilder.Build();
    }
}