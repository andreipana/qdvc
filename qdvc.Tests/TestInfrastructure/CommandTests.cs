using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qdvc.Tests.TestInfrastructure
{
    public abstract class CommandTests
    {
        private protected ITestableConsole Console;

        public CommandTests()
        {
            Console = new TestConsole();
            SystemContext.Initialize(Console);
        }
    }
}
