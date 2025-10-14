using Microsoft.EntityFrameworkCore;
using GeradorDeClientes.Models;

namespace GeradorDeClientes.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
    }
}
