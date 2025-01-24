using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AnonimizarDados.Dtos;

public class ParametrosAtualizacaoPorValor : EstruturaTabela
{
    public string Valor { get; set; }

    public ParametrosAtualizacaoPorValor()
    {
        Valor = string.Empty;
    }
}

public class InstrucaoAtualizacaoPorValor
{
    public EstruturaTabela Tabela { get; set; }

    public IEnumerable<KeyValuePair<string, object?>> ColunaValor { get; }

    public InstrucaoAtualizacaoPorValor(
        EstruturaTabela tabela,
        IEnumerable<KeyValuePair<string, object?>> colunaValor)
    {
        Tabela = tabela;
        ColunaValor = colunaValor;
    }
}

public class ParametrosAtualizacaoDeValorEqualityComparer : IEqualityComparer<ParametrosAtualizacaoPorValor>
{
    public bool Equals(ParametrosAtualizacaoPorValor? x, ParametrosAtualizacaoPorValor? y)
    {
        return x?.NomeCompletoTabela == y?.NomeCompletoTabela;
    }

    public int GetHashCode([DisallowNull] ParametrosAtualizacaoPorValor obj)
    {
        return obj.NomeCompletoTabela.GetHashCode();
    }
}