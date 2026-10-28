# ADR-005 — Política de inativação de clientes

**Status:** Aceito · **Data:** 21/10/2026

## Contexto

O enunciado pede que a autenticação consulte **existência e status** do cliente. Precisávamos definir o que torna um cliente inativo e como ele volta a ficar ativo.

Uma ideia inicial era inativar para reduzir custo de máquina. Isso **não se sustenta sozinho**: a busca por CPF usa o índice único de `Documento` e custa o mesmo com 1 mil ou 1 milhão de clientes. As justificativas reais são outras.

## Decisão

- `Clientes.Ativo` (padrão `true`), `CriadoEm` e `InativadoEm`.
- **Inativação manual** por funcionário: `POST /api/clientes/{id}/inativar` e `/reativar`.
- **Inativação automática**: Lambda agendada pelo EventBridge (diariamente às 06:00 UTC) que inativa clientes **criados há mais de 24 meses, sem OS aberta nos últimos 24 meses e sem nenhuma OS em andamento**. Validada contra o schema real.
- Cliente inativo **não recebe token** pela autenticação por CPF (403).
- **Reativação automática**: quando um funcionário abre uma nova OS para um cliente inativo, a API o reativa.

## Justificativa

| Motivo | Efeito |
| --- | --- |
| Segurança | Um CPF parado há anos deixa de ser uma porta de entrada para as APIs |
| LGPD (minimização) | Dados de quem não usa o serviço saem do uso ativo; base para uma futura anonimização |
| Qualidade dos dados | Relatórios e campanhas consideram só a base ativa |

## Consequências

- O prazo (24 meses) é configurável na Lambda (`MESES_INATIVIDADE`).
- O job só atualiza `Ativo` e `InativadoEm`: é idempotente e pode rodar de novo sem efeito colateral.
