using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace qdvc
{
    internal class DvcConfig
    {
        public List<DvcConfigProperty> Properties { get; } = new();

        public string? ProjectConfigFile { get; private set; }

        public string? LocalConfigFile { get; private set; }

        private DvcConfig()
        {
        }

        public static DvcConfig ReadConfigFromFolder(string? dvcFolder)
        {
            var config = new DvcConfig();

            if (dvcFolder == null)
                return config;

            var projectConfigFile = Path.Combine(dvcFolder, "config");
            if (File.Exists(projectConfigFile))
            {
                config.ProjectConfigFile = projectConfigFile;
                config.LoadPropertiesFromFile(projectConfigFile, DvcConfigPropertySource.Project);
            }

            var localConfigFile = Path.Combine(dvcFolder, "config.local");
            if (File.Exists(localConfigFile))
            {
                config.LocalConfigFile = localConfigFile;
                config.LoadPropertiesFromFile(localConfigFile, DvcConfigPropertySource.Local);
            }

            return config;
        }

        private void LoadPropertiesFromFile(string file, DvcConfigPropertySource source)
        {
            var lines = File.ReadAllLines(file);
            string currentCategory = "";

            foreach (var line in lines)
            {
                var tline = line.TrimEnd();
                if (tline.StartsWith('['))
                {
                    currentCategory = tline.Trim('[', ']').Trim();
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                {
                    Console.WriteLine($"Invalid line in config file: {line}");
                    continue;
                }

                var propertyName = $"{currentCategory}.{parts[0].Trim()}";
                var propertyValue = parts[1].Trim();

                Properties.Add(new DvcConfigProperty(propertyName, propertyValue, source));
            }
        }

        public string? GetCacheDirAbsolutePath()
        {
            var cacheDirProperty = Properties.FirstOrDefault(p => p.Name == "cache.dir");
            if (cacheDirProperty == null)
                return null;

            if (Path.IsPathFullyQualified(cacheDirProperty.Value))
                return cacheDirProperty.Value;

            var dir = Path.GetDirectoryName(ProjectConfigFile);
            if (dir == null)
                return null;
            
            var absolutePath = Path.GetFullPath(Path.Combine(dir, cacheDirProperty.Value));

            return absolutePath;
        }
    }

    internal record DvcConfigProperty(string Name, string Value, DvcConfigPropertySource source)
    {
    }

    internal enum DvcConfigPropertySource
    {
        Default,
        Local,
        Project,
        Global,
        System
    }
}
