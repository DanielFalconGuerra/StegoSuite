# StegoSuite

Esteganografía LSB en imágenes con .NET 10: oculta texto o archivos en los bits menos significativos de los píxeles, con cifrado AES-GCM opcional.

> Proyecto educativo. El LSB secuencial es detectable con estegoanálisis estadístico (chi-cuadrado, RS); ver *Roadmap*.

## Cómo funciona

```
mensaje ──► [AES-256-GCM + PBKDF2]? ──► payload
                                          │
cabecera (15 B, siempre a 1 bit/canal) ───┤
                                          ▼
          canales R,G,B de la imagen ◄── LSB (1, 2 o 4 bits/canal)
```

| Cabecera | Bytes |
|---|---|
| Firma `STG1` | 4 |
| Versión | 1 |
| Flags (cifrado) | 1 |
| Bits por canal del payload | 1 |
| Longitud del payload | 4 |
| CRC32 del payload | 4 |

## Uso

```csharp
using Stego.Core;
using Stego.Core.Imaging;

ImageStego.HideInFile("foto.png", "salida.png", "secreto"u8, new StegoOptions(Password: "clave"));
byte[] mensaje = ImageStego.RevealFromFile("salida.png", "clave");
```

Guarda siempre en **PNG**: la compresión JPEG destruye el mensaje (hay un test que lo demuestra).

## Estructura

```
src/Stego.Core/
├─ Lsb/LsbCodec.cs          # Inserción/extracción de bits sobre bytes crudos
├─ Format/StegoHeader.cs    # Cabecera binaria
├─ Crypto/PayloadCipher.cs  # AES-GCM + PBKDF2-SHA256 (600k iteraciones)
├─ Imaging/ImageStego.cs    # Adaptador SkiaSharp (RGBA sin premultiplicar)
└─ StegoEngine.cs           # Orquestación independiente del formato
tests/Stego.Core.Tests/     # xUnit
```

## Desarrollo

```bash
dotnet test
```

## Roadmap

- [ ] `Stego.Cli` con `System.CommandLine`
- [ ] Posiciones pseudoaleatorias derivadas de la contraseña
- [ ] `Stego.Analysis`: chi-cuadrado, análisis RS, planos de bits
- [ ] Soporte WAV
- [ ] UI en Blazor
