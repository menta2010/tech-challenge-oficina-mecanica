# Kubernetes — oficina-api (Fase 3, Amazon EKS)

Manifests aplicados pelo pipeline (`.github/workflows/ci-cd.yml`) no cluster `oficina-<ambiente>-eks`. Valores `${...}` são preenchidos com `envsubst` no deploy.

| Arquivo | Recurso | Função |
| --- | --- | --- |
| `00-namespace.yaml` | Namespace | Isola os recursos em `oficina` |
| `01-configmap.yaml` | ConfigMap | Configuração não sensível, tópico SNS e variáveis do agente New Relic |
| `02-migracao-job.yaml` | Job | Aplica migrations e seed uma vez, antes do deploy (`--migrar`) |
| `03-deployment.yaml` | Deployment | API com 2 réplicas, rolling update `maxUnavailable: 0`, startup/readiness/liveness probes, requests/limits, espalhamento por zona |
| `04-service.yaml` | Service (NodePort 30080) | Alvo do NLB interno criado pelo `oficina-infra-k8s` |
| `05-hpa.yaml` | HPA + PodDisruptionBudget | 2–8 pods por CPU (70%) e memória (80%); mínimo de 1 réplica em manutenção |

O Secret `oficina-api-secret` **não é versionado**: o pipeline o cria a partir do AWS Secrets Manager (`oficina/<amb>/db`, `jwt`, `webhook`) e dos secrets do GitHub.

## Aplicar manualmente

```bash
aws eks update-kubeconfig --name oficina-homolog-eks --region us-east-1
export IMAGEM=<ecr>/oficina-homolog-api:<tag> VERSAO=<tag> AMBIENTE=homolog AWS_REGION=us-east-1 SNS_TOPIC_ARN=<arn>
VARS='${IMAGEM} ${VERSAO} ${AMBIENTE} ${SNS_TOPIC_ARN} ${AWS_REGION}'
kubectl apply -f 00-namespace.yaml
for f in 01-configmap.yaml 02-migracao-job.yaml; do envsubst "$VARS" < $f | kubectl apply -f -; done
kubectl -n oficina wait --for=condition=complete job/oficina-migracao-$VERSAO --timeout=300s
for f in 03-deployment.yaml 04-service.yaml 05-hpa.yaml; do envsubst "$VARS" < $f | kubectl apply -f -; done
kubectl -n oficina rollout status deploy/oficina-api
```

## Acompanhar o autoscaling

```bash
kubectl -n oficina get hpa,pods -w
kubectl get nodes -w
```
