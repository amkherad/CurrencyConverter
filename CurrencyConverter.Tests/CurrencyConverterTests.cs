using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace CurrencyConverter.Tests;

public class CurrencyConverterTests
{
    private readonly global::CurrencyConverter.Core.CurrencyConverter _currencyConverter;

    private readonly IEnumerable<Tuple<string, string, double>> _conversionRates = new[]
    {
        //Tuple.Create("USD", "EUR", 0.86),
        Tuple.Create("CAD", "GBP", 0.58),
        Tuple.Create("EUR", "IRR", 30000D),
        Tuple.Create("GBP", "EUR", 1.18),
        Tuple.Create("USD", "CAD", 1.34),
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
    [InlineData("CAD", "USD", 0, 0)]
    [InlineData("EUR", "USD", 0, 0)]
    [InlineData("EUR", "CAD", 0, 0)]
    public void GivenImpossibleData_WhenCallingConvert_ThenItShouldThrow(
        string from,
        string to,
        double value,
        double? expectedResult
    )
    {
        Action action = () => _currencyConverter.Convert(from, to, value);

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
        var result = _currencyConverter.Convert(from, to, value);

        // Doubles are not precise enough to compare with a delta.
        ((double?)result)
            .Should()
            .BeApproximately(expectedResult, 0.000001D, "The result and the expected value should be the same.");
    }
}