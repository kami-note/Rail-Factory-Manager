using System.Security.Cryptography;
using System.Text.Json;
using RailFactory.BuildingBlocks.Integrations;

namespace RailFactory.Tenancy.Api.Infrastructure;

public sealed class CredentialEncryptionService
{
    private readonly byte[] _kek;

    public CredentialEncryptionService(IConfiguration configuration)
    {
        var kekBase64 = configuration["Tenancy:Kek"]
            ?? throw new InvalidOperationException(
                "Tenancy:Kek (env: TENANCY__KEK) is required for credential encryption.");

        _kek = Convert.FromBase64String(kekBase64);

        if (_kek.Length < 32)
            throw new InvalidOperationException(
                $"Tenancy:Kek must be at least 32 bytes (256 bits). Current length: {_kek.Length} bytes.");
    }

    public (byte[] Ciphertext, byte[] WrappedDek, byte[] Iv) EncryptCredentials(
        Dictionary<string, string> credentials)
    {
        // Serialize credentials to JSON bytes; zero the buffer after use to limit heap exposure.
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(credentials);
        try
        {
            var dek = RandomNumberGenerator.GetBytes(32);
            var iv = RandomNumberGenerator.GetBytes(12); // 96-bit nonce for GCM
            var tag = new byte[16];
            var ciphertext = new byte[plaintext.Length];

            using var aes = new AesGcm(dek, tagSizeInBytes: 16);
            aes.Encrypt(iv, plaintext, ciphertext, tag);

            // [tag(16)] + [ciphertext]
            var combined = new byte[16 + ciphertext.Length];
            tag.CopyTo(combined, 0);
            ciphertext.CopyTo(combined, 16);

            // Wrap the DEK with the KEK: [dekIv(12)] + [dekTag(16)] + [encryptedDek(32)]
            var dekIv = RandomNumberGenerator.GetBytes(12);
            var dekTag = new byte[16];
            var encryptedDek = new byte[dek.Length];
            using var kekAes = new AesGcm(_kek, tagSizeInBytes: 16);
            kekAes.Encrypt(dekIv, dek, encryptedDek, dekTag);

            var wrappedDek = new byte[12 + 16 + 32];
            dekIv.CopyTo(wrappedDek, 0);
            dekTag.CopyTo(wrappedDek, 12);
            encryptedDek.CopyTo(wrappedDek, 28);

            CryptographicOperations.ZeroMemory(dek);

            return (combined, wrappedDek, iv);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public SecureCredentials DecryptCredentials(
        byte[] encryptedCredentials,
        byte[] credentialsDek,
        byte[] credentialsIv)
    {
        // Unwrap the DEK using the KEK
        var dekIv = credentialsDek[..12];
        var dekTag = credentialsDek[12..28];
        var encryptedDek = credentialsDek[28..];
        var dek = new byte[32];
        using var kekAes = new AesGcm(_kek, tagSizeInBytes: 16);
        kekAes.Decrypt(dekIv, encryptedDek, dekTag, dek);

        // Decrypt the credentials
        var tag = encryptedCredentials[..16];
        var ciphertext = encryptedCredentials[16..];
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(dek, tagSizeInBytes: 16);
        aes.Decrypt(credentialsIv, ciphertext, tag, plaintext);

        CryptographicOperations.ZeroMemory(dek);

        // Deserialize directly from the byte span; build SecureCredentials (byte[] per value)
        // so the caller can zero the values when done. The intermediate string dict from
        // JsonSerializer is transient — SecureCredentials.FromStrings re-encodes to bytes and
        // the strings become unreachable immediately after.
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(plaintext.AsSpan())
                ?? new Dictionary<string, string>();
            return SecureCredentials.FromStrings(dict);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }
}
