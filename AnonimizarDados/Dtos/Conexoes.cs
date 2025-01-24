using AnonimizarDados.Enums;

namespace AnonimizarDados.Dtos
{
    public class Conexoes
    {
        public required string Oracle { get; set; }
        public required string Postgres { get; set; }
        public required string SqlServer { get; set; }

        public string ObterStringConexao()
        {
            if (!string.IsNullOrEmpty(Oracle)) return Oracle;
            if (!string.IsNullOrEmpty(Postgres)) return Postgres;
            return SqlServer;
        }

        public TipoServicoBanco TipoBanco()
        {
            if (!string.IsNullOrEmpty(Oracle)) return TipoServicoBanco.Oracle;
            if (!string.IsNullOrEmpty(Postgres)) return TipoServicoBanco.Postgres;
            return TipoServicoBanco.SqlServer;
        }
    }
}
