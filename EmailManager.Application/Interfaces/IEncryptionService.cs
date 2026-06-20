namespace EmailManager.Application.Interfaces;

/// <summary>
/// Metin şifreleme ve çözme sözleşmesi.
/// Implementasyon AES-256-CBC kullanır; her şifreleme için rastgele IV üretilir.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Düz metni AES-256 ile şifreler.
    /// </summary>
    /// <returns>"IV_BASE64:CIPHERTEXT_BASE64" formatında şifreli metin.</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// "IV_BASE64:CIPHERTEXT_BASE64" formatındaki şifreli metni çözer.
    /// </summary>
    string Decrypt(string cipherText);
}
