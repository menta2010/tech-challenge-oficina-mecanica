# Relatório de Análise de Vulnerabilidades

Projeto: Sistema Integrado de Atendimento e Execução de Serviços (Oficina Mecânica) — MVP back-end.
Data da análise: junho/2026 · Responsável: [preencher].

## 1. Objetivo

Identificar vulnerabilidades em dependências (SCA) e no código/configuração do back-end, e registrar o resultado do scan conforme exigido na entrega da Fase 1.

## 2. Metodologia e ferramentas

| Camada | Ferramenta | Comando |
|---|---|---|
| Dependências (.NET) | `dotnet list package --vulnerable` | `dotnet list package --vulnerable --include-transitive` |
| Dependências/segredos/config | Trivy (filesystem) | `trivy fs --scanners vuln,secret,misconfig .` |
| Imagem Docker | Trivy (image) | `docker build -t oficina-api . && trivy image oficina-api` |
| Pacotes desatualizados | dotnet | `dotnet list package --outdated` |

O script `scripts/security-scan.sh` executa os scans e orienta onde colar a saída. A execução requer o .NET 8 SDK (e Trivy, opcional).

> Como reproduzir: rode `./scripts/security-scan.sh` na raiz e cole a saída completa na seção 6 deste documento.

## 3. Análise das dependências diretas

Versões fixadas (pinned) no momento da análise:

| Pacote | Versão | Função | Status conhecido |
|---|---|---|---|
| Microsoft.EntityFrameworkCore | 8.0.6 | ORM | Sem advisory conhecido na linha 8.0.6 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.4 | Provider PostgreSQL | Não afetado pelo CVE-2024-32655 (ver 4) |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.6 | Validação JWT | Sem advisory conhecido na 8.0.6 |
| System.IdentityModel.Tokens.Jwt | 8.0.1 | Emissão de JWT | Não afetado pelo CVE-2024-21319 (ver 4) |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger/OpenAPI | Sem advisory conhecido |
| FluentValidation | 11.9.2 | Validação | Sem advisory conhecido |

Dependências transitivas relevantes (ex.: `System.Text.Json`, `Microsoft.IdentityModel.*`) são puxadas pelo runtime .NET 8 e pelos pacotes acima; devem ser confirmadas com `--include-transitive`. Manter o SDK/`Microsoft.NET.Sdk` atualizado garante os patches mais recentes dessas transitivas.

## 4. CVEs verificados explicitamente

- **CVE-2024-32655 — Npgsql (SQL Injection via overflow do tamanho de mensagem do protocolo).** Afeta versões `<= 8.0.2`; corrigido em `8.0.3+`. O projeto usa **8.0.4 → não afetado**.
- **CVE-2024-21319 — System.IdentityModel.Tokens.Jwt / Microsoft.IdentityModel (DoS por JWE com alta taxa de compressão).** Corrigido na linha 7.1.2+; o projeto usa **8.0.1 → não afetado**.

Nenhuma vulnerabilidade de severidade alta/crítica foi identificada nas versões fixadas com base nessa verificação. A confirmação definitiva deve vir da saída do scan (seção 6).

## 5. Revisão de segurança do código e configuração

Pontos positivos implementados:

- **Acesso a dados sem SQL dinâmico.** Todo acesso usa EF Core/LINQ com parâmetros; não há concatenação de SQL — mitiga SQL Injection.
- **Senhas com hash forte.** PBKDF2 (SHA-256, 100.000 iterações, salt aleatório de 16 bytes) e comparação em tempo constante (`CryptographicOperations.FixedTimeEquals`). Senhas nunca são armazenadas em texto puro.
- **Autenticação/autorização.** JWT Bearer com validação de issuer, audience, assinatura e expiração (`ClockSkew = 0`). Endpoints administrativos exigem `[Authorize]`; apenas health, login e consulta pública de andamento são anônimos.
- **Validação de entrada.** CPF/CNPJ (dígitos verificadores) e placa (formato antigo e Mercosul) validados em Value Objects; FluentValidation nas bordas. Reduz dados malformados e abuso.
- **Tratamento de erros.** Middleware central retorna mensagens padronizadas e **não expõe stack trace** ao cliente; exceções não tratadas são logadas no servidor e respondem 500 genérico.

Riscos residuais e recomendações (hardening):

| Item | Risco | Recomendação |
|---|---|---|
| `Jwt:SecretKey` no appsettings | Segredo em repositório | Em produção, injetar via variável de ambiente/secret manager; trocar a chave default. |
| Usuário admin seed (`admin/admin123`) | Credencial fraca conhecida | Trocar/remover após o primeiro acesso; não usar em produção. |
| TLS/HTTPS | Tráfego em claro | Terminar TLS no proxy/reverse proxy; habilitar HSTS em produção. |
| Brute force no login | Tentativas ilimitadas | Adicionar rate limiting/lockout no endpoint de login. |
| CORS | Origem ampla | Restringir origens permitidas conforme o front. |
| Container | Executa como root | Opcional: usar usuário não-root na imagem final. |

## 6. Resultado do scan (colar saída)

```
# Saída de: dotnet list package --vulnerable --include-transitive
[colar aqui]

# Saída de: trivy fs --scanners vuln,secret,misconfig .
[colar aqui]
```

## 7. Conclusão

Com as versões de pacotes fixadas e a verificação dos CVEs aplicáveis, **não foram identificadas vulnerabilidades de severidade alta/crítica** nas dependências, e o código adota práticas de segurança adequadas a um MVP (hash de senha forte, JWT validado, acesso a dados parametrizado, validação de entrada e tratamento de erros sem vazamento). As recomendações de hardening da seção 5 devem ser aplicadas antes de um ambiente produtivo. A saída dos scans (seção 6) complementa este relatório com os resultados ao vivo no momento da entrega.

## Fontes

- [CVE-2024-32655 — Npgsql SQL Injection (GitHub Advisory)](https://github.com/advisories/GHSA-x9vc-6hfv-hg8c)
- [CVE-2024-21319 — System.IdentityModel.Tokens.Jwt DoS (Snyk)](https://security.snyk.io/vuln/SNYK-DOTNET-SYSTEMIDENTITYMODELTOKENSJWT-6148655)
