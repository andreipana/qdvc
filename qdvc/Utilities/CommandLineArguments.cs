using System;
using System.Collections.Generic;
using System.Linq;

namespace qdvc.Utilities
{
    public class CommandLineArguments
    {
        public string? Username { get; private set; }
        public string? Password { get; private set; }
        public IEnumerable<string> Paths { get; private set; } = [];

        public string? Command { get; private set; } = null;

        public bool Force { get; set; } = false;

        public CommandLineArguments(string? command, IEnumerable<string> paths, string? username, string? password)
        {
            Command = command;
            Paths = paths;
            Username = username;
            Password = password;
        }

        [Obsolete($"Use {nameof(ParseUsingSCL)}")]
        public static CommandLineArguments Parse(string[] args)
        {
            string[] AllowedCommands = ["pull", "status", "add", "push"];

            string? username = null;
            string? password = null;
            string? command = null;

            int nextArgIndex = 0;

            var firstArg = args.FirstOrDefault()?.ToLower();
            if (firstArg != null && AllowedCommands.Contains(firstArg))
            {
                command = firstArg;
                nextArgIndex = 1;
            }
            else
            {
                command = "pull";
                Console.WriteLine("WARNING: no command provided, pull implied.");
            }

            var paths = new List<string>();

            for (var i = nextArgIndex; i < args.Length; i++)
            {
                if (args[i] == "-u" && i + 1 < args.Length)
                {
                    username = args[i + 1];
                    i++;
                }
                else if (args[i] == "-p" && i + 1 < args.Length)
                {
                    password = args[i + 1];
                    i++;
                }
                else
                {
                    paths.Add(args[i]);
                }
            }

            return new CommandLineArguments(command, paths, username, password);
        }

        public static CommandLineArguments ParseUsingSCL(string[] args)
        {
            return new SystemCommandLineFacade().Parse(args);
        }
    }
}
