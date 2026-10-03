namespace PngtoWebp.Core;

public enum ConversionStatus
{
    Success,
    Skipped,
    Failed
}

/// <summary>
/// Resultado de la conversión de un archivo.
/// </summary>
public sealed record ConversionResult(
    string InputPath,
    string? OutputPath,
    ConversionStatus Status,
    long SourceBytes,
    long OutputBytes,
    string? Message = null)
{
    public long SavedBytes => Status == ConversionStatus.Success ? SourceBytes - OutputBytes : 0;

    public double SavingsPercentage =>
        Status == ConversionStatus.Success && SourceBytes > 0
            ? (SourceBytes - OutputBytes) * 100d / SourceBytes
            : 0d;
}

/// <summary>
/// Resultado agregado para una entrada que puede contener uno o muchos archivos.
/// </summary>
public sealed record BatchConversionResult(IReadOnlyList<ConversionResult> Files)
{
    public int Total => Files.Count;
    public int Successful => Files.Count(x => x.Status == ConversionStatus.Success);
    public int Skipped => Files.Count(x => x.Status == ConversionStatus.Skipped);
    public int Failed => Files.Count(x => x.Status == ConversionStatus.Failed);
    public long TotalSourceBytes => Files.Where(x => x.Status == ConversionStatus.Success).Sum(x => x.SourceBytes);
    public long TotalOutputBytes => Files.Where(x => x.Status == ConversionStatus.Success).Sum(x => x.OutputBytes);

    public double SavingsPercentage =>
        TotalSourceBytes > 0
            ? (TotalSourceBytes - TotalOutputBytes) * 100d / TotalSourceBytes
            : 0d;
}
