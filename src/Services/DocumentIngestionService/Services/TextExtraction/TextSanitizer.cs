using System.Text;

namespace Services.TextExtraction;

/// <summary>
/// Strips non-text artifacts (control characters, decode-failure markers) that can otherwise leak into
/// extracted text from embedded fonts, invisible OCR text layers, or decoding errors, and would corrupt chunks.
/// </summary>
public static class TextSanitizer
{
    private const char ReplacementCharacter = (char)0xFFFD;

    public static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c == ReplacementCharacter)
            {
                continue;
            }

            var isAllowedWhitespace = c is '\n' or '\r' or '\t';
            if (char.IsControl(c) && !isAllowedWhitespace)
            {
                continue;
            }

            sb.Append(c);
        }

        return sb.ToString();
    }
}
