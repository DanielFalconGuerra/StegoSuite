namespace Stego.Core;

/// <summary>
/// Opciones para ocultar un mensaje.
/// </summary>
/// <param name="BitsPerChannel">Bits menos significativos usados por canal: 1, 2 o 4.
/// Más bits = más capacidad, pero más ruido visible y más fácil de detectar.</param>
/// <param name="Password">Si se indica, el mensaje se cifra con AES-GCM antes de ocultarse.</param>
public sealed record StegoOptions(int BitsPerChannel = 1, string? Password = null)
{
    public static StegoOptions Default { get; } = new();
}
