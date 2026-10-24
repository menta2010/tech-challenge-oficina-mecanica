# RFC-003 — Estratégia de autenticação

| | |
| --- | --- |
| **Status** | Aceita |
| **Data** | 20/10/2026 |
| **Autores** | Grupo 60 |

## Contexto

O enunciado pede proteger as rotas sensíveis com **autenticação via CPF**, usando uma **function serverless** que valide o CPF, consulte existência e status do cliente e devolva um **JWT**. Funcionários (administrador e atendentes de cada loja) também precisam se autenticar, e até a Fase 2 o login de funcionário ficava dentro da própria API.

## Opções avaliadas

| Critério | A) Lambda própria emitindo JWT HS256 | B) Amazon Cognito (user pool) | C) Lambda authorizer no API Gateway validando cada chamada |
| --- | --- | --- | --- |
| Login só com CPF (sem senha) | Natural | Exige fluxo customizado (custom auth challenge com 3 Lambdas) | Não emite token |
| Consulta status no nosso banco | Direto | Via trigger | Direto, mas a cada requisição |
| Mudança na API | Nenhuma: já valida HS256 | Trocar para RS256/JWKS | Nenhuma |
| Latência por requisição | Só no login | Só no login | Lambda em toda chamada |
| Complexidade | Baixa | Alta | Média |

## Proposta

**Opção A.** Uma Lambda .NET 8 com duas rotas no API Gateway:

- `POST /auth/cpf`: valida os dígitos do CPF, consulta `Clientes` (existe? `Ativo`?) e emite um JWT com `role = Cliente` e `sub = id do cliente`.
- `POST /auth/login`: confere usuário e senha (hash PBKDF2 já gravado pela API) e emite JWT com `role = Admin | Atendente` e `loja_id`.

A chave HS256 fica no Secrets Manager (`oficina/<amb>/jwt`), lida pela Lambda e injetada na API pelo pipeline. A API valida issuer, audience, assinatura e expiração (já fazia isso), e aplica o **escopo** por perfil:

| Perfil | Pode |
| --- | --- |
| Cliente | Abrir OS para si, listar e acompanhar só as próprias |
| Atendente | Operar OS e estoque somente da própria loja |
| Admin | Tudo, incluindo lojas e catálogo |

## Riscos e mitigações

| Risco | Mitigação |
| --- | --- |
| Força bruta de CPF ou senha | Throttling de 5 req/s nas rotas `/auth/*` no API Gateway; mesma mensagem para usuário inexistente e senha errada |
| Vazamento da chave simétrica | Só no Secrets Manager e no Secret do Kubernetes, nunca no Git; rotação = novo valor no segredo + redeploy |
| CPF em logs (LGPD) | Log mascarado (`***.***.***-25`) |
| Cliente inativo com token antigo | Expiração de 60 minutos |

## Consequências

- O Cognito fica como evolução se o produto precisar de cadastro próprio, MFA ou login social (registrado como alternativa).
- O endpoint `/api/auth/login` da API continua existindo só para desenvolvimento local e testes; na nuvem o login oficial é o da Lambda.
