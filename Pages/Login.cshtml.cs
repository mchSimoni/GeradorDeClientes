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

            var normalizedEmail = Usuario.Email!.Trim().ToLowerInvariant();
            var pwd = Usuario.Senha.Trim();
            var stored = await _userService.GetByEmailAsync(normalizedEmail);

            if (stored == null)
            {
                _logger.LogWarning("Login failed: user not found for {Email}", normalizedEmail);
                MensagemErro = "Usuário ou senha inválidos.";
                return Page();
            }

            _logger.LogInformation("User found: Id={Id} Email={Email} StoredHashLength={Len}", stored.Id, stored.Email, stored.Senha?.Length ?? 0);

            var computedHash = Sha256(pwd);
            var hashesEqual = string.Equals(computedHash, stored.Senha, StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("Password hash comparison for {Email}: computedLen={ComputedLen} equal={Equal}", normalizedEmail, computedHash.Length, hashesEqual);

            if (!hashesEqual)
            {
                MensagemErro = "Usuário ou senha inválidos.";
                _logger.LogWarning("Invalid password for {Email}", normalizedEmail);
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, normalizedEmail),
                new Claim(ClaimTypes.Email, normalizedEmail)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            try
            {
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                _logger.LogInformation("Login realizado com sucesso para {Email}", normalizedEmail);
                return RedirectToPage("/GerarExcel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignInAsync failed for {Email}", normalizedEmail);
                MensagemErro = "Ocorreu um erro ao autenticar. Verifique os logs do servidor.";
                return Page();
            }
        }
    }
}
