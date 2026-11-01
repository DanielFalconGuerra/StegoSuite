using System.CommandLine;
using Stego.Cli.Commands;
using Stego.Core;

namespace Stego.Cli;

/// <summary>
/// Punto de entrada testeable: recibe los writers y el origen de contraseñas en lugar de usar
/// <see cref="Console"/> directamente.
/// </summary>
public static class StegoCli
{
    public const int Success = 0;
    public const int Failure = 1;

    public static int Run(string[] args, TextWriter output, TextWriter error, IPasswordSource passwords)
    {
        var root = new RootCommand("Esteganografía LSB en imágenes: oculta y extrae mensajes en PNG.")
        {
            HideCommand.Create(passwords),
            RevealCommand.Create(passwords),
            InfoCommand.Create(),
        };

        return root.Parse(args).Invoke(new InvocationConfiguration { Output = output, Error = error });
    }

    /// <summary>
    /// Ejecuta la acción de un comando traduciendo los errores esperados a un mensaje y código de salida.
    /// </summary>
    internal static int Execute(ParseResult parseResult, Action<TextWriter> action)
    {
        TextWriter error = parseResult.InvocationConfiguration.Error;
        try
        {
            action(parseResult.InvocationConfiguration.Output);
            return Success;
        }
        catch (Exception ex) when (ex is StegoException or IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"error: {ex.Message}");
            return Failure;
        }
    }
}
