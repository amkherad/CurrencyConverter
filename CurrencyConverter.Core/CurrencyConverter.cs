using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CurrencyConverter.Core;

public class CurrencyConverter : ICurrencyConverter
{
    private ConcurrentDictionary<CurrencyTuple, double> _currencyConversionRateMap = null;

    public void ClearConfiguration()
    {
        _currencyConversionRateMap?.Clear();
    }

    public ReadOnlyDictionary<(string, string), double> GetConversionRateMap()
    {
        return new ReadOnlyDictionary<(string, string), double>(
            _currencyConversionRateMap?.ToDictionary(k => (k.Key.From, k.Key.To), v => v.Value)
                ?? new Dictionary<(string, string), double>()
        );
    }

    public void UpdateConfiguration(
        IEnumerable<Tuple<string, string, double>> conversionRates
    )
    {
        ArgumentNullException.ThrowIfNull(conversionRates, nameof(conversionRates));

        var rates = conversionRates.Select(cr => (new CurrencyTuple(cr.Item1, cr.Item2), cr.Item3)).ToArray();

        // Atomic swap of the currency conversion rate map without mutating the old map.
        _currencyConversionRateMap = new ConcurrentDictionary<CurrencyTuple, double>(CreateConversionRateMap(rates));
    }

    public double Convert(
        string fromCurrency,
        string toCurrency,
        double amount
    )
    {
        var map = new CurrencyTuple(fromCurrency, toCurrency);

        var localCopyOfMap = _currencyConversionRateMap;

        if (localCopyOfMap is null || !localCopyOfMap.TryGetValue(map, out var rate))
        {
            throw new Exception( // Or a custom exception
                $"No conversion rate found for {fromCurrency} to {toCurrency}"
            );
        }

        return amount * rate;
    }

    private Dictionary<CurrencyTuple, double> CreateConversionRateMap(
        (CurrencyTuple ConversionKey, double Rate)[] rateMap
    )
    {
        var rates = rateMap
            .DistinctBy(x => x.ConversionKey) // Remove duplicate keys (UpdateConfiguration's caller must handle any bad data)
            .ToDictionary(k => k.ConversionKey, v => v.Rate);

        var keys = rates.Keys.ToArray();

        var leftHand = keys.Select(k => k.From).ToArray();
        var rightHand = keys.Select(k => k.To).ToArray();

        var allPermutations =
            from l in leftHand
            from r in rightHand
            where l != r
            select new CurrencyTuple(l, r);

        var nonExistingPermutations = allPermutations
            //.Where(k => k.From != k.To)
            .Except(rates.Keys)
            .ToHashSet();

        var paths = new Dictionary<CurrencyTuple, List<LinkedList<CurrencyTuple>>>();
        foreach (var path in rates)
        {
            paths.Add(path.Key, new List<LinkedList<CurrencyTuple>>
            {
                new LinkedList<CurrencyTuple>(new[] { path.Key })
            });
        }

        for (; ; )
        {
            var anyFound = false;
            foreach (var conversion in nonExistingPermutations)
            {
                var path = new LinkedList<CurrencyTuple>();
                var result = AddNewPaths(rates, paths, path, conversion);

                foreach (var p in result)
                {
                    if (SetPath(paths, conversion, p))
                    {
                        anyFound = true;
                    }
                }
            }

            if (!anyFound)
            {
                break;
            }
        }

        while (paths.Count > 0)
        {
            foreach (var path in paths)
            {
                var shortestPath = path.Value
                    .OrderBy(x => x.Count)
                    .FirstOrDefault();

                if (shortestPath is null)
                {
                    paths.Remove(path.Key);
                    continue;
                }

                if (rates.ContainsKey(path.Key))
                {
                    paths.Remove(path.Key);
                    continue;
                }

                var rate = 1D;
                var skipPath = false;

                foreach (var pathNode in shortestPath.Reverse())
                {
                    if (!rates.TryGetValue(pathNode, out var nodeRate))
                    {
                        skipPath = true;
                        break;
                    }
                    rate *= nodeRate;
                }

                if (!skipPath)
                {
                    paths.Remove(path.Key);
                    rates.Add(path.Key, rate);
                }
            }
        }

        return rates;
    }

    private List<LinkedList<CurrencyTuple>> AddNewPaths(
        Dictionary<CurrencyTuple, double> rates,
        Dictionary<CurrencyTuple, List<LinkedList<CurrencyTuple>>> paths,
        LinkedList<CurrencyTuple> currentPath,
        CurrencyTuple node
    )
    {
        var result = new List<LinkedList<CurrencyTuple>>();
        if (rates.ContainsKey(node))
        {
            var path = new LinkedList<CurrencyTuple>(new[] { node });
            result.Add(path);
            return result;
        }

        var links = paths
            .Where(x => x.Key.From == node.From)
            .ToArray();

        foreach (var link in links)
        {
            var p = new CurrencyTuple(link.Key.To, node.To);

            if (currentPath.Contains(p))
            {
                continue;
            }

            var newPath = new LinkedList<CurrencyTuple>(currentPath);
            newPath.AddLast(p);

            var childPaths = AddNewPaths(rates, paths, newPath, p);

            foreach (var path in childPaths)
            {
                if (path.First.Value.From == p.From && path.Last.Value.To == p.To)
                {
                    var newChildPath = new LinkedList<CurrencyTuple>();
                    newChildPath.AddLast(new LinkedListNode<CurrencyTuple>(new CurrencyTuple(node.From, link.Key.To)));
                    foreach (var childPathNode in path)
                    {
                        newChildPath.AddLast(childPathNode);
                    }
                    result.Add(newChildPath);
                }
            }
        }

        return result;
    }

    private bool SetPath(
        Dictionary<CurrencyTuple, List<LinkedList<CurrencyTuple>>> paths,
        CurrencyTuple node,
        LinkedList<CurrencyTuple> path
    )
    {
        if (paths.TryGetValue(node, out var existingPathList))
        {
            foreach (var p in existingPathList)
            {
                if (p.SequenceEqual(path))
                {
                    return false;
                }
            }

            existingPathList.Add(path);
        }
        else
        {
            paths[node] = new List<LinkedList<CurrencyTuple>> { path };
        }

        return true;
    }

    // private bool PathExists(
    //     Dictionary<CurrencyTuple, LinkedList<CurrencyTuple>> paths,
    //     CurrencyTuple node,
    //     LinkedList<CurrencyTuple> path
    // )
    // {
    //     if (paths.TryGetValue(node, out var existingPaths))
    //     {
    //         foreach (var currentPath in existingPaths)
    //         {
    //             if (currentPath.SequenceEqual(path))
    //             {
    //                 return true;
    //             }
    //         }
    //     }

    //     return false;
    // }


    // We could have used ValueTuple instead of CurrencyMap, but we can add validations and keep currencies in UpperCase form.
    [DebuggerDisplay("{From}=>{To}")]
    private struct CurrencyTuple : IEquatable<CurrencyTuple>
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
}
