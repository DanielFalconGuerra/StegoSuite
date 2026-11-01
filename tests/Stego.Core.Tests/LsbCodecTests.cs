using Stego.Core.Lsb;

namespace Stego.Core.Tests;

public class LsbCodecTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Embed_then_Extract_returns_original_data(int bits)
    {
        byte[] data = TestData.RandomBytes(100, seed: bits);
        byte[] carrier = TestData.RandomBytes(LsbCodec.CarrierBytesFor(data.Length, bits), seed: 42);

        LsbCodec.Embed(carrier, data, bits);

        byte[] extracted = new byte[data.Length];
        LsbCodec.Extract(carrier, extracted, bits);
        Assert.Equal(data, extracted);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public void Embed_only_modifies_the_low_bits(int bits)
    {
        byte[] data = TestData.RandomBytes(64, seed: 1);
        byte[] original = TestData.RandomBytes(LsbCodec.CarrierBytesFor(data.Length, bits), seed: 2);
        byte[] carrier = (byte[])original.Clone();
        int highMask = ~((1 << bits) - 1) & 0xFF;

        LsbCodec.Embed(carrier, data, bits);

        for (int i = 0; i < carrier.Length; i++)
            Assert.Equal(original[i] & highMask, carrier[i] & highMask);
    }

    [Fact]
    public void Embed_writes_most_significant_bit_first()
    {
        byte[] carrier = new byte[8];

        LsbCodec.Embed(carrier, [0b1010_0001], bitsPerChannel: 1);

        Assert.Equal(new byte[] { 1, 0, 1, 0, 0, 0, 0, 1 }, carrier);
    }

    [Fact]
    public void Embed_returns_consumed_carrier_bytes()
    {
        byte[] carrier = new byte[100];

        int used = LsbCodec.Embed(carrier, new byte[5], bitsPerChannel: 2);

        Assert.Equal(20, used);
    }

    [Theory]
    [InlineData(80, 1, 10)]
    [InlineData(80, 2, 20)]
    [InlineData(80, 4, 40)]
    [InlineData(7, 1, 0)]
    public void Capacity_is_carrier_bits_divided_by_eight(int carrierLength, int bits, int expected)
    {
        Assert.Equal(expected, LsbCodec.Capacity(carrierLength, bits));
    }

    [Fact]
    public void Embed_throws_when_data_does_not_fit()
    {
        byte[] carrier = new byte[15];

        Assert.Throws<StegoException>(() => LsbCodec.Embed(carrier, new byte[2], bitsPerChannel: 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(8)]
    public void Unsupported_bits_per_channel_throw(int bits)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => LsbCodec.Capacity(100, bits));
    }
}
