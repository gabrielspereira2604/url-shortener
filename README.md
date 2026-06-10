# URL Shortener

Encurtador de links construído com ASP.NET Core, PostgreSQL e Redis, focado em estudar conceitos de sistemas distribuídos e heavy-read workloads.

## Stack

- **ASP.NET Core Minimal API** — camada HTTP
- **PostgreSQL** — persistência
- **Entity Framework Core** — acesso ao banco
- **Redis** — cache (padrão cache-aside)

## Modelo de dados

```sql
CREATE TABLE short_links (
    id           BIGSERIAL PRIMARY KEY,
    code         VARCHAR(10) NOT NULL UNIQUE,
    original_url TEXT        NOT NULL,
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at   TIMESTAMPTZ NULL,
    hit_count    BIGINT      NOT NULL DEFAULT 0
);

CREATE UNIQUE INDEX idx_short_links_code ON short_links (code);
```

## Geração do código curto

O campo `code` é gerado na aplicação usando **base62 aleatório** de 6 caracteres.

### Por que base62 e não auto-incremento?

Auto-incremento sequencial (`1`, `2`, `3`...) parecia simples, mas tem problemas reais:

**1. Enumerable por qualquer pessoa**
Com auto-incremento, qualquer um consegue varrer todos os links criados fazendo `/1`, `/2`, `/3`... Não é necessário nenhum conhecimento prévio sobre os links existentes.

**2. Expõe métricas de negócio**
A URL `/9999` revela que existem aproximadamente 10 mil links cadastrados. Isso expõe volume e crescimento da plataforma.

**3. Gargalo em escala distribuída**
Uma sequência centralizada no banco se torna ponto único de contenção quando múltiplos servidores precisam gerar IDs simultaneamente.

### Por que base62 aleatório?

O alfabeto base62 usa `0-9`, `a-z`, `A-Z` — 62 caracteres. Com 6 caracteres:

```
62^6 = 56.800.235.584 combinações possíveis (~56 bilhões)
```

Isso torna enumeração impraticável e não revela nenhuma informação sobre volume.

É a abordagem usada pelos encurtadores de links em produção (bit.ly, t.co, TinyURL).

### Colisões

Com código aleatório, colisão é possível (dois códigos iguais gerados ao mesmo tempo). O tratamento é simples:

```
gera code aleatório de 6 chars
→ tenta INSERT
→ UNIQUE constraint violou? gera outro e tenta de novo
→ na prática, com 56 bilhões de combinações, colisão é raríssima
```

O `BIGSERIAL` existe como chave primária interna do banco. O `code` é o identificador público que aparece na URL.

## Fluxo de criação

```
POST /shorten { url: "https://exemplo.com/pagina-longa" }
  → gera code aleatório (ex: "aB3kZm")
  → INSERT INTO short_links (code, original_url)
  → guarda no Redis: key="aB3kZm", value=url, TTL=expires_at
  → retorna https://seudominio.com/aB3kZm
```

## Fluxo de acesso (heavy-read)

```
GET /aB3kZm
  → busca no Redis (hit? redirect imediato)
  → miss? busca no PostgreSQL WHERE code = 'aB3kZm'
  → repopula o Redis
  → redirect 302 para a URL original
```

Redis é obrigatório aqui, não opcional — encurtadores de link têm volume de leitura muito maior que de escrita.

## Estratégia de cache (Redis LFU)

### Por que não TTL fixo?

Com TTL fixo (ex: 7 dias), links populares expiram e voltam a bater no banco desnecessariamente. Links esquecidos ficam ocupando memória até o TTL vencer.

### LFU — Least Frequently Used

O Redis é configurado com limite de memória e política de eviction `allkeys-lfu`:

```
maxmemory 256mb
maxmemory-policy allkeys-lfu
```

O Redis rastreia a frequência de acesso de cada key. Quando a memória enche, descarta automaticamente os links menos acessados — links populares ficam em cache naturalmente, links raramente acessados são removidos.

É a abordagem usada por encurtadores em produção como bit.ly e t.co.

**LFU vs LRU:**

| | Critério de descarte |
|---|---|
| LRU (Least Recently Used) | menos recentemente acessado — ignora frequência |
| LFU (Least Frequently Used) | menos frequentemente acessado — mais preciso para este caso |

Um link acessado 1 milhão de vezes mas sem acesso há uma semana não deve ser descartado antes de um link acessado uma única vez ontem. LFU resolve isso, LRU não.

**Links com expiração explícita** (`expiresAt`) ainda recebem `AbsoluteExpiration` no Redis para garantir que expirem na data correta, independente da política de eviction.
