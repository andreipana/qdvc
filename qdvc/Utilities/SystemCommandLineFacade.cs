using System.CommandLine;

namespace qdvc.Utilities
{
    internal class SystemCommandLineFacade
    {
        public CommandLineArguments? Parse(string[] args)
        {
            CommandLineArguments? Args = null;

            var rootCommand = new RootCommand();

            var usernameOption = new Option<string>(aliases: ["--username", "-u"], "The username for the remote repository");
            rootCommand.AddGlobalOption(usernameOption);

            var passwordOption = new Option<string>(aliases: ["--password", "-p"], "The password for the remote repository");
            rootCommand.AddGlobalOption(passwordOption);

            var pathsArgument = new Argument<string[]>("path", "Path to a file or folder which will be processed by the given command.");
            pathsArgument.Arity = ArgumentArity.OneOrMore;

            var statusCommand = new Command("status", "Shows the status of the tracked files.");
            statusCommand.AddArgument(pathsArgument);
            statusCommand.SetHandler((paths, username, password) =>
            {
                Args = new CommandLineArguments("status", paths, username, password);
            }, pathsArgument, usernameOption, passwordOption);
            rootCommand.AddCommand(statusCommand);

            var pullCommand = new Command("pull", "Pulls from cache or remote repository the files specified by the .dvc files in the given paths.");
            pullCommand.AddArgument(pathsArgument);
            pullCommand.SetHandler((paths, username, password) =>
            {
                Args = new CommandLineArguments("pull", paths, username, password);
            }, pathsArgument, usernameOption, passwordOption);
            rootCommand.AddCommand(pullCommand);

            var addCommand = new Command("add", "Adds new files under dvc control or updates any previously added file modified locally.");
            addCommand.AddArgument(pathsArgument);
            addCommand.SetHandler((paths, username, password) =>
            {
                Args = new CommandLineArguments("add", paths, username, password);
            }, pathsArgument, usernameOption, passwordOption);
            rootCommand.AddCommand(addCommand);

            var pushCommand = new Command("push", "Pushes files from cache to the remote repository.");
            pushCommand.AddArgument(pathsArgument);
            pushCommand.SetHandler((paths, username, password) =>
            {
                Args = new CommandLineArguments("push", paths, username, password);
            }, pathsArgument, usernameOption, passwordOption);
            rootCommand.AddCommand(pushCommand);

            rootCommand.Invoke(args);

            return Args;
        }
    }
}
