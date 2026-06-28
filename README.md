# Oficina Mecanica - Sistema Integrado de Atendimento e Execucao de Servicos

MVP back-end (monolito em camadas, DDD) do Tech Challenge FIAP / Pos Tech - Fase 1.
Gestao de Ordens de Servico (OS), clientes, veiculos, servicos, pecas/insumos e
acompanhamento da OS pelo cliente.

## Stack

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core (Npgsql) + PostgreSQL
- Swagger / OpenAPI
- JWT Bearer (APIs administrativas)
- xUnit + FluentAssertions + WebApplicationFactory + Testcontainers
- Docker / docker-compose

## Links

- **Event Storming (board no Miro):** https://miro.com/app/board/uXjVHDVaKxg=/?share_link_id=818426939332

## Arquitetura (camadas + DDD)

Camadas, da mais externa para o nucleo:

- **Oficina.API** - controllers REST, Swagger, JWT, middleware de erros, injecao de dependencia.
- **Oficina.Application** - casos de uso (services), DTOs e validacao (FluentValidation).
- **Oficina.Domain** - entidades, Value Objects, enums e regras de negocio (sem dependencias).
- **Oficina.Infrastructure** - EF Core, repositorios, UnitOfWork, JWT e seed (implementa as interfaces da Application).
- **tests/** - `Oficina.UnitTests` (dominio) e `Oficina.IntegrationTests` (API + Postgres).

Fluxo de dependencia: API -> Application -> Domain. A Infrastructure implementa as interfaces
da Application e e injetada na API; o Domain nao referencia nenhuma outra camada.

As regras de negocio e a maquina de estados da OS ficam no agregado `OrdemServico` (o `Orcamento`
e os itens de servico/peca sao partes internas do agregado, persistidos como entidades filhas 1-N).

## Como rodar

### Opcao 1 - Docker (recomendado: API + Postgres juntos)

Pre-requisito: Docker Desktop em execucao.

```bash
docker compose up --build
```

- API:     http://localhost:8080
- Swagger:  http://localhost:8080/swagger
- Health:   http://localhost:8080/health

No primeiro start o schema e os seeds (catalogo de servicos, estoque e usuario admin)
sao criados automaticamente.

### Opcao 2 - Local (dotnet)

Pre-requisitos: .NET 8 SDK e um Postgres acessivel (ajuste `ConnectionStrings:Default`
em `appsettings.json`). Dica: suba so o banco com `docker compose up -d db`.

```bash
dotnet restore
dotnet build
dotnet run --project src/Oficina.API
```

- Swagger: http://localhost:5080/swagger

## Autenticacao (JWT)

As APIs administrativas (CRUDs e fluxos da OS) exigem token JWT. Endpoints publicos:
`/health`, `/api/auth/login` e a consulta de andamento da OS pelo cliente
(`GET /api/acompanhamento/{id}`).

Usuario seed inicial (criado automaticamente):

| Username | Senha    | Role  |
|----------|----------|-------|
| admin    | admin123 | Admin |

Fluxo no Swagger:

1. `POST /api/auth/login` com `{ "username": "admin", "password": "admin123" }` -> retorna `token`.
2. Clique em **Authorize** e informe o token.
3. Chame os endpoints protegidos.

Seguranca: as senhas sao armazenadas com hash **PBKDF2 (SHA-256, 100k iteracoes, salt aleatorio)**.
A senha do seed pode ser definida pela variavel `SEED_ADMIN_PASSWORD` e o `Jwt:SecretKey`
deve vir de variavel de ambiente (`Jwt__SecretKey`) em producao.

## Fluxo de uso da OS (ponta a ponta)

1. `POST /api/clientes` e `POST /api/veiculos` (cadastros).
2. `POST /api/ordens-servico` { clienteId, veiculoId } -> OS **Recebida**.
3. `POST /api/ordens-servico/{id}/iniciar-diagnostico` -> **Em diagnostico**.
4. `POST .../{id}/servicos` { servicoId } e `.../{id}/pecas` { pecaId, quantidade }.
5. `POST .../{id}/finalizar-diagnostico` -> gera orcamento, **Aguardando aprovacao**.
6. `POST .../{id}/aprovar` (ou `/cancelar`, ou `DELETE .../servicos/{itemId}` para recusar item) -> **Em execucao**.
7. `POST .../servicos/{itemId}/executar` e `POST .../pecas/{itemId}/usar` (baixa estoque).
8. `POST .../{id}/finalizar-execucao` -> **Finalizada**.
9. `POST .../{id}/entregar` -> **Entregue**.

Consulta publica do cliente: `GET /api/acompanhamento/{id}`.
Relatorio gerencial: `GET /api/ordens-servico/relatorios/tempo-medio`.

## Banco de dados e migrations

A persistencia usa EF Core + Npgsql. No startup a aplicacao chama `DbInitializer.InitializeAsync`,
que aplica as migrations se existirem ou, caso contrario, faz `EnsureCreated()` (cria o schema
a partir do modelo). Assim o `docker compose up` ja sobe com schema + seeds.

Para gerar a migration inicial (opcional, requer `dotnet-ef`):

```bash
dotnet ef migrations add InitialCreate \
  --project src/Oficina.Infrastructure \
  --startup-project src/Oficina.API \
  --output-dir Persistence/Migrations
```

> Nao use o mesmo volume de banco alternando entre EnsureCreated e migrations. Para a entrega,
> gere a migration antes do primeiro `up` (ou apague o volume `oficina-pgdata`).

## Justificativa do banco de dados (PostgreSQL)

A escolha do banco era livre no desafio; optamos pelo **PostgreSQL**:

1. **Gratuito e open-source (sem custo de licenca).** Diferente do SQL Server, que tem
   licenciamento pago para uso em producao, o PostgreSQL e totalmente gratuito - ideal para
   um MVP e para a banca rodar o projeto sem nenhuma barreira de licenca.
2. **Maduro, confiavel e ACID.** Garante consistencia transacional entre OS, orcamento e baixa
   de estoque (operacoes que precisam ser atomicas).
3. **Otima integracao com .NET / EF Core** via provider Npgsql (migrations, LINQ e conversores
   totalmente suportados).
4. **Conteinerizacao trivial.** A imagem oficial leve (`postgres:16-alpine`) sobe junto da API
   no docker-compose com healthcheck, facilitando rodar em qualquer maquina.
5. **Comunidade grande e recursos avancados** (tipos JSON, indices ricos, full-text) caso o
   dominio evolua, sem necessidade de trocar de banco.

Alem disso, o acesso a dados e abstraido por repositorios (interfaces na Application,
implementacao na Infrastructure), entao uma eventual troca de banco no futuro tem impacto
controlado.

## Testes

- **Unitarios (dominio):** Value Objects (Documento, Placa, Money), baixa/reposicao de estoque,
  ciclo de vida e transicoes da OS, hash de senha e geracao de JWT.
- **Integracao (API + Postgres real via Testcontainers):** autenticacao, CRUD completo e o fluxo
  ponta-a-ponta da OS com baixa de estoque e consulta publica. **Requer Docker em execucao.**

```bash
dotnet test
# com cobertura
dotnet test --collect:"XPlat Code Coverage"
```

Cobertura de testes: **95,1%** (Overall), acima da meta de 80% nos dominios criticos.

## Qualidade e seguranca

- **SonarQube Community:** Quality Gate **Passed** - 0 bugs, 0 vulnerabilidades abertas,
  0 security hotspots, 0% de duplicacao e 95,1% de cobertura.
- **Trivy:** analise de dependencias, imagem e configuracoes. A imagem final usa a base
  **chiseled** (minima e nao-root), reduzindo as vulnerabilidades de SO de ~144 (base Debian)
  para ~0.
- Dependencias .NET sem CVEs aplicaveis (Npgsql 8.0.4, JwtBearer/JWT 8.x).

Detalhes e evidencias (antes/depois) em `docs/RELATORIO-VULNERABILIDADES.md`.

## Documentacao do projeto

- `DDD-Oficina-Mecanica.md` - Event Storming, bounded contexts, agregados e linguagem ubiqua.
- `docs/Documentacao-Tecnica-Oficina.docx` - documentacao tecnica consolidada (arquitetura,
  testes, qualidade e seguranca).
- `docs/RELATORIO-VULNERABILIDADES.md` - analise de seguranca (SonarQube + Trivy).
- `docs/DOCUMENTO-ENTREGA.md` - checklist de requisitos e dados da entrega.
- `docs/` - prints/evidencias dos scans (Sonar e Trivy, antes e depois).

## Entregaveis da Fase 1 (checklist)

- [x] Back-end monolitico em camadas (DDD)
- [x] CRUDs de clientes, veiculos, servicos e pecas/insumos (com controle de estoque)
- [x] Criacao/acompanhamento da OS com 6 status e alteracao automatica
- [x] Orcamento automatico, aprovacao/recusa/cancelamento
- [x] Baixa de estoque no uso de peca
- [x] Consulta de andamento da OS pelo cliente (publica, via API)
- [x] Relatorio de tempo medio de execucao
- [x] Autenticacao JWT nas APIs administrativas
- [x] Validacao de CPF/CNPJ e placa
- [x] Testes unitarios e de integracao (cobertura 95,1%)
- [x] Dockerfile + docker-compose.yml
- [x] Swagger / OpenAPI
- [x] README com execucao local e justificativa do banco
- [x] Relatorio de analise de vulnerabilidades
