namespace Watermelon
{
    public class AnalyticsIAPFailData : IAnalyticsEventData
    {
        public IAPItem Item;
        public Watermelon.PurchaseFailureReason FailureReason;
    }
}
