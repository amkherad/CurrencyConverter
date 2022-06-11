using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CurrencyConverter.Core;

// We could have used ValueTuple instead of CurrencyMap, but we can add validations and keep currencies in UpperCase form.
[DebuggerDisplay("{ToString()}")]
internal struct ConversionPath : IEnumerable<CurrencyTuple>
{
    private List<CurrencyTuple> Path { get; }

    public ConversionPath(
        IEnumerable<CurrencyTuple> path
    )
    {
        ArgumentNullException.ThrowIfNull(path, nameof(path));

        Path = path.ToList();
    }


    public ConversionPath(
        CurrencyTuple node
    )
    {
        Path = new List<CurrencyTuple> { node };
    }


    public ConversionPath()
    {
        Path = new List<CurrencyTuple>();
    }


    public IEnumerator<CurrencyTuple> GetEnumerator()
    {
        return Path.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => Path.Count;

    public CurrencyTuple this[int index]
    {
        get
        {
            return Path[index];
        }
    }


    public override string ToString()
    {
        if (Path.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("=>", Path.Select(x => x.From)) + "=>" + Path[^1].To;
    }


    public override bool Equals([NotNullWhen(true)] object obj)
    {
        if (obj is ConversionPath other)
        {
            return Path.SequenceEqual(other.Path);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool operator ==(
        ConversionPath left,
        ConversionPath right
    )
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        ConversionPath left,
        ConversionPath right
    )
    {
        return !left.Equals(right);
    }

    public static ConversionPath operator +(
        ConversionPath left,
        ConversionPath right
    )
    {
        return new ConversionPath(
            left.Path.Concat(right.Path)
        );
    }

    public static ConversionPath operator +(
        ConversionPath left,
        CurrencyTuple right
    )
    {
        return new ConversionPath(
            left.Path.Append(right)
        );
    }
}
