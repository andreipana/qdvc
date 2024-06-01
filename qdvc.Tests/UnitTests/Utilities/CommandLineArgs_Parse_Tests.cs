using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Utilities;

namespace qdvc.Tests.UnitTests.Utilities
{
    [TestClass]
    public class CommandLineArgs_Parse_Tests
    {
        [TestMethod]
        public void Constructor_DetectsUsernameAndPassword()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var args = CommandLineArguments.Parse(["-u", "andrei", "-p", "asdfgh", "Data"]);
#pragma warning restore CS0618 // Type or member is obsolete

            args.Username.Should().Be("andrei");
            args.Password.Should().Be("asdfgh");
        }

        [TestMethod]
        [DataRow(@"Data\assets", new[] { @"Data\assets" })]
        [DataRow(@"Data\assets Data\sources", new[] { @"Data\assets", @"Data\sources" })]
        [DataRow(@"Data\assets Data\file.dvc", new[] { @"Data\assets", @"Data\file.dvc" })]
        [DataRow(@"-u andrei -p asdfgh Data\assets Data\sources", new[] { @"Data\assets", @"Data\sources" })]
        public void Constructor_DetectsPaths(string input, string[] expectedPaths)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var args = CommandLineArguments.Parse(input.Split(' '));
#pragma warning restore CS0618 // Type or member is obsolete

            args.Paths.Should().BeEquivalentTo(expectedPaths);
        }

        [TestMethod]
        [DataRow(@"Data\assets")]
        [DataRow(@"Data\assets Data\sources")]
        [DataRow(@"Data\assets Data\file.dvc")]
        [DataRow(@"-u andrei -p asdfgh Data\assets Data\sources")]
        public void CommandIsPull_WhenNoCommandSpecified(string input)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var args = CommandLineArguments.Parse(input.Split(' '));
#pragma warning restore CS0618 // Type or member is obsolete

            args.Command.Should().Be("pull");
        }

        [TestMethod]
        [DataRow(@"status Data\assets", "status")]
        [DataRow(@"pull Data\assets Data\sources", "pull")]
        [DataRow(@"add Data\assets Data\file.dvc", "add")]
        [DataRow(@"push -u andrei -p asdfgh Data\assets Data\sources", "push")]
        public void CommandIsDetected_WhenItIsTheFirstArgument(string input, string expectedCommand)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            var args = CommandLineArguments.Parse(input.Split(' '));
#pragma warning restore CS0618 // Type or member is obsolete

            args.Command.Should().Be(expectedCommand);
        }
    }
}
