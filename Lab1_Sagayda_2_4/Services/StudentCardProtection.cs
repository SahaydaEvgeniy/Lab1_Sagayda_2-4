using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Storage;

namespace Lab1_Sagayda_2_4.Services;

public sealed class StudentCardProtection(ISecureStorage secureStorage) : IStudentCardProtection
{
    private const string KeyName = "student_card_aes256_v1";
    private readonly SemaphoreSlim _keyLock = new(1, 1);

    public async Task<string> EncryptAsync(string value, Guid studentId)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Length > 64) throw new ArgumentException("Номер квитка не може перевищувати 64 символи.");
        var key = await GetKeyAsync(create: true);
        var plain = Encoding.UTF8.GetBytes(value);
        try
        {
            var nonce = RandomNumberGenerator.GetBytes(12);
            var tag = new byte[16];
            var cipher = new byte[plain.Length];
            using var aes = new AesGcm(key, tagSizeInBytes: 16);
            aes.Encrypt(nonce, plain, cipher, tag, AssociatedData(studentId));
            return "v1:" + Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    public async Task<string> DecryptAsync(string? encryptedValue, Guid studentId)
    {
        if (string.IsNullOrEmpty(encryptedValue)) return string.Empty;
        if (!encryptedValue.StartsWith("v1:", StringComparison.Ordinal))
            throw new CryptographicException("Непідтримуваний формат зашифрованого номера.");
        byte[] envelope;
        try { envelope = Convert.FromBase64String(encryptedValue[3..]); }
        catch (FormatException ex) { throw new CryptographicException("Пошкоджено зашифрований номер.", ex); }
        if (envelope.Length <= 28) throw new CryptographicException("Пошкоджено зашифрований номер.");
        var key = await GetKeyAsync(create: false);
        var plain = new byte[envelope.Length - 28];
        try
        {
            using var aes = new AesGcm(key, tagSizeInBytes: 16);
            aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain, AssociatedData(studentId));
            return Encoding.UTF8.GetString(plain);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    private async Task<byte[]> GetKeyAsync(bool create)
    {
        await _keyLock.WaitAsync();
        try
        {
            var stored = await secureStorage.GetAsync(KeyName);
            if (stored is null)
            {
                if (!create) throw new CryptographicException("Ключ розшифрування відсутній у захищеному сховищі.");
                var generated = RandomNumberGenerator.GetBytes(32);
                try
                {
                    await secureStorage.SetAsync(KeyName, Convert.ToBase64String(generated));
                    return generated.ToArray();
                }
                finally { CryptographicOperations.ZeroMemory(generated); }
            }
            byte[] key;
            try { key = Convert.FromBase64String(stored); }
            catch (FormatException ex) { throw new CryptographicException("Ключ шифрування пошкоджено.", ex); }
            if (key.Length == 32) return key;
            CryptographicOperations.ZeroMemory(key);
            throw new CryptographicException("Ключ шифрування має некоректну довжину.");
        }
        finally { _keyLock.Release(); }
    }

    private static byte[] AssociatedData(Guid studentId) => Encoding.UTF8.GetBytes($"StudentCardNumber:{studentId:D}:v1");
}
