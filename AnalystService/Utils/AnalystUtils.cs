using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Analyst.Utils
{
    static class AnalystUtils
    {
        public static string SimplifyQuery(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            name = name.ToLowerInvariant();

            // Удаляем все символы кроме букв, цифр, пробелов и дефисов
            name = Regex.Replace(name, @"[^\w\s\-]", "");

            // Заменяем несколько пробелов на один и убираем пробелы по краям
            name = Regex.Replace(name, @"\s+", " ").Trim();

            // Извлекаем объём или вес
            string volume = ExtractVolumeOrWeight(name);

            // Берём максимум 5 слов
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string basePart = string.Join(" ", words.Take(5));

            // Склеиваем с volume и приводим к TitleCase
            string result = (basePart + " " + volume).Trim();
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result);
        }

        public static string ExtractVolumeOrWeight(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var pattern = @"(\d+(\.\d+)?)(\s)?(ml|мл|g|гр|kg|кг|л|l)\b";
            var match = Regex.Match(name.ToLower(), pattern, RegexOptions.IgnoreCase);

            return match.Success ? match.Value.Replace(" ", "") : string.Empty;
        }
    }
}
