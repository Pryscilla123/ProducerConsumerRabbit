using Consumer.Api.Models;

namespace Consumer.Api.Repository
{
    public interface IArmazemRepository
    {
        Task CriarArmazem(Armazem armazem);
    }
}
