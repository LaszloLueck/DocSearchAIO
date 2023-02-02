using System;
using System.Collections.Generic;
using System.Linq;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using DocSearchAIO.Utilities;
using FluentAssertions;
using LanguageExt;
using LanguageExt.SomeHelp;
using Xunit;

namespace DocSearchAIO_Test;

public class CSharpFunctionalHelperTest
{

    [Fact]
    public void TestIfNullWithValue()
    {
        int? value = 5;
        int noneValue = 0;

        var result = value.IfNull(noneValue);

        Assert.Equal(value, result);
    }

    [Fact]
    public void TestIfNullWithoutValue()
    {
        int? value = null;
        int noneValue = 0;

        var result = value.IfNull(noneValue);

        Assert.Equal(noneValue, result);
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
    public void Check_IfTrueFalse_of_any_way()
    {
        var retVal = true.IfTrueFalse(() => throw new FieldAccessException("The expected result should be true"),
            () => true);

        Assert.True(retVal);
    }

    [Fact]
    public void Resolve_Nullable_With_Value()
    {
        var toTest = "the quick brown fox jumps over the lazy dog";
#nullable enable
        static string GenerateNullable(string toTest)
        {
            return toTest;
        }
#nullable disable

        var result = GenerateNullable(toTest).ResolveNullable("alternative", (a, _) => a);

        result.Should().Be(toTest);
    }

    [Fact]
    public void Resolve_Nullable_With_Alternative()
    {
#nullable enable
        static string? GenerateNullable()
        {
            return null;
        }
#nullable disable

        var result = GenerateNullable().ResolveNullable("alternative", (a, _) => a);

        result.Should().Be("alternative");
    }

    [Fact]
    public void Filter_Maybe_None_from_IEnumerable_wrapped_on_Source()
    {
        var materializer = ActorMaterializer.Create(ActorSystem.Create("testActorsystem"));
        IEnumerable<Option<int>> list = new[]
            { 8.ToSome(), Option<int>.None, 5.ToSome(), 1.ToSome(), Option<int>.None };

        Source<IEnumerable<Option<int>>, NotUsed> source = Source.From(new List<IEnumerable<Option<int>>> { list });

        var result = source
            .WithMaybeFilter()
            .RunWith(Sink.Seq<IEnumerable<int>>(), materializer)
            .Result
            .Flatten();

        materializer.Dispose();

        result.Count().Should().Be(3);
        result.First().Should().Be(8);
        result.Last().Should().Be(1);
    }

    [Fact]
    public void Test_ForEach_on_Tuple()
    {
        IEnumerable<(int, int)> list = new List<(int, int)>
            { (1, 1), (2, 2), (3, 3) };

        var result = 0;
        list.ForEach((t1, t2) => result += t1);

        result.Should().Be(6);
    }

    [Fact]
    public void Test_IfTrueFalse_with_positive_value_and_return_value()
    {
        var result = true.IfTrueFalse(
            () => throw new FieldAccessException("The expected result should be true"),
            () => "trueValue");

        "trueValue".Should().Be(result);
    }

    [Fact]
    public void Test_IfTrueFalse_with_negative_value_and_return_value()
    {
        var result = false.IfTrueFalse(
            () => "falseValue",
            () => throw new FieldAccessException("The expected result should be false")
        );

        "falseValue".Should().Be(result);
    }

}