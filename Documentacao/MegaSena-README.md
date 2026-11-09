# MegaSena (Clean Architecture)

Simulador de sorteio da Mega-Sena em C# / .NET 8, sem dependências externas.
Filtros de jogo (globo reduzido, no máximo N pares consecutivos), simulador de física 2D (ventilador, peso da bolinha, colisões) e Monte Carlo.

> Aviso honesto: nenhum filtro nem simulação aumenta a chance de acerto. Cada jogo continua com 1 em 50.063.860.

## Arquitetura

```
MegaSena.Cli ───────────────► MegaSena.Infrastructure
   (apresentação +                  │  implementa as portas
    composition root)               ▼
        │                    MegaSena.Application ──► MegaSena.Domain
        └───────────────────►  (casos de uso + portas)   (regras puras)
```

A dependência sempre aponta para dentro: o Domain não conhece ninguém, a Application só conhece o Domain, e só o Cli conhece as implementações concretas.

| Projeto | Responsabilidade | Principais tipos |
|---|---|---|
| **Domain** | Regras de negócio, sem I/O | `Jogo` (bitmask), `Universo` (o globo), `ResultadoSorteio`, `IRegraDeJogo`, `MaximoParesConsecutivos`, `ContadorDeJogosValidos`, `Combinatoria` |
| **Application** | Casos de uso e portas (interfaces) | `GerarJogoUseCase`, `ContarJogosValidosUseCase`, `TestarUniformidadeUseCase`, `CompararUniversosUseCase`, `IGeradorDeSorteio`, `IHistoricoRepository`, `UniversoFactory` |
| **Infrastructure** | Adaptadores das portas | `GeradorAleatorioRapido`, `SimuladorFisicoGerador`, `ConfiguracaoFisica`, `HistoricoJsonRepository` |
| **Cli** | Entrada do usuário e montagem das peças | `Program.cs`, `historico.json`, `simulador.json` |

## Como rodar

```
dotnet build
dotnet run --project src/MegaSena.Cli -- sortear                 # 1 jogo, simulador físico
dotnet run --project src/MegaSena.Cli -- contar                  # quantos jogos válidos (60 / 42 / 38 bolas)
dotnet run --project src/MegaSena.Cli -- monte --n 5000000       # Monte Carlo
dotnet run --project src/MegaSena.Cli -- uniformidade --n 300    # qui-quadrado do simulador físico
```

Opções: `--ultimos N` (padrão 3), `--menos N` (padrão 4), `--max-pares N` (padrão 1), `--rapido` / `--fisico`, `--seed N`.

## Configuração
- `src/MegaSena.Cli/historico.json`: últimos sorteios (mais recente primeiro) e dezenas menos sorteadas. **Os valores que vêm no arquivo são exemplos: troque pelos reais.**
- `src/MegaSena.Cli/simulador.json`: parâmetros físicos. Oficiais (Caixa): bola de 66 g e 50 mm. Todo o resto é estimativa para calibrar.

## Como estender
- **Nova forma de sortear** (ex.: física 3D): implemente `IGeradorDeSorteio` na Infrastructure e troque no composition root (`Program.cs`).
- **Histórico real** (API/CSV/banco): implemente `IHistoricoRepository`.
- **Nova regra de aposta** (paridade, soma, faixas): implemente `IRegraDeJogo` no Domain e use no `GerarJogoUseCase`.
- **Testes**: não incluí projeto de testes porque o xUnit é um pacote NuGet e eu estava sem internet. Com internet: `dotnet new xunit -o tests/MegaSena.Domain.Tests`. Bons primeiros testes: `Jogo.ParesConsecutivos`, `ContadorDeJogosValidos` (42 bolas → 4.955.700) e `Universo.Sem`.
