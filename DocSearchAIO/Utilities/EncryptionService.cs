using System.Security.Cryptography;
using System.Text;
using Akka.Util.Internal;
using DocSearchAIO.Classes;
using LanguageExt;

namespace DocSearchAIO.Utilities;

public static class EncryptionService
{
    public static async Task<byte[]> ComputeHashAsync(TypedHashedInputString input)
    {
        return await Task.Run(() => SHA256.HashData(Encoding.UTF8.GetBytes(input.Value)));
    }

    public static async Task<string> ConvertToStringFromByteArray(byte[] array)
    {
         var fut = await array
             .Map(async bt => await Task.Run(() => bt.ToString("x2")))
             .SequenceSerial();
         return fut.Concat();
    }
}