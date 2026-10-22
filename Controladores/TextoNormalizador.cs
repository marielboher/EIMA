using System.Globalization;
using System.Text;

namespace Controladores;

/// <summary>
/// Comparación de textos ignorando mayúsculas/minúsculas y acentos
/// (p. ej. "Aula 1" ≡ "aula 1", "Geografía" ≡ "geografia").
/// </summary>
internal static class TextoNormalizador
{
    /// <summary>Minúsculas + sin diacríticos (á→a, é→e); conserva ñ.</summary>
    public static string Normalizar(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return string.Empty;

        // Preservar ñ (no es un acento: "año" ≠ "ano").
        const char marcadorEne = '\u0001';
        var preparado = valor.Trim()
            .ToLowerInvariant()
            .Replace('ñ', marcadorEne);

        var formD = preparado.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace(marcadorEne, 'ñ');
    }

    /// <summary>Compara dos textos con la misma normalización.</summary>
    public static bool SonIguales(string? a, string? b) =>
        Normalizar(a) == Normalizar(b);
}
