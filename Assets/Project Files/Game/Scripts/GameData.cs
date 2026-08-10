using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Game Data", menuName = "Data/Game Data")]
    public class GameData : ScriptableObject
    {
        [SerializeField] bool showTutorial = true;
        public bool ShowTutorial => showTutorial;

        [SerializeField] bool showCardTutorial = true;
        public bool ShowCardTutorial => showCardTutorial;

        [SerializeField] float maxTileSize = 1.5f;
        public float MaxTileSize => maxTileSize;

        [SerializeField, Tooltip("Cannot be lower than 1 and higher than MaxTileSize")] Vector2 tileSize = Vector2.one;
        public Vector2 TileSize => tileSize / (tileSize.x > tileSize.y ? tileSize.y : tileSize.x);

        [SerializeField] bool infiniteLevels;
        public bool InfiniteLevels => infiniteLevels;

        [SerializeField, Min(0f)] float reviveTimerBonusSeconds = 15f;
        public float ReviveTimerBonusSeconds => reviveTimerBonusSeconds;

        private void OnValidate()
        {
            tileSize.x = Mathf.Clamp(tileSize.x, 1, maxTileSize);
            tileSize.y = Mathf.Clamp(tileSize.y, 1, maxTileSize);
        }
    }
}
