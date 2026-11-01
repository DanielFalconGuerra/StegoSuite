using System.Text;
using Stego.Core.Format;

namespace Stego.Core.Tests;

public class StegoEngineTests
{
    private static readonly byte[] Message = Encoding.UTF8.GetBytes("Heródoto ya lo hacía con tablillas de cera.");

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Hide_then_Reveal_returns_message(int bits)
    {
        byte[] carrier = TestData.RandomBytes(2_000, seed: bits);

        StegoEngine.Hide(carrier, Message, new StegoOptions(bits));

        Assert.Equal(Message, StegoEngine.Reveal(carrier));
    }

    [Fact]
    public void Hide_then_Reveal_with_password_returns_message()
    {
        byte[] carrier = TestData.RandomBytes(2_000, seed: 7);

        StegoEngine.Hide(carrier, Message, new StegoOptions(Password: TestData.Password));

        Assert.Equal(Message, StegoEngine.Reveal(carrier, TestData.Password));
    }

    [Fact]
    public void Encrypted_message_without_password_throws()
    {
        byte[] carrier = TestData.RandomBytes(2_000, seed: 8);
        StegoEngine.Hide(carrier, Message, new StegoOptions(Password: TestData.Password));

        var ex = Assert.Throws<StegoException>(() => StegoEngine.Reveal(carrier));
        Assert.Contains("contraseña", ex.Message);
    }

    [Fact]
    public void Empty_message_round_trips()
    {
        byte[] carrier = TestData.RandomBytes(StegoEngine.HeaderCarrierBytes, seed: 9);

        StegoEngine.Hide(carrier, ReadOnlySpan<byte>.Empty);

        Assert.Empty(StegoEngine.Reveal(carrier));
    }

    [Fact]
    public void Message_exactly_at_capacity_fits()
    {
        byte[] carrier = TestData.RandomBytes(1_000, seed: 10);
        byte[] message = TestData.RandomBytes(StegoEngine.GetCapacity(carrier.Length), seed: 11);

        StegoEngine.Hide(carrier, message);

        Assert.Equal(message, StegoEngine.Reveal(carrier));
    }

    [Fact]
    public void Message_over_capacity_throws()
    {
        byte[] carrier = new byte[1_000];
        byte[] message = new byte[StegoEngine.GetCapacity(carrier.Length) + 1];

        Assert.Throws<StegoException>(() => StegoEngine.Hide(carrier, message));
    }

    [Fact]
    public void Capacity_accounts_for_header_and_encryption_overhead()
    {
        int plain = StegoEngine.GetCapacity(10_000);
        int encrypted = StegoEngine.GetCapacity(10_000, encrypted: true);

        Assert.Equal((10_000 - StegoHeader.Size * 8) / 8, plain);
        Assert.Equal(plain - Crypto.PayloadCipher.Overhead, encrypted);
    }

    [Fact]
    public void Reveal_on_clean_carrier_throws()
    {
        byte[] carrier = new byte[2_000];

        var ex = Assert.Throws<StegoException>(() => StegoEngine.Reveal(carrier));
        Assert.Contains("firma", ex.Message);
    }

    [Fact]
    public void Corrupted_payload_is_detected_by_crc()
    {
        byte[] carrier = TestData.RandomBytes(2_000, seed: 12);
        StegoEngine.Hide(carrier, Message);

        // Voltea un LSB dentro del payload (después de la cabecera).
        carrier[StegoEngine.HeaderCarrierBytes + 5] ^= 0x01;

        var ex = Assert.Throws<StegoException>(() => StegoEngine.Reveal(carrier));
        Assert.Contains("CRC32", ex.Message);
    }

    [Fact]
    public void TryReadHeader_describes_hidden_message_without_password()
    {
        byte[] carrier = TestData.RandomBytes(2_000, seed: 14);
        StegoEngine.Hide(carrier, Message, new StegoOptions(BitsPerChannel: 2, Password: TestData.Password));

        Assert.True(StegoEngine.TryReadHeader(carrier, out StegoHeader header));
        Assert.True(header.IsEncrypted);
        Assert.Equal(2, header.BitsPerChannel);
        Assert.Equal(Message.Length + Crypto.PayloadCipher.Overhead, header.PayloadLength);
    }

    [Fact]
    public void TryReadHeader_returns_false_on_clean_carrier()
    {
        Assert.False(StegoEngine.TryReadHeader(new byte[2_000], out _));
    }

    [Fact]
    public void Header_is_always_readable_at_one_bit_per_channel()
    {
        byte[] carrier = TestData.RandomBytes(2_000, seed: 13);
        StegoEngine.Hide(carrier, Message, new StegoOptions(BitsPerChannel: 4));

        byte[] headerBytes = new byte[StegoHeader.Size];
        Lsb.LsbCodec.Extract(carrier, headerBytes, bitsPerChannel: 1);
        StegoHeader header = StegoHeader.ReadFrom(headerBytes);

        Assert.Equal(4, header.BitsPerChannel);
        Assert.Equal(Message.Length, header.PayloadLength);
        Assert.False(header.IsEncrypted);
    }
}
