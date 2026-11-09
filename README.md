<div align="center">
  <h1>🎰 MegaSena SaaS Engine</h1>
  <p><strong>Inteligência de Dados, Fechamentos Matemáticos e Auditoria Criptográfica</strong></p>
  
  [![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
  [![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)]()
  [![SQL Server](https://img.shields.io/badge/SQL_Server-CC292B?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)]()
  [![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen?style=for-the-badge)]()
</div>

<br/>

> **O que é?** Uma plataforma SaaS (Software as a Service) completa para geração de fechamentos matemáticos, análise estatística e auditoria de jogos da Mega-Sena. Construída com **.NET 10** e **Clean Architecture**, provando que a engenharia de software pode transformar a forma como lidamos com probabilidades.

---

## 🏗️ Arquitetura e Tecnologias
O projeto foi desenhado utilizando os princípios da **Clean Architecture**, garantindo que as regras de negócio (matemática, combinatória e física) sejam independentes de banco de dados ou frameworks.

*   **Back-end:** C# / .NET 10, ASP.NET Core (Web API), Entity Framework Core.
*   **Banco de Dados:** SQL Server (Migrations via EF Core).
*   **Front-end:** HTML5, CSS3, JavaScript puro e Canvas API (renderização do globo físico).
*   **Testes:** xUnit (101 testes automatizados cobrindo todas as camadas).
*   **Segurança:** JWT (JSON Web Tokens), Rate Limiting e Hash SHA-256 para auditoria.

---

## ⚙️ Como Rodar o Projeto

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server local ou LocalDB
- Variáveis de ambiente configuradas (`Jwt:Key` com no mínimo 32 caracteres e `ConnectionStrings:DefaultConnection`)

### 1. Banco de Dados (Migrations)
Crie as tabelas e popule os dados iniciais (incluindo o Perfil Admin):
```bash
dotnet ef database update --project src/MegaSena.Infrastructure --startup-project src/MegaSena.Api

2. Web API
Inicie o servidor back-end:

bash
dotnet run --project src/MegaSena.Api
A documentação interativa (Scalar) abrirá em: https://localhost:7155/scalar

3. Front-end
Com a API rodando, basta abrir o arquivo front/globo-megasena.html no seu navegador de preferência.

🔐 O Fluxo de Auditoria (Como funciona?)
O grande diferencial do sistema é a transparência. Para garantir que os jogos foram gerados antes do sorteio oficial, utilizamos criptografia:

O usuário gera o fechamento em POST /api/Cobertura/gerar.

O servidor calcula um Hash SHA-256 único contendo as dezenas e os parâmetros.

O Hash é salvo no banco e devolvido ao usuário.

O usuário pode publicar esse Hash nas redes sociais. Qualquer pessoa pode consultar em GET /api/Auditoria/{hash} e provar que o lote existia antes do sorteio.

<br/> <div align="center"> <img src="docs/auditoria.png" alt="Exemplo de Resposta de Auditoria" width="600"/> </div> <br/>
📱 Explorando as Funcionalidades (Telas)
1. Simulador Físico (Globo 2D)
A página inicial conta com um globo desenhado no Canvas. Ao clicar em "Sortear", um motor de física 2D calcula colisões, gravidade e jato de ar para sortear 6 dezenas, simulando a urna real.

Regra aplicada: O sistema aceita jogos com no máximo 1 par consecutivo (ex: 10 e 11), melhorando a distribuição estatística.

<br/> <div align="center"> <img src="docs/globo.png" alt="Simulador de Globo Físico" width="600"/> </div> <br/>
2. Fechamento Matemático (O SaaS)
Uma área voltada para criação de bolões. O usuário digita suas dezenas, a garantia desejada (ex: garantir pelo menos 1 quadra) e o sistema usa otimização combinatória para gerar a menor quantidade aproximadamente possível de jogos, retornando se o mínimo foi provado (minimoProvado).

<br/> <div align="center"> <img src="docs/fechamento.png" alt="Tela de Geração de Fechamento" width="600"/> </div> <br/>
3. Análise Estatística (Qui-Quadrado)
Uma aba que consome os dados reais da Caixa (importados via background service). O sistema roda o teste do Qui-Quadrado para provar matematicamente se o sorteio da Caixa é justo ou tem algum viés oculto (P-valor > 0.05).

<br/> <div align="center"> <img src="docs/estatistica.png" alt="Gráfico de Frequência de Dezenas" width="600"/> </div> <br/>
4. Painel de Controle (Admin)
Área restrita com autenticação JWT. Exibe as mensagens de contato, permite marcar mensagens como lidas e visualizar o histórico de lotes gerados pelo usuário.

<br/> <div align="center"> <img src="docs/admin.png" alt="Painel Administrativo" width="600"/> </div> <br/>
🧪 Testes Automatizados
A integridade de todo o motor de combinatória, física e regras de segurança é garantida por 101 testes unitários e de integração.

bash
# Roda todos os testes
dotnet test
<div align="center"> <p>Desenvolvido com dedicação para demonstrar a união entre matemática, física e engenharia de software de ponta.</p> </div> ```




O que você precisa fazer agora:
Copie todo o texto acima (desde o <div align="center"> até o </div> final).

Cole no seu README.md.

Crie uma pasta chamada docs na raiz do seu projeto.

Tire os prints (globo, fechamento, estatística, admin) e salve com os nomes globo.png, fechamento.png, estatistica.png e admin.png dentro dessa pasta docs.
