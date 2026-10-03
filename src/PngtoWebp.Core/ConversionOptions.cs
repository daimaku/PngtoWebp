namespace PngtoWebp.Core;

/// <summary>
/// Configuración de una conversión a WebP.
/// </summary>
public sealed record ConversionOptions
{
    /// <summary>
    /// Calidad del WebP entre 0 y 100. En modo lossless representa esfuerzo de compresión.
    /// </summary>
    public int Quality { get; init; } = 80;

    /// <summary>
    /// Cuando es true genera WebP lossless; en caso contrario utiliza compresión lossy.
    /// </summary>
    public bool Lossless { get; init; }

    /// <summary>
    /// Nivel de esfuerzo del encoder entre 0 (rápido) y 6 (mejor compresión).
    /// </summary>
    public int EncodingMethod { get; init; } = 4;

    /// <summary>
    /// Procesa subdirectorios cuando la entrada es una carpeta.
    /// </summary>
    public bool Recursive { get; init; }

    /// <summary>
    /// Permite reemplazar archivos WebP ya existentes.
    /// </summary>
    public bool Overwrite { get; init; }

    /// <summary>
    /// Elimina metadata al guardar para reducir el tamaño final.
    /// </summary>
    public bool StripMetadata { get; init; } = true;

    /// <summary>
    /// Directorio de salida opcional.
    /// </summary>
    public string? OutputDirectory { get; init; }

    internal void Validate()
    {
        if (Quality is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(Quality), Quality, "Quality debe estar entre 0 y 100.");
        }

        if (EncodingMethod is < 0 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(EncodingMethod), EncodingMethod, "EncodingMethod debe estar entre 0 y 6.");
        }
    }
}
