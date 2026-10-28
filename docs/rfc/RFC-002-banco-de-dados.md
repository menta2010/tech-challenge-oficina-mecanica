# RFC-002 — Banco de dados gerenciado

| | |
| --- | --- |
| **Status** | Aceita |
| **Data** | 20/10/2026 |
| **Autores** | Grupo 60 |

## Contexto

O banco precisa sair do pod com volume local (Fase 2) para um serviço gerenciado, sustentar a expansão para várias lojas e entregar consistência e performance. As consultas principais são: busca de cliente por CPF (a cada autenticação), listagem operacional de OS por loja, histórico de status e relatórios de tempo médio.

## Opções avaliadas

| Critério | RDS PostgreSQL | Aurora Serverless v2 (PostgreSQL) | DynamoDB |
| --- | --- | --- | --- |
| Modelo | Relacional | Relacional | Chave-valor/documento |
| Integridade (FK, UNIQUE, CHECK) | Sim | Sim | Não (fica na aplicação) |
| Transação OS + estoque | ACID | ACID | Transações limitadas a 100 itens, sem JOIN |
| Consultas por vários critérios | SQL + índices compostos | SQL + índices compostos | Um índice secundário por padrão de acesso |
| Custo no volume atual | Baixo (free tier `db.t4g.micro`) | Mínimo de ACUs sempre ligado | Baixo, mas exige remodelar tudo |
| Mudança no código | Nenhuma (EF Core + Npgsql já usados) | Nenhuma | Reescrever a persistência |

## Proposta

**Amazon RDS for PostgreSQL 16**, em subnets privadas, criptografado, com TLS obrigatório, backups automáticos, log de consultas lentas e Multi-AZ opcional (ligado em prod).

O domínio é fortemente relacional (loja ↔ OS ↔ cliente ↔ veículo ↔ itens ↔ estoque), e a baixa de estoque precisa ser atômica com a mudança da OS. DynamoDB exigiria duplicar dados e mover a integridade para o código. O Aurora Serverless fica registrado como evolução se o tráfego crescer ou ficar muito variável.

## Ajustes no modelo relacional

Detalhados no [README do oficina-infra-db](https://github.com/menta2010/oficina-infra-db#modelo-relacional-fase-3):

1. `Lojas` + `LojaId` em OS e usuários (ADR-004).
2. `EstoquesLoja` com `UNIQUE (LojaId, PecaInsumoId)` e `CHECK (Quantidade >= 0)` (ADR-003).
3. `Clientes.Ativo`, `CriadoEm` e `InativadoEm` (ADR-005).
4. `HistoricoStatusOS` para o tempo por status e a auditoria.
5. `OrdensServico.Encerrada` com índice composto para a listagem (ADR-008).
6. Chaves estrangeiras que antes não existiam (OS → loja/cliente/veículo; veículo → cliente).
7. Schema versionado por **migrations** do EF Core, aplicadas por um Job do Kubernetes antes de cada deploy.

## Consequências

- O schema passa a ser responsabilidade das migrations da aplicação; a Lambda só lê (`Clientes`, `Usuarios`) e o job de inativação só atualiza `Clientes.Ativo`.
- A Lambda e o cluster acessam o banco pela VPC; nada é exposto à internet.
