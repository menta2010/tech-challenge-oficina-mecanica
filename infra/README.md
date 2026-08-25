# Infraestrutura (Terraform) — Oficina Mecânica (Fase 2)

Terraform que **provisiona a infraestrutura**: um cluster Kubernetes local (**kind**)
e o **banco de dados PostgreSQL** dentro dele. A API é publicada por cima via os
manifestos de `/k8s` (ou pelo pipeline de CI/CD).

## O que é criado

- `kind_cluster.this` — cluster Kubernetes local (1 nó control-plane) com o NodePort da API mapeado para o host.
- `kubernetes_namespace.oficina` — namespace `oficina`.
- **Banco de dados** — `Secret`, `PersistentVolumeClaim`, `Deployment` e `Service` do PostgreSQL 16.

Saídas (`outputs.tf`): nome do cluster, caminho do kubeconfig, namespace e o DNS interno do banco.

## Pré-requisitos

- [Terraform](https://developer.hashicorp.com/terraform/downloads) >= 1.5
- [Docker](https://www.docker.com/) em execução (o kind cria o cluster em contêineres)
- [kind](https://kind.sigs.k8s.io/) e [kubectl](https://kubernetes.io/docs/tasks/tools/)

## Uso

```bash
cd infra
cp terraform.tfvars.example terraform.tfvars   # ajuste as variáveis
export TF_VAR_db_password="uma-senha-forte"     # não deixe a senha no arquivo

terraform init
terraform plan
terraform apply
```

Depois de aplicar, publique a API e o HPA sobre o cluster provisionado:

```bash
kubectl apply -f ../k8s/00-namespace.yaml
kubectl apply -f ../k8s/05-api-configmap.yaml
kubectl apply -f ../k8s/06-api-secret.yaml
kubectl apply -f ../k8s/07-api-deployment.yaml
kubectl apply -f ../k8s/08-api-service.yaml
kubectl apply -f ../k8s/09-api-hpa.yaml
```

> O banco já é provisionado pelo Terraform; por isso, ao usar este caminho, não é
> necessário reaplicar os manifestos `01`–`04` de `/k8s` (eles existem para o cenário
> **sem** Terraform, em que tudo sobe via `kubectl apply -f k8s/`).

## Destruir

```bash
terraform destroy
```

## Variáveis principais

| Variável | Default | Descrição |
|---|---|---|
| `cluster_name` | `oficina` | Nome do cluster kind |
| `namespace` | `oficina` | Namespace dos recursos |
| `api_node_port` | `30080` | NodePort exposto no host para a API |
| `db_name` / `db_user` | `oficina` | Banco e usuário |
| `db_password` | placeholder | **Sobrescreva** via `TF_VAR_db_password` |

## Nota sobre nuvem

O mesmo desenho vale para um cluster gerenciado (EKS/GKE/AKS): trocando o provider
`kind` por um módulo de cluster gerenciado e, opcionalmente, o banco por um serviço
gerenciado (RDS/Cloud SQL), o restante (namespace, Secret, Service) permanece igual.
