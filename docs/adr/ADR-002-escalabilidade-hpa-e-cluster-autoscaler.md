# ADR-002 — Escalabilidade: HPA, Cluster Autoscaler e rate limit

**Status:** Aceito · **Data:** 20/10/2026

## Contexto

Com várias unidades, a carga varia ao longo do dia (picos na abertura das lojas). O HPA da Fase 2 escalava pods, mas o número de nós era fixo: pods extras ficavam `Pending` quando não havia espaço. Também não havia proteção contra excesso de requisições.

## Decisão

Escalar em três camadas:

| Camada | Mecanismo | Configuração |
| --- | --- | --- |
| Entrada | Throttling do API Gateway | 50 req/s sustentado, rajada de 100; 5 req/s em `/auth/*`. Excedente recebe **429** sem chegar ao cluster |
| Pods | HPA (`autoscaling/v2`) por CPU (70%) e memória (80%) | 2 a 8 réplicas; sobe até 2 pods a cada 30 s; desce após 5 min estável |
| Nós | Cluster Autoscaler sobre o node group gerenciado | 1 a 4 nós `t3.medium`; remove nós ociosos após 5 min |

Complementos: `PodDisruptionBudget` (mínimo 1 réplica em manutenção), rolling update com `maxUnavailable: 0`, réplicas espalhadas por zona (`topologySpreadConstraints`) e requests/limits definidos (sem eles o HPA não calcula utilização).

O rate limit fica no gateway, e não na API, porque barra o tráfego antes de consumir CPU dos pods e vale também para a Lambda.

## Validação

O script k6 (`tests/carga/k6-ordens-servico.js`) sobe até 60 usuários virtuais por 3 minutos. Durante o teste, acompanhamos `kubectl get hpa,pods -w` e o dashboard do New Relic (CPU dos pods, réplicas e latência p95).

## Consequências

- Escalar nós leva de 2 a 4 minutos; os pods escalam em segundos. A rajada do gateway absorve o intervalo.
- O custo cresce com o pico e volta ao mínimo depois, sem intervenção manual.
