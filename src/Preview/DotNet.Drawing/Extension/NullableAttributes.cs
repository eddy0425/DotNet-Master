// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>
    /// 目标框架 (net45) 的 BCL 里没有这组可空性契约特性，与 <see cref="Runtime.CompilerServices.IsExternalInit"/>
    /// 同理，在本程序集里补一份。编译器只按全名识别这些特性，不要求它们来自 BCL，
    /// 也不要求是 public —— 特性会随参数一起写进元数据，引用本程序集的工程同样能吃到收窄效果。
    /// <para>
    /// 没有它们，<c>NotNull()</c> / <c>IsUsableRegion()</c> 这类判空辅助方法对编译器就是普通的 bool 方法：
    /// 每个调用点判过之后仍要再写一个 <c>!</c> 才能消警，等于把「已经判过了」这件事重复告诉编译器两遍。
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    internal sealed class NotNullWhenAttribute : Attribute
    {
        public NotNullWhenAttribute(bool returnValue) { ReturnValue = returnValue; }

        /// <summary> 参数保证非空时，方法的返回值。 </summary>
        public bool ReturnValue { get; private set; }
    }

    /// <summary>
    /// 允许向非空成员写入 null：getter 永不返回 null、但 setter 接受 null 并自行回落时使用
    /// （如 <c>Log.Current</c>）。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    internal sealed class AllowNullAttribute : Attribute
    {
    }
}
