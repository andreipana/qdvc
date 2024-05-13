using System.Linq;
using System.Reflection;

namespace qdvc.Utilities
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
