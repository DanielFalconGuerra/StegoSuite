using System.Buffers.Binary;

namespace Stego.Core.Format;

/// <summary>
/// Cabecera que precede al payload dentro del portador.
/// </summary>
/// <remarks>
/// Disposición (15 bytes, big-endian):
/// <code>
/// [0..3]  Magic "STG1"
/// [4]     Versión
/// [5]     Flags (bit 0 = cifrado)
/// [6]     Bits por canal del payload
/// [7..10] Longitud del payload
/// [11..14] CRC32 del payload tal como se almacena
/// </code>
/// La cabecera siempre se escribe a 1 bit por canal para poder leerla sin saber
/// de antemano con cuántos bits se ocultó el payload.
/// </remarks>
public readonly record struct StegoHeader(
    StegoFlags Flags,
    byte BitsPerChannel,
    int PayloadLength,
    uint Crc32)
{
    public const int Size = 15;
    public const byte CurrentVersion = 1;
    public const int HeaderBitsPerChannel = 1;

    private static ReadOnlySpan<byte> Magic => "STG1"u8;

    public bool IsEncrypted => Flags.HasFlag(StegoFlags.Encrypted);

    public void WriteTo(Span<byte> destination)
    {
        Magic.CopyTo(destination);
        destination[4] = CurrentVersion;
        destination[5] = (byte)Flags;
        destination[6] = BitsPerChannel;
        BinaryPrimitives.WriteInt32BigEndian(destination[7..], PayloadLength);
        BinaryPrimitives.WriteUInt32BigEndian(destination[11..], Crc32);
    }

    public static StegoHeader ReadFrom(ReadOnlySpan<byte> source)
    {
        if (source.Length < Size || !source[..4].SequenceEqual(Magic))
            throw new StegoException("No se encontró ningún mensaje oculto (firma ausente).");

        if (source[4] != CurrentVersion)
            throw new StegoException($"Versión de formato no soportada: {source[4]}.");

        int length = BinaryPrimitives.ReadInt32BigEndian(source[7..]);
        if (length < 0)
            throw new StegoException("Cabecera corrupta: longitud negativa.");

        return new StegoHeader(
            (StegoFlags)source[5],
            source[6],
            length,
            BinaryPrimitives.ReadUInt32BigEndian(source[11..]));
    }
}

[Flags]
public enum StegoFlags : byte
{
    None = 0,
    Encrypted = 1 << 0,
}
