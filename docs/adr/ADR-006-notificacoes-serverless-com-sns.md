# ADR-006 — Notificações serverless com SNS

**Status:** Aceito · **Data:** 21/10/2026

## Contexto

Desde a Fase 2 o cliente devia ser avisado a cada mudança de status da OS, mas o "e-mail" era só uma linha de log. A Fase 3 pede soluções **serverless para notificações**.

## Opções

| Opção | Prós | Contras |
| --- | --- | --- |
| SMTP direto na API | Simples | Credencial na API, envio síncrono, sem retentativa |
| Amazon SES chamado pela API | E-mail real e barato | Acopla a API ao canal; SES em sandbox só envia para e-mails verificados |
| **Amazon SNS (tópico)** | Pub/sub gerenciado, vários canais, retentativas | Assinatura de e-mail é por tópico (não por cliente) |
| SQS + Lambda + SES | Fila, retentativa, e-mail por cliente | Mais peças para a fase |

## Decisão

A API publica o evento `StatusOSAlterado` (OS, loja, cliente, e-mail do cliente, novo status, horário) no tópico **`oficina-<amb>-notificacoes-os`**, com atributos `status` e `lojaId` para filtros. Nesta fase o tópico tem uma assinatura de **e-mail** da equipe (demonstração). Localmente, sem tópico configurado, o notificador só registra em log.

## Consequências

- A API não conhece os canais; adicionar SMS ou uma Lambda que envia e-mail para o cliente (SNS → SQS → Lambda → SES) não muda o código da API. Essa é a evolução prevista.
- Falha de publicação não falha a requisição: é logada como erro e gera alerta.
- Permissão `sns:Publish` restrita ao tópico, concedida à role dos nós (ver ADR-007 sobre IRSA).
