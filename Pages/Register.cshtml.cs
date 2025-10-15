using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GeradorDeClientes.Models;
using GeradorDeClientes.Services;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace GeradorDeClientes.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly IUserService _userService;

        public RegisterModel(IUserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public Usuario Usuario { get; set; } = new Usuario();

        public string Mensagem { get; set; } = string.Empty;

        private static string Sha256(string? input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static bool IsPasswordStrong(string pwd)
        {
            if (pwd.Length < 8) return false;
            if (!Regex.IsMatch(pwd, "[A-Z]")) return false;
            if (!Regex.IsMatch(pwd, "[a-z]")) return false;
            if (!Regex.IsMatch(pwd, "[0-9]")) return false;
            return true;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Usuario == null || string.IsNullOrEmpty(Usuario.Email) || string.IsNullOrEmpty(Usuario.Senha))
            {
                Mensagem = "Preencha email e senha.";
                return Page();
            }

            if (!IsPasswordStrong(Usuario.Senha))
            {
                Mensagem = "Senha fraca. Deve ter ao menos 8 caracteres, letras maiúsculas/minúsculas e números.";
                return Page();
            }

            var normalizedEmail = Usuario.Email.Trim().ToLowerInvariant();
            if (await _userService.EmailExistsAsync(normalizedEmail))
            {
                Mensagem = "Este e-mail já está sendo usado.";
                return Page();
            }

            var pwd = Usuario.Senha.Trim();
            var hashed = Sha256(pwd);
            Usuario.Senha = hashed;
            Usuario.Email = normalizedEmail;

            var ok = await _userService.CreateUserAsync(Usuario);
            if (!ok)
            {
                Mensagem = "Não foi possível criar o usuário.";
                return Page();
            }

            return RedirectToPage("/Login");
        }
    }
}
