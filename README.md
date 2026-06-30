# Oficina Mecânica - Sistema Integrado de Atendimento e Execução de Serviços

MVP back-end do Tech Challenge Oficina Mecânica - Fase 1.

O projeto implementa um sistema para atendimento de oficina mecânica, cobrindo
cadastro de clientes e veículos, catálogo de serviços, controle de peças e
insumos, criação e acompanhamento de Ordens de Serviço (OS), orçamento
automático, autorização, execução, baixa de estoque e entrega do veículo.

## Índice

- [Oficina Mecânica - Sistema Integrado de Atendimento e Execução de Serviços](#oficina-mecânica---sistema-integrado-de-atendimento-e-execução-de-serviços)
  - [Índice](#índice)
  - [Objetivo da Entrega](#objetivo-da-entrega)
  - [Stack Utilizada](#stack-utilizada)
  - [Como Rodar o Projeto](#como-rodar-o-projeto)
    - [Opção 1 - Docker Compose](#opção-1---docker-compose)
    - [Opção 2 - Local com .NET](#opção-2---local-com-net)
  - [Como Testar os Endpoints com Swagger](#como-testar-os-endpoints-com-swagger)
  - [Links da Entrega](#links-da-entrega)
  - [Arquitetura](#arquitetura)
    - [Camadas](#camadas)
  - [Principais Domínios](#principais-domínios)
  - [Fluxo da Ordem de Serviço](#fluxo-da-ordem-de-serviço)
  - [Endpoints](#endpoints)
    - [Públicos](#públicos)
    - [Clientes](#clientes)
    - [Veículos](#veículos)
    - [Serviços](#serviços)
    - [Peças e Insumos](#peças-e-insumos)
    - [Ordens de Serviço](#ordens-de-serviço)
  - [Autenticação JWT](#autenticação-jwt)
  - [Banco de Dados](#banco-de-dados)
  - [Testes](#testes)
  - [Collection Insomnia](#collection-insomnia)
  - [Qualidade e Segurança](#qualidade-e-segurança)
  - [Entregáveis da Fase 1](#entregáveis-da-fase-1)
  - [Documento de Entrega](#documento-de-entrega)

## Objetivo da Entrega

Atender ao descritivo da Fase 1 do Tech Challenge:

- back-end monolítico em camadas;
- aplicação de DDD nos domínios centrais;
- APIs REST documentadas com Swagger;
- autenticação JWT para APIs administrativas;
- validação de CPF/CNPJ e placa de veículo;
- testes unitários e de integração nos fluxos principais;
- Dockerfile e docker-compose para execução simples;
- justificativa do banco de dados;
- relatório de vulnerabilidades;
- documento de entrega em PDF.

## Stack Utilizada

- .NET 8 / ASP.NET Core Web API
- Entity Framework Core 8 + Npgsql
- PostgreSQL 16
- FluentValidation
- Swagger / OpenAPI (Swashbuckle)
- JWT Bearer
- PBKDF2 para hash de senha
- xUnit + FluentAssertions
- WebApplicationFactory + Testcontainers para testes de integração
- Coverlet para coleta de cobertura
- Docker / Docker Compose
- SonarQube Community e Trivy para qualidade e segurança

## Como Rodar o Projeto

### Opção 1 - Docker Compose

Pré-requisito: Docker Desktop em execução.

```bash
docker compose up --build
```

URLs:

- API: http://localhost:8080
- Swagger: http://localhost:8080/swagger
- Health: http://localhost:8080/health

No primeiro start, o schema e os dados seed são criados automaticamente:

- serviços iniciais;
- peças/insumos iniciais;
- usuário admin.

### Opção 2 - Local com .NET

Pré-requisitos:

- .NET 8 SDK;
- PostgreSQL acessível.

Suba apenas o banco pelo Compose:

```bash
docker compose up -d db
```

Rode a API:

```bash
dotnet restore
dotnet build
dotnet run --project src/Oficina.API
```

URL esperada:

- Swagger: http://localhost:5080/swagger

## Como Testar os Endpoints com Swagger

1. Acesse o Swagger:
   - Docker: http://localhost:8080/swagger
   - Local dotnet: http://localhost:5080/swagger
2. Execute `POST /api/auth/login` com:

```json
{
  "username": "admin",
  "password": "admin123"
}
```

3. Copie o campo `token` retornado.
4. Clique em `Authorize` no topo do Swagger e informe o token. Se a interface pedir o header completo, use `Bearer {token}`.
5. Execute o fluxo principal:
   - `POST /api/clientes`
   - `POST /api/veiculos`
   - `GET /api/servicos` e copie um `servicoId`
   - `GET /api/pecas-insumos` e copie um `pecaId`
   - `POST /api/ordens-servico`
   - `POST /api/ordens-servico/{id}/iniciar-diagnostico`
   - `POST /api/ordens-servico/{id}/servicos`
   - `POST /api/ordens-servico/{id}/pecas`
   - `POST /api/ordens-servico/{id}/finalizar-diagnostico`
   - `POST /api/ordens-servico/{id}/aprovar`
   - `POST /api/ordens-servico/{id}/servicos/{itemId}/executar`
   - `POST /api/ordens-servico/{id}/pecas/{itemId}/usar`
   - `POST /api/ordens-servico/{id}/finalizar-execucao`
   - `POST /api/ordens-servico/{id}/entregar`
6. Consulte o andamento público sem token:
   - `GET /api/acompanhamento/{id}`
7. Consulte o relatório gerencial:
   - `GET /api/ordens-servico/relatorios/tempo-medio`

Também há uma collection do Insomnia em `docs/insomnia-oficina-mecanica.json`
para testar o mesmo fluxo em cliente HTTP.

## Links da Entrega

- Documentação DDD / Event Storming (Miro): https://miro.com/app/board/uXjVHDVaKxg=/?share_link_id=818426939332
- Repositório: https://github.com/menta2010/tech-challenge-oficina-mecanica
- Collection Insomnia: `docs/insomnia-oficina-mecanica.json`
- Documento de entrega: `docs/DOCUMENTO-ENTREGA.pdf`
- Relatório de vulnerabilidades: `docs/RELATORIO-VULNERABILIDADES.md`

## Arquitetura

O projeto é um monolito em camadas, com separação entre API, casos de uso,
domínio e infraestrutura.

```text
src/
  Oficina.API/
    Controllers/
    Middlewares/
    Program.cs
  Oficina.Application/
    Abstractions/
    Clientes/
    Estoque/
    Identidade/
    OrdensServico/
    Servicos/
    Veiculos/
  Oficina.Domain/
    Clientes/
    Estoque/
    Identidade/
    OrdensServico/
    Servicos/
    Shared/
    Veiculos/
  Oficina.Infrastructure/
    Identity/
    Persistence/
tests/
  Oficina.UnitTests/
  Oficina.IntegrationTests/
docs/
  evidências, relatórios e documento de entrega
```

### Camadas

- `Oficina.API`: controllers REST, Swagger, autenticação/autorização, middleware global de erros e health checks.
- `Oficina.Application`: services/casos de uso, DTOs, validadores e interfaces de repositório.
- `Oficina.Domain`: entidades, Value Objects, agregados, enums e regras de negócio.
- `Oficina.Infrastructure`: EF Core, PostgreSQL, configurações de entidades, repositórios, UnitOfWork, JWT, hash de senha e seed.
- `tests`: testes unitários do domínio e testes de integração da API com PostgreSQL real via Testcontainers.

Fluxo de dependência: `API -> Application -> Domain`. A Infrastructure implementa
as interfaces da Application e é injetada na API.

## Principais Domínios

- **Cliente**: cadastro e identificação por CPF/CNPJ via Value Object `Documento`.
- **Veículo**: vinculado ao cliente, com placa validada nos formatos antigo e Mercosul.
- **Serviço**: catálogo de serviços, valor base e tempo estimado.
- **Peça/Insumo**: cadastro, valor unitário, estoque, reposição e baixa.
- **OrdemServico**: agregado central; controla status, itens, orçamento e timestamps.
- **Identidade**: usuário administrativo, hash PBKDF2 e emissão de token JWT.

## Fluxo da Ordem de Serviço

Status previstos no desafio:

1. `Recebida`
2. `EmDiagnostico`
3. `AguardandoAprovacao`
4. `EmExecucao`
5. `Finalizada`
6. `Entregue`

O sistema também possui `Cancelada` como estado adicional para representar
recusa/cancelamento do orçamento.

Fluxo ponta a ponta:

1. Criar cliente e veículo.
2. Criar OS para cliente/veículo.
3. Iniciar diagnóstico.
4. Adicionar serviços e peças/insumos.
5. Finalizar diagnóstico, gerando orçamento automaticamente.
6. Aprovar orçamento ou cancelar OS.
7. Executar serviços e registrar uso de peças.
8. Baixar estoque ao usar peça.
9. Finalizar execução.
10. Entregar veículo.
11. Consultar acompanhamento público da OS.

## Endpoints

### Públicos

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Health check da aplicação |
| GET | `/api/health` | Health check via controller |
| POST | `/api/auth/login` | Login administrativo |
| GET | `/api/acompanhamento/{id}` | Consulta pública do andamento da OS |

### Clientes

| Método | Rota |
|---|---|
| POST | `/api/clientes` |
| GET | `/api/clientes` |
| GET | `/api/clientes/{id}` |
| PUT | `/api/clientes/{id}` |
| DELETE | `/api/clientes/{id}` |

### Veículos

| Método | Rota |
|---|---|
| POST | `/api/veiculos` |
| GET | `/api/veiculos` |
| GET | `/api/veiculos?clienteId={clienteId}` |
| GET | `/api/veiculos/{id}` |
| PUT | `/api/veiculos/{id}` |
| DELETE | `/api/veiculos/{id}` |

### Serviços

| Método | Rota |
|---|---|
| POST | `/api/servicos` |
| GET | `/api/servicos` |
| GET | `/api/servicos/{id}` |
| PUT | `/api/servicos/{id}` |
| DELETE | `/api/servicos/{id}` |

### Peças e Insumos

| Método | Rota |
|---|---|
| POST | `/api/pecas-insumos` |
| GET | `/api/pecas-insumos` |
| GET | `/api/pecas-insumos/{id}` |
| PUT | `/api/pecas-insumos/{id}` |
| POST | `/api/pecas-insumos/{id}/reabastecer` |
| DELETE | `/api/pecas-insumos/{id}` |

### Ordens de Serviço

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/ordens-servico` | Cria OS |
| GET | `/api/ordens-servico` | Lista OS |
| GET | `/api/ordens-servico?status={status}` | Lista por status |
| GET | `/api/ordens-servico/{id}` | Detalha OS |
| POST | `/api/ordens-servico/{id}/iniciar-diagnostico` | Muda para diagnóstico |
| POST | `/api/ordens-servico/{id}/servicos` | Adiciona serviço |
| DELETE | `/api/ordens-servico/{id}/servicos/{itemId}` | Remove item de serviço do orçamento |
| POST | `/api/ordens-servico/{id}/pecas` | Adiciona peça |
| DELETE | `/api/ordens-servico/{id}/pecas/{itemId}` | Remove item de peça do orçamento |
| POST | `/api/ordens-servico/{id}/finalizar-diagnostico` | Gera orçamento |
| POST | `/api/ordens-servico/{id}/aprovar` | Aprova orçamento |
| POST | `/api/ordens-servico/{id}/cancelar` | Cancela OS |
| POST | `/api/ordens-servico/{id}/servicos/{itemId}/executar` | Marca serviço executado |
| POST | `/api/ordens-servico/{id}/pecas/{itemId}/usar` | Usa peça e baixa estoque |
| POST | `/api/ordens-servico/{id}/finalizar-execucao` | Finaliza execução |
| POST | `/api/ordens-servico/{id}/entregar` | Entrega veículo |
| GET | `/api/ordens-servico/relatorios/tempo-medio` | Tempo médio de execução |

## Autenticação JWT

As APIs administrativas exigem token JWT. Endpoints públicos:

- `GET /health`
- `GET /api/health`
- `POST /api/auth/login`
- `GET /api/acompanhamento/{id}`

Usuário seed:

| Username | Senha | Role |
|---|---|---|
| `admin` | `admin123` | `Admin` |

Login:

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

No Swagger, clique em `Authorize` e informe o token retornado. Em clientes HTTP,
use `Authorization: Bearer {token}`.

As senhas são armazenadas com PBKDF2 SHA-256, 100k iterações e salt aleatório.
Em ambiente real, altere `Jwt__SecretKey` e `SEED_ADMIN_PASSWORD` por variáveis
de ambiente.

## Banco de Dados

Banco escolhido: **PostgreSQL**.

Justificativa:

1. Gratuito e open-source, sem barreira de licença.
2. Maduro, confiável e ACID, adequado para transações entre OS, orçamento e estoque.
3. Boa integração com .NET via EF Core e Npgsql.
4. Execução simples em container com imagem oficial.
5. Possui recursos avançados para evolução futura.

A persistência é isolada por interfaces na Application e implementações na
Infrastructure, reduzindo acoplamento com o banco.

No startup, `DbInitializer.InitializeAsync` aplica migrations se existirem; caso
contrário, usa `EnsureCreated()` para criar o schema do MVP.

## Testes

Rodar todos os testes:

```bash
dotnet test
```

Rodar com cobertura:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Observação: testes de integração exigem Docker, pois usam Testcontainers com
PostgreSQL real.

Cobertura declarada nas evidências: **95,1%**, acima da meta mínima de 80% nos
domínios críticos.

Cobertura funcional:

- CPF/CNPJ, placa e valor monetário;
- regras de estoque;
- ciclo de vida e transições da OS;
- autenticação e rotas protegidas;
- CRUDs administrativos;
- fluxo completo da OS com baixa de estoque;
- consulta pública de acompanhamento.

## Collection Insomnia

Collection alternativa para testar os endpoints fora do Swagger:

- arquivo: `docs/insomnia-oficina-mecanica.json`
- importe pelo Insomnia em `Import > From File`;
- execute o login e copie o token para a variável `token`;
- preencha os IDs retornados nas variáveis do ambiente.

## Qualidade e Segurança

Evidências em `docs/`:

- `Scan_Vulnerabilidade_Sonar_Antes_De_Corrigir.png`
- `Scan_Vulnerabilidade_Sonar_Depois_De_Corrigir.png`
- `Activity_Sonar.png`
- `sonar-issues_de_corrigir.json`
- `Scan_Vulnerabilidade_Trivy_Antes_De_Corrigir.html`
- `trivy-depois.html`
- `RELATORIO-VULNERABILIDADES.md`

Resumo:

- SonarQube: issues iniciais analisadas e corrigidas/justificadas.
- Trivy: imagem inicial baseada em Debian apresentou CVEs de SO; imagem final
  usa runtime chiseled/non-root, reduzindo o total para 6 achados LOW/MEDIUM e
  sem CRITICAL/HIGH.
- Dependências .NET revisadas, sem CVEs aplicáveis nas versões utilizadas.

## Entregáveis da Fase 1

| Requisito | Status | Evidência |
|---|---|---|
| Back-end monolítico | OK | Solução `.sln` em camadas |
| Arquitetura em camadas | OK | `src/Oficina.*` |
| DDD | OK | Agregados e Value Objects em `Oficina.Domain` + Miro |
| CRUD clientes | OK | `ClientesController` |
| CRUD veículos | OK | `VeiculosController` |
| CRUD serviços | OK | `ServicosController` |
| CRUD peças/insumos com estoque | OK | `PecasInsumosController` |
| Criação de OS | OK | `OrdensServicoController` |
| Orçamento automático | OK | `OrdemServico.FinalizarDiagnostico` |
| Status automáticos da OS | OK | agregado `OrdemServico` |
| Consulta da OS pelo cliente | OK | `AcompanhamentoController` |
| Tempo médio de execução | OK | `/relatorios/tempo-medio` |
| JWT nas APIs administrativas | OK | `[Authorize]` + `AddJwtBearer` |
| Validação CPF/CNPJ | OK | `Documento` |
| Validação placa | OK | `Placa` |
| Swagger | OK | `Program.cs` |
| Dockerfile | OK | `Dockerfile` |
| docker-compose | OK | `docker-compose.yml` |
| Testes automatizados | OK | `tests/` |
| Cobertura mínima de 80% | OK | evidências de 95,1% |
| README explicativo | OK | este arquivo |
| Relatório de vulnerabilidades | OK | `docs/RELATORIO-VULNERABILIDADES.md` |
| Documento de entrega PDF | OK, com campos `PREENCHER` a revisar | `docs/DOCUMENTO-ENTREGA.pdf` |
| Acesso ao usuário `soat-architecture` | PENDENTE DE CONFIRMAÇÃO | confirmar no GitHub |

## Documento de Entrega

O documento solicitado no descritivo está em:

- Fonte editável: `docs/DOCUMENTO-ENTREGA.md`
- PDF: `docs/DOCUMENTO-ENTREGA.pdf`

Antes da submissão final, revise os campos marcados como `PREENCHER`, em especial
o username do Discord dos participantes e o nome oficial do grupo caso exista.
