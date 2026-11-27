# 04: MemberFeedReader needs IngestionOptions

the feed reader takes IOptions<IngestionOptions> just to read StrictSchema. thats the Feeds namespace reaching up into Ingestion. it also means you cant new up a reader in a small tool without building the whole options thing.

could Read just take a bool, and the ingestor pass it in? not urgent but it bugs me every time i see it. appsettings shouldnt need to change.
