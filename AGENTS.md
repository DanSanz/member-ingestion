## Build and test

Run `powershell -File scripts\check.ps1`: it builds, then runs the unit and module tests. The README's `dotnet build` and `dotnet test` fail, because they compile the acceptance tests.

## Docs

- `docs/standards.md`: coding standards for C#, tests, personal data, commits and PRs. Read before writing or reviewing code.
- `docs/runbook.md`: how the nightly job runs, fails and reruns. Read before changing job behaviour.
- `GLOSSARY.md`: domain terms. Use its vocabulary when naming things.

## Agent skills

### Issue tracker

Issues and specs live as markdown files under `.scratch/<feature-slug>/` in this repo. See `docs/agents/issue-tracker.md`.

### Triage labels

The five default triage roles, unchanged: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` and `docs/adr/` at the repo root. See `docs/agents/domain.md`.
