# Documento de Entrega — Fase 1

Tech Challenge FIAP/Pós Tech — Oficina Mecânica (Sistema Integrado de Atendimento e Execução de Serviços).

## Identificação

- **Nome do grupo:** [preencher]
- **Participantes e usernames no Discord:**

| Nome | Username Discord |
|---|---|
| [preencher] | [preencher] |
| [preencher] | [preencher] |

- **Link da documentação (Miro/DDD):** [preencher] — ver também `DDD-Oficina-Mecanica.md`
- **Link do repositório (privado, com acesso a `soat-architecture`):** [preencher]
- **Vídeo de demonstração (até 15 min):** [preencher]

## Resumo da solução

Back-end monolítico em .NET 8, arquitetura em camadas (API, Application, Domain, Infrastructure) com DDD. PostgreSQL via EF Core. APIs REST documentadas no Swagger, autenticação JWT nas rotas administrativas, validação de CPF/CNPJ e placa, controle de estoque com baixa automática, e consulta pública de andamento da OS. Testes unitários (domínio) e de integração (API + Postgres em container).

## Checklist dos requisitos (PDF) → onde é atendido

| # | Requisito | Status | Onde |
|---|---|---|---|
| 1 | Identificação do cliente por CPF/CNPJ | OK | `Documento` (VO), `ClientesController` |
| 2 | Cadastro de veículo (placa, marca, modelo, ano) | OK | `Placa` (VO), `Veiculo`, `VeiculosController` |
| 3 | Inclusão de serviços na OS | OK | `OrdemServicoService.AdicionarServico` |
| 4 | Inclusão de peças/insumos na OS | OK | `OrdemServicoService.AdicionarPeca` |
| 5 | Orçamento gerado automaticamente | OK | `OrdemServico.FinalizarDiagnostico` → `Orcamento` |
| 6 | Envio do orçamento para aprovação | OK | Status `AguardandoAprovacao` + `/aprovar` |
| 7 | Status da OS (6 estados) | OK | enum `StatusOS` + máquina de estados |
| 8 | Alteração automática de status | OK | Políticas no agregado `OrdemServico` |
| 9 | Consulta da OS pelo cliente via API | OK | `AcompanhamentoController` (público) |
| 10 | CRUD de clientes | OK | `ClientesController` |
| 11 | CRUD de veículos | OK | `VeiculosController` |
| 12 | CRUD de serviços | OK | `ServicosController` |
| 13 | CRUD de peças/insumos com estoque | OK | `PecasInsumosController` (+ reabastecer) |
| 14 | Listagem e detalhamento de OS | OK | `OrdensServicoController` GET (lista/detalhe) |
| 15 | Tempo médio de execução | OK | `GET /api/ordens-servico/relatorios/tempo-medio` |
| 16 | Baixa de estoque ao usar peça | OK | `OrdemServicoService.UsarPeca` |
| 17 | Aprovação/recusa/cancelamento | OK | `/aprovar`, `DELETE itens`, `/cancelar` |
| 18 | Autenticação JWT (APIs admin) | OK | `AddInfrastructure` (JwtBearer) + `[Authorize]` |
| 19 | Validação de CPF/CNPJ | OK | `Documento.Criar` |
| 20 | Validação de placa | OK | `Placa.Criar` |
| 21 | Testes unitários | OK | `Oficina.UnitTests` |
| 22 | Testes de integração | OK | `Oficina.IntegrationTests` |
| 23 | Cobertura ≥ 80% nos domínios críticos | A confirmar | Rodar `dotnet test --collect` (ver README) |
| 24 | Back-end monolítico em camadas | OK | Solução `Oficina.sln` (4 projetos src) |
| 25 | API RESTful documentada (Swagger) | OK | `Program.cs` (SwaggerGen + UI) |
| 26 | Dockerfile | OK | `Dockerfile` (multi-stage) |
| 27 | docker-compose.yml | OK | `docker-compose.yml` (API + Postgres) |
| 28 | Justificativa do banco | OK | `README.md` (PostgreSQL) |
| 29 | README com execução local | OK | `README.md` |
| 30 | Relatório de vulnerabilidades | OK | `docs/RELATORIO-VULNERABILIDADES.md` |
| 31 | Repositório organizado | OK | `src/`, `tests/`, `docs/`, `scripts/` |

## Como executar (resumo)

```bash
# Ambiente completo (API + Postgres)
docker compose up --build
# Swagger: http://localhost:8080/swagger

# Testes (requer Docker para integração)
dotnet test
```

Credencial inicial (seed): `admin` / `admin123`. Detalhes no `README.md`.

## Entregáveis incluídos no repositório

- Código-fonte (`src/`, `tests/`).
- `Dockerfile` e `docker-compose.yml`.
- `README.md` (execução, justificativa de banco, autenticação, fluxo da OS).
- Documentação DDD: `DDD-Oficina-Mecanica.md` (Event Storming, contextos, agregados, linguagem ubíqua).
- Relatório de vulnerabilidades: `docs/RELATORIO-VULNERABILIDADES.md` + `scripts/security-scan.sh`.
- Este documento de entrega.
