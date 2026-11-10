using MegaSena.Application.CasosDeUso;
using MegaSena.Application.Portas;
using MegaSena.Domain;
using MegaSena.Infrastructure;
using MegaSena.Infrastructure.Cobertura;   // <-- novo

// --- Parsing de argumentos ---
var cmd = args.FirstOrDefault() ?? "sortear";
var opts = ParseArgs(args.Skip(1));

int ultimos = opts.GetValueOrDefault("ultimos", 3);
int menos = opts.GetValueOrDefault("menos", 4);
int maxPares = opts.GetValueOrDefault("max-pares", 1);
int seed = opts.GetValueOrDefault("seed", 0);
int n = opts.GetValueOrDefault("n", 1_000_000);
bool fisico = opts.ContainsKey("fisico");

var baseDir = AppContext.BaseDirectory;

// --- Composition Root ---

// Histórico vazio: o universo usa as 60 dezenas (a API de histórico agora fica só no servidor)
var historico = new HistoricoVazio();

var universoFactory = new UniversoFactoryPadrao(historico);
var regra = new MaximoParesConsecutivos(maxPares);

// Escolha do gerador (físico vs rápido)
IGeradorDeSorteio gerador;
if (fisico)
{
    var config = ConfiguracaoFisica.Carregar(Path.Combine(baseDir, "simulador.json"));
    gerador = new SimuladorFisicoGerador(config, seed);
}
else
{
    gerador = new GeradorAleatorioRapido();
}

// NOVO: Criamos a Factory UMA ÚNICA VEZ para usar em todos os comandos.
// Passamos o 'gerador' já instanciado e o 'seed' para o caso do físico.
var geradorFactory = new GeradorDeSorteioFactory(
    new GeradorAleatorioRapido(),
    new SimuladorFisicoGerador(new ConfiguracaoFisica(), seed)
);

// --- Execução dos comandos ---
Console.WriteLine($"Gerador: {gerador.Nome} | Universo: {universoFactory.Criar(ultimos, menos).Tamanho} bolas | Seed: {seed}");

// Aviso de divergência entre CLI e API
if (historico is HistoricoVazio && (ultimos > 0 || menos > 0))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("⚠️  CLI: universo completo (60 dezenas). Use a API para universo reduzido pelo histórico.");
    Console.ResetColor();
}

Console.WriteLine(new string('-', 50));


switch (cmd)
{
    case "sortear":
        {
            var useCase = new GerarJogoUseCase(geradorFactory, universoFactory);

            // Agora o Executar recebe os 4 parâmetros
            var resultado = useCase.Executar(ultimos, menos, fisico, maxPares);

            Console.WriteLine($"Jogo sorteado: {resultado.Jogo}");
            Console.WriteLine($"Tempo de geração: simulação concluída.");
            break;
        }
    case "contar":
        {
            var u60 = Universo.Completo();
            var u42 = universoFactory.Criar(3, 0);
            var u38 = universoFactory.Criar(3, 4);

            Console.WriteLine($"60 bolas: {ContadorDeJogosValidos.Contar(u60, maxPares):N0} válidos");
            Console.WriteLine($"42 bolas: {ContadorDeJogosValidos.Contar(u42, maxPares):N0} válidos");
            Console.WriteLine($"38 bolas: {ContadorDeJogosValidos.Contar(u38, maxPares):N0} válidos");
            break;
        }
    case "uniformidade":
        {
            var uTeste = universoFactory.Criar(ultimos, menos);
            Console.WriteLine($"Rodando {n} sorteios para teste de uniformidade...");
            var r = new TestarUniformidadeUseCase().Executar(gerador, uTeste, n);
            Console.WriteLine($"Qui-quadrado (gl={r.GrausDeLiberdade}): {r.QuiQuadrado:F2}   p-valor: {r.PValor:F4}");
            Console.WriteLine(r.SemEvidenciaDeVies
                ? "✅ Sem evidência de viés (p ≥ 0,05)"
                : "❌ Possível viés (p < 0,05)");
            break;
        }
    case "monte":
        {
            // Monte Carlo simplificado (apostas válidas contra sorteio real de 60 bolas)
            var useCase = new GerarJogoUseCase(geradorFactory, universoFactory);

            int quadras = 0, quinas = 0, senas = 0;
            var rnd = seed == 0 ? new Random() : new Random(seed);

            Console.WriteLine($"Simulando {n:N0} sorteios...");
            for (int i = 0; i < n; i++)
            {
                var aposta = useCase.Executar(ultimos, menos, fisico, maxPares).Jogo;
                var sorteioReal = SortearOficial(rnd);
                int acertos = aposta.Dezenas().Count(sorteioReal.Contem);

                if (acertos == 4) quadras++;
                if (acertos == 5) quinas++;
                if (acertos == 6) senas++;
            }
            Console.WriteLine($"Quadras: {quadras:N0}");
            Console.WriteLine($"Quinas: {quinas:N0}");
            Console.WriteLine($"Senas: {senas:N0}");
            break;
        }
    case "cobertura":
        {
            var dezenas = LerDezenas(args);
            var garantia = opts.GetValueOrDefault("garantia", 4);
            var universo = opts.GetValueOrDefault("universo", 4);
            var maxParesCob = opts.GetValueOrDefault("max-pares", 1);

            var guloso = new GulosoCoberturaSolver();
            var exato = new SolverExato();
            var solver = new SolverRoteador(guloso, exato);

            var uc = new GerarCoberturaUseCase(solver);
            var r = uc.Executar(dezenas, garantia, universo, maxParesCob);

            Console.WriteLine($"Dezenas: {string.Join(",", dezenas.OrderBy(x => x))}");
            Console.WriteLine($"Garantia: {garantia} acertos em {universo} sorteadas");
            Console.WriteLine($"Jogos: {r.TotalJogos}");
            Console.WriteLine($"Custo: R$ {r.CustoTotal:F2}");
            Console.WriteLine($"Cobertura: {r.CombinacoesCobertas}/{r.TotalCombinacoesSorteaveis} " +
                              $"({(r.GarantiaTotal ? "GARANTIDA" : "parcial")})");

            foreach (var j in r.Cobertura.Jogos)
                Console.WriteLine($"  {j}");

            break;
        }
    case "prob":
        {
            int dezenas = opts.GetValueOrDefault("dezenas", 6);
            var uc = new CalcularProbabilidadeUseCase();
            var r = uc.Executar(dezenas);

            Console.WriteLine($"Aposta de {r.Dezenas} dezenas — Custo: R$ {r.Custo:F2}");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"Sena:   {r.ProbSena:E3}  (1 em {1 / r.ProbSena:N0})");
            Console.WriteLine($"Quina:  {r.ProbQuina:E3}  (1 em {1 / r.ProbQuina:N0})");
            Console.WriteLine($"Quadra: {r.ProbQuadra:E3}  (1 em {1 / r.ProbQuadra:N0})");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine($"P(Pelo menos Quadra): {r.ProbPeloMenosQuadra:P4}");
            Console.WriteLine($"Retorno Esperado (EV): R$ {r.RetornoEsperado:F2}");
            Console.WriteLine($"Retorno sobre o Custo: {r.RetornoPercentual:P2} (Esperado: ~43% para a Caixa)");

            break;
        }
    case "comparar":
        {
            int dezenas = opts.GetValueOrDefault("dezenas", 8);
            var uc = new CompararEstrategiasUseCase();
            var r = uc.Executar(dezenas);

            Console.WriteLine($"=== COMPARAÇÃO COM O MESMO ORÇAMENTO (R$ {r.CustoTotal:F2}) ===");
            Console.WriteLine();
            Console.WriteLine($"Estratégia A: 1 aposta múltipla de {r.DezenasMultipla} dezenas");
            Console.WriteLine($"  P(≥ Quadra): {r.ProbMultipla:P4}");
            Console.WriteLine();
            Console.WriteLine($"Estratégia B: {r.QtdJogosSimples} jogos simples espalhados");
            Console.WriteLine($"  P(≥ Quadra em algum jogo): {r.ProbJogosSimples:P4}");
            Console.WriteLine();
            Console.WriteLine($"Conclusão: {r.Conclusao}");
            break;
        }
    case "gerar-lote":
        {
            int quantidade = opts.GetValueOrDefault("quantidade", 100);
            var caminhoSaida = "lote_megasena.csv";

            Console.WriteLine($"⏳ Gerando {quantidade:N0} jogos... Aguarde.");

            var uc = new GerarLoteJogosUseCase(geradorFactory, universoFactory);

            try
            {
                // Agora o Executar recebe 4 parâmetros (quantidade, ultimos, menos, maxPares)
                var jogos = uc.Executar(quantidade, ultimos, menos, maxPares).ToList();

                using (var writer = new StreamWriter(caminhoSaida))
                {
                    writer.WriteLine("Jogo;Dezena1;Dezena2;Dezena3;Dezena4;Dezena5;Dezena6");
                    int index = 1;
                    foreach (var jogo in jogos)
                    {
                        var dezenas = jogo.Dezenas().Select(d => d.ToString("00"));
                        writer.WriteLine($"{index};{string.Join(";", dezenas)}");
                        index++;
                    }
                }

                Console.WriteLine($"✅ Sucesso! {jogos.Count:N0} jogos gerados.");
                Console.WriteLine($"📁 Arquivo salvo em: {Path.GetFullPath(caminhoSaida)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro: {ex.Message}");
            }

            break;
        }
    default:
        Console.WriteLine($"Comando desconhecido: {cmd}");
        Console.WriteLine("Uso: dotnet run -- [sortear|contar|uniformidade|monte|cobertura|prob|comparar|gerar-lote] [--fisico] [--ultimos N] [--menos N] [--n N] [--seed N]");
        Console.WriteLine("     cobertura: --dezenas 1,3,5,7,9,11,13,15 --garantia 4 --universo 4");
        Console.WriteLine("     gerar-lote: --quantidade 100");
        Console.WriteLine("     Nota: a CLI NÃO consulta o banco. O universo é sempre completo (60 dezenas).");
        Console.WriteLine("     Para universo reduzido, use a API (/api/MegaSena/sortear).");
        break;
}

// --- Funções auxiliares ---
static Jogo SortearOficial(Random rnd)
{
    var all = Enumerable.Range(1, 60).ToArray();
    for (int i = 0; i < 6; i++)
    {
        int j = rnd.Next(i, all.Length);
        (all[i], all[j]) = (all[j], all[i]);
    }
    return Jogo.DasDezenas(all.Take(6));
}

static Dictionary<string, int> ParseArgs(IEnumerable<string> args)
{
    var d = new Dictionary<string, int>();
    var list = args.ToList();

    for (int i = 0; i < list.Count; i++)
    {
        if (list[i].StartsWith("--"))
        {
            var key = list[i][2..];
            if (i + 1 < list.Count && int.TryParse(list[i + 1], out var v))
            {
                // REGRA ESPECÍFICA PARA --ultimos
                if (key == "ultimos" && (v < 0 || v > 10))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Erro: A opção --ultimos aceita apenas valores entre 0 e 10.");
                    Console.ResetColor();
                    Environment.Exit(1);
                }

                d[key] = v;
                i++;
            }
            else
            {
                d[key] = 1; // flag booleana
            }
        }
    }
    return d;
}

static HashSet<int> LerDezenas(string[] args)
{
    var idx = Array.IndexOf(args, "--dezenas");
    if (idx < 0 || idx + 1 >= args.Length)
        throw new ArgumentException("--dezenas é obrigatório. Ex.: --dezenas 1,3,5,7,9,11,13,15");

    var dezenas = args[idx + 1]
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(int.Parse)
        .ToHashSet();

    if (dezenas.Count < 6)
        throw new ArgumentException($"--dezenas precisa de pelo menos 6 dezenas (recebi {dezenas.Count}).");
    if (dezenas.Any(d => d < 1 || d > 60))
        throw new ArgumentException("Dezenas devem estar entre 1 e 60.");

    return dezenas;
}

// --- Tipos (devem ficar depois de todas as instruções de nível superior) ---
sealed class HistoricoVazio : IHistoricoRepository
{
    public IReadOnlyList<int[]> ObterUltimosSorteios(int quantidade) => Array.Empty<int[]>();
    public IReadOnlyList<int> ObterMenosSorteadas(int quantidade) => Array.Empty<int>();
}