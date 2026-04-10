using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace InSpectra.Gen.StartupHook.Reflection;

internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    public static ReferenceEqualityComparer Instance { get; } = new();

    private ReferenceEqualityComparer()
    {
    }

    public new bool Equals(object? x, object? y)
        => ReferenceEquals(x, y);

    public int GetHashCode(object obj)
        => RuntimeHelpers.GetHashCode(obj);
}
