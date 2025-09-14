using System.ComponentModel.DataAnnotations;

namespace Producer.Api.Models
{
    public class ArmazemViewModel
    {
        [Key]
        public Guid Id { get; set; }
        [Required(ErrorMessage = "Campo {0} precisa ser preenchido.")]
        public string? Nome { get; set; }
        [Required(ErrorMessage = "Campo {0} precisa ser preenchido.")]
        public EnderecoViewModel? Endereco { get; set; }
        [Required(ErrorMessage = "Campo {0} precisa ser preenchido.")]
        public string? Tipo { get; set; }
    }
}
