using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MommyAndSunny.Application.Common.Utils
{
    public static class SlugGenerator
    {
        public static string GenerateSlug(string input)
        {
            var noDiacritics = RemoveDiacritics(input.ToLowerInvariant());
            var cleaned = Regex.Replace(noDiacritics, @"[^a-z0-9\s-]", "");
            var slug = Regex.Replace(cleaned, @"\s+", "-").Trim('-');
            return slug;
        }

        private static string RemoveDiacritics(string text)
        {
            // "đ"/"Đ" không tách được bằng Unicode NFD (là 1 ký tự riêng, không phải base + dấu),
            // nên phải xử lý thủ công trước khi decompose các dấu còn lại
            text = text.Replace("đ", "d").Replace("Đ", "d");

            var normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
