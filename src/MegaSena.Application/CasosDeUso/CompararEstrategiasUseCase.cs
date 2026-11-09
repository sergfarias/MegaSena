using MegaSena.Domain.Calculadora;

namespace MegaSena.Application.CasosDeUso;

public sealed record ComparacaoEstrategia(
    int DezenasMultipla,
    decimal CustoTotal,
    int QtdJogosSimples,
    double ProbMultipla,
    double ProbJogosSimples,
    string Conclusao
);

public sealed class CompararEstrategiasUseCase
{
    public ComparacaoEstrategia Executar(int dezenasMultipla)
    {
        if (dezenasMultipla < 7 || dezenasMultipla > 20)
            throw new ArgumentException("A comparação faz sentido para apostas entre 7 e 20 dezenas.");

        // 1. Custo da aposta múltipla (ex: 8 dezenas = C(8,6) = 28 jogos = R$ 168,00)
        var custoMultipla = PrecoAposta.Calcular(dezenasMultipla);

        // 2. Quantos jogos simples cabem nesse mesmo orçamento?
        int qtdJogosSimples = (int)(custoMultipla / PrecoAposta.PrecoSimples);

        // 3. Probabilidade da aposta múltipla (Pelo menos Quadra)
        // A chance de 1 aposta de N dezenas acertar a quadra.
        var probMultipla = ProbabilidadeHipergeometrica.PeloMenos(dezenasMultipla, 4);

        // 4. Probabilidade dos jogos simples espalhados
        // A chance de ganhar "algo" (quadra ou mais) com 1 jogo simples é P.
        // A chance de ganhar pelo menos 1 quadra jogando K jogos aleatórios é:
        // 1 - (1 - P)^K
        var probSimplesUnitario = ProbabilidadeHipergeometrica.PeloMenos(6, 4);
        var probJogosSimples = 1 - Math.Pow(1 - probSimplesUnitario, qtdJogosSimples);

        // 5. Conclusão didática
        string conclusao;
        if (qtdJogosSimples <= 0)
        {
            conclusao = "O orçamento não é suficiente para comparar estratégias.";
        }
        else if (probJogosSimples > probMultipla)
        {
            var vantagem = (probJogosSimples / probMultipla - 1) * 100;
            conclusao = $"Jogos simples espalhados aumentam a chance de ganhar 'algo' em {vantagem:F1}%. " +
                        "Mas a aposta múltipla concentra prêmios maiores (várias quadras simultâneas).";
        }
        else
        {
            conclusao = "As estratégias têm chances equivalentes.";
        }

        return new ComparacaoEstrategia(
            dezenasMultipla,
            custoMultipla,
            qtdJogosSimples,
            probMultipla,
            probJogosSimples,
            conclusao);
    }
}