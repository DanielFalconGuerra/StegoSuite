using SkiaSharp;

namespace Stego.Core.Tests;

/// <summary>Datos de prueba deterministas (semilla fija) para que los tests sean reproducibles.</summary>
internal static class TestData
{
    public static byte[] RandomBytes(int length, int seed)
    {
        byte[] bytes = new byte[length];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }

    /// <summary>Bitmap RGBA sin premultiplicar con colores aleatorios y alfa opaco o semitransparente.</summary>
    public static SKBitmap RandomImage(int width, int height, int seed)
    {
        var random = new Random(seed);
        var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(1, 256)));

        return bitmap;
    }
}
