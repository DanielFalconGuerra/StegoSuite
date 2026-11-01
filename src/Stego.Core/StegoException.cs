namespace Stego.Core;

/// <summary>
/// Error de dominio: capacidad insuficiente, datos corruptos, contraseña incorrecta, etc.
/// </summary>
public sealed class StegoException : Exception
{
    public StegoException(string message) : base(message) { }

    public StegoException(string message, Exception innerException) : base(message, innerException) { }
}
