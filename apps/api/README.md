# apps/api

This directory mirrors the repository layout requirement.
The actual ASP.NET Core host lives at [`src/SerenAuth.Api`](../../src/SerenAuth.Api).
Build / run with:

```bash
dotnet run --project ../../src/SerenAuth.Api
```

Or via Docker Compose from the repo root:

```bash
./infrastructure/scripts/dev-up.sh
```
