# Member ingestion

The nightly job that loads the feed files employers send to the scheme into the member store, and prints a pack for Employer Relations.

## Language

### Who and what

**Employer**:
An organisation whose staff are members of the scheme. It sends one feed file a month. Known by its employer ID.
_Avoid_: Sender, client, organisation

**Member reference**:
The scheme's identifier for a member. It is the only way to name a member outside the member store.
_Avoid_: Member ID, member number, membership number

**Joiner**:
A member whose date joined falls in the same month as the file date of the employer's feed file.
_Avoid_: New starter, new member

**Date joined**:
The day a member joined the scheme, as the employer states it in the feed file.
_Avoid_: Start date, joining date, service start

**Service**:
The whole years from a member's date joined to the date a report is run for.
_Avoid_: Long service, length of service, tenure

### Files

**Feed file**:
An employer's full list of its active members on one file date. It is a complete list, never a list of changes.
_Avoid_: Member file, drop, file drop, submission

**Row**:
One member's details in a feed file.
_Avoid_: Detail line, record, entry

**File date**:
The date the employer writes in the header of a feed file, the day its list was true. It is not the day the file arrived, which the file register keeps as the received date.
_Avoid_: As at, header file date

### The load

**Load**:
Taking every row of a feed file into the member store. A feed file is loaded whole or not at all.
_Avoid_: Ingest, upsert, imported

**Rejected file**:
A feed file the job loads nothing from, because it breaks the envelope or the contract. No audit event and no receipt are recorded, the log names it, and the job exits with code 1.
_Avoid_: Failed file, refused
