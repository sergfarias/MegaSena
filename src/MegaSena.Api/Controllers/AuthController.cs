using MegaSena.Api.Models;
using MegaSena.Application.Servicos;
using MegaSena.Infrastructure.Data;
using MegaSena.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;
    private readonly IWebHostEnvironment _env;

    public AuthController(AppDbContext context, AuthService authService, IWebHostEnvironment env)
    {
        _context = context;
        _authService = authService;
        _env = env;
    }

    [HttpPost("registrar")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Registrar([FromBody] RegistroRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verifica se o login já existe
        if (await _context.Usuarios.AnyAsync(u => u.Login == request.Login))
        {
            // Gasta o mesmo tempo de um BCrypt real para mitigar timing attack
            _authService.GerarHashSenha("dummy-timing-attack");
            return BadRequest(new { Erro = "Este login já está em uso." });
        }

        // Verifica se o e-mail já existe
        if (await _context.Usuarios.AnyAsync(u => u.Email == request.Email))
        {
            // Gasta o mesmo tempo de um BCrypt real para mitigar timing attack
            _authService.GerarHashSenha("dummy-timing-attack");
            return BadRequest(new { Erro = "Este e-mail já está cadastrado." });
        }

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Login = request.Login,
            Email = request.Email,
            SenhaHash = _authService.GerarHashSenha(request.Senha),
            Ativo = true,
            DataCriacao = DateTime.UtcNow
        };

        try
        {
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { Erro = "Não foi possível criar o usuário. Tente novamente." });
        }

        return Ok(new { Mensagem = "Usuário criado com sucesso! Faça login para continuar." });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Login == request.Login);

        // =========================================================
        // SEÇÃO 3.2: Evitar Enumeração de Usuários (Timing Attack)
        // =========================================================
        if (usuario == null)
        {
            // Se o usuário não existe, verificamos um hash falso para gastar o mesmo tempo
            // que o BCrypt levaria para verificar uma senha real.
            // _authService.VerificarSenha(request.Senha, "$2a$11$abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ");
            // Hash BCrypt válido para mitigar timing attack
            _authService.VerificarSenha(request.Senha, "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy");
            return Unauthorized(new { Erro = "Login ou senha inválidos." });
        }

        if (!_authService.VerificarSenha(request.Senha, usuario.SenhaHash))
            return Unauthorized(new { Erro = "Login ou senha inválidos." });

        if (!usuario.Ativo)
            return Unauthorized(new { Erro = "Esta conta está desativada." });

        var perfil = "Cliente";
        if (usuario.PerfilId != null)
        {
            var p = await _context.Perfis.FindAsync(usuario.PerfilId);
            if (p != null) perfil = p.Sigla ?? "Cliente";
        }

        var token = _authService.GerarTokenJwt(usuario.Id, usuario.Nome, usuario.Email ?? "", perfil);

        return Ok(new
        {
            Mensagem = "Login realizado com sucesso",
            Token = token,
            Usuario = new { usuario.Id, usuario.Nome, usuario.Login }
        });
    }

    [HttpPost("esqueci-senha")]
    [EnableRateLimiting("senha")]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (usuario == null)
            return Ok(new { Mensagem = "Se o e-mail estiver cadastrado, você receberá as instruções em instantes." });

        var tokenOriginal = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        usuario.ResetToken = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenOriginal)));
        usuario.ResetTokenExpiraEm = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

        var resposta = new
        {
            Mensagem = "Se o e-mail estiver cadastrado, você receberá as instruções em instantes."
        };

        if (_env.IsDevelopment())
        {
            return Ok(new
            {
                resposta.Mensagem,
                TokenDebug = tokenOriginal
            });
        }

        return Ok(resposta);
    }

    [HttpPost("resetar-senha")]
    [EnableRateLimiting("senha")]
    public async Task<IActionResult> ResetarSenha([FromBody] ResetarSenhaRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var hashDoTokenRecebido = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.ResetToken == hashDoTokenRecebido);

        if (usuario == null || usuario.ResetTokenExpiraEm == null || usuario.ResetTokenExpiraEm < DateTime.UtcNow)
            return BadRequest(new { Erro = "Token inválido ou expirado. Solicite uma nova recuperação." });

        usuario.SenhaHash = _authService.GerarHashSenha(request.NovaSenha);
        usuario.ResetToken = null;
        usuario.ResetTokenExpiraEm = null;
        await _context.SaveChangesAsync();

        return Ok(new { Mensagem = "Senha alterada com sucesso! Você já pode fazer login." });
    }
}