# Coding standards

## C#

- .NET 10, C# latest, nullable on, warnings are errors (`Directory.Build.props`). Fix a warning in the
  code; never switch it off for a project or a file.
- Records for data, sealed classes for services, constructor injection. A service is registered in
  `ServiceCollectionExtensions.AddMemberIngestion`, so the job and the test harness get the same wiring.
- Every `Parse`, `TryParse` and `ToString` of a date or number passes a format and a culture. Feed dates
  go through `Scheme.Common.Dates.UkDate`.
- Money is `decimal`, never `double`.
- Log with message templates (`[LoggerMessage]` methods in library code); never build a log message by
  interpolation.
- No new package without a Technical Lead's agreement (`Directory.Packages.props`).
- Namespaces depend downwards only: `Reports`, then `Ingestion`, then `Feeds` and `Members`.
  `Feeds` and `Members` never use a type from `Ingestion` or `Reports`.

## Tests

- Test at a module's interface, with in-memory data, through `IngestionHarness`. No network, no shared
  databases.
- One behaviour per test; the name says the behaviour (`Valid_row_continues_unchanged`).
- Every boundary in a spec gets a test on each side of it.
- Never change an existing test to make new code pass. If a test is wrong, say so in the PR.

## Data

- Member files carry personal data: names, NI numbers, dates of birth.
- Logs, exceptions, audit details and reports refer to a member by member reference only.
- Never log a record or a row. A C# record's `ToString()` prints every property, so `{Row}` in a log
  message writes the date of birth and NI number. Exception messages from `DateTime.Parse` and friends
  repeat their input, so never log them for a personal field.
- Test data is synthetic. Use `MemberRecords.Make` or add a fixture file marked as synthetic (see
  `tests/fixtures/README.md`).

## Commits and PRs

- One task per branch. The PR description links the task card and says what was checked.
- Commit messages: imperative, under 72 characters, what and why.
