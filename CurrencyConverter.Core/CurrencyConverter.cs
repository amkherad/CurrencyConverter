using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CurrencyConverter.Abstractions;

namespace CurrencyConverter.Core;

public partial class CurrencyConverter : ICurrencyConverter
{
    private ConcurrentDictionary<CurrencyTuple, decimal> _currencyConversionRateMap = null;

    public void ClearConfiguration()
    {
        _currencyConversionRateMap = null;
    }

    public ReadOnlyDictionary<(string, string), decimal> GetConversionRateMap()
    {
        return new ReadOnlyDictionary<(string, string), decimal>(
            _currencyConversionRateMap?.ToDictionary(k => (k.Key.From, k.Key.To), v => v.Value)
                ?? new Dictionary<(string, string), decimal>()
        );
    }

    public void UpdateConfiguration(
        IEnumerable<Tuple<string, string, decimal>> conversionRates
    )
    {
        ArgumentNullException.ThrowIfNull(conversionRates, nameof(conversionRates));

        var rates = conversionRates.Select(cr => (new CurrencyTuple(cr.Item1, cr.Item2), cr.Item3)).ToArray();

        // Atomic swap of the currency conversion rate map without mutating the old map.
        _currencyConversionRateMap = new ConcurrentDictionary<CurrencyTuple, decimal>(CreateConversionRateMap(rates));
    }

    public decimal Convert(
        string fromCurrency,
        string toCurrency,
        decimal amount
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

    private Dictionary<CurrencyTuple, decimal> CreateConversionRateMap(
        (CurrencyTuple ConversionKey, decimal Rate)[] rateMap
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
            .Except(rates.Keys)
            .ToArray();

        var paths = new Dictionary<CurrencyTuple, List<ConversionPath>>();
        foreach (var path in rates)
        {
            paths.Add(path.Key, new List<ConversionPath>
            {
                new ConversionPath(new [] { path.Key })
            });
        }

        for (; ; )
        {
            var anyFound = false;
            foreach (var conversion in nonExistingPermutations)
            {
                var path = new ConversionPath();
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
                if (path.Value.Count == 0)
                {
                    paths.Remove(path.Key);
                    continue;
                }

                if (rates.ContainsKey(path.Key))
                {
                    paths.Remove(path.Key);
                    continue;
                }

                var shortestPath = path.Value
                    .OrderBy(x => x.Count)
                    .FirstOrDefault();

                var rate = 1M;
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

    private List<ConversionPath> AddNewPaths(
        Dictionary<CurrencyTuple, decimal> rates,
        Dictionary<CurrencyTuple, List<ConversionPath>> paths,
        ConversionPath currentPath,
        CurrencyTuple node
    )
    {
        var result = new List<ConversionPath>();
        if (rates.ContainsKey(node))
        {
            var path = new ConversionPath(new[] { node });
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

            var newPath = currentPath + p;

            var childPaths = AddNewPaths(rates, paths, newPath, p);

            foreach (var path in childPaths)
            {
                if (path[0].From == p.From && path[^1].To == p.To)
                {
                    var newChildPath = new ConversionPath(path.Prepend(new CurrencyTuple(node.From, link.Key.To)));
                    result.Add(newChildPath);
                }
            }
        }

        return result;
    }

    private bool SetPath(
        Dictionary<CurrencyTuple, List<ConversionPath>> paths,
        CurrencyTuple node,
        ConversionPath path
    )
    {
        if (paths.TryGetValue(node, out var existingPathList))
        {
            foreach (var p in existingPathList)
            {
                if (p == path)
                {
                    return false;
                }
            }

            existingPathList.Add(path);
        }
        else
        {
            paths[node] = new List<ConversionPath> { path };
        }

        return true;
    }

    private class WeightedPath
    {
        public ConversionPath Path { get; }

        public int Weight { get; private set; }

        public WeightedPath(
            ConversionPath path,
            int weight
        )
        {
            Path = path;
            Weight = weight;
        }

        public override string ToString()
        {
            return $"{Path} ({Weight})";
        }

        public void AddWeight(int weight)
        {
            Weight += weight;
        }
    }
}
