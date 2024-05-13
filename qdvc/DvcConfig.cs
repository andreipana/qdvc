using System;
using System.Collections.Generic;
using System.Linq;
using qdvc.Infrastructure;

namespace qdvc
{
    public class DvcConfig
    {
        public Dictionary<string, DvcConfigProperty> Properties { get; } = new();

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

            var projectConfigFile = IOContext.FileSystem.Path.Combine(dvcFolder, "config");
            if (IOContext.FileSystem.File.Exists(projectConfigFile))
            {
                config.ProjectConfigFile = projectConfigFile;
                config.LoadPropertiesFromFile(projectConfigFile, DvcConfigPropertySource.Project);
            }

            var localConfigFile = IOContext.FileSystem.Path.Combine(dvcFolder, "config.local");
            if (IOContext.FileSystem.File.Exists(localConfigFile))
            {
                config.LocalConfigFile = localConfigFile;
                config.LoadPropertiesFromFile(localConfigFile, DvcConfigPropertySource.Local);
            }

            return config;
        }

        private void LoadPropertiesFromFile(string file, DvcConfigPropertySource source)
        {
            var lines = IOContext.FileSystem.File.ReadAllLines(file);
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

                Properties[propertyName] = (new DvcConfigProperty(propertyName, propertyValue, source));
            }
        }

        public string? GetCacheDirAbsolutePath()
        {
            var cacheDirProperty = Properties.GetValueOrDefault("cache.dir");
            if (cacheDirProperty == null)
                return null;

            if (IOContext.FileSystem.Path.IsPathFullyQualified(cacheDirProperty.Value))
                return cacheDirProperty.Value;

            var dir = IOContext.FileSystem.Path.GetDirectoryName(ProjectConfigFile ?? LocalConfigFile);
            if (dir == null)
                return null;
            
            var absolutePath = IOContext.FileSystem.Path.GetFullPath(IOContext.FileSystem.Path.Combine(dir, cacheDirProperty.Value));

            return absolutePath;
        }
    }

    public record DvcConfigProperty(string Name, string Value, DvcConfigPropertySource Source)
    {
    }

    public enum DvcConfigPropertySource
    {
        Default,
        Local,
        Project,
        Global,
        System
    }
}
