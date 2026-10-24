#!/usr/bin/env bash
# Cria no New Relic, pela API NerdGraph:
#   - o dashboard "Oficina - Operacao (<ambiente>)" (dashboard-oficina.json)
#   - o monitor sintetico de health check da API publicada
#   - a politica de alertas "Oficina - <ambiente>" com as condicoes de alertas.json
#
# Pre-requisitos: curl e jq.
# Uso:
#   export NEW_RELIC_API_KEY=NRAK-...          # User key (Profile -> API keys), nao a License key
#   export NEW_RELIC_ACCOUNT_ID=1234567
#   ./docs/newrelic/importar.sh homolog https://xxxx.execute-api.us-east-1.amazonaws.com
#
# Depois, no New Relic: Alerts -> Workflows -> ligar a politica a um destino de e-mail/Slack.
set -euo pipefail

AMBIENTE="${1:?informe o ambiente: homolog ou prod}"
API_URL="${2:-}"
: "${NEW_RELIC_API_KEY:?defina NEW_RELIC_API_KEY (User key)}"
: "${NEW_RELIC_ACCOUNT_ID:?defina NEW_RELIC_ACCOUNT_ID}"
ENDPOINT="${NEW_RELIC_GRAPHQL:-https://api.newrelic.com/graphql}"
DIR="$(cd "$(dirname "$0")" && pwd)"

# Envia uma mutation com variaveis e falha se a resposta trouxer erros
nerdgraph() {
  local corpo="$1"
  local resposta
  resposta=$(curl -sS "$ENDPOINT" -H "Content-Type: application/json" -H "API-Key: ${NEW_RELIC_API_KEY}" -d "$corpo")
  if jq -e '(.errors // []) | length > 0' <<<"$resposta" >/dev/null \
     || jq -e '[.. | objects | select(has("errors")) | .errors // [] | select(length > 0)] | length > 0' <<<"$resposta" >/dev/null; then
    echo "Erro do NerdGraph: $resposta" >&2
    exit 1
  fi
  echo "$resposta"
}

# --- Dashboard ----------------------------------------------------------------
dashboard=$(sed "s/__AMBIENTE__/${AMBIENTE}/g" "$DIR/dashboard-oficina.json" \
  | jq --argjson acc "$NEW_RELIC_ACCOUNT_ID" '(.. | objects | select(has("accountIds")) | .accountIds) = [$acc]')

corpo=$(jq -n --argjson acc "$NEW_RELIC_ACCOUNT_ID" --argjson d "$dashboard" '{
  query: "mutation($acc: Int!, $d: DashboardInput!) { dashboardCreate(accountId: $acc, dashboard: $d) { entityResult { guid name } errors { description type } } }",
  variables: { acc: $acc, d: $d }
}')
guid=$(nerdgraph "$corpo" | jq -r '.data.dashboardCreate.entityResult.guid')
echo "Dashboard criado: $guid"

# --- Monitor sintetico (health check / uptime) -------------------------------
if [[ -n "$API_URL" ]]; then
  corpo=$(jq -n --argjson acc "$NEW_RELIC_ACCOUNT_ID" --arg nome "oficina-health-${AMBIENTE}" --arg url "${API_URL}/health/ready" '{
    query: "mutation($acc: Int!, $m: SyntheticsCreateSimpleMonitorInput!) { syntheticsCreateSimpleMonitor(accountId: $acc, monitor: $m) { monitor { guid name } errors { description type } } }",
    variables: { acc: $acc, m: { name: $nome, uri: $url, period: "EVERY_5_MINUTES", status: "ENABLED", locations: { public: ["US_EAST_1", "SA_EAST_1"] } } }
  }')
  nerdgraph "$corpo" | jq -r '"Monitor sintetico: " + .data.syntheticsCreateSimpleMonitor.monitor.name'
else
  echo "API_URL nao informada: monitor sintetico nao criado"
fi

# --- Politica e condicoes de alerta ------------------------------------------
corpo=$(jq -n --argjson acc "$NEW_RELIC_ACCOUNT_ID" --arg nome "Oficina - ${AMBIENTE}" '{
  query: "mutation($acc: Int!, $p: AlertsPolicyInput!) { alertsPolicyCreate(accountId: $acc, policy: $p) { id name } }",
  variables: { acc: $acc, p: { name: $nome, incidentPreference: "PER_CONDITION" } }
}')
politica=$(nerdgraph "$corpo" | jq -r '.data.alertsPolicyCreate.id')
echo "Politica de alertas: $politica"

sed "s/__AMBIENTE__/${AMBIENTE}/g" "$DIR/alertas.json" | jq -c '.[]' | while read -r alerta; do
  corpo=$(jq -n --argjson acc "$NEW_RELIC_ACCOUNT_ID" --arg pol "$politica" --argjson a "$alerta" '{
    query: "mutation($acc: Int!, $pol: ID!, $c: AlertsNrqlConditionStaticInput!) { alertsNrqlConditionStaticCreate(accountId: $acc, policyId: $pol, condition: $c) { id name } }",
    variables: { acc: $acc, pol: $pol, c: {
      name: $a.name,
      enabled: true,
      nrql: { query: $a.query },
      signal: { aggregationWindow: 60, aggregationMethod: "EVENT_FLOW", aggregationDelay: 120 },
      terms: [ { threshold: $a.threshold, thresholdDuration: $a.duracao, thresholdOccurrences: "AT_LEAST_ONCE", operator: $a.operator, priority: $a.prioridade } ],
      violationTimeLimitSeconds: 86400
    } }
  }')
  nerdgraph "$corpo" | jq -r '"  condicao: " + .data.alertsNrqlConditionStaticCreate.name'
done

echo "Pronto. Falta ligar a politica \"Oficina - ${AMBIENTE}\" a um destino (Alerts -> Workflows)."
