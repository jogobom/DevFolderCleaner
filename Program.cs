using GitLeftoversCleaner;
using Spectre.Console.Cli;

var app = new CommandApp<DeleteOrphanedFoldersCommand>();
return await app.RunAsync(args);