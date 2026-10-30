# Entrega — Tech Challenge Fase 3 (Grupo 60)

Conteúdo-base do **PDF único** enviado no Portal do Aluno (prazo: 03/11/2026, 23h59) e roteiro do vídeo.

## 1. Documento PDF

**Integrantes:** Alan Carlos Jorge de Oliveira · Natalino E. Rodrigues · Stéfany de Oliveira Marques Moura

### Repositórios (todos com o usuário `soat-architecture` como colaborador)

| # | Repositório | Link | Colaborador adicionado |
| --- | --- | --- | --- |
| 1 | Lambda (autenticação serverless) | https://github.com/menta2010/oficina-lambda-auth | ☐ |
| 2 | Infraestrutura Kubernetes (Terraform) | https://github.com/menta2010/oficina-infra-k8s | ☐ |
| 3 | Infraestrutura do banco gerenciado (Terraform) | https://github.com/menta2010/oficina-infra-db | ☐ |
| 4 | Aplicação principal (Kubernetes) | https://github.com/menta2010/tech-challenge-oficina-mecanica | ☐ |

### Vídeo

`<link do YouTube ou Vimeo, público ou não listado, até 15 min>`

### Documentação

| Documento | Link |
| --- | --- |
| Arquitetura (componentes, sequência de autenticação e de abertura de OS) | https://github.com/menta2010/tech-challenge-oficina-mecanica/blob/master/docs/arquitetura.md |
| RFCs (nuvem, banco, autenticação) | https://github.com/menta2010/tech-challenge-oficina-mecanica/tree/master/docs/rfc |
| ADRs (9 decisões) | https://github.com/menta2010/tech-challenge-oficina-mecanica/tree/master/docs/adr |
| Banco: justificativa, diagrama ER e relacionamentos | https://github.com/menta2010/oficina-infra-db#modelo-relacional-fase-3 |
| Observabilidade: dashboards NRQL e alertas | https://github.com/menta2010/tech-challenge-oficina-mecanica/blob/master/docs/observabilidade.md |
| Swagger | https://7d2ypan7l9.execute-api.us-east-1.amazonaws.com/swagger |
| Collection Postman | https://github.com/menta2010/tech-challenge-oficina-mecanica/blob/master/docs/postman/oficina-fase3.postman_collection.json |

## 2. Roteiro do vídeo (até 15 min)

| Tempo | Cena | O que mostrar |
| --- | --- | --- |
| 0:00–1:30 | Arquitetura | Diagrama de componentes; os 4 repositórios e a ordem de deploy |
| 1:30–3:30 | CI/CD | Abrir um PR (checks rodando, merge bloqueado sem os checks); merge na branch principal; pipeline do `tech-challenge-oficina-mecanica` com testes, Trivy, push no ECR, Job de migração, rollout e smoke test |
| 3:30–4:30 | Deploy automatizado | `kubectl get pods,hpa -n oficina`, nova versão (label `versao`), RDS e Lambda no console |
| 4:30–6:30 | Autenticação com CPF | Postman: CPF inválido (400) → CPF válido (200 + token) → rota sem token (401) → abrir OS com o token do cliente → `/api/acompanhamento` |
| 6:30–8:30 | Consumo das APIs protegidas | Login do atendente; fluxo da OS até a entrega; listagem padrão × `incluirEncerradas=true` |
| 8:30–9:30 | Notificação | E-mail do SNS chegando a cada mudança de status |
| 9:30–12:30 | Dashboard ao vivo | New Relic: volume diário, tempo médio por status, latência p95, CPU/memória dos pods; rodar o k6 e mostrar HPA escalando e a latência no dashboard |
| 12:30–14:30 | Logs e traces | Trace distribuído de uma abertura de OS (API → PostgreSQL → SNS); busca de logs por `correlationId`; log JSON da Lambda; alertas configurados |
| 14:30–15:00 | Fechamento | Decisões principais (ADRs) e o que evoluiria |

**Dica:** grave cada cena separadamente e junte no final; deixe o ambiente já aplicado antes de gravar, porque subir o EKS do zero leva de 15 a 20 minutos.
