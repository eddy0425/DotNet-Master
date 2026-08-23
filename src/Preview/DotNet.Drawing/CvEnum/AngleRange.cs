namespace DotNet.Drawing
{
    /// <summary>指定角度的归一化范围。</summary>
    public enum AngleRange
    {
        /// <summary>范围为 (-360, 360) 度（仅按 360 度取模，保留原符号）。</summary>
        Minus360To360 = 0,

        /// <summary>范围为 [-180, 180] 度。</summary>
        Minus180To180 = 1,

        /// <summary>范围为 [0, 180) 度（按 180 度周期折叠到非负区间，±180 同折为 0）。</summary>
        Range0To180 = 2,

        /// <summary>范围为 (-180, 0] 度（按 180 度周期折叠到非正区间，±180 同折为 0）。</summary>
        Minus180To0 = 3,

        /// <summary>范围为 [-90, 90] 度（按 180 度周期折叠；±90 保留原符号，不再合并）。</summary>
        Minus90To90 = 4
    }
}
