using GeradorDeClientes.Models;
using GeradorDeClientes.Data;
using Microsoft.EntityFrameworkCore;

namespace GeradorDeClientes.Services
{
    public class EfUserService : IUserService
    {
        private readonly ApplicationDbContext _db;

        public EfUserService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<bool> CreateUserAsync(Usuario user)
        {
            if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Senha)) return false;
            var exists = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == user.Email.ToLower());
            if (exists) return false;
            _db.Usuarios.Add(user);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            return await _db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            return await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }
    }
}
