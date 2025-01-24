using System.Collections.Generic;

namespace AnonimizarDados.Dtos;

public class Instrucoes
{
    public IEnumerable<ParametrosAtualizacaoPorValor> AtualizarPorValor { get; set; }
    
    public IEnumerable<ParametrosAtualizacaoPorRegra> AtualizarPorRegra { get; set; }
    
    public IEnumerable<InstrucaoExclusao> Excluir { get; set; }
    
    public IEnumerable<InstrucaoExclusao> Truncar { get; set; }
    
    public Instrucoes()
    {
        AtualizarPorValor = [];
        AtualizarPorRegra = [];
        Excluir = [];
        Truncar = [];
    }
}