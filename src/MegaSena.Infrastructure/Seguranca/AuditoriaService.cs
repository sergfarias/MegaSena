using System.Security.Cryptography;
using System.Text;

namespace MegaSena.Infrastructure.Seguranca;

public class AuditoriaService
{
    public string GerarHashAuditoria(string jogosJson)
    {
        if (string.IsNullOrWhiteSpace(jogosJson))
            throw new ArgumentException("O JSON de jogos não pode ser vazio.", nameof(jogosJson));

        var bytes = Encoding.UTF8.GetBytes(jogosJson);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}