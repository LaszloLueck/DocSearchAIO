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
                () => (-1, "")
            );

            returnValue.Item1.Should().Be(42);
            returnValue.Item2.Should().Be("Matrix");

        }
        
    }
}