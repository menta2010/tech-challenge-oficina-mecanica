# ADR-004 — Cliente não pertence a uma loja

**Status:** Aceito · **Data:** 21/10/2026

## Contexto

Com a criação de `Lojas`, avaliamos colocar `LojaId` também em `Clientes`.

## Decisão

**Não vincular** cliente a loja. O cliente é da rede: pode levar o carro em qualquer unidade e se autentica pelo CPF independentemente de loja. A relação cliente ↔ loja aparece pelas ordens de serviço (`OrdensServico.ClienteId` + `OrdensServico.LojaId`).

Ordens de serviço, funcionários (atendentes) e estoque, sim, pertencem a uma loja.

## Consequências

- "Clientes atendidos pela loja X" é uma consulta sobre as OS, não um atributo fixo, e continua correta se o cliente trocar de unidade.
- Sem cadastro duplicado do mesmo CPF em várias lojas (o CPF continua único na rede).
- O atendente consegue cadastrar e consultar clientes, mas só opera OS da própria loja.
