using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace CurrencyConverter.Tests;

public class CurrencyConverterTests
{
    private readonly global::CurrencyConverter.Core.CurrencyConverter _currencyConverter;

    private readonly IEnumerable<Tuple<string, string, decimal>> _conversionRates = new[]
    {
        Tuple.Create("CAD", "GBP", 0.58M),
        Tuple.Create("EUR", "JPY", 141.39M),
        Tuple.Create("GBP", "EUR", 1.18M),
        Tuple.Create("USD", "CAD", 1.34M),
    };

    public CurrencyConverterTests()
    {
        _currencyConverter = new global::CurrencyConverter.Core.CurrencyConverter();

        _currencyConverter.UpdateConfiguration(_conversionRates);
    }

    [Fact]
    public void GivenInitialConfiguration_WhenCallingClearConfiguration_ThenTheCurrencyConversionRateMapIsCleared()
    {
        _currencyConverter.ClearConfiguration();

        _currencyConverter.GetConversionRateMap()
            .Should()
            .BeEmpty();
    }

    [Theory]
    [InlineData("CAD", "USD")]
    [InlineData("EUR", "USD")]
    [InlineData("EUR", "CAD")]
    public void GivenImpossibleData_WhenCallingConvert_ThenItShouldThrow(
        string from,
        string to
    )
    {
        Action action = () => _currencyConverter.Convert(from, to, 1000);

        action
            .Should()
            .Throw<Exception>();
    }

    [Theory]
    [InlineData("USD", "CAD", 1000D, 1340D)]
    [InlineData("USD", "GBP", 1000D, 777.2D)]
    [InlineData("CAD", "EUR", 1000D, 684.4)]
    [InlineData("USD", "IRR", 1000D, 27512880D)]
    public void GivenValidData_WhenCallingConvert_ThenItShouldConvertToExpectedValue(
        string from,
        string to,
        double value,
        double? expectedResult
    )
    {
        var result = _currencyConverter.Convert(from, to, (decimal) value);

        // Doubles are not precise enough to compare with a delta.
        ((double?)result)
            .Should()
            .BeApproximately(expectedResult, 0.000001D, "The result and the expected value should be the same.");
    }
}