using System.Security.Cryptography;

namespace Stego.Core.Crypto;

/// <summary>
/// Cifrado autenticado del mensaje: AES-256-GCM con clave derivada por PBKDF2-SHA256.
/// </summary>
/// <remarks>
/// Formato de salida: <c>salt(16) | nonce(12) | tag(16) | ciphertext</c>.
/// Ocultar no es cifrar: sin esta capa, cualquiera que sospeche puede extraer el texto en claro.
/// </remarks>
public static class PayloadCipher
{
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 600_000; // Recomendación OWASP para PBKDF2-HMAC-SHA256

    public const int Overhead = SaltSize + NonceSize + TagSize;

    public static byte[] Encrypt(ReadOnlySpan<byte> plaintext, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] output = new byte[Overhead + plaintext.Length];
        Span<byte> salt = output.AsSpan(0, SaltSize);
        Span<byte> nonce = output.AsSpan(SaltSize, NonceSize);
        Span<byte> tag = output.AsSpan(SaltSize + NonceSize, TagSize);
        Span<byte> ciphertext = output.AsSpan(Overhead);

        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(nonce);

        byte[] key = DeriveKey(password, salt);
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        return output;
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> payload, string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        if (payload.Length < Overhead)
            throw new StegoException("Payload cifrado demasiado corto.");

        ReadOnlySpan<byte> salt = payload[..SaltSize];
        ReadOnlySpan<byte> nonce = payload.Slice(SaltSize, NonceSize);
        ReadOnlySpan<byte> tag = payload.Slice(SaltSize + NonceSize, TagSize);
        ReadOnlySpan<byte> ciphertext = payload[Overhead..];

        byte[] plaintext = new byte[ciphertext.Length];
        byte[] key = DeriveKey(password, salt);
        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }
        catch (AuthenticationTagMismatchException ex)
        {
            throw new StegoException("Contraseña incorrecta o datos manipulados.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    private static byte[] DeriveKey(string password, ReadOnlySpan<byte> salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
}
