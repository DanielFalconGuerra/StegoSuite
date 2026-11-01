using System.CommandLine;
using System.Text;
using Stego.Core;
using Stego.Core.Imaging;

namespace Stego.Cli.Commands;

internal static class HideCommand
{
    public static Command Create(IPasswordSource passwords)
    {
        Option<FileInfo> input = CommonOptions.Input("Imagen portadora (PNG, BMP, JPEG...).");

        var output = new Option<FileInfo?>("--output", "-o")
        {
            Description = "Imagen PNG de salida. Por defecto: <entrada>.stego.png",
        };

        var message = new Option<string?>("--message", "-m")
        {
            Description = "Texto a ocultar.",
        };

        var file = new Option<FileInfo>("--file", "-f")
        {
            Description = "Archivo cuyo contenido se ocultará.",
        }.AcceptExistingOnly();

        var bits = new Option<int>("--bits", "-b")
        {
            Description = "Bits menos significativos por canal (1, 2 o 4).",
            DefaultValueFactory = _ => 1,
        };
        bits.Validators.Add(result =>
        {
            if (result.GetValueOrDefault<int>() is not (1 or 2 or 4))
                result.AddError("--bits debe ser 1, 2 o 4.");
        });

        var encrypt = new Option<bool>("--encrypt", "-e")
        {
            Description = $"Cifra el mensaje con AES-GCM. La contraseña se pide por consola o se lee de {ConsolePasswordSource.EnvironmentVariable}.",
        };

        var force = new Option<bool>("--force")
        {
            Description = "Sobrescribe el archivo de salida si ya existe.",
        };

        var command = new Command("hide", "Oculta un mensaje o archivo dentro de una imagen.")
        {
            input, output, message, file, bits, encrypt, force,
        };

        command.Validators.Add(result =>
        {
            bool hasMessage = result.GetValue(message) is not null;
            bool hasFile = result.GetValue(file) is not null;

            if (hasMessage == hasFile)
                result.AddError("Indica exactamente uno: --message o --file.");
        });

        command.SetAction(parseResult => StegoCli.Execute(parseResult, stdout =>
        {
            FileInfo inputFile = parseResult.GetRequiredValue(input);
            FileInfo outputFile = parseResult.GetValue(output) ?? DefaultOutput(inputFile);

            if (outputFile.Exists && !parseResult.GetValue(force))
                throw new StegoException($"'{outputFile.Name}' ya existe. Usa --force para sobrescribirlo.");

            byte[] payload = parseResult.GetValue(file) is { } payloadFile
                ? File.ReadAllBytes(payloadFile.FullName)
                : Encoding.UTF8.GetBytes(parseResult.GetValue(message)!);

            string? password = parseResult.GetValue(encrypt) ? passwords.Read(confirm: true) : null;
            var options = new StegoOptions(parseResult.GetValue(bits), password);

            // Se oculta en memoria y solo se escribe al final: si el mensaje no cabe, no queda un archivo a medias.
            using var bitmap = ImageStego.Load(inputFile.FullName);
            ImageStego.Hide(bitmap, payload, options);

            using (var stream = File.Create(outputFile.FullName))
                ImageStego.SaveAsPng(bitmap, stream);

            stdout.WriteLine($"Ocultados {payload.Length} bytes en '{outputFile.Name}' " +
                             $"({options.BitsPerChannel} bit/canal{(password is null ? "" : ", cifrado")}).");
        }));

        return command;
    }

    private static FileInfo DefaultOutput(FileInfo input) =>
        new(Path.Combine(input.DirectoryName ?? ".", Path.GetFileNameWithoutExtension(input.Name) + ".stego.png"));
}
