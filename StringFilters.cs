using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DuplicateHider
{
    class PlaceboStringFilters : IFilter<string>
    {
        public override string ApplySingle(in string input)
        {
            return input;
        }
    }

    class CaseFilter : IFilter<string>
    {
        public enum Case { Keep, Upper, Lower };
        private readonly Case outputCase = Case.Keep;

        private CaseFilter() { }

        public CaseFilter(Case @case)
        {
            outputCase = @case;
        }

        public override string ApplySingle(in string input)
        {
            switch (outputCase)
            {
                case Case.Keep:
                    return input;
                case Case.Upper:
                    return input.ToUpper();
                case Case.Lower:
                    return input.ToLower();
            }
            return input;
        }
    }

    class ReplaceFilter : IFilter<string>
    {
        private readonly Regex regex;
        private readonly string _replace;
        private ReplaceFilter() { }

        public ReplaceFilter(string replace, string find)
        {
            regex = new Regex(find);
            _replace = replace;
        }

        public ReplaceFilter(string replace, params string[] find)
        {
            regex = new Regex(string.Join("|", from word in find select Regex.Escape(word)));
            _replace = replace;
        }

        public override string ApplySingle(in string input)
        {
            return regex.Replace(input, _replace);
        }
    }

    class WhiteSpaceFilter : IFilter<string>
    {
        private readonly Regex regex = new Regex(@"\s+");
        public override string ApplySingle(in string input)
        {
            return regex.Replace(input, "");
        }
    }

    class SpecialCharFilter : IFilter<string>
    {
        private readonly Regex regex = new Regex("[" + Regex.Escape("&.,:;^°_`´~+!\"§$% &/ () =?<>#|'’") + "\\-]");
        public override string ApplySingle(in string input)
        {
            return regex.Replace(input, "");
        }
    }

    class DiacriticsFilter : IFilter<string>
    {
        public override string ApplySingle(in string input)
        {
            return RemoveDiacritics(input);
        }

        static string RemoveDiacritics(in string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
