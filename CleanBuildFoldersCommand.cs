using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevFolderCleaner;

public class CleanBuildFoldersCommand : Command<CleanBuildFoldersCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [Description("Path to search for  build folders.")]
        [CommandArgument(0, "<searchPath>")]
        public required string SearchPath { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        var foldersToDelete =
            Directory.EnumerateDirectories(settings.SearchPath, "bin", SearchOption.AllDirectories).Concat(
                    Directory.EnumerateDirectories(settings.SearchPath, "obj", SearchOption.AllDirectories))
                .Where(f => !f.Contains("node_modules"))
                .ToList();

        AnsiConsole.MarkupLine("I'm going to [bold]delete[/] the following folders:");

        foreach (var folder in foldersToDelete)
        {
            Console.WriteLine(folder);
        }

        const string confirmCode = "order 66";
        AnsiConsole.MarkupLine($"[green]Say [bold]'{confirmCode}'[/] to confirm:[/]");

        if (Console.ReadLine() != confirmCode)
        {
            AnsiConsole.MarkupLine("[green]Those folders will not die this day.[/]");
            return 0;
        }

        foreach (var folder in foldersToDelete)
        {
            try
            {
                Directory.Delete(folder, true);
                AnsiConsole.MarkupLine($"Deleted {folder}");
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[red]Unable to delete {folder}[/]");
                throw;
            }
        }
        return 0;
    }
}
