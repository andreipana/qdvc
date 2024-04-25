using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace qdvc
{
    internal static class VersionUtils
    {
        public const string UnknownVersionMarker = "?";

        public static string GetAssemblyInformationalVersion()
        {
            var value = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (value == null)
                return UnknownVersionMarker;

            return value.Split('+').FirstOrDefault() ?? UnknownVersionMarker;
        }
    }
}
