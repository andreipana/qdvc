using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc
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
            public static void StdErrWriteLine(string message)
            {
                ConsoleInstance.StdErrWriteLine(message);
            }

            public static void StdOutWriteLine(string message)
            {
                ConsoleInstance.StdOutWriteLine(message);
            }
        }
    }

    public interface IConsole
    {
        public void StdOutWriteLine(string message);
        public void StdErrWriteLine(string message);
    }

    public class StandardConsole : IConsole
    {
        public void StdErrWriteLine(string message)
        {
            Console.Error.Write(message);
        }

        public void StdOutWriteLine(string message)
        {
            Console.Write(message);
        }
    }
}
