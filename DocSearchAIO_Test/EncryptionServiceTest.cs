using System.Threading.Tasks;
using DocSearchAIO.Classes;
using DocSearchAIO.Utilities;
using FluentAssertions;
using Xunit;

namespace DocSearchAIO_Test;

public class EncryptionServiceTest
{

    private const string StringToHash = "the quick brown fox jumps over the lazy dog";

    private const string Hash = "05c6e08f1d9fdafa03147fcb8f82f124c76d2f70e3d989dc8aadb5e7d7450bec";

    [Fact]
    public async Task Generate_Hash_of_a_string_in_asynchronous_way()
    {
        var bt = await EncryptionService.ComputeHashAsync(TypedHashedInputString.New(StringToHash));

        var result = await EncryptionService.ConvertToStringFromByteArray(bt);

        Hash.Should().Match(result);

    }

}