using System.Threading.Tasks;
using DocSearchAIO.Scheduler;
using FluentAssertions;
using Xunit;

namespace DocSearchAIO_Test
{
    public class EncryptionServiceTest
    {
        private readonly EncryptionService _encryptionService;

        private const string StringToHash = "the quick brown fox jumps over the lazy dog";

        private const string Hash = "05c6e08f1d9fdafa03147fcb8f82f124c76d2f70e3d989dc8aadb5e7d7450bec";

        public EncryptionServiceTest()
        {
            _encryptionService = new EncryptionService();
        }

        [Fact]
        public void Generate_Hash_of_a_string_in_synchronous_way()
        {
            var result = _encryptionService.ConvertToStringFromByteArray(_encryptionService.ComputeHashSync(StringToHash));

            Hash.Should().Match(result);
        }

        [Fact]
        public async Task Generate_Hash_of_a_string_in_asynchronous_way()
        {
            var bt = await _encryptionService.ComputeHashAsync(StringToHash);
            
            var result = _encryptionService.ConvertToStringFromByteArray(bt);

            Hash.Should().Match(result);

        }
        
    }
}