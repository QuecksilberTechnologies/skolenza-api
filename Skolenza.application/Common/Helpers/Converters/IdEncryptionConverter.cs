using skolenza.application.Interfaces.IEncryptionService;

namespace skolenza.application.Common.Helpers.Converters
{
    public class IdEncryptionConverter
    {
        private readonly IEncryptionService _encryptionService;

        public IdEncryptionConverter(IEncryptionService encryptionService)
        {
            _encryptionService = encryptionService;
        }

        public string Convert(long id, string institutionKey)
        {
            if (id == 0) return string.Empty;
            return _encryptionService.Encrypt(id.ToString(), institutionKey);
        }

        public string? ConvertNullable(long? id, string institutionKey)
        {
            if (id == null || id == 0) return string.Empty;
            return _encryptionService.Encrypt(id.Value.ToString(), institutionKey);
        }

        public long? Decrypt(string? encryptedId, string institutionKey)
        {
            if (string.IsNullOrEmpty(encryptedId)) return null;
            var decrypted = _encryptionService.Decrypt(encryptedId, institutionKey);
            return long.TryParse(decrypted, out var val) ? val : (long?)null;
        }
    }
}
