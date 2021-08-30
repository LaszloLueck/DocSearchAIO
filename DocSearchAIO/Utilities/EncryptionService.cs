using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DocSearchAIO.Utilities
{
    public class EncryptionService
    {
        private readonly SHA256Managed _sha256;
        public EncryptionService()
        {
            _sha256 = new SHA256Managed();
        }

        public byte[] ComputeHashSync(string input)
        {
            return _sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        public async Task<byte[]> ComputeHashAsync(string input)
        {
            return await _sha256.ComputeHashAsync(new MemoryStream(Encoding.UTF8.GetBytes(input)));
        }

        public string ConvertToStringFromByteArray(byte[] array)
        {
            return array.Select(bt => bt.ToString("x2")).Concat();
        }
        
    }
}