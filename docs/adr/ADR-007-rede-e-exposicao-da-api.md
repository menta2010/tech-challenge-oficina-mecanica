# ADR-007 — Rede e exposição da API

**Status:** Aceito · **Data:** 22/10/2026

## Contexto

A API precisa ser alcançada pela internet através do API Gateway, mas o banco e os nós do cluster não podem ficar expostos. O ambiente também precisa caber em créditos limitados (AWS Academy/free tier).

## Decisão

- **VPC** com 2 zonas, subnets públicas (só NAT Gateway) e privadas (nós do EKS, NLB, RDS e Lambdas).
- Caminho de entrada: **API Gateway → VPC Link → NLB interno → NodePort 30080 → pods**. Nenhum IP público na API.
- O NLB e o target group são criados pelo Terraform (repo de infra), e o target group acompanha o autoscaling do node group. Assim o API Gateway não depende de um balanceador criado depois pelo Kubernetes.
- **Um único NAT Gateway** para reduzir custo (aceita perda de saída para a internet se a AZ dele cair).
- Permissões AWS dos pods (publicar no SNS) pela **role dos nós**, restrita ao ARN do tópico.

## Alternativas registradas

| Alternativa | Por que não agora |
| --- | --- |
| AWS Load Balancer Controller + Ingress/ALB | Mais um controlador e IRSA; o ALB só existiria depois do deploy do app, invertendo a ordem dos repositórios |
| IRSA (role por service account) | Melhor isolamento, mas exige provedor OIDC e criação de roles, indisponível no AWS Academy. É a evolução recomendada fora dele |
| NAT por AZ | Dobra o custo fixo de rede |

## Consequências

- Tudo que é sensível fica em subnet privada; o único ponto público é o API Gateway, com throttling e logs.
- Trocar para IRSA ou ALB no futuro não muda a API, só a infraestrutura.
