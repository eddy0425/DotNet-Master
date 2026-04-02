using System.Runtime.CompilerServices;

namespace System
{
    /// <summary>
    /// 为 .NET Framework 提供 HashCode 兼容实现
    /// </summary>
    /// <remarks>
    /// 使用 FNV-1a 算法实现高效的哈希码组合
    /// </remarks>
    internal struct HashCode
    {
        private int _hashCode;
        private bool _initialized;

        private const int FnvPrime = 16777619;
        private const int FnvOffsetBasis = unchecked((int)2166136261);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize()
        {
            if (!_initialized)
            {
                _hashCode = FnvOffsetBasis;
                _initialized = true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add<T>(T value)
        {
            Initialize();
            int valueHash = value?.GetHashCode() ?? 0;
            _hashCode = unchecked((_hashCode ^ valueHash) * FnvPrime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToHashCode()
        {
            Initialize();
            return _hashCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine<T1, T2>(T1 value1, T2 value2)
        {
            unchecked
            {
                int hash = FnvOffsetBasis;
                hash = (hash ^ (value1?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value2?.GetHashCode() ?? 0)) * FnvPrime;
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
        {
            unchecked
            {
                int hash = FnvOffsetBasis;
                hash = (hash ^ (value1?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value2?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value3?.GetHashCode() ?? 0)) * FnvPrime;
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
        {
            unchecked
            {
                int hash = FnvOffsetBasis;
                hash = (hash ^ (value1?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value2?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value3?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value4?.GetHashCode() ?? 0)) * FnvPrime;
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
        {
            unchecked
            {
                int hash = FnvOffsetBasis;
                hash = (hash ^ (value1?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value2?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value3?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value4?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value5?.GetHashCode() ?? 0)) * FnvPrime;
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
        {
            unchecked
            {
                int hash = FnvOffsetBasis;
                hash = (hash ^ (value1?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value2?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value3?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value4?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value5?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value6?.GetHashCode() ?? 0)) * FnvPrime;
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
        {
            unchecked
            {
                int hash = FnvOffsetBasis;
                hash = (hash ^ (value1?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value2?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value3?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value4?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value5?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value6?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value7?.GetHashCode() ?? 0)) * FnvPrime;
                return hash;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
        {
            unchecked
            {
                int hash = FnvOffsetBasis;
                hash = (hash ^ (value1?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value2?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value3?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value4?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value5?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value6?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value7?.GetHashCode() ?? 0)) * FnvPrime;
                hash = (hash ^ (value8?.GetHashCode() ?? 0)) * FnvPrime;
                return hash;
            }
        }
    }
}

