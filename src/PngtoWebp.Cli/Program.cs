using PngtoWebp.Core;

namespace PngtoWebp.Cli;

internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;
    private const int ExitInvalidArguments = 2;
    private const int ExitCancelled = 130;

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args.Any(IsHelpArgument))
        {
            PrintHelp();
            return ExitSuccess;
        }

        var parsed = ParseArguments(args);
        if (parsed is null)
        {
            return ExitInvalidArguments;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        try
        {
            var converter = new ImageConverterService();
            var result = await converter.ConvertAsync(
                parsed.InputPath,
                parsed.Options,
                cancellationTokenSource.Token);

            PrintResults(result);
            return result.Failed > 0 ? ExitFailure : ExitSuccess;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Conversión cancelada.");
            return ExitCancelled;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return ExitFailure;
        }
    }

    private static ParsedArguments? ParseArguments(string[] args)
    {
        string? inputPath = null;
        var options = new ConversionOptions();

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];

            switch (argument)
            {
                case "-o":
                case "--output":
                    if (!TryReadValue(args, ref index, argument, out var outputDirectory))
                    {
                        return null;
                    }

                    options = options with { OutputDirectory = outputDirectory };
                    break;

                case "-q":
                case "--quality":
                    if (!TryReadValue(args, ref index, argument, out var qualityText))
                    {
                        return null;
                    }

                    if (!int.TryParse(qualityText, out var quality) || quality is < 0 or > 100)
                    {
                        Console.Error.WriteLine("Error: --quality debe ser un número entre 0 y 100.");
                        return null;
                    }

                    options = options with { Quality = quality };
                    break;

                case "--method":
                    if (!TryReadValue(args, ref index, argument, out var methodText))
                    {
                        return null;
                    }

                    if (!int.TryParse(methodText, out var method) || method is < 0 or > 6)
                    {
                        Console.Error.WriteLine("Error: --method debe ser un número entre 0 y 6.");
                        return null;
                    }

                    options = options with { EncodingMethod = method };
                    break;

                case "--lossless":
                    options = options with { Lossless = true };
                    break;

                case "-r":
                case "--recursive":
                    options = options with { Recursive = true };
                    break;

                case "-f":
                case "--overwrite":
                    options = options with { Overwrite = true };
                    break;

                case "--keep-metadata":
                    options = options with { StripMetadata = false };
                    break;

                default:
                    if (argument.StartsWith('-', StringComparison.Ordinal))
                    {
                        Console.Error.WriteLine($"Error: opción desconocida '{argument}'.");
                        Console.Error.WriteLine("Use --help para ver las opciones disponibles.");
                        return null;
                    }

                    if (inputPath is not null)
                    {
                        Console.Error.WriteLine("Error: solo puede especificarse una ruta de entrada.");
                        return null;
                    }

                    inputPath = argument;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            Console.Error.WriteLine("Error: debe indicar un archivo o directorio de entrada.");
            return null;
        }

        return new ParsedArguments(inputPath, options);
    }

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        out string value)
    {
        if (index + 1 >= args.Count)
        {
            Console.Error.WriteLine($"Error: falta el valor para {option}.");
            value = string.Empty;
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static void PrintResults(BatchConversionResult batch)
    {
        if (batch.Total == 0)
        {
            Console.WriteLine("No se encontraron imágenes PNG, JPG/JPEG o BMP para convertir.");
            return;
        }

        foreach (var file in batch.Files)
        {
            switch (file.Status)
            {
                case ConversionStatus.Success:
                    Console.WriteLine(
                        $"[OK]   {file.InputPath} -> {file.OutputPath} " +
                        $"({FormatBytes(file.SourceBytes)} -> {FormatBytes(file.OutputBytes)}, " +
                        $"ahorro {file.SavingsPercentage:F1}%)");
                    break;

                case ConversionStatus.Skipped:
                    Console.WriteLine($"[SKIP] {file.InputPath}: {file.Message}");
                    break;

                case ConversionStatus.Failed:
                    Console.Error.WriteLine($"[ERR]  {file.InputPath}: {file.Message}");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        Console.WriteLine();
        Console.WriteLine("Resumen");
        Console.WriteLine($"  Total:       {batch.Total}");
        Console.WriteLine($"  Convertidos: {batch.Successful}");
        Console.WriteLine($"  Omitidos:    {batch.Skipped}");
        Console.WriteLine($"  Errores:     {batch.Failed}");

        if (batch.Successful > 0)
        {
            Console.WriteLine(
                $"  Tamaño:      {FormatBytes(batch.TotalSourceBytes)} -> " +
                $"{FormatBytes(batch.TotalOutputBytes)} ({batch.SavingsPercentage:F1}% de ahorro)");
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;

        while (value >= 1024d && unit < units.Length - 1)
        {
            value /= 1024d;
            unit++;
        }

        return unit == 0
            ? $"{value:0} {units[unit]}"
            : $"{value:0.##} {units[unit]}";
    }

    private static bool IsHelpArgument(string argument) =>
        argument is "-h" or "--help" or "/?";

    private static void PrintHelp()
    {
        Console.WriteLine(
            """
            PngtoWebp - Conversor PNG/JPG/BMP a WebP para .NET 8

            USO
              pngtowebp <archivo-o-carpeta> [opciones]

            OPCIONES
              -o, --output <dir>     Directorio de salida.
              -q, --quality <0-100>  Calidad WebP. Default: 80.
                  --lossless         Genera WebP lossless.
              -r, --recursive        Procesa subdirectorios.
              -f, --overwrite        Sobrescribe WebP existentes.
                  --keep-metadata    Conserva metadata de la imagen.
                  --method <0-6>     Esfuerzo del encoder. Default: 4.
              -h, --help             Muestra esta ayuda.

            EJEMPLOS
              pngtowebp "foto.png"
              pngtowebp "./imagenes" --recursive --quality 85
              pngtowebp "logo.png" --lossless
              pngtowebp "./imagenes" -o "./webp" -r -f
            """);
    }

    private sealed record ParsedArguments(string InputPath, ConversionOptions Options);
}
