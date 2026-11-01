using System.CommandLine;
using System.Text;
using Stego.Core;
using Stego.Core.Format;
using Stego.Core.Imaging;

namespace Stego.Cli.Commands;

internal static class RevealCommand
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static Command Create(IPasswordSource passwords)
    {
        Option<FileInfo> input = CommonOptions.Input("Imagen con el mensaje oculto.");

        var output = new Option<FileInfo?>("--output", "-o")
        {
            Description = "Guarda el contenido en un archivo en lugar de mostrarlo como texto.",
        };

        var force = new Option<bool>("--force")
        {
            Description = "Sobrescribe el archivo de salida si ya existe.",
        };

        var command = new Command("reveal", "Extrae un mensaje oculto de una imagen.")
        {
            input, output, force,
        };

        command.SetAction(parseResult => StegoCli.Execute(parseResult, stdout =>
        {
            FileInfo? outputFile = parseResult.GetValue(output);
            if (outputFile is { Exists: true } && !parseResult.GetValue(force))
                throw new StegoException($"'{outputFile.Name}' ya existe. Usa --force para sobrescribirlo.");

            using var bitmap = ImageStego.Load(parseResult.GetRequiredValue(input).FullName);

            // Se mira la cabecera primero para pedir contraseña solo si hace falta.
            if (!ImageStego.TryReadHeader(bitmap, out StegoHeader header))
                throw new StegoException("No se encontró ningún mensaje oculto (firma ausente).");

            string? password = header.IsEncrypted ? passwords.Read(confirm: false) : null;
            byte[] payload = ImageStego.Reveal(bitmap, password);

            if (outputFile is not null)
            {
                File.WriteAllBytes(outputFile.FullName, payload);
                stdout.WriteLine($"Extraídos {payload.Length} bytes en '{outputFile.Name}'.");
                return;
            }

            try
            {
                stdout.WriteLine(StrictUtf8.GetString(payload));
            }
            catch (DecoderFallbackException)
            {
                throw new StegoException($"El contenido ({payload.Length} bytes) no es texto UTF-8. Usa --output para guardarlo en un archivo.");
            }
        }));

        return command;
    }
}
