# Oficina Mecanica - Sistema Integrado de Atendimento e Execucao de Servicos

MVP back-end (monolito em camadas, DDD) do Tech Challenge FIAP/Pos Tech - Fase 1.
Gestao de Ordens de Servico, clientes, veiculos, servicos, pecas/insumos e acompanhamento da OS pelo cliente.

## Stack

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core (Npgsql) + PostgreSQL
- Swagger / OpenAPI
- JWT Bearer (APIs administrativas)
- xUnit + FluentAssertions + WebApplicationFactory + Testcontainers

## Arquitetura (camadas)

```
Oficina.API            -> controllers, Swagger, JWT, DI
   -> Oficina.Application  -> casos de uso, DTOs, validators
       -> Oficina.Domain   -> entidades, VOs, enums, regras (sem dependencias)
Oficina.Infrastructure -> EF Core, repositorios, migrations (implementa interfaces)
tests/Oficina.UnitTests / tests/Oficina.IntegrationTests
```

Regra de dependencia: Domain nao referencia nada; API so conhece Infrastructure via injecao.

## Como rodar

### Opcao 1 - Docker (ambiente completo: API + Postgres)

```bash
docker compose up --build
```

- API: http://localhost:8080
- Swagger: http://localhost:8080/swagger
- Health: http://localhost:8080/health

### Opcao 2 - Local (dotnet)

Pre-requisitos: .NET 8 SDK e um Postgres acessivel (ajuste `ConnectionStrings:Default` em `appsettings.json`).

```bash
dotnet restore
dotnet build
dotnet run --project src/Oficina.API
```

- Swagger: http://localhost:5080/swagger
- Health: http://localhost:5080/health

## Documentacao do projeto

- `DDD-Oficina-Mecanica.md` — Event Storming, bounded contexts, agregados, linguagem ubiqua.
- `docs/DOCUMENTO-ENTREGA.md` — checklist de requisitos e dados da entrega.
- `docs/RELATORIO-VULNERABILIDADES.md` — analise de seguranca e scan.
- `scripts/security-scan.sh` — executa os scans de vulnerabilidade.

## Autenticacao (JWT)

As APIs administrativas (CRUDs) exigem token JWT. Publicos: `/health`, `/api/auth/login`
e (item 6) a consulta de andamento da OS pelo cliente.

Usuario seed inicial (criado automaticamente):

| Username | Senha    | Role  |
|----------|----------|-------|
| admin    | admin123 | Admin |

Fluxo:

1. `POST /api/auth/login` com `{ "username": "admin", "password": "admin123" }` -> retorna `token`.
2. No Swagger, clique em **Authorize** e informe `Bearer <token>` (ou so o token).
3. Chame os endpoints protegidos.

Senhas sao armazenadas com hash PBKDF2 (SHA256, 100k iteracoes). O `SecretKey` do JWT
fica em `appsettings.json`/variavel de ambiente e deve ser trocado em producao.

## Fluxo de uso da OS (ponta a ponta)

Todos os endpoints da OS (exceto acompanhamento) exigem JWT.

1. `POST /api/clientes` e `POST /api/veiculos` (cadastros).
2. `POST /api/ordens-servico` { clienteId, veiculoId } -> OS Recebida.
3. `POST /api/ordens-servico/{id}/iniciar-diagnostico` -> Em diagnostico.
4. `POST /api/ordens-servico/{id}/servicos` { servicoId } e `.../pecas` { pecaId, quantidade }.
5. `POST /api/ordens-servico/{id}/finalizar-diagnostico` -> gera orcamento, Aguardando aprovacao.
6. `POST /api/ordens-servico/{id}/aprovar` (ou `/cancelar`, ou `DELETE .../servicos/{itemId}` para recusar item) -> Em execucao.
7. `POST .../servicos/{itemId}/executar` e `POST .../pecas/{itemId}/usar` (baixa estoque).
8. `POST /api/ordens-servico/{id}/finalizar-execucao` -> Finalizada.
9. `POST /api/ordens-servico/{id}/entregar` -> Entregue.

Consulta publica do cliente: `GET /api/acompanhamento/{id}`.
Relatorio gerencial: `GET /api/ordens-servico/relatorios/tempo-medio`.

## Banco de dados e migrations

A persistencia usa EF Core + Npgsql (PostgreSQL). No startup a aplicacao chama
`DbInitializer.InitializeAsync`, que:

1. aplica as migrations se existirem; ou
2. faz `EnsureCreated()` (cria o schema a partir do modelo) caso ainda nao haja migrations.

Isso garante que `docker compose up` ja sobe com o schema e os seeds (catalogo de
servicos e estoque inicial), mesmo antes de gerar migrations.

### Gerar a migration inicial (recomendado para a entrega)

Requer o EF CLI (`dotnet tool install --global dotnet-ef`) e o SDK .NET 8:

```bash
dotnet ef migrations add InitialCreate \
  --project src/Oficina.Infrastructure \
  --startup-project src/Oficina.API \
  --output-dir Persistence/Migrations
```

A classe `OficinaDbContextFactory` (design-time) fornece a connection string para o CLI.
Apos gerar, prefira `Database.Migrate()` (ja contemplado pelo `DbInitializer`).

> Ponto de atencao: nao use o mesmo volume de banco alternando entre EnsureCreated e
> migrations. Para a entrega final, gere a migration antes do primeiro `up` (ou apague o
> volume `oficina-pgdata`).

## Testes

- Unitarios (dominio): VOs, baixa de estoque e ciclo de vida da OS (xUnit + FluentAssertions).
- Integracao (API + EF + Postgres real via Testcontainers): autenticacao, CRUD e fluxo
  ponta-a-ponta da OS. **Requer Docker em execucao** na maquina que roda os testes.

```bash
dotnet test
# com cobertura (coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

Dica para visualizar cobertura em HTML:

```bash
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:Html
```

Os dominios criticos (Value Objects e agregado OrdemServico) tem testes unitarios diretos
das regras, somados aos fluxos de integracao, para atingir a meta de 80%.

## Justificativa do banco de dados (PostgreSQL)

Escolhemos **PostgreSQL** para o MVP pelos seguintes motivos:

1. **Custo zero / open-source** - sem licenciamento, diferente do SQL Server, adequado a um MVP.
2. **Suporte maduro no EF Core** via provider Npgsql, com migrations e LINQ completos.
3. **Conteinerizacao trivial** - imagem oficial leve (`postgres:16-alpine`), sobe junto da API no docker-compose com healthcheck.
4. **Confiabilidade transacional (ACID)** - importante para consistencia entre OS, orcamento e baixa de estoque.
5. **Recursos uteis** (tipos JSON, indices ricos) caso o dominio evolua.

A camada de acesso e abstraida por repositorios no Domain/Application, entao a troca de banco no futuro tem impacto controlado.

## Estado do projeto

- Item 1 (Fundacao): solution, 5 projetos em camadas, Swagger, health check, Docker. CONCLUIDO.
- Item 2 (Dominio): VOs (Documento, Placa, Money), enum StatusOS, agregados
  Cliente/Veiculo/Servico/PecaInsumo/OrdemServico com maquina de estados e invariantes,
  + testes unitarios das regras criticas. CONCLUIDO.

- Item 3 (Persistencia): EF Core + PostgreSQL, DbContext, conversores de VOs, mapeamentos
  (owned types de itens/orcamento), repositorios, UnitOfWork, seed e design-time factory. CONCLUIDO.

- Item 4 (CRUDs): camada Application (DTOs, FluentValidation, services) + controllers REST de
  Cliente, Veiculo, Servico e Peca/Insumo (com reabastecimento de estoque); middleware global
  de erros (400/404/409/500); validacao de CPF/CNPJ e placa nas bordas. CONCLUIDO.

- Item 5 (JWT): usuario administrativo, login com emissao de token, PBKDF2 para senha,
  protecao [Authorize] nos CRUDs e seed do usuario admin. CONCLUIDO.

- Item 6 (Fluxos da OS): OrdemServicoService cobrindo todas as transicoes (criar, diagnostico,
  registrar/remover itens, gerar orcamento, aprovar/cancelar, executar, usar peca com baixa de
  estoque, finalizar, entregar); consulta publica de andamento; relatorio de tempo medio. CONCLUIDO.

- Item 7 (Testes de integracao): WebApplicationFactory + Testcontainers (Postgres), cobrindo
  auth, CRUD e o fluxo completo da OS com baixa de estoque e acompanhamento publico. CONCLUIDO.

Backlog tecnico do MVP concluido. Itens finais (rodar coverage real, gravar video,
preencher dados do grupo/links) dependem do ambiente local e da entrega.

## Entregaveis da Fase 1 (checklist resumido)

- [x] Estrutura monolitica em camadas
- [x] Dockerfile + docker-compose.yml
- [x] Swagger configurado
- [x] README com execucao local
- [x] CRUDs administrativos (Cliente, Veiculo, Servico, Peca/Insumo)
- [x] Fluxos da OS (criar/diagnostico/orcamento/execucao/entrega)
- [x] Consulta de andamento da OS pelo cliente (publica)
- [x] Relatorio de tempo medio de execucao
- [x] Baixa de estoque no uso de peca
- [x] Autenticacao JWT (login + [Authorize])
- [x] Testes unitarios e de integracao dos principais fluxos
- [ ] Confirmar cobertura >= 80% (rodar coverage localmente)
- [x] Relatorio de vulnerabilidades (docs/RELATORIO-VULNERABILIDADES.md)
