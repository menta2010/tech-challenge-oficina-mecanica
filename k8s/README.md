# Kubernetes — Oficina Mecânica (Fase 2)

Manifestos para subir a API .NET e o PostgreSQL em um cluster Kubernetes
(local com **kind**/**minikube**, ou gerenciado). Recursos criados no namespace `oficina`.

## Conteúdo

| Arquivo | Recurso | Função |
|---|---|---|
| `00-namespace.yaml` | Namespace | Isola os recursos em `oficina` |
| `01-postgres-secret.yaml` | Secret | Usuário/senha/database do Postgres |
| `02-postgres-pvc.yaml` | PersistentVolumeClaim | Persistência dos dados do banco |
| `03-postgres-deployment.yaml` | Deployment | Container do PostgreSQL 16 |
| `04-postgres-service.yaml` | Service (ClusterIP) | DNS interno `oficina-postgres:5432` |
| `05-api-configmap.yaml` | ConfigMap | Configuração **não sensível** da API |
| `06-api-secret.yaml` | Secret | Connection string, chave JWT, token do webhook, senha do admin |
| `07-api-deployment.yaml` | Deployment | API .NET (2 réplicas, probes em `/health`) |
| `08-api-service.yaml` | Service (NodePort) | Expõe a API na porta `30080` do nó |
| `09-api-hpa.yaml` | HorizontalPodAutoscaler | Escala a API por CPU (2→5 réplicas) |

## Pré-requisitos

- Cluster ativo. Para local:
  - **kind:** `kind create cluster --name oficina` (ou use o Terraform em `/infra`).
  - **minikube:** `minikube start`.
- **metrics-server** (necessário para o HPA). O script da raiz automatiza a instalação e a
  adaptação para clusters locais:
  ```bash
  scripts/demo-hpa.sh prepare
  ```
- Imagem da API acessível pelo cluster. Em produção o CI publica em
  `ghcr.io/menta2010/tech-challenge-oficina-mecanica:latest`. Para testar local com kind:
  ```bash
  docker build -t ghcr.io/menta2010/tech-challenge-oficina-mecanica:latest .
  kind load docker-image ghcr.io/menta2010/tech-challenge-oficina-mecanica:latest --name oficina
  ```

## Deploy

```bash
kubectl apply -f k8s/                       # aplica todos os manifestos
kubectl -n oficina rollout status deploy/oficina-postgres
kubectl -n oficina rollout status deploy/oficina-api
```

## Acesso à API

```bash
# minikube
minikube service oficina-api -n oficina --url
# kind / genérico (port-forward)
kubectl -n oficina port-forward svc/oficina-api 8080:80
# depois: http://localhost:8080/swagger
# liveness: http://localhost:8080/health/live
# readiness (inclui PostgreSQL): http://localhost:8080/health/ready
```

## Verificação

```bash
kubectl -n oficina get pods,svc,hpa
kubectl -n oficina logs deploy/oficina-api
```

## Demonstrar escalabilidade automática

O roteiro abaixo prepara métricas, mostra o estado inicial, inicia carga controlada e acompanha
o aumento de réplicas. Rode os comandos a partir da raiz do repositório e somente em um cluster
local/de avaliação.

```bash
scripts/demo-hpa.sh prepare
scripts/demo-hpa.sh status
scripts/demo-hpa.sh start
scripts/demo-hpa.sh watch
```

Durante `watch`, aguarde o HPA aumentar o Deployment de 2 para até 5 pods. Encerre a observação
com `Ctrl+C` e remova o gerador de carga:

```bash
scripts/demo-hpa.sh stop
scripts/demo-hpa.sh status
```

Por padrão são usados 20 workers. Se a máquina tiver poucos recursos, reduza a carga; se o
limite de CPU não for atingido, aumente-a:

```bash
LOAD_WORKERS=10 scripts/demo-hpa.sh start
LOAD_WORKERS=40 scripts/demo-hpa.sh start
```

O gerador envia tentativas inválidas de login para exercitar o hash de senha. Nenhuma senha
válida ou token é armazenado pelo script.

## Observações de segurança

- Os `Secret`s versionados contêm apenas **placeholders** (`CHANGE-ME`) para avaliação.
  Em produção, gere-os fora do Git (`kubectl create secret`, SealedSecrets ou Vault).
- A API roda como usuário **não-root** (imagem chiselada) e consome as credenciais
  exclusivamente via `Secret`/`ConfigMap` (nenhum segredo embutido na imagem).
