namespace Anma.Applications.Helpers;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public static class SlugHelper
{
    public static string GenerateSlug(string input)
    {
        string normalized = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder();
        foreach (char c in normalized)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        string slug = sb.ToString();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");  
        slug = Regex.Replace(slug, @"\s+", "-");         
        slug = Regex.Replace(slug, @"-+", "-");         
        slug = slug.Trim('-');

        slug += "-" + GenerateShortHash(6);  // Exemple: jean-dupont-3f9a4c

        return slug;
    }

    private static string GenerateShortHash(int length)
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("=", "")
            .Replace("+", "")
            .Replace("/", "")
            .ToLower()
            .Substring(0, length);
    }
}

