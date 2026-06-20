using EmailManager.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace EmailManager.Infrastructure.Services;

/// <summary>
/// AES-256-CBC tabanlı şifreleme servisi.
/// Her şifreleme işleminde rastgele 16-byte IV üretilir.
/// Çıktı formatı: "BASE64(IV):BASE64(CIPHERTEXT)"
/// Anahtar appsettings'teki "EncryptionKey" (Base64, 32 byte) değerinden okunur.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private const int KeySize = 32;   // AES-256 = 32 byte
    private const int BlockSize = 16; // AES CBC block size

    public AesEncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["EncryptionKey"]
            ?? throw new InvalidOperationException(
                "EncryptionKey appsettings.json veya ortam değişkenlerinde tanımlanmamış.");

        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != KeySize)
            throw new InvalidOperationException(
                $"EncryptionKey 32 byte (Base64) olmalıdır. Mevcut: {_key.Length} byte.");
    }

    /// <inheritdoc/>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV(); // Her mesajda farklı, rastgele IV

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var ivBase64 = Convert.ToBase64String(aes.IV);
        var cipherBase64 = Convert.ToBase64String(cipherBytes);

        return $"{ivBase64}:{cipherBase64}";
    }

    /// <inheritdoc/>
    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return string.Empty;

        var parts = cipherText.Split(':', 2);
        if (parts.Length != 2)
            throw new FormatException("Geçersiz şifreli metin formatı. Beklenen: 'IV:Cipher'");

        var iv = Convert.FromBase64String(parts[0]);
        var cipher = Convert.FromBase64String(parts[1]);

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
