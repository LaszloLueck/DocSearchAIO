using System;
using System.Collections.Generic;
using CSharpFunctionalExtensions;
using Xunit;
using DocSearchAIO.Utilities;
using FluentAssertions;

namespace DocSearchAIO_Test
{
    public class CSharpFunctionalHelperTest
    {
        [Fact]
        public void Match_Test_With_KeyValuePair_Deconstruction_On_Some_And_Return_Value()
        {
            var tValue =
                Maybe<KeyValuePair<int, string>>.From(new KeyValuePair<int, string>(42, "Matrix"));

            var returnValue = tValue.Match(
                (intValue, stringValue) => (intValue, stringValue),
                () => throw new FieldAccessException("value is none")
            );

            returnValue.Item1.Should().Be(42);
            returnValue.Item2.Should().Be("Matrix");
        }

        [Fact]
        public void Match_Test_With_KeyValuePair_Deconstruction_On_None_And_Return_None_Value()
        {
            var tValue = Maybe<KeyValuePair<int, string>>.None;
            var returnValue = tValue.Match(
                (_, _) => throw new FieldAccessException("value is none"),
                () => (-1, "No Matrix")
            );

            returnValue.Item1.Should().Be(-1);
            returnValue.Item2.Should().Be("No Matrix");
        }

        [Fact]
        public void Match_Test_With_KeyValuePair_Deconstruction_With_Void_Return_On_Some()
        {
            var tValue =
                Maybe<KeyValuePair<int, string>>.From(new KeyValuePair<int, string>(42, "Matrix"));

            tValue.Match(
                (intValue, stringValue) =>
                {
                    intValue.Should().Be(42);
                    stringValue.Should().Be("Matrix");
                },
                () => throw new FieldAccessException("value is none")
            );
        }

        [Fact]
        public void Match_Test_With_KeyValuePair_Deconstruction_With_Void_Return_On_None()
        {
            var tValue = Maybe<KeyValuePair<int, string>>.None;

            tValue.Match(
                (_, _) => throw new FieldAccessException("value should be none"),
                () => Assert.True(true)
            );
        }
    }
}