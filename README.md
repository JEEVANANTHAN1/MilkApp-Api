# MilkApp-Api

.NET 10 Web API for monitoring milk deposits, backed by Supabase (Postgres) via the
[`supabase-csharp`](https://github.com/supabase-community/supabase-csharp) client SDK.

## Domain

- **Farmer** — supplier registry (name, phone, village).
- **MilkDeposit** — a deposit record (quantity, fat %, rate/liter, computed total amount, shift, timestamp) linked to a farmer.

## Supabase setup

1. Create a project at https://supabase.com.
2. In the SQL editor, run:

```sql
create extension if not exists "uuid-ossp";

create table farmers (
    id uuid primary key default uuid_generate_v4(),
    name text not null,
    phone_number text,
    village text,
    created_at timestamptz not null default now()
);

create table milk_deposits (
    id uuid primary key default uuid_generate_v4(),
    farmer_id uuid not null references farmers(id) on delete cascade,
    quantity_liters numeric(10, 2) not null,
    fat_percentage numeric(4, 2) not null,
    rate_per_liter numeric(10, 2) not null,
    total_amount numeric(10, 2) not null,
    shift text not null default 'Morning',
    deposited_at timestamptz not null default now(),
    created_at timestamptz not null default now()
);

create index idx_milk_deposits_farmer_id on milk_deposits(farmer_id);
create index idx_milk_deposits_deposited_at on milk_deposits(deposited_at);
```

3. In **Project Settings → API**, copy the **Project URL** and an API key
   (`anon` key for client-restricted access, or the `service_role` key if this API
   is a trusted backend and should bypass Row Level Security).

## Configuration

Credentials are read from configuration section `Supabase:Url` / `Supabase:Key`.
Do **not** commit real credentials to `appsettings.json`. Use user-secrets locally:

```bash
dotnet user-secrets set "Supabase:Url" "https://<project-ref>.supabase.co"
dotnet user-secrets set "Supabase:Key" "<anon-or-service-role-key>"
```

In production, set the equivalent environment variables instead:

```bash
Supabase__Url=https://<project-ref>.supabase.co
Supabase__Key=<key>
```

> Note: this app uses the Supabase REST layer (Postgrest) over HTTPS — it does **not**
> connect directly to the Postgres port (5432). If you have direct connection details
> (host/port/database/user/password) for `db.<project-ref>.supabase.co:5432`, those are
> for tools that need raw SQL access (psql, migrations, EF Core) and aren't used by this
> project's SDK-based approach.

## Running

```bash
dotnet run
```

Browse the interactive Swagger UI at `/swagger` (raw spec at `/swagger/v1/swagger.json`), or hit the endpoints directly:

- `GET/POST /api/farmers`, `GET/PUT/DELETE /api/farmers/{id}`
- `GET/POST /api/milkdeposits`, `GET/PUT/DELETE /api/milkdeposits/{id}`
  - Query filters: `?farmerId=&from=&to=`
- `GET /api/milkdeposits/summary?date=2026-08-04` — daily totals (count, liters, amount)
