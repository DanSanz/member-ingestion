# MemberIngestion

Nightly job that loads employer member files into the member store.

## Getting started

```
dotnet build
dotnet test
```

## Layout

- `src/MemberIngestion` – the ingestion library
- `src/MemberIngestion.Worker` – the nightly worker, e.g. `dotnet run --project src/MemberIngestion.Worker -- <file>`
- `vendor/Scheme.Common` – shared scheme library: dates, audit, feed envelope
- `tests/` – xUnit tests

Date helpers come from `Scheme.Common.Calendar`.

Ask in #member-ingestion-dev if you get stuck.

## Skills

`.claude/skills` holds the skills from https://github.com/mattpocock/skills at commit `5863806dd4e15c63174bee4e7515a4256792ecfa`, under the MIT licence in `.claude/skills/LICENSE`.
