# Pick Me — Şoför & Vale Platformu

İstanbul merkezli şoför + vale hizmeti rezervasyon platformu. Kurumsal site + müşteri portalı + admin paneli + şoför paneli.

Bu monorepo plan dosyasının ([`C:/Users/Ali/.claude/plans/0-dan-100-e-delegated-book.md`](C:/Users/Ali/.claude/plans/0-dan-100-e-delegated-book.md)) icra edilmesiyle kurulmuştur.

## Mimari

- **frontend/** — React 18 + Vite + TypeScript + Tailwind CSS + shadcn/ui. Public site pre-render (SSG).
- **backend/** — ASP.NET Core 10 Web API (Clean Architecture: Domain / Application / Infrastructure / Api / Contracts) + EF Core + MSSQL.
- **shared/** — Zod şemaları + `validation-rules.json` (tek doğrulama kaynağı) + NSwag-generate TypeScript tipleri.
- **infra/** — Docker Compose (MSSQL + smtp4dev), IIS web.config, SQL backup scripts, deploy scripts.
- **docs/** — Kurulum, runbook, admin + şoför kılavuzları.

## Önkoşullar

- Node.js 22+
- pnpm 10+
- .NET SDK 10+
- Docker Desktop (MSSQL + smtp4dev için)
- MSSQL 2022 (lokal veya Docker)

## Lokal çalıştırma

```bash
# 1) Dependencies
pnpm install
dotnet restore backend/PickMe.slnx

# 2) .env dosyasını oluştur
cp .env.example .env   # Windows: copy .env.example .env

# 3) Docker (MSSQL + smtp4dev)
docker compose -f infra/dev/docker-compose.yml up -d

# 4) DB migration
pnpm run backend:migrate

# 5) Backend (port 5001) ve frontend (port 5173) — iki ayrı terminal
pnpm run backend:run
pnpm dev
```

Frontend: https://localhost:5173 — Backend Swagger: https://localhost:5001/swagger — smtp4dev UI: http://localhost:5050

## Testler

```bash
pnpm run backend:test      # .NET: unit + integration + architecture
pnpm test                  # Frontend: Vitest unit
pnpm test:e2e              # Playwright E2E
```

## Geliştirme akışı

Her fazın bitiş şartları ve `DUR VE KONTROL ET` checkpoint'leri plan dosyasında. Her modül tamamlandığında:

1. İlgili testler yeşil.
2. Önceki fazların testleri hâlâ yeşil (regression).
3. `shared/api-types.ts` drift yok.
4. `docs/` ilgili bölüm güncel.

## Durum

Faz 0 (iskelet) — tamamlandı. Faz 1+ — devam ediyor.
