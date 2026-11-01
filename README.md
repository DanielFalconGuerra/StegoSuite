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

## CLI

```bash
# Ocultar texto (o un archivo con -f) a 2 bits por canal, cifrado
stego hide -i foto.jpg -o secreto.png -m "Mensaje oculto" -b 2 --encrypt

# Ver capacidad y si hay mensaje (sin pedir contraseña)
stego info -i secreto.png

# Extraer (la contraseña se pide solo si el mensaje está cifrado)
stego reveal -i secreto.png
stego reveal -i secreto.png -o extraido.bin
```

La contraseña **nunca** se pasa como argumento (quedaría en el historial de la shell): se pide por consola sin mostrarse, o se lee de la variable `STEGO_PASSWORD` en scripts.

Ejecutar desde el código fuente: `dotnet run --project src/Stego.Cli -- info -i foto.png`.
Instalar como herramienta global: `dotnet pack src/Stego.Cli -o nupkg && dotnet tool install -g StegoSuite.Cli --add-source nupkg`.

## Librería

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
src/Stego.Cli/              # Comandos hide, reveal, info (System.CommandLine)
tests/Stego.Core.Tests/     # xUnit
tests/Stego.Cli.Tests/      # Tests de extremo a extremo de la CLI
```

## Desarrollo

```bash
dotnet test
```

## Roadmap

- [x] `Stego.Cli` con `System.CommandLine`
- [ ] Posiciones pseudoaleatorias derivadas de la contraseña
- [ ] `Stego.Analysis`: chi-cuadrado, análisis RS, planos de bits
- [ ] Soporte WAV
- [ ] UI en Blazor

## Licencia

[MIT](LICENSE)
