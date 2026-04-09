#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{

    [System.Serializable]
    public class LevelData : ScriptableObject
    {
        [SerializeField, LevelEditorSetting] Layer[] layers;
        public int AmountOfLayers => layers.Length;

        [SerializeField, LevelEditorSetting] int bottomLayerWidth = 10;
        public int BottomLayerWidth => bottomLayerWidth;

        [SerializeField, LevelEditorSetting] int bottomLayerHeight = 10;
        public int BottomLayerHeight => bottomLayerHeight;

        [SerializeField, LevelEditorSetting] bool useInRandomizer;
        public bool UseInRandomizer => useInRandomizer;

        [SerializeField, LevelEditorSetting] int elementsPerLevel = 8;
        public int ElementsPerLevel => elementsPerLevel;

        [SerializeField, LevelEditorSetting] int coinsReward = 20;
        public int CoinsReward => coinsReward;

        [SerializeField, LevelEditorSetting] string editorNote; // used only in level editor

        [SerializeField]
        private IntToggle nonDefaultStartingSlots;
        public IntToggle NonDefaultStartingSlots => nonDefaultStartingSlots;
        
        [SerializeField]
        private bool usesCards = false;
        public bool UsesCards => usesCards;
        
        [SerializeField]
        private bool overrideDefaultDeck = false;
        public bool OverrideDefaultDeck => overrideDefaultDeck;
        
        
        [SerializeField]
        private CompositeToggle<int,CardDeckSO> gameplayTimer = new CompositeToggle<int,CardDeckSO>(false,60);
        public CompositeToggle<int,CardDeckSO> GameplayTimer => gameplayTimer;
        
        [SerializeField] 
        private CompositeToggle<int,CardDeckSO> scoreTarget = new CompositeToggle<int,CardDeckSO>(false, 1000);
        public CompositeToggle<int,CardDeckSO> ScoreTarget => scoreTarget;
        
        /*
        [Header("Music")]
        [Tooltip("Determines which playlist the MusicManager will use for this level.")]
        [SerializeField] private LevelPlaylistType playlistType = LevelPlaylistType.Regular;
        public LevelPlaylistType PlaylistType => playlistType;
    */
        [Header("Music")]
        [Tooltip("When enabled, uses the selected playlist type instead of the auto-detected one.")]
        [SerializeField] private ToggleType<LevelPlaylistType> playlistOverride =
            new ToggleType<LevelPlaylistType>(false, LevelPlaylistType.Regular);

        /// <summary>
        /// The playlist type that will be used by MusicManager for this level.
        /// Returns the override value when the override is enabled, otherwise
        /// derives the type automatically from the level's gameplay toggles.
        /// </summary>
        public LevelPlaylistType PlaylistType => playlistOverride.Enabled
            ? playlistOverride.Value
            : DetectPlaylistType();

        private LevelPlaylistType DetectPlaylistType()
        {
            bool hasTimer = gameplayTimer.Enabled;
            bool hasScore = scoreTarget.Enabled;
            bool hasCards = usesCards;

            if (hasTimer && hasCards) return LevelPlaylistType.TimerCards;
            if (hasTimer) return LevelPlaylistType.Timer;
            if (hasScore && hasCards) return LevelPlaylistType.ScoreCards;
            if (hasScore) return LevelPlaylistType.Score;
            if (hasCards) return LevelPlaylistType.RegularCards;

            return LevelPlaylistType.Regular;
        }

        public int SetsAmount => (GetAmountOfFilledCells() - (GetAmountOfFilledCells() % 3)) / 3;
        public float Difficulty => Mathf.Round(Mathf.Clamp(SetsAmount / (float)elementsPerLevel, 1, float.MaxValue) * 10.0f) * 0.1f;

        public Layer GetLayer(int i)
        {
            if (i < AmountOfLayers && i >= 0) return layers[i];

            return null;
        }

        public int GetAmountOfFilledCells()
        {
            int counter = 0;

            for (int i = 0; i < AmountOfLayers; i++)
            {
                counter += layers[i].GetAmountOfFilledCells();
            }

            return counter;
        }
    }
}