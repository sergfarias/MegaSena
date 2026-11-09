// src/MegaSena.Application/Servicos/AuditoriaService.cs
using System.Security.Cryptography;
using System.Text;

namespace MegaSena.Application.Servicos;

public class AuditoriaService
{
    public string GerarHashDosJogos(string jsonJogos)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(jsonJogos);
        byte[] hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    public string GerarHashAuditoria(string jogosJson)
    {
        if (string.IsNullOrWhiteSpace(jogosJson))
            throw new ArgumentException("O JSON de jogos não pode ser vazio.", nameof(jogosJson));

        using var sha256 = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(jogosJson);
        byte[] hash = sha256.ComputeHash(bytes);

        return Convert.ToHexString(hash);
    }

}