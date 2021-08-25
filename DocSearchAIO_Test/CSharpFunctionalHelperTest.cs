using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        [Fact]
        public void Check_if_a_dictionary_contains_key_and_process_the_result()
        {
            var kv1 = new KeyValuePair<int, string>(1, "A");
            var kv2 = new KeyValuePair<int, string>(2, "B");
            IDictionary<int, string> tDictionary = new Dictionary<int, string>();
            tDictionary.Add(kv1);
            tDictionary.Add(kv2);

            tDictionary.DictionaryKeyExistsAction(1, (intKey, stringValue) =>
            {
                1.Should().Be(intKey);
                "A".Should().Be(stringValue);
            });
        }
        
        [Fact]
        public void Check_if_a_dictionary_contains_not_a_key_and_do_nothing()
        {
            var kv1 = new KeyValuePair<int, string>(1, "A");
            var kv2 = new KeyValuePair<int, string>(2, "B");
            IDictionary<int, string> tDictionary = new Dictionary<int, string>();
            tDictionary.Add(kv1);
            tDictionary.Add(kv2);

            tDictionary.DictionaryKeyExistsAction(3, (_, _) => throw new FieldAccessException("value should be none"));
            
            Assert.True(true);
        }

        [Fact]
        public async Task Check_WhenAll_Function()
        {
            var taskA = Task.Run(() => "A");
            var taskB = Task.Run(() => "B");
            var taskC = Task.Run(() => "C");

            var testList = new List<Task<string>> { taskA, taskB, taskC };

            var resultList = await testList.WhenAll();
            var result = string.Join(" ", resultList);
            "A B C".Should().Match(result);
        }

        [Fact]
        public void Check_IfTrueFalse_of_any_way()
        {
            var retVal = true.IfTrueFalse(() => throw new FieldAccessException("The expected result should be true"),
                () => true);
            
            Assert.True(retVal);
        }
        
    }
}