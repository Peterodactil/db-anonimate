namespace AnonimizarDados.Dtos;

public abstract class EstruturaTabela : DefinicaoTabela
{
    public string Coluna { get; set; }

    protected EstruturaTabela()
    {
        Coluna = string.Empty;
    }
}