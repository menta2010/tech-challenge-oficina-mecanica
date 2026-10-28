# ADR-009 — Convenção de linguagem no código

**Status:** Aceito · **Data:** 22/10/2026

## Contexto

O código misturava português e inglês sem regra: `CriarOrdemServicoRequest` convivia com `CreateClienteRequest`, e `InvalidStatusTransitionException` convivia com `OrdemServico`.

## Decisão

| Categoria | Idioma | Exemplos |
| --- | --- | --- |
| Domínio (linguagem ubíqua do DDD): entidades, value objects, regras, exceções de negócio, casos de uso | **Português** | `OrdemServico`, `EstoqueLoja`, `AlterarStatus`, `TransicaoStatusInvalidaException`, `CriarClienteRequest` |
| Padrões e infraestrutura técnica | **Inglês** | `Repository`, `Service`, `Controller`, `Middleware`, `Dto`, `UnitOfWork`, `NotFoundException` |
| Mensagens para o usuário e logs | Português | "Cliente nao encontrado." |
| Rotas HTTP | Português, kebab-case | `/api/ordens-servico/{id}/finalizar-diagnostico` |

O que foi padronizado nesta fase: `Create*/Update*Request` → `Criar*/Atualizar*Request` (clientes, veículos, serviços, peças) e `InvalidStatusTransitionException` → `TransicaoStatusInvalidaException`.

## Consequências

- O vocabulário do negócio aparece igual no código, na API, nos diagramas e na conversa com a oficina.
- Termos técnicos consagrados continuam reconhecíveis por qualquer pessoa desenvolvedora.
