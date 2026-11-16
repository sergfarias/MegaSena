using MegaSena.Api.Models;
using MegaSena.Application.CasosDeUso;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MegaSena.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string MensagemRecuperacao = "Se o e-mail estiver cadastrado, você receberá as instruções em instantes.";

    private readonly ContaUseCase _conta;
    private readonly IWebHostEnvironment _env;

    public AuthController(ContaUseCase conta, IWebHostEnvironment env)
    {
        _conta = conta;
        _env = env;
    }

    [HttpPost("registrar")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Registrar([FromBody] RegistroRequest request)
    {
        var status = await _conta.RegistrarAsync(request.Nome, request.Login, request.Email, request.Senha);

        // S2: a resposta é a mesma para cadastro novo, e-mail repetido e login repetido
        return status switch
        {
            StatusRegistro.Criado or StatusRegistro.EmailEmUso => Ok(new
            {
                Mensagem = "Cadastro recebido. Se o e-mail informado ainda não tinha conta, ela foi criada e você já pode fazer login " +
                           "com o seu e-mail (ou com o login escolhido; se ele já estivesse em uso por outra pessoa, ganhou um número no final). " +
                           "Se o e-mail já tinha conta, enviamos orientações para ele."
            }),
            _ => BadRequest(new { Erro = "Não foi possível criar o usuário. Tente novamente." })
        };
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var resultado = await _conta.LoginAsync(request.Login, request.Senha);

        return resultado.Status switch
        {
            StatusLogin.Sucesso => Ok(new
            {
                Mensagem = "Login realizado com sucesso",
                Token = resultado.Token,
                RefreshToken = resultado.RefreshToken,
                Usuario = new { resultado.Usuario!.Id, resultado.Usuario.Nome, resultado.Usuario.Login }
            }),
            StatusLogin.ContaDesativada => Unauthorized(new { Erro = "Esta conta está desativada." }),
            _ => Unauthorized(new { Erro = "Login ou senha inválidos." })
        };
    }

    /// <summary>Troca um refresh token por um JWT novo e um refresh novo. Cada refresh token vale uma única vez.</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var r = await _conta.RenovarAsync(request.RefreshToken);

        if (r.Status != StatusRenovacao.Sucesso)
            return Unauthorized(new { Erro = "Sessão expirada. Faça login novamente." });

        return Ok(new { Token = r.Token, RefreshToken = r.RefreshToken });
    }

    /// <summary>Encerra a sessão revogando a cadeia do refresh token. Responde 200 mesmo se o token já era inválido.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await _conta.SairAsync(request.RefreshToken);
        return Ok(new { Mensagem = "Sessão encerrada." });
    }

    [HttpPost("esqueci-senha")]
    [EnableRateLimiting("senha")]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaRequest request)
    {
        var token = await _conta.SolicitarRecuperacaoAsync(request.Email);

        // Resposta sempre neutra; o token só aparece em Development, para testes manuais
        if (token != null && _env.IsDevelopment())
            return Ok(new { Mensagem = MensagemRecuperacao, TokenDebug = token });

        return Ok(new { Mensagem = MensagemRecuperacao });
    }

    [HttpPost("resetar-senha")]
    [EnableRateLimiting("senha")]
    public async Task<IActionResult> ResetarSenha([FromBody] ResetarSenhaRequest request)
    {
        var ok = await _conta.RedefinirSenhaAsync(request.Token, request.NovaSenha);

        if (!ok)
            return BadRequest(new { Erro = "Token inválido ou expirado. Solicite uma nova recuperação." });

        return Ok(new { Mensagem = "Senha alterada com sucesso! Você já pode fazer login." });
    }
}
