using System.ComponentModel.DataAnnotations;

namespace Producer.Api.Models
{
    public class EnderecoViewModel
    {
        [Required(ErrorMessage = "Campo {0} precisa ser preenchido.")]
        public string? Rua { get; set; }

        [Required(ErrorMessage = "Campo {0} precisa ser preenchido.")]
        public string? Bairro { get; set; }

        [Required(ErrorMessage = "Campo {0} precisa ser preenchido.")]
        [Range(0, 1000, ErrorMessage = "O campo {0} deve ter no máximo {1} caracteres.")]
        public int Numero { get; set; }

        [Required(ErrorMessage = "Campo {0} precisa ser preenchido.")]
        public string? Cidade { get; set; }

        [Required(ErrorMessage = "Campo {0} precisa ser preenchido.")]
        public string? Estado { get; set; }

        [Required(ErrorMessage = "Campo {0} precisa ser preenchido.")]
        [RegularExpression(@"^\d{5}-\d{3}$", ErrorMessage = "O campo {0} deve estar no formato 12345-678.")]
        public string? Cep { get; set; }
    }
}
