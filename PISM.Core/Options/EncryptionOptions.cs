namespace PISM.Core.Options;

public class EncryptionOptions
{
    public const string Section = "Encryption";

    /// <summary>Base64-encoded 32-byte AES-256 key.</summary>
    public string Key { get; set; } = null!;
}
