using AnonimizarDados.Enums;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AnonimizarDados.Dtos;

public class ParametrosAtualizacaoPorRegra : EstruturaTabela
{
    public string ColunaId { get; set; }
    
    public RegrasEnum Regra { get; set; }

    public ParametrosAtualizacaoPorRegra()
    {
        ColunaId = string.Empty;
        Regra = RegrasEnum.Infefinido;
    }
}

public class InstrucaoAtualizacaoPorRegra
{
    public string ColunaId { get; }

    public EstruturaTabela Tabela { get; set; }

    public IEnumerable<KeyValuePair<string, RegrasEnum>> ColunaRegra { get; }

    public InstrucaoAtualizacaoPorRegra(
        EstruturaTabela tabela,
        string colunaId,
        IEnumerable<KeyValuePair<string, RegrasEnum>> colunaRegra)
    {
        ColunaId = colunaId;
        Tabela = tabela;
        ColunaRegra = colunaRegra;
    }
}

public class ParametrosAtualizacaoDeRegraEqualityComparer : IEqualityComparer<ParametrosAtualizacaoPorRegra>
{
    public bool Equals(ParametrosAtualizacaoPorRegra? x, ParametrosAtualizacaoPorRegra? y)
    {
        return x?.NomeCompletoTabela == y?.NomeCompletoTabela;
    }

    public int GetHashCode([DisallowNull] ParametrosAtualizacaoPorRegra obj)
    {
        return obj.NomeCompletoTabela.GetHashCode();
    }
}