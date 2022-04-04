using System.Security.Cryptography;
using System.Text;

namespace DocSearchAIO.Utilities;

public static class EncryptionService
{
    public static async Task<byte[]> ComputeHashAsync(string input)
    {
        return await Task.Run(() => SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    public static string ConvertToStringFromByteArray(byte[] array)
    {
        return array.Select(bt => bt.ToString("x2")).Concat();
    }
        
}