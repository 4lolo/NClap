﻿using System.Linq;
using System.Reflection;
using FluentAssertions;
using FluentAssertions.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NClap.Parser;

namespace NClap.Tests
{
    [TestClass]
    public class AssemblyTests
    {
        public static readonly Assembly AssemblyUnderTest = typeof(CommandLineParser).Assembly;

        [TestMethod]
        public void VerifyNamespace()
        {
            const string expectedNs = nameof(NClap);
            const string expectedNsWithDot = expectedNs + ".";

            foreach (var type in AllTypes.From(AssemblyUnderTest).Where(t => t.IsPublic))
            {
                var ns = type.Namespace;
                if (ns != expectedNs)
                {
                    ns.Should().StartWith(expectedNsWithDot);
                }
            }
        }
    }
}
