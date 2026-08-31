using System;
using System.Linq.Expressions;
using System.Collections.Generic;


namespace DotNet.Drawing
{
    /// <summary>
    /// 利用表达式树进行对象浅拷贝 / 同构映射
    /// </summary>
    /// <remarks>
    /// <b>重要限制</b>：只复制 <c>TOut</c> 上<b>可写的公开属性</b>（<c>GetProperties()</c>）。
    /// <b>字段一律被跳过</b>，且不会有任何提示。若源类型把状态存在字段上
    /// （例如 <c>CvRegion.HoRegion</c> 这种必须作为 <c>out</c> 实参的 Halcon 句柄），
    /// 结果对象的该字段会保持默认值，产生"拷贝成功但内容缺失"的静默错误。
    /// <para>
    /// 另外它做的是<b>浅拷贝</b>：引用类型属性与源对象共享同一实例，
    /// 不适合用于需要独立所有权（如非托管句柄）的场景——那种情况请手写克隆。
    /// </para>
    /// </remarks>
    /// <typeparam name="TIn">输入</typeparam>
    /// <typeparam name="TOut">输出</typeparam>
    public static class TransExpV2<TIn, TOut>
    {
        private static readonly Func<TIn, TOut> cache = GetFunc();
        private static Func<TIn, TOut> GetFunc()
        {
            ParameterExpression parameterExpression = System.Linq.Expressions.Expression.Parameter(typeof(TIn), "p");
            List<MemberBinding> memberBindingList = new List<MemberBinding>();

            foreach (var item in typeof(TOut).GetProperties())
            {
                if (!item.CanWrite) continue;
                MemberExpression property = System.Linq.Expressions.Expression.Property(parameterExpression, typeof(TIn).GetProperty(item.Name));
                MemberBinding memberBinding = System.Linq.Expressions.Expression.Bind(item, property);
                memberBindingList.Add(memberBinding);
            }

            MemberInitExpression memberInitExpression = System.Linq.Expressions.Expression.MemberInit(System.Linq.Expressions.Expression.New(typeof(TOut)), memberBindingList.ToArray());
            Expression<Func<TIn, TOut>> lambda = System.Linq.Expressions.Expression.Lambda<Func<TIn, TOut>>(memberInitExpression, new ParameterExpression[] { parameterExpression });

            return lambda.Compile();
        }

        public static TOut Trans(TIn tIn)
        {
            return cache(tIn);
        }
    }
}
