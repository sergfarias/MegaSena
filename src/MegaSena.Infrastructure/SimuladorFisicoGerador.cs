
using System.Numerics;
using MegaSena.Application.Portas;
using MegaSena.Domain;

namespace MegaSena.Infrastructure;

/// <summary>
/// Gerador "físico": bolas dentro de um globo com gravidade, jato de ar, turbulência e colisões.
/// A dezena sorteada é a da bola que estiver mais perto da abertura no topo do globo na hora da retirada,
/// ou seja, o resultado SAI da simulação (antes ele vinha de um Random, e a física era só enfeite).
///
/// Por que continua uniforme: todas as bolas têm o mesmo raio e as mesmas regras, e as condições iniciais
/// são sorteadas de forma idêntica para todas. Antes de simular, a ordem das bolas é embaralhada, para que a
/// ordem de resolução das colisões não dependa do número da dezena. Assim as bolas são intercambiáveis, e
/// qualquer uma tem a mesma chance de ser a mais próxima da abertura. A uniformidade pode ser conferida em
/// GET /api/Estatisticas/uniformidade?fisico=true.
/// </summary>
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

        // Trava: regra impossível para este universo (evita milhares de simulações inúteis)
        if (regra is MaximoParesConsecutivos maxRegra &&
            ContadorDeJogosValidos.Contar(universo, maxRegra.Maximo) == 0)
        {
            throw new InvalidOperationException("Regra de jogo impossível para este universo.");
        }

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

    private int[] ExecutarSimulacao(int[] dezenas)
    {
        var bolas = dezenas.Select(d => new Bola(d, _config.DiametroBola / 2.0)).ToArray();
        Embaralhar(bolas);

        // Condições iniciais: bolas espalhadas aleatoriamente no globo
        foreach (var bola in bolas)
        {
            double ang = _rnd.NextDouble() * Math.PI * 2;
            double dist = _rnd.NextDouble() * (_config.RaioGlobo - bola.Raio) * 0.9;
            bola.Pos = new Vector2((float)(Math.Cos(ang) * dist), (float)(Math.Sin(ang) * dist));
            bola.Vel = new Vector2((float)(_rnd.NextDouble() - 0.5), (float)(_rnd.NextDouble() - 0.5));
        }

        // Tempo de mistura inicial (1,5 s de física caótica)
        for (double tempo = 0; tempo < 1.5; tempo += Dt)
            StepFisica(bolas);

        // Abertura no topo do globo (eixo Y positivo é "para cima", pois a gravidade subtrai de Vel.Y)
        var abertura = new Vector2(0f, (float)_config.RaioGlobo);

        var sorteadas = new List<int>(6);
        while (sorteadas.Count < 6)
        {
            // Sai a bola mais próxima da abertura neste instante
            Bola? escolhida = null;
            float melhor = float.MaxValue;
            foreach (var bola in bolas)
            {
                if (bola.Fora) continue;
                float d2 = (bola.Pos - abertura).LengthSquared();
                if (d2 < melhor)
                {
                    melhor = d2;
                    escolhida = bola;
                }
            }

            if (escolhida is null) break; // Segurança extra

            escolhida.Fora = true;
            sorteadas.Add(escolhida.Dezena);

            // Mais 0,3 s de física para as bolas restantes se mexerem antes da próxima retirada
            for (double t = 0; t < 0.3; t += Dt)
                StepFisica(bolas);
        }

        return sorteadas.ToArray();
    }

    /// <summary>Fisher-Yates com o mesmo Random da simulação (determinístico quando há seed).</summary>
    private void Embaralhar(Bola[] bolas)
    {
        for (int i = bolas.Length - 1; i > 0; i--)
        {
            int j = _rnd.Next(i + 1);
            (bolas[i], bolas[j]) = (bolas[j], bolas[i]);
        }
    }

    private void StepFisica(Bola[] bolas)
    {
        // 1) Aplica forças
        foreach (var bola in bolas)
        {
            if (bola.Fora) continue;

            bola.Vel.Y -= (float)(_config.Gravidade * Dt);

            // Ventilador (jato de ar na parte de baixo do globo)
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
            if (bola.Fora) continue;
            bola.Pos += bola.Vel * (float)Dt;
        }

        // 3) Colisões
        ResolverColisoes(bolas);
    }

    private void ResolverColisoes(Bola[] bolas)
    {
        // Bola-Bola
        for (int i = 0; i < bolas.Length; i++)
        {
            if (bolas[i].Fora) continue;
            for (int j = i + 1; j < bolas.Length; j++)
            {
                if (bolas[j].Fora) continue;

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
            if (bola.Fora) continue;

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

    private sealed class Bola
    {
        public int Dezena;
        public Vector2 Pos;
        public Vector2 Vel;
        public float Raio;
        public bool Fora;   // já foi retirada do globo

        public Bola(int dezena, double raio)
        {
            Dezena = dezena;
            Raio = (float)raio;
        }
    }
}
