namespace AnonimizarDados.Dtos;

public class DefinicaoTabela
{
    public string Esquema { get; set; }
    
    public string Tabela { get; set; }

    public string NomeCompletoTabela => $"{Esquema}.{Tabela}";

    protected DefinicaoTabela()
    {
        Esquema = string.Empty;
        Tabela = string.Empty;
    }
}