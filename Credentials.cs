using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc
{
    internal class Credentials(string username, string Password, string source)
    {
        public string Username { get; } = username;
        public string Password { get; } = Password;

        public string Source { get; } = source;

        public static Credentials? DetectFrom(CommandLineArguments args, string? dvcFolder)
        {
            return DetectFromCommandLineArguments(args)
                ?? DetectFromFolder(dvcFolder)
                ?? DetectFromEnvironment();
        }

        private static Credentials? DetectFromCommandLineArguments(CommandLineArguments args)
        {
            if (args.Username == null || args.Password == null)
                return null;

            return new Credentials(args.Username, args.Password, "command line arguments");
        }

        private static Credentials? DetectFromFolder(string? dvcFolder)
        {
            if (dvcFolder == null)
                return null;

            try
            {
                var configLocal = Path.Combine(dvcFolder, "config.local");

                if (!File.Exists(configLocal))
                    return null;

                var lines = File.ReadAllLines(configLocal);
                string? username = null;
                string? password = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("user = "))
                        username = trimmedLine.Substring("user = ".Length);
                    else if (trimmedLine.StartsWith("password = "))
                        password = trimmedLine.Substring("password = ".Length);
                }

                if (username == null || password == null)
                    return null;

                return new Credentials(username, password, ".dvc\\config.local file");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read credentials from {dvcFolder}: {ex.Message}");
            }

            return null;
        }

        private static Credentials? DetectFromEnvironment()
        {
            var username = Environment.GetEnvironmentVariable("ARTIFACTORY_USERNAME");
            var password = Environment.GetEnvironmentVariable("ARTIFACTORY_TOKEN") ??
                Environment.GetEnvironmentVariable("ARTIFACTORY_PASSWORD");

            if (username == null || password == null)
                return null;

            return new Credentials(username, password, "environment variables");
        }
    }
}
