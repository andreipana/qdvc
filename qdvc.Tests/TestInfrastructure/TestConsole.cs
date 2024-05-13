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
        private StringBuilder _StdErr = new();
        private StringBuilder _StdOut = new();

        private string? _StdErrString;
        private string? _StdOutString;

        public string StdErr => _StdErrString ??= _StdErr.ToString();
        public string StdOut => _StdOutString ??= _StdOut.ToString();

        public void StdErrWriteLine(string message)
        {
            _StdErrString = null;
            _StdErr.AppendLine(message);
            System.Console.Error.WriteLine(message);
        }

        public void StdOutWriteLine(string message)
        {
            _StdOutString = null;
            _StdOut.AppendLine(message);
            System.Console.WriteLine(message);
        }
    }
}
