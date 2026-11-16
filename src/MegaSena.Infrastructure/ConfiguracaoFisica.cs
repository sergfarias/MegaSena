using System.Text.Json;

namespace MegaSena.Infrastructure;

/// <summary>
/// Parâmetros do globo físico. Unidades: metros, segundos e m/s².
/// Todas as propriedades abaixo são usadas pela simulação. Chaves antigas que estejam no simulador.json
/// (MassaBola, RotacaoRpm, JatoAltura) eram ignoradas pelo código e agora são simplesmente descartadas na leitura.
/// </summary>
public sealed class ConfiguracaoFisica
{
    /// <summary>Diâmetro de cada bola. O raio da simulação é a metade. (Padrão 0,09 m = o raio efetivo que a versão anterior usava.)</summary>
    public double DiametroBola { get; set; } = 0.090;
    public double RaioGlobo { get; set; } = 0.45;
    public double JatoAceleracao { get; set; } = 25.0;
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
