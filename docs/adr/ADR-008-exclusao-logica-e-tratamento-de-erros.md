# ADR-008 — Exclusão lógica explícita e tratamento de erros

**Status:** Aceito · **Data:** 22/10/2026

## Contexto

Na correção da Fase 2 (87/90) recebemos dois pontos de atenção:

1. **Listagem:** "a lógica de exclusão lógica das finalizadas/entregues não está totalmente clara". A listagem filtrava por status dentro do repositório, sem um conceito explícito no modelo.
2. **Clean Code:** "melhorar o tratamento de erros e reduzir a duplicação em métodos".

## Decisão

### Exclusão lógica

- Nova coluna **`OrdensServico.Encerrada`** (+ `EncerradaEm`), marcada pelo domínio no único método que muda status (`AlterarStatus`) quando a OS entra em `Finalizada`, `Entregue` ou `Cancelada` (`OrdemServico.StatusTerminais`).
- A listagem padrão filtra `Encerrada = false` por um índice composto `(Encerrada, LojaId, Status, CriadaEm)`, ordena por prioridade de status (Em execução → Aguardando aprovação → Em diagnóstico → Recebida) e, dentro do status, da mais antiga para a mais nova.
- `GET /api/ordens-servico?incluirEncerradas=true` traz também as encerradas, sempre ao final da lista; um filtro explícito por status terminal (`?status=Entregue`) também funciona.
- Coberto por testes de domínio (quando marca, que não desmarca) e de integração (o que aparece, a ordem e o parâmetro).

### Tratamento de erros

- Respostas de erro no padrão **Problem Details (RFC 7807)**, `application/problem+json`, sempre com `correlationId`, e lista de campos em erros de validação.
- Mapeamento único no middleware: validação/regra → 400, não autenticado → 401, **sem permissão → 403** (novo, para o escopo por loja/cliente), não encontrado → 404, conflito ou violação de FK/UNIQUE no banco → 409, demais → 500 sem detalhes internos.
- 4xx registrados como *warning*, 5xx como *error* com stack trace (este último aciona alerta).
- Falhas nas operações de OS também geram o evento `OrdemServicoEvento` com `evento = 'falha'` e `inesperada = true|false`.

### Duplicação

- O padrão "buscar ou lançar 404", repetido em todos os services, virou a extensão `ObterOuFalharAsync`.
- Todas as transições de OS passam por um único fluxo (`MutarAsync`: carregar com verificação de acesso → aplicar → salvar → métrica → notificação).
- No domínio, as buscas de item por id viraram um helper único, e toda mudança de status passa por `AlterarStatus`.

## Consequências

- A regra "o que sai da listagem" está no modelo e no banco, não escondida numa consulta.
- Clientes da API recebem um formato de erro estável e conseguem citar o `correlationId` ao reportar problemas.
