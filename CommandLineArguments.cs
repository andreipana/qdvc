﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace qdvc
{
    internal class CommandLineArguments
    {
        public string? Username { get; }
        public string? Password { get; }
        public string[] Paths { get; }

        public CommandLineArguments(string[] args)
        {
            string? username = null; // Environment.GetEnvironmentVariable("ARTIFACTORY_USERNAME");
            string? password = null; // Environment.GetEnvironmentVariable("ARTIFACTORY_TOKEN");

            var paths = new List<string>();

            for (var i = 0; i < args.Length; i++)
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

            if (Username == null || Password == null || Paths.Length == 0)
            {
                Console.WriteLine("Usage: qdvc -u <username> -p <password> <path> [<path> ...]");
                Environment.Exit(1);
            }
        }
    }
}