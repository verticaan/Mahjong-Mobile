namespace Watermelon
{
    [StaticUnload]
    public static class ConsentData
    {
        public static bool IsConsentGiven { get; private set; } = false;
        public static Watermelon.AuthorizationTrackingStatus ATTStatus { get; private set; } = Watermelon.AuthorizationTrackingStatus.NOT_DETERMINED;

        public static void SetATTStatus(Watermelon.AuthorizationTrackingStatus status)
        {
            ATTStatus = status;
        }

        public static void SetConsentGiven(bool consentGiven)
        {
            IsConsentGiven = consentGiven;
        }

        private static void UnloadStatic()
        {
            IsConsentGiven = false;
            ATTStatus = Watermelon.AuthorizationTrackingStatus.NOT_DETERMINED;
        }
    }
}
