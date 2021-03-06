﻿using System;

using FluentAssertions;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NClap.Utilities;

namespace NClap.Tests.Utilities
{
    [TestClass]
    public class MutableFieldInfoTests
    {
        class TestObject<T>
        {
        #pragma warning disable 0649 // Field is never assigned to, and will always have its default value
            public T Value;
        #pragma warning restore 0649
        }

        [TestMethod]
        public void ConversionFails()
        {
            var prop = new MutableFieldInfo(typeof(TestObject<Guid>).GetField("Value"));
            var obj = new TestObject<Guid>();
            Action setter = () => prop.SetValue(obj, 3.0);
            setter.ShouldThrow<ArgumentException>();
        }

        [TestMethod]
        public void ConversionSucceeds()
        {
            var prop = new MutableFieldInfo(typeof(TestObject<int>).GetField("Value"));
            var obj = new TestObject<int>();
            Action setter = () => prop.SetValue(obj, 3L);
            setter.ShouldNotThrow();
        }
    }
}
