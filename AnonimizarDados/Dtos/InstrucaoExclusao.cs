namespace AnonimizarDados.Dtos;

public class InstrucaoExclusao : DefinicaoTabela
{
    public ushort Prioridade { get; set; }

    public InstrucaoExclusao()
    {
        Prioridade = 0;
    }
}