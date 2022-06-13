# CurrencyConverter
A CurrencyConverter service which finds all possible conversions from an incomplete set of conversion mappings.

For example give the following data:
From | To  | Rate
-----|-----|-----
 CAD | GBP | 0.58
 EUR | JPY | 141.39
 GBP | EUR | 1.18
 USD | CAD | 1.34

Will generate the following:
From | To  | Rate           | Path (Shortest) | Paths Found
-----|-----|----------------|-----------------|------------
 CAD | GBP | 0.58       | CAD=>GBP        | 1
 EUR | JPY | 141.39     | EUR=>JPY        | 1
 GBP | EUR | 1.18       | GBP=>EUR        | 1
 USD | CAD | 1.34       | USD=>CAD        | 1
 CAD | JPY | 0.6843999  | CAD=>EUR=>JPY   | `2`
 CAD | EUR | 166.840199 | CAD=>GBP=>EUR   | 1
 GBP | JPY | 0.7772     | GBP=>EUR=>JPY   | 1
 USD | GBP | 0.9170959  | USD=>CAD=>GBP   | 1
 USD | JPY | 96.7673159 | USD=>EUR=>JPY   | `4`
 USD | EUR | 129.668203 | USD=>GBP=>EUR   | `2`
