
# 🏗️ Arquitetura do Sistema (Clean Architecture)

Este documento detalha as decisões arquiteturais, os padrões de projeto e a organização do código deste simulador. O projeto segue os rigorosos princípios da **Clean Architecture** (Arquitetura Limpa) de Robert C. Martin.

O principal objetivo é isolar as **regras de negócio** (o "cérebro" do sistema) de detalhes de infraestrutura, frameworks, UI ou bancos de dados.

## 📐 O Diagrama de Dependências

A regra de ouro da Clean Architecture é: **As dependências do código-fonte apontam apenas para dentro**. As camadas externas podem depender das internas, mas as internas não sabem da existência das externas.

```text
       [ Front-end HTML/JS ]
                |
                v
       [ MegaSena.Api (Web) ] ---------> [ MegaSena.Cli (Console) ]
                |                                   |
                +-----------------+-----------------+
                                  |
                                  v
                      [ MegaSena.Infrastructure ]
                      (Física, JSON, API Externa)
                                  |
                                  v
                      [ MegaSena.Application ]
                      (Casos de Uso + Portas)
                                  |
                                  v
                      [ MegaSena.Domain ]
                      (Regras Puras, sem I/O)
🧩 Detalhes das Camadas
1. MegaSena.Domain (O Coração)
É a camada mais interna. Não possui nenhuma dependência externa (nem mesmo o System.Text.Json ou pacotes NuGet de terceiros).

O que contém: Entidades, regras de negócio e lógica pura.

Tipos Principais:

Jogo: Representado por uma máscara de bits ulong (Bitmask) de 60 bits. Isso permite calcular pares consecutivos com uma única operação bitwise (bits & (bits >> 1)) em tempo O(1).

Universo: O globo de dezenas disponíveis (respeita os buracos e números excluídos).

ContadorDeJogosValidos: Utiliza Programação Dinâmica (DP) para contar exatamente quantos jogos válidos existem em um universo, sem precisar rodar força bruta de 50 milhões de combinações.

2. MegaSena.Application (Os Casos de Uso)
Orquestra o fluxo de dados entre o Domain e o mundo externo. Ela conhece o Domain, mas não sabe como o mundo externo funciona (não sabe se há um banco de dados, uma API ou a física).

O que contém: Casos de Uso (Use Cases) e as Portas (Interfaces).

Portas (Contratos): IGeradorDeSorteio, IHistoricoRepository, IUniversoFactory. Elas definem o que a aplicação precisa, sem se importar com quem vai implementar.

Casos de Uso: GerarJogoUseCase, ContarJogosValidosUseCase, TestarUniformidadeUseCase.

3. MegaSena.Infrastructure (Os Adaptadores)
Esta camada implementa as Portas definidas na Application. É a "mão na massa" que conversa com o mundo real.

O que contém: Implementações concretas das interfaces.

Classes:

GeradorAleatorioRapido: Implementa Fisher-Yates usando RandomNumberGenerator para aleatoriedade criptográfica.

SimuladorFisicoGerador: O motor de física 2D (gravidade, colisões, ventilador, turbulência).

HistoricoJsonRepository e HistoricoApiRepository: Implementações de IHistoricoRepository que buscam dados de um arquivo local ou da API da Caixa (via Guidi), respectivamente.

4. MegaSena.Api / MegaSena.Cli / Front (Apresentação)
São os "Composition Roots" (Raízes de Composição). É a única parte do sistema que sabe como amarrar todas as peças.

MegaSena.Api: Expõe os Casos de Uso via HTTP REST (Controllers) e documenta no Scalar.

MegaSena.Cli: Roda os Casos de Uso via terminal, ideal para simulações pesadas (Monte Carlo) e automação.

Front-end: Interface visual em Canvas que consome a API (a lógica de negócio fica 100% no C#).

🛠️ Decisões de Design Importantes
Bitmask para Dezenas: Em vez de usar listas ou arrays (que consomem memória e são lentos), um jogo de 6 dezenas é um número inteiro de 64 bits. Isso torna a validação de pares consecutivos absurdamente rápida.

Contagem por Programação Dinâmica (DP): Contar quantos jogos válidos existem em um universo de 42 bolas exigiria testar 5.245.786 combinações. A DP resolve o problema em milissegundos avaliando o estado (posição, dezenas escolhidas, pares formados).

Inversão de Dependência no Histórico: A interface IHistoricoRepository permite que o sistema alterne entre ler um JSON local ou consumir a internet alterando apenas uma linha no Program.cs, sem tocar no Domain.

Validação Estatística: A física foi validada usando o teste de Qui-quadrado (χ²). O sistema prova matematicamente que a simulação não tem viés e é justa.

text

### O que fazer agora?
Crie o arquivo e salve. Depois, se quiser fechar o pacote, crie também o **`API_DOCS.md`** (que passei na mensagem anterior) e o **`.gitignore`**. 

Com esses 4 arquivos (`README`, `ARCHITECTURE`, `API_DOCS` e `.gitignore`), o seu repositório estará com a documentação completa, do nível iniciante ao sênior. É só dar o `git push` e mostrar para o mundo!
