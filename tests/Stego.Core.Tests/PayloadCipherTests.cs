using System.Text;
using Stego.Core.Crypto;

namespace Stego.Core.Tests;

public class PayloadCipherTests
{
    private static readonly byte[] Plaintext = Encoding.UTF8.GetBytes("Mensaje secreto ñ ✓");

    [Fact]
    public void Encrypt_then_Decrypt_returns_original()
    {
        byte[] encrypted = PayloadCipher.Encrypt(Plaintext, "clave-segura");

        Assert.Equal(Plaintext, PayloadCipher.Decrypt(encrypted, "clave-segura"));
    }

    [Fact]
    public void Encrypted_output_has_expected_overhead()
    {
        byte[] encrypted = PayloadCipher.Encrypt(Plaintext, "clave");

        Assert.Equal(Plaintext.Length + PayloadCipher.Overhead, encrypted.Length);
    }

    [Fact]
    public void Same_input_produces_different_ciphertexts()
    {
        byte[] a = PayloadCipher.Encrypt(Plaintext, "clave");
        byte[] b = PayloadCipher.Encrypt(Plaintext, "clave");

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Wrong_password_throws()
    {
        byte[] encrypted = PayloadCipher.Encrypt(Plaintext, "correcta");

        Assert.Throws<StegoException>(() => PayloadCipher.Decrypt(encrypted, "incorrecta"));
    }

    [Fact]
    public void Tampered_ciphertext_throws()
    {
        byte[] encrypted = PayloadCipher.Encrypt(Plaintext, "clave");
        encrypted[^1] ^= 0x01;

        Assert.Throws<StegoException>(() => PayloadCipher.Decrypt(encrypted, "clave"));
    }
}
