# Architecture Decision Records

Decisões arquiteturais permanentes do projeto. Cada ADR registra o contexto, a decisão e as consequências; uma decisão só muda com um novo ADR que substitua o anterior.

| ADR | Decisão | Status |
| --- | --- | --- |
| [ADR-001](ADR-001-padrao-de-comunicacao.md) | REST síncrono pelo API Gateway + eventos assíncronos via SNS | Aceito |
| [ADR-002](ADR-002-escalabilidade-hpa-e-cluster-autoscaler.md) | HPA nos pods + Cluster Autoscaler nos nós + rate limit no gateway | Aceito |
| [ADR-003](ADR-003-estoque-por-loja.md) | Catálogo global de peças e saldo por loja, sem Centro de Distribuição | Aceito |
| [ADR-004](ADR-004-cliente-nao-pertence-a-loja.md) | Cliente não é vinculado a uma loja | Aceito |
| [ADR-005](ADR-005-politica-de-inativacao-de-clientes.md) | Inativação automática de clientes sem OS há 24 meses | Aceito |
| [ADR-006](ADR-006-notificacoes-serverless-com-sns.md) | Notificações de status pelo Amazon SNS | Aceito |
| [ADR-007](ADR-007-rede-e-exposicao-da-api.md) | API em subnets privadas, exposta só via API Gateway → VPC Link → NLB | Aceito |
| [ADR-008](ADR-008-exclusao-logica-e-tratamento-de-erros.md) | Exclusão lógica explícita (`Encerrada`) e erros em Problem Details | Aceito |
| [ADR-009](ADR-009-convencao-de-linguagem-no-codigo.md) | Domínio em português, termos técnicos em inglês | Aceito |
