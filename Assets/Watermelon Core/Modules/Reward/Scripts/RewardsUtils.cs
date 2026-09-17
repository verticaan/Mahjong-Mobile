using System;

namespace Watermelon
{
    public static class RewardsUtils
    {
        public static Type GetViewTypeFor(Type rewardType)
        {
            RewardsMap.ViewMap.TryGetValue(rewardType, out Type viewType);

            return viewType;
        }
    }
}