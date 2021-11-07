using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

    public class ReplaceFilter : IFilter<string>
    {
        public Regex regex;
        public string _replace;
        public bool asRegex = false;

        private ReplaceFilter() { }

        public ReplaceFilter(string replace, string find)
        {
            regex = new Regex(find, RegexOptions.IgnoreCase);
            _replace = replace;
        }

        public ReplaceFilter(string replace, params string[] find)
        {
            regex = new Regex(string.Join("|", from word in find select Regex.Escape(word)), RegexOptions.IgnoreCase);
            _replace = replace;
        }

        public ReplaceFilter(string replace, Regex _regex)
        {
            _replace = replace;
            regex = _regex;
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
        private readonly Regex regex = new Regex("[" + Regex.Escape("–&.,:;^°_`´~+!\"§$% &/ () =?<>#|'’") + "\\-]");
        public override string ApplySingle(in string input)
        {
            var stringBuilder = new StringBuilder();
            foreach (var c in input)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != UnicodeCategory.OtherSymbol)
                {
                    stringBuilder.Append(c);
                }
            }
            return regex.Replace(stringBuilder.ToString(), "");
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

    class NumberToRomanFilter : IFilter<string>
    {
        static readonly Regex numberRegex = new Regex(@"(?<!\w)[1-9][0-9]*(?!\w)");

        public override string ApplySingle(in string input)
        {
            return numberRegex.Replace(input, match => {
                if (int.TryParse(match.Value, out var number))
                {
                    return ToRoman(number);
                } else
                {
                    return match.Value;
                }
            });
        }

        public string ToRoman(int number)
        {
            if (number < 1 || number > 4000)
            {
                return number.ToString();
            }

            var roman = new StringBuilder();
            while (number > 0)
            {
                if (number >= 1000) { roman.Append("M"); number -= 1000; continue; }
                if (number >= 900) { roman.Append("CM"); number -= 900; continue; }
                if (number >= 500) { roman.Append("D"); number -= 500; continue; }
                if (number >= 400) { roman.Append("CD"); number -= 400; continue; }
                if (number >= 100) { roman.Append("C"); number -= 100; continue; }
                if (number >= 90) { roman.Append("XC"); number -= 90; continue; }
                if (number >= 50) { roman.Append("L"); number -= 50; continue; }
                if (number >= 40) { roman.Append("XL"); number -= 40; continue; }
                if (number >= 10) { roman.Append("X"); number -= 10; continue; }
                if (number >= 9) { roman.Append("IX"); number -= 9; continue; }
                if (number >= 5) { roman.Append("V"); number -= 5; continue; }
                if (number >= 4) { roman.Append("IV"); number -= 4; continue; }
                if (number >= 1) { roman.Append("I"); number -= 1; continue; }
            }
            return roman.ToString();
        }
    }
}
