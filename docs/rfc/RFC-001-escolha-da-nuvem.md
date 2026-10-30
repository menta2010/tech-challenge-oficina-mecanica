# RFC-001 — Escolha da nuvem

| | |
| --- | --- |
| **Status** | Aceita |
| **Data** | 20/10/2026 |
| **Autores** | Grupo 60 |

## Contexto

A Fase 3 exige API Gateway, function serverless, banco gerenciado, Kubernetes com escalabilidade e Terraform, com livre escolha de nuvem. Até a Fase 2 tudo rodava em um cluster `kind` local, criado no próprio runner do GitHub Actions.

## Opções avaliadas

| Critério | AWS | Azure | GCP |
| --- | --- | --- | --- |
| API Gateway + Function | API Gateway HTTP API + Lambda (`dotnet8` nativo) | API Management + Azure Functions | API Gateway + Cloud Functions |
| Kubernetes gerenciado | EKS (plano de controle pago por hora) | AKS (plano de controle gratuito no tier Free) | GKE (1 cluster zonal gratuito) |
| Banco gerenciado | RDS PostgreSQL (free tier `db.t4g.micro`) | Azure Database for PostgreSQL | Cloud SQL |
| Conteúdo da fase | Serverless, Lambda, Cognito, API Gateway, Datadog/New Relic em AWS | API Management (APIM) | — |
| Conta para o grupo | AWS Academy (créditos) + free tier | Créditos de estudante | Créditos de avaliação |
| Experiência do grupo | Maior | Média | Baixa |

## Proposta

Usar a **AWS**: o conteúdo da disciplina de Desenvolvimento Serverless é todo em AWS (Lambda, API Gateway, ECS/EKS, Cognito), o grupo tem mais experiência e há créditos do AWS Academy. Os repositórios de Terraform aceitam roles existentes (`LabRole`) para funcionar no AWS Academy, que não permite criar IAM.

## Consequências

- Custo do EKS por hora: o ambiente fica ligado só durante desenvolvimento e demonstração, e é destruído com `terraform destroy` (ordem inversa do deploy).
- Lock-in moderado: Lambda e API Gateway são específicos da AWS. A API principal continua portátil (container + Kubernetes + PostgreSQL).
- Decisões derivadas: [RFC-002](RFC-002-banco-de-dados.md) (banco) e [RFC-003](RFC-003-estrategia-de-autenticacao.md) (autenticação).
