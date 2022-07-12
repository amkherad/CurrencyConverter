namespace CurrencyConverter.Abstractions;

public interface ICurrencyConverter
{
    void ClearConfiguration();

    void UpdateConfiguration(
        IEnumerable<Tuple<string, string, decimal>> conversionRates
    );

    decimal Convert(
        string fromCurrency,
        string toCurrency,
        decimal amount
    );
}
