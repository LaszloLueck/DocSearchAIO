using System.Security.Cryptography;
using System.Text;
using DocSearchAIO.Classes;

namespace DocSearchAIO.Utilities;

public static class EncryptionService
{
    public static async Task<byte[]> ComputeHashAsync(TypedHashedInputString input)
    {
        return await Task.Run(() => SHA256.HashData(Encoding.UTF8.GetBytes(input.Value)));
    }

    public static string ConvertToStringFromByteArray(byte[] array)
    {
        return array.Map(bt => bt.ToString("x2")).Concat();
    }
        
}