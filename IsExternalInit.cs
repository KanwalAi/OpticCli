// ============================================================
// File: ISExternalInit.cs
// Project: OpticCli
// Namespace: System
// Description: Backports C# 9+ features (init, Index, Range)
//              so they work on older .NET target frameworks.
// ============================================================
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace System
{
    internal readonly struct Index
    {
        private readonly int _value;
        public Index(int value, bool fromEnd = false)
            => _value = fromEnd ? ~value : value;
        public static implicit operator Index(int value)
            => new Index(value);
        public static Index FromEnd(int value)
            => new Index(~value);
        public int GetOffset(int length)
            => _value < 0 ? length + _value + 1 : _value;
    }

    internal readonly struct Range
    {
        public Index Start { get; }
        public Index End { get; }
        public Range(Index start, Index end) { Start = start; End = end; }
        public static Range StartAt(Index start) => new Range(start, Index.FromEnd(0));
        public static Range EndAt(Index end) => new Range(0, end);
        public static Range All => new Range(0, Index.FromEnd(0));
        public (int Offset, int Length) GetOffsetAndLength(int length)
        {
            int s = Start.GetOffset(length);
            int e = End.GetOffset(length);
            return (s, e - s);
        }
    }
}