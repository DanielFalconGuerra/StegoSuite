using SkiaSharp;
using Stego.Core.Imaging;

namespace Stego.Core.Tests;

public class ImageStegoTests
{
    private const string Message = "La esteganografía oculta la existencia del mensaje 🕵️";

    [Fact]
    public void Message_survives_png_round_trip()
    {
        using var bitmap = TestData.RandomImage(64, 64, seed: 1);
        ImageStego.Hide(bitmap, Message);

        using var reloaded = PngRoundTrip(bitmap);

        Assert.Equal(Message, ImageStego.RevealText(reloaded));
    }

    [Fact]
    public void Encrypted_message_survives_png_round_trip()
    {
        using var bitmap = TestData.RandomImage(64, 64, seed: 2);
        ImageStego.Hide(bitmap, Message, new StegoOptions(BitsPerChannel: 2, Password: TestData.Password));

        using var reloaded = PngRoundTrip(bitmap);

        Assert.Equal(Message, ImageStego.RevealText(reloaded, TestData.Password));
    }

    [Fact]
    public void Jpeg_compression_destroys_the_message()
    {
        using var bitmap = TestData.RandomImage(64, 64, seed: 3);
        ImageStego.Hide(bitmap, Message);

        using var stream = new MemoryStream();
        bitmap.Encode(stream, SKEncodedImageFormat.Jpeg, quality: 95);
        stream.Position = 0;
        using var reloaded = ImageStego.Load(stream);

        Assert.Throws<StegoException>(() => ImageStego.Reveal(reloaded));
    }

    [Fact]
    public void Alpha_channel_is_untouched()
    {
        using var bitmap = TestData.RandomImage(32, 32, seed: 4);
        byte[] alphaBefore = AlphaOf(bitmap);

        ImageStego.Hide(bitmap, Message);

        Assert.Equal(alphaBefore, AlphaOf(bitmap));
    }

    [Fact]
    public void Capacity_uses_three_channels_per_pixel()
    {
        using var bitmap = TestData.RandomImage(100, 100, seed: 5);

        Assert.Equal(StegoEngine.GetCapacity(100 * 100 * 3), ImageStego.GetCapacity(bitmap));
    }

    [Fact]
    public void Premultiplied_bitmap_is_rejected()
    {
        using var bitmap = new SKBitmap(new SKImageInfo(16, 16, SKColorType.Rgba8888, SKAlphaType.Premul));

        Assert.Throws<ArgumentException>(() => ImageStego.Hide(bitmap, Message));
    }

    private static SKBitmap PngRoundTrip(SKBitmap bitmap)
    {
        using var stream = new MemoryStream();
        ImageStego.SaveAsPng(bitmap, stream);
        stream.Position = 0;
        return ImageStego.Load(stream);
    }

    private static byte[] AlphaOf(SKBitmap bitmap) =>
        bitmap.Pixels.Select(p => p.Alpha).ToArray();
}
