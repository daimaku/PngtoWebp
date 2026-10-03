# PngtoWebp

Herramienta CLI en **.NET 8** para convertir imágenes **PNG, JPG/JPEG y BMP a WebP**, pensada para optimizar assets para web.

El proyecto usa [SkiaSharp](https://github.com/mono/SkiaSharp), un motor gráfico open source con licencia MIT, para decodificar las imágenes y generar WebP sin depender de `System.Drawing`.

## Características

- PNG, JPG/JPEG y BMP → WebP.
- Conversión de un archivo individual o de una carpeta completa.
- Exploración recursiva opcional.
- Calidad configurable de 0 a 100.
- Modo WebP **lossy** (por defecto) o **lossless**.
- Preserva transparencias cuando el formato de origen las contiene.
- La recodificación elimina metadata innecesaria del archivo de salida, reduciendo bytes que no son necesarios para servir la imagen en web.
- Escritura atómica mediante archivo temporal para evitar archivos WebP incompletos.
- Opción de sobrescritura explícita.
- Mantiene la estructura de subdirectorios durante conversiones recursivas.
- Resumen final con cantidad de archivos y ahorro real de espacio.
- Código de conversión aislado en una librería reutilizable (`PngtoWebp.Core`).
- Build automático con GitHub Actions.

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

La jerarquía de subcarpetas se conserva dentro de la carpeta de salida.

### Calidad 85

```bash
dotnet run --project src/PngtoWebp.Cli -- "./imagenes" --quality 85 --recursive
```

### Lossless

Útil principalmente para logos, UI, capturas o imágenes en las que no se quiere pérdida adicional:

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
| `-q`, `--quality <0-100>` | Calidad WebP. Default: `80`. En lossless controla el esfuerzo de compresión. |
| `--lossless` | Usa WebP lossless. |
| `-r`, `--recursive` | Procesa subdirectorios. |
| `-f`, `--overwrite` | Sobrescribe archivos `.webp` existentes. |
| `-h`, `--help` | Muestra ayuda. |

## Comportamiento de salida

- **Archivo individual:** si no se especifica `--output`, el `.webp` se crea junto al archivo original.
- **Carpeta:** si no se especifica `--output`, se crea `<carpeta>/webp`.
- Si el `.webp` ya existe, se omite de forma segura salvo que se use `--overwrite`.
- En conversiones recursivas, la estructura relativa de directorios se conserva.
- Si dos archivos de la misma carpeta tienen el mismo nombre base (por ejemplo `foto.png` y `foto.jpg`), ambos apuntan a `foto.webp`; el segundo se omitirá salvo que se use `--overwrite`.

## Publicar como ejecutable standalone

SkiaSharp utiliza una librería nativa. Al publicar como single-file conviene habilitar la extracción automática de librerías nativas.

### Windows x64

```bash
dotnet publish src/PngtoWebp.Cli/PngtoWebp.Cli.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./publish/win-x64
```

### Linux x64

```bash
dotnet publish src/PngtoWebp.Cli/PngtoWebp.Cli.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./publish/linux-x64
```

## Estructura

```text
PngtoWebp/
├── src/
│   ├── PngtoWebp.Core/       # Motor de conversión reutilizable
│   └── PngtoWebp.Cli/        # Interfaz de línea de comandos
├── .github/workflows/        # Build y smoke test automáticos
├── PngtoWebp.sln
└── README.md
```

## Arquitectura

`PngtoWebp.Core` no conoce nada de la consola. Recibe una ruta y `ConversionOptions`, realiza la conversión y devuelve `BatchConversionResult`. La CLI solamente interpreta argumentos y presenta resultados.

Esto permite agregar después una interfaz **WinForms, WPF o Avalonia**, una API REST o un worker batch sin duplicar la lógica de conversión.

## Valores recomendados para web

Como punto de partida:

- **Fotografías:** quality `75-85`, lossy.
- **Assets con transparencia:** quality `80-90`, lossy; revisar visualmente bordes y sombras.
- **Logos/UI que no toleran pérdida:** `--lossless`.

El resultado depende del contenido de cada imagen. La CLI muestra el tamaño original, el tamaño WebP y el porcentaje de ahorro para que la decisión se base en el resultado real.
