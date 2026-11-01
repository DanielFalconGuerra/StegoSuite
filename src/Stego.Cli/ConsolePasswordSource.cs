using System.Text;
using Stego.Core;

namespace Stego.Cli;

/// <summary>
/// Lee la contraseña de la variable de entorno <c>STEGO_PASSWORD</c> (para scripts)
/// o, si no existe, la pide por consola sin mostrarla.
/// </summary>
public sealed class ConsolePasswordSource : IPasswordSource
{
    public const string EnvironmentVariable = "STEGO_PASSWORD";

    public string Read(bool confirm)
    {
        string? fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrEmpty(fromEnvironment))
            return fromEnvironment;

        if (Console.IsInputRedirected)
            throw new StegoException($"Se requiere contraseña: define {EnvironmentVariable} o ejecuta en una terminal interactiva.");

        string password = Prompt("Contraseña: ");
        if (password.Length == 0)
            throw new StegoException("La contraseña no puede estar vacía.");

        if (confirm && Prompt("Confirmar contraseña: ") != password)
            throw new StegoException("Las contraseñas no coinciden.");

        return password;
    }

    private static string Prompt(string label)
    {
        Console.Error.Write(label);
        var buffer = new StringBuilder();

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
                break;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                    buffer.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                buffer.Append(key.KeyChar);
            }
        }

        Console.Error.WriteLine();
        return buffer.ToString();
    }
}
