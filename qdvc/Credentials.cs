using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc
{
    public class Credentials(string username, string Password, string source)
    {
        public string Username { get; } = username;
        public string Password { get; } = Password;

        public string Source { get; } = source;

        public static Credentials? DetectFrom(CommandLineArguments? args, DvcConfig? dvcConfig)
        {
            return DetectFromCommandLineArguments(args)
                ?? DetectFromDvcConfig(dvcConfig)
                ?? DetectFromEnvironment();
        }

        private static Credentials? DetectFromCommandLineArguments(CommandLineArguments? args)
        {
            if (args?.Username == null || args.Password == null)
                return null;

            return new Credentials(args.Username, args.Password, "command line arguments");
        }

        private static Credentials? DetectFromDvcConfig(DvcConfig? dvcConfig)
        {
            if (dvcConfig == null)
                return null;

            var repository = dvcConfig.GetProperty("core.remote");
            if (repository?.Value == null)
                return null;

            var repositoryCategory = $"'remote \"{repository.Value}\"'";
            var username = dvcConfig.GetProperty($"{repositoryCategory}.user");
            var password = dvcConfig.GetProperty($"{repositoryCategory}.password");

            if (username != null && password != null)
                return new Credentials(username.Value, password.Value, "config");

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
