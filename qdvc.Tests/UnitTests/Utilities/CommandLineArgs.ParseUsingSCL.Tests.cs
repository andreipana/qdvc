using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Infrastructure;
using qdvc.Tests.TestInfrastructure;
using qdvc.Utilities;
using System;
using System.Linq;

namespace qdvc.Tests.UnitTests.Utilities
{
    [TestClass]
    public class CommandLineArgs_ParseUsingSCL_Tests
    {
        ITestableConsole Console = new TestConsole();

        public CommandLineArgs_ParseUsingSCL_Tests()
        {
            SystemContext.Initialize(Console);
        }

        [TestMethod]
        public void ParseUsingSCL_DetectsUsernameAndPassword()
        {
            var args = CommandLineArguments.ParseUsingSCL(["pull", "-u", "andrei", "-p", "asdfgh", "Data"]);

            args.Username.Should().Be("andrei");
            args.Password.Should().Be("asdfgh");
        }

        [TestMethod]
        [DataRow(@"pull Data\assets", new[] { @"Data\assets" })]
        [DataRow(@"pull Data\assets Data\sources", new[] { @"Data\assets", @"Data\sources" })]
        [DataRow(@"pull Data\assets Data\file.dvc", new[] { @"Data\assets", @"Data\file.dvc" })]
        [DataRow(@"pull -u andrei -p asdfgh Data\assets Data\sources", new[] { @"Data\assets", @"Data\sources" })]
        public void ParseUsingSCL_DetectsPaths(string input, string[] expectedPaths)
        {
            var args = CommandLineArguments.ParseUsingSCL(input.Split(' '));

            args.Paths.Should().BeEquivalentTo(expectedPaths);
        }

        [TestMethod]
        [DataRow(@"Data\assets")]
        [DataRow(@"Data\assets Data\sources")]
        [DataRow(@"Data\assets Data\file.dvc")]
        [DataRow(@"-u andrei -p asdfgh Data\assets Data\sources")]
        public void CommandIsNull_WhenNoCommandSpecified(string input)
        {
            var args = CommandLineArguments.ParseUsingSCL(input.Split(' '));

            args.Should().NotBeNull();
            args.Command.Should().BeNull();
        }


        [TestMethod]
        [DataRow(@"status Data\assets", "status")]
        [DataRow(@"pull Data\assets Data\sources", "pull")]
        [DataRow(@"add Data\assets Data\file.dvc", "add")]
        [DataRow(@"push -u andrei -p asdfgh Data\assets Data\sources", "push")]
        public void CommandIsDetected_WhenItIsTheFirstArgument(string input, string expectedCommand)
        {
            var args = CommandLineArguments.ParseUsingSCL(input.Split(' '));

            args.Command.Should().Be(expectedCommand);
        }

        [TestMethod]
        [DataRow(@"Data\assets")]
        [DataRow(@"Data\assets Data\sources")]
        [DataRow(@"Data\assets Data\file.dvc")]
        [DataRow(@"-u andrei -p asdfgh Data\assets Data\sources")]
        public void ConsoleOutput_When_NoCommandSpecified(string input)
        {
            var args = CommandLineArguments.ParseUsingSCL(input.Split(' '));

            Console.StdErr.Should().Contain($"Required command was not provided.{Environment.NewLine}");
        }

        [TestMethod]
        //Below, `improve` is an invalid command.
        [DataRow(@"improve Data\assets")]
        [DataRow(@"improve Data\assets Data\sources")]
        [DataRow(@"improve Data\assets Data\file.dvc")]
        [DataRow(@"improve -u andrei -p asdfgh Data\assets Data\sources")]
        public void ConsoleOutput_When_InvalidCommandSpecified(string input)
        {
            var args = CommandLineArguments.ParseUsingSCL(input.Split(' '));

            Console.StdErr.Should().Contain($"Required command was not provided.{Environment.NewLine}");
        }
    }
}
