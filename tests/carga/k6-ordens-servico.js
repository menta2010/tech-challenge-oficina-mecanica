// Teste de carga da API publicada (API Gateway -> EKS -> RDS) com k6.
// Objetivo: verificar se o provisionamento (HPA + Cluster Autoscaler + rate limit) aguenta
// picos de abertura e consulta de OS, e gerar trafego para os dashboards do New Relic.
//
// Uso:
//   k6 run -e API_URL=https://xxxx.execute-api.us-east-1.amazonaws.com -e SENHA_ADMIN=*** tests/carga/k6-ordens-servico.js
// Em outro terminal, acompanhe o escalonamento:
//   kubectl -n oficina get hpa,pods -w
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend } from 'k6/metrics';

const API = __ENV.API_URL;
const latenciaListagem = new Trend('latencia_listagem_ms', true);

export const options = {
  scenarios: {
    pico: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '1m', target: 20 },   // aquecimento
        { duration: '3m', target: 60 },   // pico: deve disparar o HPA
        { duration: '1m', target: 0 },    // desaceleracao
      ],
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.02'],                  // menos de 2% de erro (429 do rate limit conta como falha)
    'http_req_duration{rota:listagem}': ['p(95)<800'], // 95% das listagens abaixo de 800 ms
    'http_req_duration{rota:abertura}': ['p(95)<1200'],
  },
};

const json = (token, correlacao) => ({
  headers: {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
    'x-correlation-id': correlacao,
  },
});

// Executa uma vez: autentica e prepara cliente/veiculo/loja usados por todos os VUs
export function setup() {
  const login = http.post(`${API}/auth/login`,
    JSON.stringify({ usuario: 'admin', senha: __ENV.SENHA_ADMIN }),
    { headers: { 'Content-Type': 'application/json' } });
  check(login, { 'login 200': (r) => r.status === 200 });
  const token = login.json('token');

  const lojas = http.get(`${API}/api/lojas`, json(token, 'k6-setup')).json();
  const servicos = http.get(`${API}/api/servicos`, json(token, 'k6-setup')).json();

  const cpf = gerarCpf();
  const cliente = http.post(`${API}/api/clientes`,
    JSON.stringify({ nome: 'Cliente Carga k6', documento: cpf, email: null, telefone: null }),
    json(token, 'k6-setup')).json();
  const veiculo = http.post(`${API}/api/veiculos`,
    JSON.stringify({ clienteId: cliente.id, placa: `K6${String(Date.now()).slice(-5)}`, marca: 'VW', modelo: 'Gol', ano: 2020 }),
    json(token, 'k6-setup')).json();

  return { token, lojaId: lojas[0].id, servicoId: servicos[0].id, clienteId: cliente.id, veiculoId: veiculo.id };
}

export default function (dados) {
  const correlacao = `k6-${__VU}-${__ITER}`;

  // 1) Abertura de OS com servico (entra em diagnostico)
  const abertura = http.post(`${API}/api/ordens-servico`, JSON.stringify({
    lojaId: dados.lojaId,
    clienteId: dados.clienteId,
    veiculoId: dados.veiculoId,
    servicos: [{ servicoId: dados.servicoId }],
  }), Object.assign(json(dados.token, correlacao), { tags: { rota: 'abertura' } }));
  check(abertura, { 'abertura 201': (r) => r.status === 201 });

  // 2) Listagem operacional (consulta mais frequente)
  const listagem = http.get(`${API}/api/ordens-servico`,
    Object.assign(json(dados.token, correlacao), { tags: { rota: 'listagem' } }));
  check(listagem, { 'listagem 200': (r) => r.status === 200 });
  latenciaListagem.add(listagem.timings.duration);

  sleep(1);
}

function gerarCpf() {
  const n = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  const dig = (arr) => {
    let soma = 0;
    arr.forEach((d, i) => { soma += d * (arr.length + 1 - i); });
    const r = soma % 11;
    return r < 2 ? 0 : 11 - r;
  };
  const d1 = dig(n);
  const d2 = dig([...n, d1]);
  return [...n, d1, d2].join('');
}
