# ADR-001 — Padrão de comunicação

**Status:** Aceito · **Data:** 20/10/2026

## Contexto

Clientes, funcionários e o sistema externo de aprovação de orçamento conversam com a oficina. Algumas interações precisam de resposta imediata (abrir OS, consultar status, autenticar); outras são efeitos colaterais que não devem atrasar nem derrubar a requisição (avisar o cliente de que a OS mudou de status).

## Decisão

- **Síncrono, REST/JSON sobre HTTPS**, sempre entrando pelo **API Gateway**: autenticação (Lambda) e API de ordens de serviço (EKS). O contrato está no Swagger e na collection Postman.
- **Assíncrono, por eventos** no **Amazon SNS** para notificações: a API publica `StatusOSAlterado` e segue; o SNS entrega aos assinantes (e-mail hoje; SMS ou outra Lambda no futuro).
- O **webhook** de aprovação do orçamento continua síncrono (REST), autenticado por token de serviço (`X-Webhook-Token`) guardado no Secrets Manager.
- Toda requisição carrega `x-correlation-id`, devolvido na resposta e gravado em todos os logs.

## Consequências

- Uma falha no SNS não desfaz a mudança de status: o erro vai para o log e para o alerta do New Relic.
- Novos canais de notificação entram como novos assinantes do tópico, sem mudar a API.
- A notificação é *at-least-once*: o consumidor deve tolerar mensagens duplicadas.
