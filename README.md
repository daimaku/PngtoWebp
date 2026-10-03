# PngtoWebp

Herramienta CLI en **.NET 8** para convertir imágenes **PNG, JPG/JPEG y BMP a WebP**, pensada para optimizar assets para web.

El proyecto usa [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) para decodificación y codificación WebP, por lo que no depende de `System.Drawing` ni de componentes nativos de Windows.

## Características

- PNG, JPG/JPEG y BMP → WebP.
- Conversión de un archivo individual o de una carpeta completa.
- Exploración recursiva opcional.
- Calidad configurable de 0 a 100.
- Modo WebP **lossy** (por defecto) o **lossless**.
- Nivel de esfuerzo del encoder configurable de 0 a 6.
- Preserva transparencias cuando el formato de origen las contiene.
- Auto-orientación según EXIF antes de convertir.
- Elimina metadata por defecto para reducir peso (`--keep-metadata` para conservarla).
- Escritura atómica mediante archivo temporal para evitar archivos WebP incompletos.
- Opción de sobrescritura explícita.
- Mantiene la estructura de subdirectorios durante conversiones recursivas.
- Resumen final con cantidad de archivos y ahorro de espacio.

## Requisitos para compilar

- .NET SDK 8.0 o superior.

## Compilar

```bash
dotnet restore PngtoWebp.sln
dotnet build PngtoWebp.sln -c Release
```

## Uso rápido

```bash
dotnet run --project src/PngtoWebp.Cli -- "foto.png"
```

El resultado será `foto.webp` en la misma carpeta.

### Convertir una carpeta

```bash
dotnet run --project src/PngtoWebp.Cli -- "./imagenes"
```

Cuando la entrada es una carpeta y no se especifica `--output`, los archivos se escriben en una subcarpeta `webp`.

### Convertir recursivamente

```bash
dotnet run --project src/PngtoWebp.Cli -- "./imagenes" --recursive
```

### Calidad 85

```bash
dotnet run --project src/PngtoWebp.Cli -- "./imagenes" --quality 85 --recursive
```

### Lossless

```bash
dotnet run --project src/PngtoWebp.Cli -- "logo.png" --lossless
```

### Carpeta de salida y sobrescritura

```bash
dotnet run --project src/PngtoWebp.Cli -- "./imagenes" --output "./imagenes-optimizadas" --recursive --overwrite
```

## Opciones

| Opción | Descripción |
|---|---|
| `-o`, `--output <dir>` | Directorio de salida. |
| `-q`, `--quality <0-100>` | Calidad WebP. Default: `80`. |
| `--lossless` | Usa WebP lossless. |
| `-r`, `--recursive` | Procesa subdirectorios. |
| `-f`, `--overwrite` | Sobrescribe archivos `.webp` existentes. |
| `--keep-metadata` | Conserva metadata de la imagen. Por defecto se elimina. |
| `--method <0-6>` | Esfuerzo del encoder: 0 = más rápido, 6 = mejor compresión. Default: `4`. |
| `-h`, `--help` | Muestra ayuda. |

## Publicar como ejecutable standalone

### Windows x64

```bash
dotnet publish src/PngtoWebp.Cli/PngtoWebp.Cli.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/win-x64
```

### Linux x64

```bash
dotnet publish src/PngtoWebp.Cli/PngtoWebp.Cli.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish/linux-x64
```

## Estructura

```text
PngtoWebp/
├── src/
│   ├── PngtoWebp.Core/       # Lógica de conversión reutilizable
│   └── PngtoWebp.Cli/        # Interfaz de línea de comandos
├── .github/workflows/        # Build automático
├── PngtoWebp.sln
└── README.md
```

## Decisiones técnicas

La lógica de conversión está aislada en `PngtoWebp.Core`, de modo que más adelante se puede agregar una interfaz WinForms, WPF, Avalonia, API REST o integración batch sin duplicar la lógica de imágenes.

Para optimización web, el valor inicial recomendado es **quality 80 / method 4**. El resultado real depende del contenido de la imagen; por eso la CLI informa el tamaño antes y después de cada conversión.
