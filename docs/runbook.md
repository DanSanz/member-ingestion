# Nightly run

| Job | Command | Reads |
|---|---|---|
| Member load | `dotnet run --project src/MemberIngestion.Job -- <files>` | MEMBERS files |

- The scheduler starts the job at 02:00 over the drops that landed the day before. Each client
  sends one submission a month.
- Each member file is the client's full list of active members on the file date. It is not a
  list of changes.
- A file is imported whole or not at all. A failed file is named in the log. Exit code 1 means
  at least one file was rejected.
- If the job fails, on-call fixes the cause and reruns it over the same files. A rerun must
  leave things as a clean run would have.
- The on-call alert shows the first line of the job's error output, not the whole log.
- The job never contacts clients or members. The pack it prints goes to Employer Relations,
  who send it on.
- The pack lists the new members in each file, so Employer Relations can send each one a
  welcome pack. Quote the membership number, never the name, when you query an entry in the
  pack with the client.
