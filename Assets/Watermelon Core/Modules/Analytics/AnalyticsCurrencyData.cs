using System.Collections.Generic;

namespace Watermelon
{
    public class AnalyticsCurrencyData : IAnalyticsEventData
    {
        public string Source;
        public Dictionary<CurrencyType, int> CurrenciesDelta;
    }
}
