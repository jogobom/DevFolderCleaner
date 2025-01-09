using System.ComponentModel;
using LibGit2Sharp;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevFolderCleaner;

internal sealed class DeleteOrphanedFoldersCommand : Command<DeleteOrphanedFoldersCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Path to search. Defaults to current directory.")]
        [CommandArgument(0, "[searchPath]")]
        public string? SearchPath { get; init; }

        [Description("Just describe what would happen but don't actually do anything. Safe mode.")]
        [CommandOption("-d|--dry-run")]
        public bool DryRun { get; init; }
        
        [Description("Ignore any normal checks, such as a dirty repo, and go ahead anyway. Dangerous mode. Combine with --dry-run if you want to safely check what would happen if you forced it.")]
        [CommandOption("-f|--force")]
        public bool Force { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[green]Dry run requested so I'm not going to actually delete anything.[/]");
        }
        
        AnsiConsole.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");
        AnsiConsole.WriteLine($"Specified search path: {settings.SearchPath}");
        
        var searchPath = settings.SearchPath switch
        {
            null => Directory.GetCurrentDirectory(),
            _ => new DirectoryInfo(settings.SearchPath).FullName
        };
        
        AnsiConsole.WriteLine($"Therefore search will start from {searchPath}");
        AnsiConsole.WriteLine();

        var dotGitDir = Repository.Discover(searchPath);
        if (dotGitDir == null)
        {
            AnsiConsole.MarkupLine($"[red]Error: {searchPath} doesn't seem to be part of a repository.[/]");
            return 1;
        }

        using var repo = new Repository(dotGitDir);
        var repoRoot = new DirectoryInfo(dotGitDir).Parent!;
        var fullDotGitDirPath = Path.GetDirectoryName(Path.Combine(repoRoot.FullName, dotGitDir))!;
        
        AnsiConsole.WriteLine($"Found git repository at {repoRoot}");
        
        if (repo.RetrieveStatus().IsDirty)
        {
            if (!settings.Force)
            {
                AnsiConsole.MarkupLine($"[red]Error: The repository is dirty, I can't do my job if the index is not up-to-date. I might delete important things. Please commit and clean up the repository, and try again.[/]");
                return 1;
            }
            AnsiConsole.MarkupLine($"[red]Repository is dirty, I might delete important things, but you're forcing me to proceed anyway...[/]");
        }
        
        var trackedDirectories = repo.Index.Select(entry => Path.Combine(repoRoot.FullName, entry.Path)).GroupBy(Path.GetDirectoryName)
            .Where(g => g.Key is not null).Select(g => g.Key!).ToList();

        List<string> toBeDeleted = [];

        AnsiConsole.Status().Spinner(Spinner.Known.Default).SpinnerStyle(Style.Parse("green")).Start(
            "Searching...", ctx =>
            {
                toBeDeleted = SearchForOrphanedDirectories(fullDotGitDirPath, searchPath, trackedDirectories).ToList();
            });

        if (settings.DryRun)
        {
            AnsiConsole.MarkupLine("[green]This was a dry run, exiting without deletion.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine(
            $"Do you want to [red]delete {toBeDeleted.Count} orphaned folder{(toBeDeleted.Count == 1 ? "" : "s")}?[/]");
        
        // CJP Delete all or Ask about each one in turn 

        return 0;
    }

    private static IEnumerable<string> SearchForOrphanedDirectories(string fullDotGitDirPath, string searchPath,
        IList<string> trackedDirectories)
    {
        if (searchPath == fullDotGitDirPath)
        {
            yield break;
        }

        if (!trackedDirectories.Contains(searchPath))
        {
            AnsiConsole.MarkupLine($"Found orphan [yellow]{searchPath}[/]");
            yield return searchPath;
            yield break;
        }

        foreach (var subDirectory in new DirectoryInfo(searchPath).EnumerateDirectories())
        {
            foreach (var orphanedDirectory in SearchForOrphanedDirectories(fullDotGitDirPath, subDirectory.FullName, trackedDirectories))
            {
                yield return orphanedDirectory;
            }
        }
    }
}