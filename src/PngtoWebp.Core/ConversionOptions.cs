namespace PngtoWebp.Core;

/// <summary>
/// Configuración de una conversión de imagen.
/// </summary>
public sealed record ConversionOptions
{
    /// <summary>
    /// Formato de salida. WebP es el valor por defecto.
    /// </summary>
    public OutputFormat Format { get; init; } = OutputFormat.Webp;

    /// <summary>
    /// Calidad del WebP entre 0 y 100. No se utiliza para SVG.
    /// </summary>
    public int Quality { get; init; } = 80;

    /// <summary>
    /// Cuando es true genera WebP lossless. No se utiliza para SVG.
    /// </summary>
    public bool Lossless { get; init; }

    /// <summary>
    /// Procesa subdirectorios cuando la entrada es una carpeta.
    /// </summary>
    public bool Recursive { get; init; }

    /// <summary>
    /// Permite reemplazar archivos de salida ya existentes.
    /// </summary>
    public bool Overwrite { get; init; }

    /// <summary>
    /// Directorio de salida opcional.
    /// </summary>
    public string? OutputDirectory { get; init; }

    internal void Validate()
    {
        if (!Enum.IsDefined(Format))
        {
            throw new ArgumentOutOfRangeException(nameof(Format), Format, "Formato de salida no soportado.");
        }

        if (Quality is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(Quality), Quality, "Quality debe estar entre 0 y 100.");
        }
    }
}
