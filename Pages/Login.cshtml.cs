using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using GeradorDeClientes.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Cryptography;
using System.Text;

namespace GeradorDeClientes.Pages
{
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;
        private readonly GeradorDeClientes.Services.IUserService _userService;

        public LoginModel(ILogger<LoginModel> logger, GeradorDeClientes.Services.IUserService userService)
        {
            _logger = logger;
            _userService = userService;
        }

        [BindProperty]
        public Usuario Usuario { get; set; } = new Usuario();

        public string MensagemErro { get; set; } = string.Empty;

        private static string Sha256(string? input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input ?? string.Empty));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public async Task<IActionResult> OnPost()
        {
            _logger.LogInformation("Tentativa de login para {Email}", Usuario?.Email ?? "(null)");

            if (Usuario == null || string.IsNullOrEmpty(Usuario.Email) || string.IsNullOrEmpty(Usuario.Senha))
            {
                MensagemErro = "Usuário ou senha inválidos.";
                _logger.LogInformation("Falha no login por credenciais ausentes para {Email}", Usuario?.Email ?? "(null)");
                return Page();
            }

            var stored = await _userService.GetByEmailAsync(Usuario.Email!);
            if (stored != null && Sha256(Usuario.Senha) == stored.Senha)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, Usuario.Email),
                    new Claim(ClaimTypes.Email, Usuario.Email)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                _logger.LogInformation("Login realizado com sucesso para {Email}", Usuario.Email);
                return RedirectToPage("/GerarExcel");
            }

            MensagemErro = "Usuário ou senha inválidos.";
            _logger.LogInformation("Falha no login para {Email}", Usuario.Email);
            return Page();
        }
    }
}
