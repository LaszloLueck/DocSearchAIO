using System.Security.Cryptography;
using System.Text;
using DocSearchAIO.Classes;
using LanguageExt;

namespace DocSearchAIO.Utilities;

public static class EncryptionService
{
    public static async Task<byte[]> ComputeHashAsync(TypedHashedInputString input)
    {
        return await Task.FromResult(SHA256.HashData(Encoding.UTF8.GetBytes(input.Value)));
    }

    public static async Task<string> ConvertToStringFromByteArray(byte[] array)
    {
        return (await array.Map(async bt => await Task.FromResult(bt.ToString("x2"))).SequenceParallel()).Concat();
    }
}