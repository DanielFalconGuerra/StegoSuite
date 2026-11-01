namespace Stego.Cli;

/// <summary>
/// Origen de contraseñas. La contraseña nunca se pasa como argumento de línea de comandos
/// porque quedaría en el historial de la shell y en la lista de procesos.
/// </summary>
public interface IPasswordSource
{
    /// <param name="confirm">Si es true, se pide dos veces y deben coincidir (al cifrar).</param>
    string Read(bool confirm);
}
