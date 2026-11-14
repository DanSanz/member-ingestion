# Vendored packages

| Package | Version | Source |
|---|---|---|
| `Scheme.Common` | 4.2.0 | Internal package feed, `Scheme.Common 4.2.0` (source package) |

`vendor/Scheme.Common/` is a copy of a released package's source, not a fork. It is vendored so the service
builds on machines that cannot reach the internal feed. To update it, replace the folder with the new
release and change the version above. Local edits are lost at the next update; ask the platform team
for a change instead.

## Changes by version

- **4.2.0**: `FeedReader` parses the header file date with `UkDate`, not `DateTime.Parse`. Adds
  `UkDate.AgeOn`.
- **4.1.0**: `HttpAuditLog`. `AuditEvent.RuleId`.
- **4.0.0**: `Scheme.Common.Calendar` removed; its contents moved to `Scheme.Common.Dates`.
