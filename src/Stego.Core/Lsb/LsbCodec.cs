namespace Stego.Core.Lsb;

/// <summary>
/// Inserta y extrae bits en los bits menos significativos (LSB) de un buffer portador.
/// No conoce formatos de imagen: trabaja sobre bytes crudos (p. ej. canales R, G, B).
/// </summary>
public static class LsbCodec
{
    /// <summary>
    /// Solo se admiten divisores de 8 para que ningún fragmento quede partido entre dos bytes del portador.
    /// </summary>
    public static bool IsSupported(int bitsPerChannel) => bitsPerChannel is 1 or 2 or 4;

    /// <summary>Número de bytes de datos que caben en <paramref name="carrierLength"/> bytes de portador.</summary>
    public static int Capacity(int carrierLength, int bitsPerChannel)
    {
        EnsureSupported(bitsPerChannel);
        return carrierLength < 0 ? 0 : (int)((long)carrierLength * bitsPerChannel / 8);
    }

    /// <summary>Bytes de portador necesarios para alojar <paramref name="dataLength"/> bytes.</summary>
    public static int CarrierBytesFor(int dataLength, int bitsPerChannel)
    {
        EnsureSupported(bitsPerChannel);
        return dataLength * (8 / bitsPerChannel);
    }

    /// <summary>
    /// Escribe <paramref name="data"/> en el portador, bit más significativo primero.
    /// </summary>
    /// <returns>Bytes del portador consumidos.</returns>
    public static int Embed(Span<byte> carrier, ReadOnlySpan<byte> data, int bitsPerChannel)
    {
        int needed = CarrierBytesFor(data.Length, bitsPerChannel);
        if (needed > carrier.Length)
            throw new StegoException($"Capacidad insuficiente: se necesitan {needed} bytes de portador y hay {carrier.Length}.");

        int mask = (1 << bitsPerChannel) - 1;
        int i = 0;

        foreach (byte b in data)
        {
            for (int shift = 8 - bitsPerChannel; shift >= 0; shift -= bitsPerChannel)
            {
                int chunk = (b >> shift) & mask;
                carrier[i] = (byte)((carrier[i] & ~mask) | chunk);
                i++;
            }
        }

        return i;
    }

    /// <summary>
    /// Lee <paramref name="destination"/>.Length bytes desde el portador.
    /// </summary>
    /// <returns>Bytes del portador consumidos.</returns>
    public static int Extract(ReadOnlySpan<byte> carrier, Span<byte> destination, int bitsPerChannel)
    {
        int needed = CarrierBytesFor(destination.Length, bitsPerChannel);
        if (needed > carrier.Length)
            throw new StegoException($"El portador es demasiado pequeño: se necesitan {needed} bytes y hay {carrier.Length}.");

        int mask = (1 << bitsPerChannel) - 1;
        int i = 0;

        for (int d = 0; d < destination.Length; d++)
        {
            int value = 0;
            for (int read = 0; read < 8; read += bitsPerChannel)
            {
                value = (value << bitsPerChannel) | (carrier[i] & mask);
                i++;
            }
            destination[d] = (byte)value;
        }

        return i;
    }

    private static void EnsureSupported(int bitsPerChannel)
    {
        if (!IsSupported(bitsPerChannel))
            throw new ArgumentOutOfRangeException(nameof(bitsPerChannel), bitsPerChannel, "Valores admitidos: 1, 2 o 4.");
    }
}
