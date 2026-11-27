# 05: No audit event when a member file is rejected

Audit asked which member files we turned away in October and I had to grep the logs to answer. We record member_file_loaded but nothing at all when a file is rejected.

Can we record an event for that too? Same actor. Put the reason in the detail but no field values obviously.
