using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using qdvc.Infrastructure;

namespace qdvc.Tests.TestInfrastructure
{
    internal interface ITestableConsole : IConsole
    {
        string StdErr { get; }
        string StdOut { get; }
    }

    internal class TestConsole : ITestableConsole
    {
        public bool IsRedirected { get; set; }

        public bool OutputToStandard { get; set; } = true;

        private StringBuilder _StdErr = new();
        private StringBuilder _StdOut = new();

        private string? _StdErrString;
        private string? _StdOutString;

        public string StdErr => _StdErrString ??= _StdErr.ToString();
        public string StdOut => _StdOutString ??= _StdOut.ToString();

        public bool IsStdOutRedirected => IsRedirected;

        public bool IsStdErrRedirected => IsRedirected;

        public void StdErrWrite(string? message)
        {
            _StdErrString = null;
            _StdErr.Append(message);

            if (OutputToStandard)
                Console.Error.Write(message);
        }

        public void StdErrWriteLine(string? message)
        {
            _StdErrString = null;
            _StdErr.AppendLine(message);

            if (OutputToStandard)
                Console.Error.WriteLine(message);
        }

        public void StdOutWrite(string? message)
        {
            _StdOutString = null;
            _StdOut.Append(message);

            if (OutputToStandard)
                Console.Write(message);
        }

        public void StdOutWriteLine(string? message)
        {
            _StdOutString = null;
            _StdOut.AppendLine(message);

            if (OutputToStandard)
                Console.WriteLine(message);
        }
    }
}
