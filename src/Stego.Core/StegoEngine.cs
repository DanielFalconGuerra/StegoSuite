using Stego.Core.Crypto;
using Stego.Core.Format;
using Stego.Core.Lsb;

namespace Stego.Core;

/// <summary>
/// Orquesta cabecera + cifrado opcional + LSB sobre un buffer portador genérico.
/// Es independiente del formato: las clases de <c>Imaging</c> le pasan los canales de color.
/// </summary>
public static class StegoEngine
{
    /// <summary>Bytes de portador que ocupa la cabecera (escrita a 1 bit por canal).</summary>
    public static int HeaderCarrierBytes { get; } =
        LsbCodec.CarrierBytesFor(StegoHeader.Size, StegoHeader.HeaderBitsPerChannel);

    /// <summary>
    /// Bytes de mensaje que caben en el portador, descontando la cabecera y,
    /// si <paramref name="encrypted"/> es true, la sobrecarga del cifrado.
    /// </summary>
    public static int GetCapacity(int carrierLength, int bitsPerChannel = 1, bool encrypted = false)
    {
        int capacity = LsbCodec.Capacity(carrierLength - HeaderCarrierBytes, bitsPerChannel);
        if (encrypted)
            capacity -= PayloadCipher.Overhead;
        return Math.Max(0, capacity);
    }

    public static void Hide(Span<byte> carrier, ReadOnlySpan<byte> message, StegoOptions? options = null)
    {
        options ??= StegoOptions.Default;

        if (!LsbCodec.IsSupported(options.BitsPerChannel))
            throw new ArgumentOutOfRangeException(nameof(options), options.BitsPerChannel, "BitsPerChannel debe ser 1, 2 o 4.");

        bool encrypt = !string.IsNullOrEmpty(options.Password);
        byte[] payload = encrypt ? PayloadCipher.Encrypt(message, options.Password!) : message.ToArray();

        int capacity = GetCapacity(carrier.Length, options.BitsPerChannel);
        if (payload.Length > capacity)
            throw new StegoException($"El mensaje ({payload.Length} bytes) supera la capacidad del portador ({capacity} bytes).");

        var header = new StegoHeader(
            encrypt ? StegoFlags.Encrypted : StegoFlags.None,
            (byte)options.BitsPerChannel,
            payload.Length,
            System.IO.Hashing.Crc32.HashToUInt32(payload));

        Span<byte> headerBytes = stackalloc byte[StegoHeader.Size];
        header.WriteTo(headerBytes);

        LsbCodec.Embed(carrier, headerBytes, StegoHeader.HeaderBitsPerChannel);
        LsbCodec.Embed(carrier[HeaderCarrierBytes..], payload, options.BitsPerChannel);
    }

    /// <summary>
    /// Lee y valida solo la cabecera, sin extraer ni descifrar el payload.
    /// </summary>
    /// <returns>false si el portador no contiene un mensaje válido.</returns>
    public static bool TryReadHeader(ReadOnlySpan<byte> carrier, out StegoHeader header)
    {
        try
        {
            header = ReadHeader(carrier);
            return true;
        }
        catch (StegoException)
        {
            header = default;
            return false;
        }
    }

    public static byte[] Reveal(ReadOnlySpan<byte> carrier, string? password = null)
    {
        StegoHeader header = ReadHeader(carrier);

        byte[] payload = new byte[header.PayloadLength];
        LsbCodec.Extract(carrier[HeaderCarrierBytes..], payload, header.BitsPerChannel);

        if (System.IO.Hashing.Crc32.HashToUInt32(payload) != header.Crc32)
            throw new StegoException("El mensaje está corrupto (CRC32 no coincide). ¿Se recomprimió la imagen?");

        if (!header.IsEncrypted)
            return payload;

        if (string.IsNullOrEmpty(password))
            throw new StegoException("El mensaje está cifrado: se requiere contraseña.");

        return PayloadCipher.Decrypt(payload, password);
    }

    private static StegoHeader ReadHeader(ReadOnlySpan<byte> carrier)
    {
        if (carrier.Length < HeaderCarrierBytes)
            throw new StegoException("El portador es demasiado pequeño para contener una cabecera.");

        Span<byte> headerBytes = stackalloc byte[StegoHeader.Size];
        LsbCodec.Extract(carrier, headerBytes, StegoHeader.HeaderBitsPerChannel);
        StegoHeader header = StegoHeader.ReadFrom(headerBytes);

        if (!LsbCodec.IsSupported(header.BitsPerChannel))
            throw new StegoException($"Cabecera corrupta: bits por canal inválidos ({header.BitsPerChannel}).");

        if (header.PayloadLength > GetCapacity(carrier.Length, header.BitsPerChannel))
            throw new StegoException("Cabecera corrupta: la longitud declarada excede la capacidad del portador.");

        return header;
    }
}
