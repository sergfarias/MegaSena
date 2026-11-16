
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace MegaSena.Application.Servicos;

public class AuthService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessMinutos;

    /// <summary>Validade do refresh token em dias (Jwt:RefreshDias, padrão 14).</summary>
    public int RefreshDias { get; }

    public AuthService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key não configurada no appsettings ou variáveis de ambiente.");

        _issuer = configuration["Jwt:Issuer"] ?? "MegaSenaApi";
        _audience = configuration["Jwt:Audience"] ?? "MegaSenaFront";

        // Sem depender do pacote Binder: lê como texto e converte
        _accessMinutos = int.TryParse(configuration["Jwt:AccessMinutos"], out var min) && min is >= 5 and <= 240 ? min : 30;
        RefreshDias = int.TryParse(configuration["Jwt:RefreshDias"], out var dias) && dias is >= 1 and <= 90 ? dias : 14;
    }

    /// <summary>Carimbo curto derivado do hash atual da senha. Muda quando a senha muda.</summary>
    public static string CarimboDaSenha(string senhaHash) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(senhaHash)))[..16];

    public string GerarHashSenha(string senha)
    {
        return BCrypt.Net.BCrypt.HashPassword(senha);
    }

    public bool VerificarSenha(string senha, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(senha, hash);
    }

    public string GerarTokenJwt(Guid id, string nome, string email, string perfilNome, string? carimboSenha = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        // Alterado para UTF8 para casar com o Program.cs
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, id.ToString()),
            new(ClaimTypes.Name, nome),
            new(ClaimTypes.Email, email ?? ""),
            new(ClaimTypes.Role, perfilNome)
        };

        if (!string.IsNullOrEmpty(carimboSenha))
            claims.Add(new Claim("pwd", carimboSenha));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_accessMinutos),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
