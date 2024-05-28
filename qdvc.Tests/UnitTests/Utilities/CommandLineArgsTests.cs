﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using qdvc.Utilities;

namespace qdvc.Tests.UnitTests.Utilities
{
    [TestClass]
    public class CommandLineArgsTests
    {
        [TestMethod]
        public void Constructor_DetectsUsernameAndPassword()
        {
            var args = CommandLineArguments.Parse(new[] { "-u", "andrei", "-p", "asdfgh", "Data" });

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
            var args = CommandLineArguments.Parse(input.Split(' '));

            args.Paths.Should().BeEquivalentTo(expectedPaths);
        }

        [TestMethod]
        [DataRow(@"Data\assets")]
        [DataRow(@"Data\assets Data\sources")]
        [DataRow(@"Data\assets Data\file.dvc")]
        [DataRow(@"-u andrei -p asdfgh Data\assets Data\sources")]
        public void CommandIsPull_WhenNoCommandSpecified(string input)
        {
            var args = CommandLineArguments.Parse(input.Split(' '));

            args.Command.Should().Be("pull");
        }

        [TestMethod]
        [DataRow(@"status Data\assets", "status")]
        [DataRow(@"pull Data\assets Data\sources", "pull")]
        [DataRow(@"add Data\assets Data\file.dvc", "add")]
        [DataRow(@"push -u andrei -p asdfgh Data\assets Data\sources", "push")]
        public void CommandIsDetected_WhenItIsTheFirstArgument(string input, string expectedCommand)
        {
            var args = CommandLineArguments.Parse(input.Split(' '));

            args.Command.Should().Be(expectedCommand);
        }
    }
}
