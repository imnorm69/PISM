namespace PISM.Core.Services;

public interface IEncryptionService
{
    /// <summary>Encrypts plaintext using AES-256-GCM. Returns [nonce(12)][tag(16)][ciphertext].</summary>
    byte[] Encrypt(byte[] plaintext);

    /// <summary>Decrypts data produced by Encrypt. Input format: [nonce(12)][tag(16)][ciphertext].</summary>
    byte[] Decrypt(byte[] ciphertext);
}
