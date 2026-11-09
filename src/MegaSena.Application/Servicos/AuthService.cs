using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MegaSena.Application.Servicos;

public class AuthService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public AuthService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key não configurada no appsettings ou variáveis de ambiente.");

        _issuer = configuration["Jwt:Issuer"] ?? "MegaSenaApi";
        _audience = configuration["Jwt:Audience"] ?? "MegaSenaFront";
    }

    public string GerarHashSenha(string senha)
    {
        return BCrypt.Net.BCrypt.HashPassword(senha);
    }

    public bool VerificarSenha(string senha, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(senha, hash);
    }

    public string GerarTokenJwt(Guid id, string nome, string email, string perfilNome)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        // Alterado para UTF8 para casar com o Program.cs
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Name, nome),
                new Claim(ClaimTypes.Email, email ?? ""),
                new Claim(ClaimTypes.Role, perfilNome)
            }),
            Expires = DateTime.UtcNow.AddMinutes(30),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
