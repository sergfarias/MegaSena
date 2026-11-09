using System.Text.Json;

namespace MegaSena.Infrastructure;

public sealed class ConfiguracaoFisica
{
    public double MassaBola { get; set; } = 0.066;
    public double DiametroBola { get; set; } = 0.050;
    public double RaioGlobo { get; set; } = 0.45;
    public double RotacaoRpm { get; set; } = 30.0;
    public double JatoAceleracao { get; set; } = 25.0;
    public double JatoAltura { get; set; } = 0.20;
    public double Turbulencia { get; set; } = 0.5;
    public double Restituicao { get; set; } = 0.7;
    public double Atrito { get; set; } = 0.02;
    public double Gravidade { get; set; } = 9.81;

    public static ConfiguracaoFisica Carregar(string caminho)
    {
        if (!File.Exists(caminho)) return new ConfiguracaoFisica();
        var json = File.ReadAllText(caminho);
        return JsonSerializer.Deserialize<ConfiguracaoFisica>(json) ?? new ConfiguracaoFisica();
    }
}