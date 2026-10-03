using SkiaSharp;

namespace PngtoWebp.Core;

/// <summary>
/// Servicio reutilizable para convertir PNG, JPG/JPEG y BMP a WebP.
/// </summary>
public sealed class ImageConverterService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp"
    };

    public static bool IsSupported(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path));

    public Task<BatchConversionResult> ConvertAsync(
        string inputPath,
        ConversionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        cancellationToken.ThrowIfCancellationRequested();

        options ??= new ConversionOptions();
        options.Validate();

        var fullInputPath = Path.GetFullPath(inputPath);

        if (File.Exists(fullInputPath))
        {
            if (!IsSupported(fullInputPath))
            {
                throw new NotSupportedException(
                    $"Formato no soportado: '{Path.GetExtension(fullInputPath)}'. Formatos permitidos: PNG, JPG/JPEG y BMP.");
            }

            var outputPath = BuildSingleFileOutputPath(fullInputPath, options.OutputDirectory);
            var result = ConvertFile(fullInputPath, outputPath, options, cancellationToken);
            return Task.FromResult(new BatchConversionResult(new[] { result }));
        }

        if (Directory.Exists(fullInputPath))
        {
            return Task.FromResult(ConvertDirectory(fullInputPath, options, cancellationToken));
        }

        throw new FileNotFoundException($"No existe el archivo o directorio de entrada: {fullInputPath}", fullInputPath);
    }

    private static BatchConversionResult ConvertDirectory(
        string inputDirectory,
        ConversionOptions options,
        CancellationToken cancellationToken)
    {
        var outputRoot = string.IsNullOrWhiteSpace(options.OutputDirectory)
            ? Path.Combine(inputDirectory, "webp")
            : Path.GetFullPath(options.OutputDirectory);

        Directory.CreateDirectory(outputRoot);

        var searchOption = options.Recursive
            ? SearchOption.AllDirectories
            : SearchOption.TopDirectoryOnly;

        var outputIsChildDirectory =
            !PathsEqual(inputDirectory, outputRoot) &&
            IsInsideDirectory(outputRoot, inputDirectory);

        var files = Directory
            .EnumerateFiles(inputDirectory, "*", searchOption)
            .Where(IsSupported)
            .Where(path => !outputIsChildDirectory || !IsInsideDirectory(path, outputRoot))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var results = new List<ConversionResult>(files.Length);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(inputDirectory, file);
            var relativeDirectory = Path.GetDirectoryName(relativePath);
            var targetDirectory = string.IsNullOrEmpty(relativeDirectory)
                ? outputRoot
                : Path.Combine(outputRoot, relativeDirectory);

            var outputPath = Path.Combine(
                targetDirectory,
                $"{Path.GetFileNameWithoutExtension(file)}.webp");

            results.Add(ConvertFile(file, outputPath, options, cancellationToken));
        }

        return new BatchConversionResult(results);
    }

    private static ConversionResult ConvertFile(
        string inputPath,
        string outputPath,
        ConversionOptions options,
        CancellationToken cancellationToken)
    {
        var sourceBytes = new FileInfo(inputPath).Length;

        if (File.Exists(outputPath) && !options.Overwrite)
        {
            return new ConversionResult(
                inputPath,
                outputPath,
                ConversionStatus.Skipped,
                sourceBytes,
                new FileInfo(outputPath).Length,
                "El archivo de salida ya existe. Use --overwrite para reemplazarlo.");
        }

        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new InvalidOperationException($"No se pudo determinar el directorio de salida para '{outputPath}'.");
        }

        Directory.CreateDirectory(outputDirectory);

        var tempPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var bitmap = SKBitmap.Decode(inputPath)
                ?? throw new InvalidDataException($"No se pudo decodificar la imagen '{inputPath}'.");
            using var pixmap = bitmap.PeekPixels();

            var encoderOptions = new SKWebpEncoderOptions(
                options.Lossless
                    ? SKWebpEncoderCompression.Lossless
                    : SKWebpEncoderCompression.Lossy,
                options.Quality);

            using (var outputStream = new FileStream(
                       tempPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None))
            {
                if (!SKWebpEncoder.Encode(outputStream, pixmap, encoderOptions))
                {
                    throw new InvalidOperationException($"El encoder WebP no pudo procesar '{inputPath}'.");
                }

                outputStream.Flush(flushToDisk: true);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(tempPath, outputPath, options.Overwrite);

            var outputBytes = new FileInfo(outputPath).Length;
            return new ConversionResult(
                inputPath,
                outputPath,
                ConversionStatus.Success,
                sourceBytes,
                outputBytes);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ConversionResult(
                inputPath,
                outputPath,
                ConversionStatus.Failed,
                sourceBytes,
                0,
                ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Si el sistema operativo mantiene un handle abierto, no se oculta el resultado principal.
                }
            }
        }
    }

    private static string BuildSingleFileOutputPath(string inputPath, string? outputDirectory)
    {
        var targetDirectory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.GetDirectoryName(inputPath)
            : Path.GetFullPath(outputDirectory);

        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new InvalidOperationException($"No se pudo determinar el directorio de salida para '{inputPath}'.");
        }

        return Path.Combine(targetDirectory, $"{Path.GetFileNameWithoutExtension(inputPath)}.webp");
    }

    private static bool IsInsideDirectory(string candidatePath, string directoryPath)
    {
        var relative = Path.GetRelativePath(
            Path.GetFullPath(directoryPath),
            Path.GetFullPath(candidatePath));

        return !Path.IsPathRooted(relative)
               && !string.Equals(relative, "..", StringComparison.Ordinal)
               && !relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
               && !relative.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static bool PathsEqual(string left, string right)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            comparison);
    }
}
