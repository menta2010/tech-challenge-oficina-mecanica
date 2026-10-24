# Arquitetura da solução — Fase 3

A oficina passou a operar como uma **rede de unidades** e a aplicação saiu de um cluster local para a **AWS**. Toda chamada entra pelo **API Gateway**: a autenticação é **serverless** (Lambda) e a API de ordens de serviço roda no **EKS**, com banco **RDS PostgreSQL**, notificações por **SNS** e observabilidade no **New Relic**.

| Documento | Conteúdo |
| --- | --- |
| Este arquivo | Diagrama de componentes e diagramas de sequência |
| [RFCs](rfc/) | Propostas técnicas discutidas pelo grupo: nuvem, banco e autenticação |
| [ADRs](adr/) | Decisões arquiteturais permanentes |
| [Observabilidade](observabilidade.md) | Logs, métricas, traces, dashboards (NRQL) e alertas |
| [Modelo de dados](https://github.com/menta2010/oficina-infra-db#modelo-relacional-fase-3) | Diagrama ER, justificativa do PostgreSQL e índices |

## Repositórios

| Repositório | Conteúdo | Deploy |
| --- | --- | --- |
| [oficina-infra-k8s](https://github.com/menta2010/oficina-infra-k8s) | VPC, EKS, NLB, API Gateway, segredos, SNS, ECR, New Relic | Terraform |
| [oficina-infra-db](https://github.com/menta2010/oficina-infra-db) | RDS PostgreSQL, security groups, segredo do banco | Terraform |
| [oficina-lambda-auth](https://github.com/menta2010/oficina-lambda-auth) | Lambdas de autenticação (CPF e funcionário) e job de inativação | .NET 8 + Terraform |
| [oficina-app](https://github.com/menta2010/tech-challenge-oficina-mecanica) (este) | API de ordens de serviço | Docker → ECR → EKS |

Todos usam GitHub Actions com `develop` → **homolog** e `main` → **prod**, branches protegidas e merge só por Pull Request.

## Diagrama de componentes

```mermaid
flowchart TB
    subgraph Usuarios["Usuários"]
        CLI([Cliente da oficina<br/>app / web])
        FUN([Funcionário<br/>admin · atendente])
        EXT([Sistema externo<br/>aprovação de orçamento])
    end

    subgraph AWS["AWS · us-east-1"]
        GW["Amazon API Gateway (HTTP API)<br/>throttling 50 req/s · 5 req/s em /auth<br/>CORS · logs de acesso JSON"]

        subgraph Serverless["Serverless"]
            LA["Lambda auth<br/>POST /auth/cpf · POST /auth/login<br/>emite JWT HS256"]
            LI["Lambda inativação<br/>diária (EventBridge)"]
            SNS[["Amazon SNS<br/>notificacoes-os"]]
        end

        SM[("Secrets Manager<br/>db · jwt · webhook")]

        subgraph VPC["VPC privada"]
            NLB["NLB interno"]
            subgraph EKS["Amazon EKS"]
                API["oficina-api (.NET 8)<br/>2 a 8 pods · HPA"]
                JOB["Job de migração<br/>(EF Core)"]
                PLAT["metrics-server · Cluster Autoscaler<br/>New Relic nri-bundle"]
            end
            RDS[("RDS PostgreSQL 16<br/>TLS · backups")]
        end
        ECR[("ECR")]
    end

    NR["New Relic<br/>APM · logs · infra · dashboards · alertas"]
    EMAIL([E-mail])

    CLI -- HTTPS --> GW
    FUN -- HTTPS --> GW
    EXT -- "HTTPS + X-Webhook-Token" --> GW
    GW -- "/auth/*" --> LA
    GW -- "demais rotas · VPC Link" --> NLB --> API
    LA -- "consulta cliente/usuário" --> RDS
    LI -- "inativa clientes sem OS" --> RDS
    API -- "EF Core / Npgsql" --> RDS
    JOB --> RDS
    API -- "status da OS" --> SNS --> EMAIL
    LA -. segredos .-> SM
    API -. "segredos (pipeline)" .-> SM
    EKS -. imagem .-> ECR
    API -. "APM · logs · eventos" .-> NR
    PLAT -. "CPU · memória · eventos K8s" .-> NR
    GW -. "logs/métricas (CloudWatch)" .-> NR
    LA -. "logs/métricas (CloudWatch)" .-> NR
```

**Leitura:** o cliente só enxerga o API Gateway. As rotas `/auth/*` vão para a Lambda; o resto atravessa o VPC Link até o NLB interno e os pods da API. Banco, NLB e nós ficam em subnets privadas. Os dois emissores de tráfego para o New Relic são o agente APM nos pods e a integração Kubernetes; Lambda, API Gateway e RDS chegam pelo CloudWatch.

## Sequência 1 — Autenticação por CPF

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant G as API Gateway
    participant L as Lambda auth
    participant S as Secrets Manager
    participant DB as RDS PostgreSQL

    C->>G: POST /auth/cpf {"cpf": "529.982.247-25"}
    Note over G: throttling: 5 req/s nesta rota
    G->>L: invoca (payload 2.0, requestId)
    opt cold start
        L->>S: lê oficina/<amb>/db e oficina/<amb>/jwt
    end
    L->>L: valida dígitos verificadores
    alt CPF inválido
        L-->>G: 400 {"erro": "CPF invalido."}
    else CPF válido
        L->>DB: SELECT Id, Nome, Ativo FROM Clientes WHERE Documento = $1
        alt não encontrado
            L-->>G: 404
        else cliente inativo
            L-->>G: 403 "Cliente inativo."
        else cliente ativo
            L->>L: JWT HS256 (sub = id do cliente, role = Cliente, cpf, exp 60 min)
            L-->>G: 200 {token, expiraEm, cliente}
        end
    end
    G-->>C: resposta + x-correlation-id
    Note over L: log JSON com correlationId e CPF mascarado (***-25)
```

## Sequência 2 — Abertura de ordem de serviço

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente (token de CPF)
    participant G as API Gateway
    participant N as NLB interno
    participant A as oficina-api (EKS)
    participant DB as RDS PostgreSQL
    participant T as SNS
    participant R as New Relic

    C->>G: POST /api/ordens-servico<br/>Authorization: Bearer JWT · x-correlation-id
    G->>N: VPC Link
    N->>A: NodePort 30080 → pod
    A->>A: valida JWT (issuer, audience, assinatura, expiração)
    A->>A: perfil Cliente → ClienteId vem do token (não do corpo)
    A->>DB: carrega loja (ativa), cliente e veículo (do próprio cliente)
    alt regra violada
        A-->>C: 400/403/404/409 Problem Details + correlationId
    else válido
        A->>A: new OrdemServico → status Recebida + 1º período no histórico
        A->>DB: INSERT OrdensServico + HistoricoStatusOS (mesma transação)
        A--)R: evento OrdemServicoEvento "criada" + log JSON
        A--)T: Publish StatusOSAlterado (Recebida)
        T--)C: e-mail "sua OS mudou para Recebida"
        A-->>G: 201 Created + Location + corpo da OS
        G-->>C: 201
    end
```

A partir daí, cada transição feita por um funcionário (diagnóstico, orçamento, execução, finalização, entrega) fecha o período do status anterior, registra a duração no New Relic e publica a notificação. Esses eventos alimentam os dashboards de tempo médio por status.
