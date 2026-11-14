// Regenerates src/MemberIngestion/Generated/ from the feed contract.
// Run from the repository root: dotnet run --project tools/FeedTypes

using FeedTypes;

var code = FeedTypeGenerator.Generate(File.ReadAllText(FeedTypeGenerator.ContractPath));
File.WriteAllText(FeedTypeGenerator.OutputPath, code);
Console.WriteLine($"Wrote {FeedTypeGenerator.OutputPath}");
