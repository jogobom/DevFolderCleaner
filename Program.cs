using DevFolderCleaner;
using Spectre.Console;
using Spectre.Console.Cli;

Console.CancelKeyPress += (sender, e) =>
{
    AnsiConsole.MarkupLine("[red]Operation cancelled by user.[/]");
};

var app = new CommandApp();
app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
    config.AddCommand<CleanBuildFoldersCommand>("cleanBuilds")
        .WithExample("cleanBuilds", "C:/code");
    config.AddCommand<DeleteOrphanedFoldersCommand>("deleteOrphans")
        .WithExample("deleteOrphans", ".")
        .WithExample("deleteOrphans", "C:/code", "--dry-run")
        .WithExample("deleteOrphans", "../repos", "--force");
});
try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    return -1;
}
