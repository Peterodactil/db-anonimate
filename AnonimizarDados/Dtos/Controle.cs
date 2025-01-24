namespace AnonimizarDados.Dtos
{
    public class Controle
    {
        public int Quantidade { get; private set; }

        public int Pular { get; private set; }

        public int Resto { get; private set; }

        public Controle(
            int resto,
            short quantidade = 3_000,
            short pular = 0)
        {
            Quantidade = quantidade;
            Pular = pular;
            Resto = resto;
        }

        public void AtualizarControle()
        {
            Pular += Quantidade;
            Resto -= Quantidade;

            Quantidade = Resto > Quantidade ?
                Quantidade :
                Resto;
        }
    }
}
