using System.Text;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;


namespace DotNet.Drawing
{
    public static class StringExtension
    {
        public static string ToTmplPoint(this string strategyOutputKey)
        {
            return strategyOutputKey.Substring(0, strategyOutputKey.LastIndexOf('/') + 1) + "TmplPoint";
        }

        #region 数字处理
        private static readonly Regex _digitRegex = new Regex(@"\p{N}", RegexOptions.Compiled);

        /// <summary>
        /// 提取字符串中的所有数字字符并合并为整数（支持全角/汉字数字）
        /// </summary>
        public static int ExtractNumber(this string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;

            // 复用字符串处理方法
            var numberStr = str.ExtractNumberAsString();

            // 处理超大数字和无效格式
            return int.TryParse(numberStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
                ? result
                : 0;
        }

        /// <summary>
        /// 提取字符串中的所有数字字符（支持全角/半角自动转换）
        /// </summary>
        public static string ExtractNumberAsString(this string str)
        {
            if (string.IsNullOrEmpty(str)) return string.Empty;

            var normalized = str.Normalize(NormalizationForm.FormKC);
            var matches = _digitRegex.Matches(normalized);

            return string.Join("",
                matches.Cast<Match>()
                      .Select(m => ConvertToWesternDigit(m.Value))
            );
        }

        /// <summary>
        /// 将 Unicode 数字转换为半角数字（支持汉字、全角等）
        /// </summary>
        private static string ConvertToWesternDigit(string input)
        {
            return input.Aggregate(new StringBuilder(), (sb, c) =>
            {
                if (char.IsNumber(c))
                {
                    var num = char.GetNumericValue(c);
                    sb.Append(num.ToString(CultureInfo.InvariantCulture).FirstOrDefault());
                }
                return sb;
            }).ToString();
        }
        #endregion

    }
}
