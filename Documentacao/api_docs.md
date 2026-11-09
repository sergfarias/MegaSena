# 🌐 Documentação da API (Endpoints)

Esta API expõe os casos de uso do simulador Mega-Sena via HTTP REST. Ela foi construída em ASP.NET Core usando Controllers e a documentação interativa é gerada automaticamente pelo **Scalar** (OpenAPI).

Para acessar a interface do Scalar (onde você pode testar os endpoints diretamente pelo navegador), inicie a API e acesse:
👉 `https://localhost:7155/scalar`

---

## 1. Sortear Jogo
Gera um jogo de 6 dezenas únicas usando o motor escolhido, aplicando a regra de pares consecutivos e os filtros do histórico.

**GET** `/api/MegaSena/sortear`

### Parâmetros (Query String)
| Parâmetro | Tipo | Obrigatório | Padrão | Descrição |
| :--- | :--- | :--- | :--- | :--- |
| `fisico` | `boolean` | Não | `false` | Se `true`, usa o motor de física 2D. Se `false`, usa o gerador rápido (Fisher-Yates). |
| `ultimos` | `int` | Não | `3` | Quantidade de sorteios passados a serem ignorados (excluídos do globo). |
| `menos` | `int` | Não | `4` | Quantidade de dezenas menos sorteadas a serem ignoradas. |

### Exemplo de Requisição
```http
GET /api/MegaSena/sortear?fisico=true&ultimos=3&menos=4
Exemplo de Resposta (200 OK)
json
{
  "jogo": [ 5, 12, 36, 39, 49, 57 ],
  "universo": 42,
  "motor": "Físico"
}
2. Contar Jogos Válidos
Retorna a contagem combinatória exata de jogos válidos para os universos de 60, 42 e 38 bolas, com base nos filtros aplicados. É útil para fins estatísticos e para validar a matemática do sistema.

GET /api/MegaSena/contar

Parâmetros (Query String)
Parâmetro	Tipo	Obrigatório	Padrão	Descrição
maxPares	int	Não	1	Máximo de pares consecutivos permitidos em um jogo.
ultimos	int	Não	3	Quantidade de sorteios passados a serem ignorados.
menos	int	Não	4	Quantidade de dezenas menos sorteadas a serem ignoradas.
Exemplo de Requisição
http
GET /api/MegaSena/contar?maxPares=1&ultimos=3&menos=4
Exemplo de Resposta (200 OK)
json
{
  "universo42": 4955700,
  "universo38": 2651467,
  "universo60": 46383480
}
(Nota: O universo42 representa o globo sem os últimos 3 sorteios. O universo38 remove também as 4 menos sorteadas).

3. Testar Uniformidade (Qui-Quadrado)
Roda N simulações no motor escolhido e retorna o cálculo de Qui-quadrado para provar que não há viés matemático no sorteio (o motor é justo).

GET /api/MegaSena/uniformidade

Parâmetros (Query String)
Parâmetro	Tipo	Obrigatório	Padrão	Descrição
fisico	boolean	Não	false	Usar o motor de física 2D para o teste (mais lento, porém real).
n	int	Não	300	Número de sorteios simulados para o cálculo estatístico.
ultimos	int	Não	3	Quantidade de sorteios passados a serem ignorados.
menos	int	Não	4	Quantidade de dezenas menos sorteadas a serem ignoradas.
Exemplo de Requisição
http
GET /api/MegaSena/uniformidade?fisico=true&n=300&ultimos=3&menos=4
Exemplo de Resposta (200 OK)
json
{
  "quiQuadrado": 59.87,
  "aprovado": true,
  "limite": 77.9,
  "universo": 38
}
Como interpretar o resultado:
quiQuadrado: O valor calculado. Se estiver abaixo do limite, o sorteio é uniforme.

aprovado: true se o valor estiver dentro do esperado para 95% de confiança.

limite: O valor máximo tolerado (calculado dinamicamente com base no tamanho do universo). Para 60 bolas, o limite é ~77.9. Para 38 bolas, o limite cai para ~52.2.

universo: O tamanho do globo (quantidade de bolas) usado no teste.

text

---

### O que fazer agora?
1. Salve este arquivo na raiz do projeto.
2. Se ainda não criou o **`.gitignore`**, crie-o agora (é o último arquivo obrigatório).
3. Seu projeto está **oficialmente documentado e pronto para o portfólio**!

Se você quiser subir tudo agora para o GitHub, me avise que eu te passo o passo a passo (ou relembro os comandos do Git) para você não ter erro!