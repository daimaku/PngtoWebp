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
    /// Procesa subdirectorios cuando la entrada es una carpeta.
    /// </summary>
    public bool Recursive { get; init; }

    /// <summary>
    /// Permite reemplazar archivos WebP ya existentes.
    /// </summary>
    public bool Overwrite { get; init; }

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
    }
}
