#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="${NAMESPACE:-oficina}"
HPA_NAME="${HPA_NAME:-oficina-api-hpa}"
LOAD_POD="${LOAD_POD:-oficina-load-generator}"
LOAD_WORKERS="${LOAD_WORKERS:-20}"
METRICS_SERVER_VERSION="${METRICS_SERVER_VERSION:-v0.9.0}"

usage() {
  cat <<'EOF'
Uso: scripts/demo-hpa.sh <comando>

Comandos:
  prepare  instala e valida o metrics-server no cluster atual
  start    inicia carga de CPU contra a API da oficina
  status   mostra HPA, pods e consumo atual
  watch    acompanha HPA e pods ate Ctrl+C
  stop     encerra o gerador de carga

Variaveis opcionais:
  NAMESPACE, HPA_NAME, LOAD_POD, LOAD_WORKERS, METRICS_SERVER_VERSION

Execute somente em um cluster local/de avaliacao.
EOF
}

require_cluster() {
  command -v kubectl >/dev/null 2>&1 || {
    echo "kubectl nao encontrado no PATH."
    exit 1
  }

  kubectl cluster-info >/dev/null 2>&1 || {
    echo "Nenhum cluster Kubernetes acessivel no contexto atual."
    exit 1
  }
}

prepare_metrics() {
  echo "Instalando metrics-server ${METRICS_SERVER_VERSION}..."
  kubectl apply -f \
    "https://github.com/kubernetes-sigs/metrics-server/releases/download/${METRICS_SERVER_VERSION}/components.yaml"

  if ! kubectl -n kube-system get deploy metrics-server \
    -o jsonpath='{.spec.template.spec.containers[0].args}' | grep -q -- '--kubelet-insecure-tls'; then
    kubectl -n kube-system patch deployment metrics-server --type=json \
      -p='[{"op":"add","path":"/spec/template/spec/containers/0/args/-","value":"--kubelet-insecure-tls"}]'
  fi

  kubectl -n kube-system rollout status deploy/metrics-server --timeout=180s

  for attempt in {1..12}; do
    if kubectl top nodes >/dev/null 2>&1; then
      echo "Metrics Server pronto."
      kubectl top nodes
      return
    fi
    echo "Aguardando metricas (${attempt}/12)..."
    sleep 5
  done

  echo "Metrics Server nao disponibilizou metricas dentro do prazo."
  exit 1
}

start_load() {
  kubectl -n "${NAMESPACE}" get hpa "${HPA_NAME}" >/dev/null
  kubectl -n "${NAMESPACE}" delete pod "${LOAD_POD}" --ignore-not-found --wait=true

  echo "Iniciando ${LOAD_WORKERS} workers de carga no cluster de avaliacao..."
  # As variaveis do comando devem ser expandidas dentro do container BusyBox.
  # shellcheck disable=SC2016
  kubectl -n "${NAMESPACE}" run "${LOAD_POD}" \
    --image=busybox:1.36 \
    --restart=Never \
    --env="LOAD_WORKERS=${LOAD_WORKERS}" \
    -- /bin/sh -c '
      worker=0
      while [ "$worker" -lt "$LOAD_WORKERS" ]; do
        (
          while true; do
            wget -q -O /dev/null \
              --header="Content-Type: application/json" \
              --post-data="{\"username\":\"admin\",\"password\":\"invalid-load-test-password\"}" \
              http://oficina-api/api/auth/login || true
          done
        ) &
        worker=$((worker + 1))
      done
      wait
    '

  kubectl -n "${NAMESPACE}" wait --for=condition=Ready "pod/${LOAD_POD}" --timeout=60s
  echo "Carga iniciada. Use 'scripts/demo-hpa.sh watch' em outro terminal."
}

show_status() {
  kubectl -n "${NAMESPACE}" get hpa "${HPA_NAME}"
  kubectl -n "${NAMESPACE}" get pods -o wide
  kubectl -n "${NAMESPACE}" top pods || true
}

stop_load() {
  kubectl -n "${NAMESPACE}" delete pod "${LOAD_POD}" --ignore-not-found --wait=true
  echo "Gerador de carga encerrado."
}

case "${1:-}" in
  prepare|start|status|watch|stop) require_cluster ;;
  *) usage; exit 1 ;;
esac

case "${1}" in
  prepare) prepare_metrics ;;
  start) start_load ;;
  status) show_status ;;
  watch) kubectl -n "${NAMESPACE}" get hpa,pods -w ;;
  stop) stop_load ;;
esac
