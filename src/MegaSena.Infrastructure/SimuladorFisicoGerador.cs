using System.Numerics;
using MegaSena.Application.Portas;
using MegaSena.Domain;

namespace MegaSena.Infrastructure;

public sealed class SimuladorFisicoGerador : IGeradorDeSorteio
{
    private readonly ConfiguracaoFisica _config;
    private readonly Random _rnd;
    private const double Dt = 1.0 / 120.0; // 120 Hz

    public string Nome => "Físico";

    public SimuladorFisicoGerador(ConfiguracaoFisica config, int seed = 0)
    {
        _config = config;
        _rnd = seed == 0 ? new Random() : new Random(seed);
    }

    public Jogo Sortear(Universo universo, IRegraDeJogo regra)
    {
        var dezenas = universo.Dezenas.ToArray();

        // TRAVA DE SEGURANÇA (Item 4.3 do PDF)
        for (int tentativa = 0; tentativa < 10_000; tentativa++)
        {
            var sorteadas = ExecutarSimulacao(dezenas);
            var jogo = Jogo.DasDezenas(sorteadas);

            if (regra.Aceita(jogo))
                return jogo;
        }

        throw new InvalidOperationException("Nenhum jogo válido possível neste universo/regra.");
    }

    // O MÉTODO QUE ESTAVA FALTANDO:
    private int[] ExecutarSimulacao(int[] dezenas)
    {
        var bolas = dezenas.Select(d => new Bola(d, _config.RaioGlobo)).ToArray();
        var sorteadas = new List<int>();
        double tempo = 0;

        // Condições iniciais: bolas espalhadas aleatoriamente no globo
        foreach (var bola in bolas)
        {
            double ang = _rnd.NextDouble() * Math.PI * 2;
            double dist = _rnd.NextDouble() * (_config.RaioGlobo - bola.Raio) * 0.9;
            bola.Pos = new Vector2((float)(Math.Cos(ang) * dist), (float)(Math.Sin(ang) * dist));
            bola.Vel = new Vector2((float)(_rnd.NextDouble() - 0.5), (float)(_rnd.NextDouble() - 0.5));
        }

        // Tempo de mistura inicial (1.5s de pura física caótica)
        //while (tempo < 1.5)
        int tentativas = 0;
        while (sorteadas.Count < 6 && tentativas < 1000)
        {
            tentativas++;
            StepFisica(bolas, sorteadas);
            tempo += Dt;
        }

        // Agora começam as retiradas. 
        // A cada 0.3 segundos, uma bola é "sugada" para fora.
        int tentativasRetirada = 0;
        //while (sorteadas.Count < 6)
        while (sorteadas.Count < 6 && tentativasRetirada < 1000) // <-- Teto
        {
            tentativasRetirada++;
            // Escolhe uma bola aleatória que ainda está no globo
            var disponiveis = bolas.Where(b => !sorteadas.Contains(b.Dezena)).ToArray();
            var bolaSorteada = disponiveis[_rnd.Next(disponiveis.Length)];

            sorteadas.Add(bolaSorteada.Dezena);
            bolaSorteada.Pos = new Vector2(1000, 1000); // Move para fora do globo

            // Roda a física por mais 0.3s para as outras bolas se moverem (efeito de agitação)
            double tempoRetirada = 0;
            while (tempoRetirada < 0.3)
            {
                StepFisica(bolas, sorteadas);
                tempoRetirada += Dt;
            }
        }

        return sorteadas.ToArray();
    }

    private void StepFisica(Bola[] bolas, List<int> sorteadas)
    {
        // 1) Aplica forças
        foreach (var bola in bolas)
        {
            if (sorteadas.Contains(bola.Dezena)) continue;

            bola.Vel.Y -= (float)(_config.Gravidade * Dt);

            // Ventilador
            if (bola.Pos.Y < -_config.RaioGlobo * 0.3)
            {
                bola.Vel.Y += (float)(_config.JatoAceleracao * Dt);
            }

            // Turbulência
            bola.Vel.X += (float)((_rnd.NextDouble() - 0.5) * _config.Turbulencia * Dt);
            bola.Vel.Y += (float)((_rnd.NextDouble() - 0.5) * _config.Turbulencia * Dt);

            // Atrito
            bola.Vel *= (float)(1.0 - _config.Atrito);
        }

        // 2) Integração
        foreach (var bola in bolas)
        {
            if (sorteadas.Contains(bola.Dezena)) continue;
            bola.Pos += bola.Vel * (float)Dt;
        }

        // 3) Colisões
        ResolverColisoes(bolas, sorteadas);
    }

    private void ResolverColisoes(Bola[] bolas, List<int> sorteadas)
    {
        // Bola-Bola
        for (int i = 0; i < bolas.Length; i++)
        {
            if (sorteadas.Contains(bolas[i].Dezena)) continue;
            for (int j = i + 1; j < bolas.Length; j++)
            {
                if (sorteadas.Contains(bolas[j].Dezena)) continue;

                var delta = bolas[i].Pos - bolas[j].Pos;
                float dist = delta.Length();
                float minDist = bolas[i].Raio + bolas[j].Raio;

                if (dist < minDist && dist > 0)
                {
                    float overlap = minDist - dist;
                    var normal = delta / dist;
                    bolas[i].Pos += normal * overlap * 0.5f;
                    bolas[j].Pos -= normal * overlap * 0.5f;

                    var relVel = bolas[i].Vel - bolas[j].Vel;
                    float velAlongNormal = Vector2.Dot(relVel, normal);
                    if (velAlongNormal > 0) continue;

                    float e = (float)_config.Restituicao;
                    float jImpulse = -(1 + e) * velAlongNormal / 2.0f;
                    var impulse = normal * jImpulse;
                    bolas[i].Vel += impulse;
                    bolas[j].Vel -= impulse;
                }
            }
        }

        // Bola-Parede
        foreach (var bola in bolas)
        {
            if (sorteadas.Contains(bola.Dezena)) continue;

            float dist = bola.Pos.Length();
            float limit = (float)(_config.RaioGlobo - bola.Raio);

            if (dist > limit)
            {
                var normal = bola.Pos / dist;
                bola.Pos = normal * limit;

                float velNormal = Vector2.Dot(bola.Vel, normal);
                if (velNormal > 0)
                {
                    float e = (float)_config.Restituicao;
                    bola.Vel -= (1 + e) * velNormal * normal;
                }
            }
        }
    }

    private class Bola
    {
        public int Dezena;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Raio;

        public Bola(int dezena, double raio)
        {
            Dezena = dezena;
            Raio = (float)(raio * 0.1);
        }
    }
}