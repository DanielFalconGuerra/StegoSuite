using System.Runtime.InteropServices;
using System.Text;
using SkiaSharp;
using Stego.Core.Format;

namespace Stego.Core.Imaging;

/// <summary>
/// Adaptador de <see cref="StegoEngine"/> para imágenes SkiaSharp.
/// Usa solo los canales R, G y B; el canal alfa no se toca.
/// </summary>
/// <remarks>
/// El bitmap debe ser <see cref="SKColorType.Rgba8888"/> con alfa <b>no premultiplicado</b>:
/// premultiplicar redondea los canales de color y borraría los LSB. Usa <see cref="Load(Stream)"/>.
/// Guarda siempre en un formato sin pérdida (PNG). JPEG destruye los LSB.
/// </remarks>
public static class ImageStego
{
    private const int BytesPerPixel = 4;

    public static int GetCapacity(SKBitmap bitmap, int bitsPerChannel = 1, bool encrypted = false) =>
        StegoEngine.GetCapacity(bitmap.Width * bitmap.Height * 3, bitsPerChannel, encrypted);

    public static void Hide(SKBitmap bitmap, ReadOnlySpan<byte> message, StegoOptions? options = null)
    {
        byte[] pixels = ReadPixels(bitmap);
        byte[] channels = ToRgbChannels(bitmap, pixels);

        StegoEngine.Hide(channels, message, options);

        FromRgbChannels(bitmap, channels, pixels);
        Marshal.Copy(pixels, 0, bitmap.GetPixels(), pixels.Length);
    }

    public static void Hide(SKBitmap bitmap, string message, StegoOptions? options = null) =>
        Hide(bitmap, Encoding.UTF8.GetBytes(message), options);

    public static byte[] Reveal(SKBitmap bitmap, string? password = null) =>
        StegoEngine.Reveal(ToRgbChannels(bitmap, ReadPixels(bitmap)), password);

    public static string RevealText(SKBitmap bitmap, string? password = null) =>
        Encoding.UTF8.GetString(Reveal(bitmap, password));

    /// <inheritdoc cref="StegoEngine.TryReadHeader"/>
    public static bool TryReadHeader(SKBitmap bitmap, out StegoHeader header) =>
        StegoEngine.TryReadHeader(ToRgbChannels(bitmap, ReadPixels(bitmap)), out header);

    /// <summary>Decodifica una imagen directamente a RGBA de 8 bits sin premultiplicar.</summary>
    public static SKBitmap Load(Stream stream)
    {
        using var codec = SKCodec.Create(stream)
            ?? throw new StegoException("Formato de imagen no reconocido.");

        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);

        SKCodecResult result = codec.GetPixels(info, bitmap.GetPixels());
        if (result is not (SKCodecResult.Success or SKCodecResult.IncompleteInput))
        {
            bitmap.Dispose();
            throw new StegoException($"No se pudo decodificar la imagen: {result}.");
        }

        return bitmap;
    }

    public static SKBitmap Load(string path)
    {
        using var stream = File.OpenRead(path);
        return Load(stream);
    }

    public static void SaveAsPng(SKBitmap bitmap, Stream destination)
    {
        if (!bitmap.Encode(destination, SKEncodedImageFormat.Png, quality: 100))
            throw new StegoException("No se pudo codificar la imagen como PNG.");
    }

    /// <summary>Carga <paramref name="inputPath"/>, oculta el mensaje y guarda como PNG.</summary>
    public static void HideInFile(string inputPath, string outputPath, ReadOnlySpan<byte> message, StegoOptions? options = null)
    {
        using var bitmap = Load(inputPath);
        Hide(bitmap, message, options);

        using var output = File.Create(outputPath);
        SaveAsPng(bitmap, output);
    }

    public static byte[] RevealFromFile(string path, string? password = null)
    {
        using var bitmap = Load(path);
        return Reveal(bitmap, password);
    }

    private static byte[] ReadPixels(SKBitmap bitmap)
    {
        if (bitmap.ColorType != SKColorType.Rgba8888 || bitmap.AlphaType == SKAlphaType.Premul)
            throw new ArgumentException("Se requiere un bitmap Rgba8888 sin premultiplicar. Usa ImageStego.Load.", nameof(bitmap));

        byte[] pixels = new byte[bitmap.RowBytes * bitmap.Height];
        Marshal.Copy(bitmap.GetPixels(), pixels, 0, pixels.Length);
        return pixels;
    }

    private static byte[] ToRgbChannels(SKBitmap bitmap, byte[] pixels)
    {
        byte[] channels = new byte[bitmap.Width * bitmap.Height * 3];
        int c = 0;

        for (int y = 0; y < bitmap.Height; y++)
        {
            int p = y * bitmap.RowBytes;
            for (int x = 0; x < bitmap.Width; x++, p += BytesPerPixel)
            {
                channels[c++] = pixels[p];     // R
                channels[c++] = pixels[p + 1]; // G
                channels[c++] = pixels[p + 2]; // B
            }
        }

        return channels;
    }

    private static void FromRgbChannels(SKBitmap bitmap, byte[] channels, byte[] pixels)
    {
        int c = 0;

        for (int y = 0; y < bitmap.Height; y++)
        {
            int p = y * bitmap.RowBytes;
            for (int x = 0; x < bitmap.Width; x++, p += BytesPerPixel)
            {
                pixels[p] = channels[c++];
                pixels[p + 1] = channels[c++];
                pixels[p + 2] = channels[c++];
            }
        }
    }
}
