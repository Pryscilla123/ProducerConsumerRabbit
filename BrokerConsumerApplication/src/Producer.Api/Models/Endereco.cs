namespace Consumer.Api.Models
{
    public class Endereco
    {
        public string? Rua { get; set; }

        public string? Bairro { get; set; }
        public int Numero { get; set; }
        public string? Cidade { get; set; }
        public string? Estado { get; set; }
        public string? Cep { get; set; }
    }
}
