using System.Diagnostics;

namespace CurrencyConverter.Core;

// We could have used ValueTuple instead of CurrencyMap, but we can add validations and keep currencies in UpperCase form.
[DebuggerDisplay("{From}=>{To}")]
internal struct CurrencyTuple : IEquatable<CurrencyTuple>
{
    public string From { get; }
    public string To { get; }

    public CurrencyTuple(
        string from,
        string to
    )
    {
        ArgumentNullException.ThrowIfNull(from, nameof(from));
        ArgumentNullException.ThrowIfNull(to, nameof(to));

        if (from.Length != 3 || to.Length != 3)
        {
            throw new Exception(
                $"Currency codes must be 3 characters long. {from} and {to} are not valid."
            );
        }

        From = from.ToUpper();
        To = to.ToUpper();
    }

    public bool Equals(
        CurrencyTuple other
    )
    {
        return From == other.From && To == other.To;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            From.GetHashCode(),
            To.GetHashCode()
        );
    }

    public override bool Equals(
        object obj
    )
    {
        return obj is CurrencyTuple other && Equals(other);
    }

    public static bool operator ==(
        CurrencyTuple left,
        CurrencyTuple right
    )
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        CurrencyTuple left,
        CurrencyTuple right
    )
    {
        return !left.Equals(right);
    }
}
