using System;
using System.Collections.Generic;
using System.Linq;

namespace qdvc
{
    public class CommandLineArguments
    {
        public string? Username { get; }
        public string? Password { get; }
        public string[] Paths { get; }

        public string Command { get; }

        private string[] AllowedCommands =
        {
            "pull", "status", "add", "push"
        };

        public CommandLineArguments(string[] args)
        {
            string? username = null;
            string? password = null;

            int nextArgIndex = 0;

            var firstArg = args.FirstOrDefault()?.ToLower();
            if (AllowedCommands.Contains(firstArg))
            {
                Command = firstArg;
                nextArgIndex = 1;
            }
            else
            {
                Command = "pull";
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

            Username = username;
            Password = password;
            Paths = paths.ToArray();

            if (Paths.Length == 0)
            {
                Console.WriteLine("ERROR: No paths provided.");
                Console.WriteLine("Usage: qdvc <command> [-u <username>] [-p <password>] <path> [<path> ...]");
                Console.WriteLine("  <command>  must be one of: status, pull, add, push");
                Environment.Exit(1);
            }
        }
    }
}
