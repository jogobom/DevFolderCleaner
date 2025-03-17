using DevFolderCleaner;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
    config.AddCommand<DeleteOrphanedFoldersCommand>("deleteOrphans");
    config.AddCommand<CleanBuildFoldersCommand>("cleanBuilds").WithExample("cleanBuilds", "C:/code");
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