using GeradorDeClientes.Models;
using System.Text.Json;

namespace GeradorDeClientes.Services
{
    public class FileUserService : IUserService
    {
        private readonly string _filePath;
        private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1,1);

        public FileUserService(IWebHostEnvironment env)
        {
            var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dataDir);
            _filePath = Path.Combine(dataDir, "users.json");
        }

        private async Task<List<Usuario>> ReadAllAsync()
        {
            await _mutex.WaitAsync();
            try
            {
                if (!File.Exists(_filePath)) return new List<Usuario>();
                using var s = File.OpenRead(_filePath);
                return await JsonSerializer.DeserializeAsync<List<Usuario>>(s) ?? new List<Usuario>();
            }
            finally { _mutex.Release(); }
        }

        private async Task WriteAllAsync(List<Usuario> users)
        {
            await _mutex.WaitAsync();
            try
            {
                using var s = File.Create(_filePath);
                await JsonSerializer.SerializeAsync(s, users, new JsonSerializerOptions { WriteIndented = true });
            }
            finally { _mutex.Release(); }
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            var all = await ReadAllAsync();
            return all.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<bool> CreateUserAsync(Usuario user)
        {
            if (user == null || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Senha)) return false;
            var all = await ReadAllAsync();
            if (all.Any(u => string.Equals(u.Email, user.Email, StringComparison.OrdinalIgnoreCase))) return false;
            all.Add(user);
            await WriteAllAsync(all);
            return true;
        }

        public async Task<Usuario?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            var all = await ReadAllAsync();
            return all.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        }
    }
}
