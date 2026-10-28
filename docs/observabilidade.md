# Observabilidade — New Relic

A solução é monitorada no **New Relic** (escolha livre do enunciado; ver justificativa no fim). Cada fonte de telemetria e o que ela cobre:

| Fonte | Como chega ao New Relic | Cobre |
| --- | --- | --- |
| Agente APM .NET nos pods da API | Pacote `NewRelic.Agent` na imagem, ativado pelas variáveis `CORECLR_*`/`NEW_RELIC_*` do ConfigMap | Latência e erros por endpoint, traces distribuídos, chamadas ao banco e ao SNS |
| Logs da API (Serilog JSON) | Encaminhados pelo agente (*logs in context*), com as propriedades do log como `context.*` | Logs estruturados ligados ao trace, `CorrelationId`, `OrdemServicoId`, `LojaId` |
| Eventos de negócio `OrdemServicoEvento` | `NewRelic.RecordCustomEvent` na API | Volume de OS, tempo por status, falhas no processamento |
| Integração Kubernetes (`nri-bundle`) | Helm no repositório `oficina-infra-k8s` | CPU e memória de nós, pods e containers; réplicas do HPA; readiness; reinícios; eventos do cluster |
| Lambda, API Gateway e RDS | Integração AWS do New Relic (CloudWatch metric streams + envio de logs do CloudWatch) | Invocações, erros e duração da Lambda; latência e 4xx/5xx do gateway; CPU e conexões do RDS; logs JSON da Lambda e de acesso do gateway |
| Monitor sintético | New Relic Synthetics (ping) | Health check e uptime vistos de fora, pela URL pública |

## Correlação entre requisições

1. O cliente pode enviar `x-correlation-id`. Se não enviar, a Lambda usa o `requestId` do API Gateway e a API gera um GUID.
2. A API devolve o mesmo valor no header da resposta, coloca em **todas** as linhas de log da requisição (`CorrelationId`) e no atributo `correlationId` da transação APM.
3. O log de acesso do API Gateway também registra `correlationId` e `requestId`.

Para seguir uma requisição de ponta a ponta:

```sql
SELECT timestamp, service, level, message FROM Log
WHERE context.CorrelationId = 'demo-001' OR correlationId = 'demo-001'
SINCE 1 hour ago ORDER BY timestamp
```

Formato de um log da API (stdout, JSON compacto do Serilog):

```json
{"@t":"2026-10-23T16:08:14.04Z","@m":"HTTP \"GET\" \"/api/lojas\" respondeu 200 em 54.6 ms","@tr":"6a7cf411...","@sp":"8795322a...","StatusCode":200,"Elapsed":54.6,"CorrelationId":"teste-local-1","service":"oficina-api","environment":"Production"}
```

## Dashboard "Oficina — Operação"

Cada widget e a consulta NRQL correspondente (troque `homolog` por `prod` conforme o ambiente).

### Negócio (exigidos pelo enunciado)

**Volume diário de ordens de serviço** (barras por dia, por loja)

```sql
SELECT count(*) FROM OrdemServicoEvento
WHERE evento = 'criada' AND appName = 'oficina-api-homolog'
FACET lojaId SINCE 30 days ago TIMESERIES 1 day
```

**Tempo médio de execução por status** (Diagnóstico, Execução, Finalização)

```sql
SELECT average(minutosNoStatusAnterior) AS 'minutos' FROM OrdemServicoEvento
WHERE evento = 'status_alterado' AND statusAnterior IN ('EmDiagnostico', 'EmExecucao', 'Finalizada')
FACET statusAnterior SINCE 7 days ago
```

`EmDiagnostico` = diagnóstico, `EmExecucao` = execução, `Finalizada` = finalização (até a entrega). A mesma informação existe na API em `GET /api/ordens-servico/relatorios/tempo-por-status`, calculada a partir de `HistoricoStatusOS`.

**Erros e falhas nas integrações**

```sql
-- Falhas inesperadas no processamento de OS, por operação
SELECT count(*) FROM OrdemServicoEvento
WHERE evento = 'falha' AND inesperada IS true
FACET operacao, erro SINCE 1 day ago TIMESERIES

-- Integração com SNS (notificações)
SELECT count(*) FROM Log
WHERE message LIKE 'Falha ao publicar notificacao%' SINCE 1 day ago TIMESERIES

-- Erros de banco, HTTP externo e demais exceções capturadas pelo APM
SELECT count(*) FROM TransactionError
WHERE appName = 'oficina-api-homolog' FACET error.class SINCE 1 day ago
```

### APIs

**Latência das APIs (p50, p95, p99 por endpoint)**

```sql
SELECT percentile(duration * 1000, 50, 95, 99) AS 'ms' FROM Transaction
WHERE appName = 'oficina-api-homolog' FACET name SINCE 1 hour ago TIMESERIES
```

**Latência vista pelo cliente (API Gateway, inclui a Lambda)**

```sql
SELECT average(aws.apigateway.Latency), max(aws.apigateway.Latency) FROM Metric
WHERE aws.apigateway.ApiName = 'oficina-homolog-api' SINCE 1 hour ago TIMESERIES
```

**Taxa de erro e 429 do rate limit**

```sql
SELECT percentage(count(*), WHERE error IS true) AS 'erro %' FROM Transaction
WHERE appName = 'oficina-api-homolog' SINCE 1 hour ago TIMESERIES

SELECT sum(aws.apigateway.4xx), sum(aws.apigateway.5xx) FROM Metric
WHERE aws.apigateway.ApiName = 'oficina-homolog-api' SINCE 1 hour ago TIMESERIES
```

### Kubernetes

**CPU e memória dos pods da API**

```sql
SELECT average(cpuUsedCores) AS 'CPU (cores)', average(memoryWorkingSetBytes) / 1e6 AS 'Memória (MB)'
FROM K8sContainerSample
WHERE clusterName = 'oficina-homolog-eks' AND namespaceName = 'oficina' AND containerName = 'api'
FACET podName SINCE 1 hour ago TIMESERIES
```

**Réplicas do HPA e nós do cluster** (mostra o autoscaling durante o teste de carga)

```sql
SELECT latest(currentReplicas) AS 'atuais', latest(desiredReplicas) AS 'desejadas' FROM K8sHpaSample
WHERE clusterName = 'oficina-homolog-eks' AND namespaceName = 'oficina' SINCE 1 hour ago TIMESERIES

SELECT uniqueCount(nodeName) AS 'nós' FROM K8sNodeSample
WHERE clusterName = 'oficina-homolog-eks' SINCE 1 hour ago TIMESERIES
```

### Healthchecks e uptime

```sql
-- Uptime externo (monitor sintético na URL pública /health/ready, a cada 5 min)
SELECT percentage(count(*), WHERE result = 'SUCCESS') AS 'uptime %' FROM SyntheticCheck
WHERE monitorName = 'oficina-health-homolog' SINCE 7 days ago

-- Pods prontos (readiness probe) e reinícios
SELECT latest(isReady) FROM K8sPodSample
WHERE namespaceName = 'oficina' FACET podName SINCE 30 minutes ago
SELECT sum(restartCountDelta) FROM K8sContainerSample
WHERE namespaceName = 'oficina' FACET podName SINCE 1 day ago TIMESERIES
```

## Alertas

Política **"Oficina — homolog/prod"**, com notificação por e-mail (workflow do New Relic) e, opcionalmente, Slack.

| Alerta | Condição (NRQL) | Limite | Severidade |
| --- | --- | --- | --- |
| **Falha no processamento de OS** | `SELECT count(*) FROM OrdemServicoEvento WHERE evento = 'falha' AND inesperada IS true` | > 0 em 5 min | Crítico |
| Notificação não publicada | `SELECT count(*) FROM Log WHERE message LIKE 'Falha ao publicar notificacao%'` | > 0 em 5 min | Alerta |
| Taxa de erro da API | `SELECT percentage(count(*), WHERE error IS true) FROM Transaction WHERE appName = 'oficina-api-<amb>'` | > 5% por 5 min | Crítico |
| Latência alta | `SELECT percentile(duration, 95) FROM Transaction WHERE appName = 'oficina-api-<amb>'` | > 1 s por 5 min | Alerta |
| API fora do ar | Monitor sintético `oficina-health-<amb>` | 2 falhas seguidas | Crítico |
| Pods reiniciando | `SELECT sum(restartCountDelta) FROM K8sContainerSample WHERE namespaceName = 'oficina'` | > 2 em 10 min | Alerta |
| HPA no teto | `SELECT latest(currentReplicas) FROM K8sHpaSample WHERE namespaceName = 'oficina'` | = 8 por 10 min | Alerta |
| Erros na Lambda de autenticação | `SELECT sum(aws.lambda.Errors) FROM Metric WHERE aws.lambda.FunctionName LIKE 'oficina-%-auth-cpf'` | > 0 em 5 min | Crítico |

As regras de negócio violadas (transição de status inválida, estoque insuficiente) **não** disparam o alerta de falha: elas geram o evento com `inesperada = false` e um log *warning*, pois são respostas esperadas (4xx).

## Configuração (uma vez por ambiente)

1. Criar a conta New Relic (free tier: 100 GB/mês de ingestão) e copiar a **License key (ingest)**.
2. Cadastrar o secret `NEW_RELIC_LICENSE_KEY` nos GitHub Environments dos repositórios `oficina-infra-k8s` (integração Kubernetes) e `oficina-app` (agente APM).
3. Em **Infrastructure → AWS**, conectar a conta AWS via CloudWatch metric streams e ativar o envio de logs dos grupos `/aws/lambda/oficina-*` e `/aws/apigateway/oficina-*`.
4. Criar **dashboard, monitor sintético e alertas de uma vez** com o script (precisa de `curl` e `jq`; a *User key* fica em Profile → API keys):

   ```bash
   export NEW_RELIC_API_KEY=NRAK-...
   export NEW_RELIC_ACCOUNT_ID=<id da conta>
   ./docs/newrelic/importar.sh homolog <api_endpoint>
   ```

   O script lê [dashboard-oficina.json](newrelic/dashboard-oficina.json) (3 páginas e 18 widgets com as consultas acima) e [alertas.json](newrelic/alertas.json) (7 condições). Para o dashboard sem script: **Dashboards → Import dashboard**, colando o JSON depois de trocar `__AMBIENTE__` e o `0` de `accountIds`.
5. Em **Alerts → Workflows**, ligar a política `Oficina - <ambiente>` a um destino de e-mail (ou Slack).

## Por que New Relic

O free tier cobre o projeto inteiro (APM, logs, infraestrutura, Kubernetes, sintéticos, dashboards e alertas) sem prazo de expiração; o trial do Datadog acaba em 14 dias, antes do fim da avaliação. Os dois cobrem os requisitos; a escolha foi por custo e prazo.
