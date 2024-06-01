using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc.Infrastructure
{
    public static class SystemContext
    {
        private static IConsole? _Console;

        private static IConsole ConsoleInstance => _Console ?? throw new InvalidOperationException("SystemContext is not initialized.");

        public static void Initialize()
        {
            _Console = new StandardConsole();
        }

        public static void Initialize(IConsole console)
        {
            _Console = console ?? throw new ArgumentNullException(nameof(console));
        }

        public static class Console
        {
            public static void StdOutWrite(string? message) => ConsoleInstance.StdOutWrite(message);

            public static void StdErrWrite(string? message) => ConsoleInstance.StdErrWrite(message);

            public static void StdOutWriteLine(string? message) => ConsoleInstance.StdOutWriteLine(message);

            public static void StdErrWriteLine(string? message) => ConsoleInstance.StdErrWriteLine(message);

            public static bool IsStdOutRedirected => ConsoleInstance.IsStdOutRedirected;

            public static bool IsStdErrRedirected => ConsoleInstance.IsStdErrRedirected;
        }
    }

    public interface IConsole
    {
        public void StdOutWrite(string? message);

        public void StdErrWrite(string? message);

        public void StdOutWriteLine(string? message);

        public void StdErrWriteLine(string? message);

        public bool IsStdOutRedirected { get; }

        public bool IsStdErrRedirected { get; }
    }

    public class StandardConsole : IConsole
    {
        public void StdOutWrite(string? message) => Console.Write(message);

        public void StdErrWrite(string? message) => Console.Error.Write(message);

        public void StdOutWriteLine(string? message) => Console.WriteLine(message);

        public void StdErrWriteLine(string? message) => Console.Error.WriteLine(message);

        public bool IsStdOutRedirected => Console.IsOutputRedirected;

        public bool IsStdErrRedirected => Console.IsErrorRedirected;
    }
}
