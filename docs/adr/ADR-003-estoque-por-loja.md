# ADR-003 — Estoque por loja

**Status:** Aceito · **Data:** 21/10/2026

## Contexto

Até a Fase 2 o saldo ficava em `PecasInsumos.QuantidadeEmEstoque`: um estoque único. Com várias unidades, uma peça usada no Centro não pode baixar o saldo da Zona Sul. Também avaliamos um **Centro de Distribuição** (CD) abastecendo as lojas.

## Decisão

- `PecasInsumos` vira **catálogo global** (nome e preço iguais para a rede).
- Nova tabela `EstoquesLoja (LojaId, PecaInsumoId, Quantidade)`, com `UNIQUE (LojaId, PecaInsumoId)` e `CHECK (Quantidade >= 0)`.
- O uso de peça numa OS baixa o saldo **da loja da OS**; o reabastecimento é por loja (`POST /api/lojas/{id}/estoque/{pecaId}/reabastecer`).
- **Sem Centro de Distribuição** nesta fase: transferências entre CD e lojas trariam pedidos, trânsito e conciliação, fora do escopo do desafio.

## Consequências

- Atendentes só consultam e repõem o estoque da própria loja.
- O CD pode entrar depois como mais uma "loja" do tipo depósito, com um fluxo de transferência, sem mudar o modelo de saldo.
- A regra "saldo nunca negativo" fica no domínio (`EstoqueLoja.Baixar`) e também no banco (`CHECK`).
