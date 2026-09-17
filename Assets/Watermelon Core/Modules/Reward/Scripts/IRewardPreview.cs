using UnityEngine;

namespace Watermelon
{
    public interface IRewardPreview
    {
        public Sprite Icon { get; }
        public string Text { get; }

        public int SortingOrder { get; }

        GameObject GetCustomUIPrefab();
    }
}
