# PngtoWebp

Aplicación en **.NET 8** para convertir imágenes **PNG, JPG/JPEG y BMP a WebP o SVG**.

Incluye dos interfaces sobre el mismo motor de conversión:

- **GUI WinForms para Windows**: uso visual, selección múltiple y drag & drop, sin escribir comandos.
- **CLI multiplataforma**: útil para scripts, automatizaciones y procesamiento batch.

El proyecto usa [SkiaSharp](https://github.com/mono/SkiaSharp), motor gráfico open source con licencia MIT, para decodificar imágenes y generar WebP. Para SVG, la imagen raster se incorpora dentro de un documento SVG válido como PNG embebido en Base64.

> **Importante sobre SVG:** exportar una imagen raster a SVG no la convierte automáticamente en vectores. El modo SVG de esta aplicación conserva la imagen original como raster embebido dentro de un contenedor SVG. Esto mantiene dimensiones y transparencia, pero no realiza vectorización/trazado de formas.

## GUI de Windows

La aplicación visual está en `src/PngtoWebp.Gui`.

### Funciones

- Selección múltiple de PNG, JPG/JPEG y BMP.
- Drag & drop de archivos sobre la ventana.
- Dropdown **Formato de salida** con:
  - **WebP** por defecto.
  - **SVG**.
- La conversión **nunca comienza automáticamente** al seleccionar o arrastrar archivos.
- El usuario elige el formato y luego pulsa **Convertir archivos**.
- Calidad WebP configurable de 0 a 100.
- WebP lossy o lossless.
- Los controles de calidad y lossless se deshabilitan al seleccionar SVG porque no aplican a ese formato.
- Opción para sobrescribir archivos existentes.
- Guardar cada archivo convertido junto al original o elegir una carpeta común de salida.
- Barra de progreso.
- Cancelación de una conversión en curso.
- Estado individual por archivo.
- En WebP muestra el porcentaje de ahorro conseguido por cada imagen.
- Evita agregar archivos duplicados a la lista.

### Flujo normal

1. Abrir `PngtoWebp.exe`.
2. Pulsar **Agregar archivos...** o arrastrar imágenes sobre la ventana.
3. Elegir **WebP** o **SVG** en el dropdown **Formato de salida**.
4. Si se eligió WebP, configurar calidad y opcionalmente lossless.
5. Elegir dónde guardar los archivos si no se desea usar la carpeta original.
6. Pulsar **Convertir archivos**.

Seleccionar archivos solamente los agrega a la lista; nunca inicia la conversión por sí solo.

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

GitHub Actions publica automáticamente un artifact llamado **PngtoWebp-Windows-x64** en cada build exitoso de `master`.

## CLI

La interfaz de línea de comandos continúa disponible en `src/PngtoWebp.Cli` y ahora también permite seleccionar el formato de salida.

### WebP

```bash
dotnet run --project src/PngtoWebp.Cli -- "foto.png" --format webp --quality 80
```

### SVG

```bash
dotnet run --project src/PngtoWebp.Cli -- "foto.png" --format svg
```

### Carpeta completa

```bash
dotnet run --project src/PngtoWebp.Cli -- "./imagenes" --format webp --recursive --quality 85
```

### Opciones CLI

| Opción | Descripción |
|---|---|
| `-o`, `--output <dir>` | Directorio de salida. |
| `--format <webp|svg>` | Formato de salida. Default: `webp`. |
| `-q`, `--quality <0-100>` | Calidad WebP. Default: `80`. No aplica a SVG. |
| `--lossless` | Usa WebP lossless. No aplica a SVG. |
| `-r`, `--recursive` | Procesa subdirectorios. |
| `-f`, `--overwrite` | Sobrescribe archivos de salida existentes. |
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

`PngtoWebp.Core` contiene toda la lógica de conversión. `ConversionOptions.Format` define si el destino es `Webp` o `Svg`, y tanto la GUI como la CLI consumen el mismo motor.

## Comportamiento de SVG

El archivo SVG generado contiene:

- `width` y `height` iguales a las dimensiones de la imagen.
- `viewBox` equivalente.
- La imagen convertida internamente a PNG y embebida mediante `data:image/png;base64,...`.
- Transparencia conservada cuando la imagen original la contiene.
- Un único archivo `.svg` autocontenido, sin dependencias externas.

Este enfoque es adecuado cuando se necesita entregar o consumir la imagen dentro de un documento SVG. Si el objetivo es convertir logos o dibujos raster en **paths vectoriales editables**, hace falta una etapa adicional de vectorización/trazado, que es un proceso distinto.

## Valores recomendados para WebP

- **Fotografías:** quality `75-85`, lossy.
- **Assets con transparencia:** quality `80-90`, lossy; revisar visualmente bordes y sombras.
- **Logos/UI que no toleran pérdida:** lossless.

El tamaño final depende del contenido de cada imagen. En WebP la aplicación visual informa el porcentaje de ahorro por archivo.
