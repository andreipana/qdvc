using qdvc.Infrastructure;
using System.CommandLine;
using System.CommandLine.IO;

namespace qdvc.Utilities
{
    internal class SystemCommandLineFacade
    {
        private readonly System.CommandLine.IConsole _Console = new Console();

        public CommandLineArguments Parse(string[] args)
        {
            CommandLineArguments Args = new(null, [], null, null);

            var rootCommand = new RootCommand("Quick DVC, a faster alternative to DVC");

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

            rootCommand.Invoke(args, _Console);

            return Args;
        }

        internal class Console : System.CommandLine.IConsole
        {
            private StandardOutputStreamWriter _out = new StandardOutputStreamWriter();
            private bool _isOutputRedirected;
            private StandardErrorStreamWriter _error = new StandardErrorStreamWriter();

            public IStandardStreamWriter Out => _out;

            public bool IsOutputRedirected => _out.IsRedirected;

            public IStandardStreamWriter Error => _error;

            public bool IsErrorRedirected => _error.IsRedirected;

            public bool IsInputRedirected => false;
        }

        internal class StandardOutputStreamWriter : IStandardStreamWriter
        {
            public void Write(string? value)
            {
                SystemContext.Console.StdOutWrite(value);
            }

            public bool IsRedirected => SystemContext.Console.IsStdOutRedirected;
        }

        internal class StandardErrorStreamWriter : IStandardStreamWriter
        {
            public void Write(string? value)
            {
                SystemContext.Console.StdErrWrite(value);
            }

            public bool IsRedirected => SystemContext.Console.IsStdErrRedirected;
        }
    }
}
