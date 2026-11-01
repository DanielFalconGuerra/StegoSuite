using System.CommandLine;
using Stego.Core.Format;
using Stego.Core.Imaging;

namespace Stego.Cli.Commands;

internal static class InfoCommand
{
    public static Command Create()
    {
        Option<FileInfo> input = CommonOptions.Input("Imagen a analizar.");

        var command = new Command("info", "Muestra la capacidad de una imagen y si contiene un mensaje.")
        {
            input,
        };

        command.SetAction(parseResult => StegoCli.Execute(parseResult, stdout =>
        {
            FileInfo file = parseResult.GetRequiredValue(input);
            using var bitmap = ImageStego.Load(file.FullName);

            stdout.WriteLine($"Imagen:    {file.Name} ({bitmap.Width}x{bitmap.Height})");
            stdout.WriteLine("Capacidad:");
            foreach (int bits in new[] { 1, 2, 4 })
            {
                stdout.WriteLine($"  {bits} bit/canal: {ImageStego.GetCapacity(bitmap, bits)} bytes " +
                                 $"({ImageStego.GetCapacity(bitmap, bits, encrypted: true)} cifrado)");
            }

            stdout.WriteLine(ImageStego.TryReadHeader(bitmap, out StegoHeader header)
                ? $"Mensaje:   sí, {header.PayloadLength} bytes a {header.BitsPerChannel} bit/canal{(header.IsEncrypted ? ", cifrado" : "")}"
                : "Mensaje:   no detectado");
        }));

        return command;
    }
}
