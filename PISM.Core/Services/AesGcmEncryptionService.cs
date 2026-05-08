using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using PISM.Core.Options;

namespace PISM.Core.Services;

public class AesGcmEncryptionService : IEncryptionService
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmEncryptionService(IOptions<EncryptionOptions> options)
    {
        _key = Convert.FromBase64String(options.Value.Key);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption key must be 32 bytes (256-bit), Base64-encoded.");
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);
        return result;
    }

    public byte[] Decrypt(byte[] data)
    {
        var nonce = data[..NonceSize];
        var tag = data[NonceSize..(NonceSize + TagSize)];
        var ciphertext = data[(NonceSize + TagSize)..];

        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
