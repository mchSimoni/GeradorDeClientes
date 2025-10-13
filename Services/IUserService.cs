using GeradorDeClientes.Models;

namespace GeradorDeClientes.Services
{
    public interface IUserService
    {
        Task<bool> EmailExistsAsync(string email);
        Task<bool> CreateUserAsync(Usuario user);
        Task<Usuario?> GetByEmailAsync(string email);
    }
}
