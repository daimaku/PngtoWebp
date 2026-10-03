# PngtoWebp

Aplicación en **.NET 8** para convertir imágenes **PNG, JPG/JPEG y BMP a WebP**, pensada para optimizar assets para web.

Incluye dos interfaces sobre el mismo motor de conversión:

- **GUI WinForms para Windows**: uso visual, selección múltiple y drag & drop, sin escribir comandos.
- **CLI multiplataforma**: útil para scripts, automatizaciones y procesamiento batch.

El proyecto usa [SkiaSharp](https://github.com/mono/SkiaSharp), motor gráfico open source con licencia MIT, para decodificar imágenes y generar WebP sin depender de `System.Drawing`.

## GUI de Windows

La aplicación visual está en `src/PngtoWebp.Gui`.

### Funciones

- Selección múltiple de PNG, JPG/JPEG y BMP.
- Drag & drop de archivos sobre la ventana.
- Conversión automática al agregar archivos, configurable mediante checkbox.
- Botón manual **Convertir archivos**.
- Calidad WebP configurable de 0 a 100.
- WebP lossy o lossless.
- Opción para sobrescribir archivos existentes.
- Guardar cada WebP junto al archivo original o elegir una carpeta común de salida.
- Barra de progreso.
- Cancelación de una conversión en curso.
- Estado individual por archivo.
- Muestra el porcentaje de ahorro conseguido por cada imagen.
- Evita agregar archivos duplicados a la lista.

### Flujo normal

1. Abrir `PngtoWebp.exe`.
2. Dejar calidad `80` como punto de partida o elegir otra.
3. Pulsar **Agregar archivos...** y seleccionar todas las imágenes deseadas.
4. Con **Convertir automáticamente al agregar archivos** activado, la conversión comienza inmediatamente.
5. Los `.webp` se guardan junto a cada original por defecto.

También se pueden arrastrar varios archivos directamente sobre la ventana.

## Ejecutar desde Visual Studio

Abrir `PngtoWebp.sln`, establecer `PngtoWebp.Gui` como proyecto de inicio y ejecutar.

## Compilar

```bash
dotnet restore PngtoWebp.sln
dotnet build PngtoWebp.sln -c Release
```

## Publicar la GUI como ejecutable Windows x64

```bash
dotnet publish src/PngtoWebp.Gui/PngtoWebp.Gui.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o ./publish/win-x64
```

Esto genera una versión self-contained para Windows, por lo que el usuario final no necesita instalar manualmente el runtime de .NET.

GitHub Actions también publica automáticamente un artifact llamado **PngtoWebp-Windows-x64** en cada build exitoso de `master`.

## CLI

La interfaz de línea de comandos continúa disponible en `src/PngtoWebp.Cli`.

### Archivo individual

```bash
dotnet run --project src/PngtoWebp.Cli -- "foto.png"
```

### Carpeta completa

```bash
dotnet run --project src/PngtoWebp.Cli -- "./imagenes" --recursive --quality 85
```

### Lossless

```bash
dotnet run --project src/PngtoWebp.Cli -- "logo.png" --lossless
```

### Opciones CLI

| Opción | Descripción |
|---|---|
| `-o`, `--output <dir>` | Directorio de salida. |
| `-q`, `--quality <0-100>` | Calidad WebP. Default: `80`. |
| `--lossless` | Usa WebP lossless. |
| `-r`, `--recursive` | Procesa subdirectorios. |
| `-f`, `--overwrite` | Sobrescribe archivos `.webp` existentes. |
| `-h`, `--help` | Muestra ayuda. |

## Arquitectura

```text
PngtoWebp/
├── src/
│   ├── PngtoWebp.Core/       # Motor de conversión reutilizable
│   ├── PngtoWebp.Cli/        # Interfaz de línea de comandos
│   └── PngtoWebp.Gui/        # GUI WinForms para Windows
├── .github/workflows/        # CI, pruebas y publicación del ejecutable
├── PngtoWebp.sln
└── README.md
```

`PngtoWebp.Core` contiene toda la lógica de conversión. Tanto la GUI como la CLI consumen ese proyecto, evitando duplicar código o mantener dos implementaciones distintas del encoder.

## Valores recomendados para web

- **Fotografías:** quality `75-85`, lossy.
- **Assets con transparencia:** quality `80-90`, lossy; revisar visualmente bordes y sombras.
- **Logos/UI que no toleran pérdida:** lossless.

El tamaño final depende del contenido de cada imagen. La aplicación visual informa el porcentaje de ahorro por archivo y la CLI informa tamaño original, tamaño WebP y ahorro.
