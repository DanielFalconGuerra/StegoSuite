using SkiaSharp;
using Stego.Core.Imaging;

namespace Stego.Cli.Tests;

/// <summary>
/// Directorio temporal por test y helper para invocar el CLI capturando salida y errores.
/// </summary>
public sealed class CliFixture : IDisposable
{
    public string Directory { get; } = Path.Combine(Path.GetTempPath(), "stego-cli-tests", Guid.NewGuid().ToString("N"));

    public FakePasswordSource Passwords { get; } = new();

    public CliFixture() => System.IO.Directory.CreateDirectory(Directory);

    public string PathOf(string fileName) => Path.Combine(Directory, fileName);

    public CliResult Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        int exitCode = StegoCli.Run(args, output, error, Passwords);
        return new CliResult(exitCode, output.ToString(), error.ToString());
    }

    /// <summary>Crea un PNG con píxeles aleatorios (deterministas) y devuelve su ruta.</summary>
    public string CreatePng(string fileName = "cover.png", int width = 64, int height = 64, int seed = 1)
    {
        var random = new Random(seed);
        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                bitmap.SetPixel(x, y, new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256), 255));

        string path = PathOf(fileName);
        using var stream = File.Create(path);
        ImageStego.SaveAsPng(bitmap, stream);
        return path;
    }

    public void Dispose()
    {
        try { System.IO.Directory.Delete(Directory, recursive: true); }
        catch (IOException) { }
    }
}

public sealed record CliResult(int ExitCode, string Output, string Error);

/// <summary>Devuelve una contraseña fija (o la siguiente de la cola) y registra cuántas veces se pidió.</summary>
public sealed class FakePasswordSource : IPasswordSource
{
    private readonly Queue<string> _queued = new();

    public string Default { get; set; } = Guid.NewGuid().ToString("N");

    public int Calls { get; private set; }

    public bool? LastConfirm { get; private set; }

    public void Enqueue(string password) => _queued.Enqueue(password);

    public string Read(bool confirm)
    {
        Calls++;
        LastConfirm = confirm;
        return _queued.Count > 0 ? _queued.Dequeue() : Default;
    }
}
