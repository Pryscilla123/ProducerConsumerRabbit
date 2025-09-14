namespace Consumer.Api.Models
{
    public class Armazem
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }
        public Endereco? Endereco { get; set; }
        public string? Tipo { get; set; }
    }
}
