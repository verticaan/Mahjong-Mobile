using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Watermelon
{
    public class LevelController : MonoBehaviour
    {
        private static LevelController instance;

        [SerializeField] LevelDatabase database;
        [SerializeField] LevelSpawnAnimation levelSpawnAnimation;
        
        [Space]
        [SerializeField] LevelScaler levelScaler;
        [SerializeField] GameObject levelObject;
        [SerializeField] GameObject layersParentObject;
        [SerializeField] DockBehavior dock;
        [SerializeField] ScoreDataModel scoreDataModel;
        

        private static bool isLevelLoaded;
        public static bool IsLevelLoaded => isLevelLoaded;

        private static LevelData level;
        public static LevelData Level => level;

        private static CardLogicController cardLogicController;
        
        public static CardLogicController CardLogicController => cardLogicController;
        
        private static CardBuffService buffService;
        public static CardBuffService BuffService => buffService;
        
        public static ScoreDataModel ScoreDataModel => instance.scoreDataModel;

        private static LevelSave levelSave;

        public static LevelDatabase Database => instance.database;

        public static int MaxReachedLevelIndex => levelSave.MaxReachedLevelIndex;
        public static int DisplayedLevelIndex => levelSave.DisplayLevelIndex;

        private static int loadedLevelIndex;

        public static GameObject LevelObject => instance.levelObject;

        private static LevelRepresentation levelRepresentation;
        public static LevelRepresentation LevelRepresentation => levelRepresentation;

        private static Dictionary<TileEffectType, TileEffect> effectsLink;

        public static Vector2Int EvenLayerSize => new Vector2Int(Level.GetLayer(Level.AmountOfLayers - 1).GetRow(0).AmountOfCells, Level.GetLayer(Level.AmountOfLayers - 1).AmountOfRows);
        public static Vector2Int OddLayerSize => new Vector2Int(Level.GetLayer(Level.AmountOfLayers - 2).GetRow(0).AmountOfCells, Level.GetLayer(Level.AmountOfLayers - 2).AmountOfRows);
        public static bool IsEvenLayerBigger => EvenLayerSize.x > OddLayerSize.x;

        public static int CurrentReward => GetCurrentLevelReward();
        
        public static DockBehavior Dock => instance.dock;

        public static BackgroundBehavior Background { get; private set; }

        private static bool isCustomLevel;
        public static bool IsCustomLevel => isCustomLevel;

        public static bool IsRaycastEnabled { get; set; } = true;

        private static bool firstTimeCompletedLevel = false;

        private static bool isBusy;
        public static bool IsBusy => isBusy;

        // ── Endless / randomised mode ────────────────────────────────────────
        [Header("Endless Mode")]
        [Tooltip("Configuration asset that drives procedurally-generated levels " +
                 "played after all hand-crafted levels are completed.")]
        [SerializeField] private RandomisedLevelData randomisedLevelConfig;

        /// <summary>True while the player is in endless randomised-level mode.</summary>
        private static bool isEndlessMode;
        public static bool IsEndlessMode => isEndlessMode;

        /// <summary>
        /// The result produced by the last call to
        /// <see cref="RandomisedLevelData.Randomise"/>; used by
        /// <see cref="LoadLevelData"/> to configure gameplay subsystems.
        /// </summary>
        private static RandomisedLevelResult activeRandomisedResult;

        public static GameplayTimer GameplayTimer { get; private set; }

        // ── Endless mode helper ──────────────────────────────────────────────
        /// <summary>
        /// Returns true when <paramref name="levelIndex"/> is beyond the last
        /// hand-crafted level and a <see cref="RandomisedLevelData"/> asset is assigned,
        /// meaning we should generate a randomised level instead of loading from the DB.
        /// </summary>
        private bool ShouldUseEndlessMode(int levelIndex)
            => levelIndex >= database.AmountOfLevels && randomisedLevelConfig != null;

        private void Awake()
        {
            instance = this;
        }

        public void Init()
        {
            LevelScaler levelScaler = GetComponent<LevelScaler>();
            levelScaler.Init();

            GameplayTimer = new GameplayTimer();
            buffService = new CardBuffService();
            GameplayTimer.OnTimerFinished += OnTimerFinished;
            scoreDataModel.OnScoreTargetReached += OnScoreTargetReached;
            scoreDataModel.Init();
            database.Init();
            dock.Init(this);
            levelSave = SaveController.GetSaveObject<LevelSave>("level");
            cardLogicController = gameObject.GetComponent<CardLogicController>();
            
            RaycastController raycastController = gameObject.AddComponent<RaycastController>();
            raycastController.Init();

            // Initialise special effects
            effectsLink = new Dictionary<TileEffectType, TileEffect>();
            TileEffect[] availableEffects = database.TileEffects;
            for (int i = 0; i < availableEffects.Length; i++)
            {
                if (effectsLink.ContainsKey(availableEffects[i].EffectType))
                {
                    Debug.LogError(string.Format("Tile effect with type {0} has duplicates in the database!", availableEffects[i].EffectType));

                    continue;
                }

                effectsLink.Add(availableEffects[i].EffectType, availableEffects[i]);
            }

            LoadBackground();
        }

        private void OnDestroy()
        {
            dock.Unload();
            database.Unload();

            buffService?.ClearAllBuffs();
            buffService = null;

            scoreDataModel.StopAll();
            
            levelRepresentation = null;
            effectsLink = null;

            loadedLevelIndex = -1;
            isLevelLoaded = false;

            isBusy = false;

            isEndlessMode = false;
            activeRandomisedResult = null;
        }
        
        private void Update()
        {
            
            // 1) Determine whether game-time should advance at all this frame
            //    (this must mirror the same block rules used by timers).
            float dt = Time.deltaTime;
            // add other pause checks here if necessary have them
            bool timeBlocked = UIController.IsPopupOpened || !UIController.focusedOnGame|| CardLogicController.IsChoosing; 

            float authoritativeDt = timeBlocked ? 0f : dt;

            // 2) Tick independent systems (they may choose to do nothing if not running)
            GameplayTimer.Tick(authoritativeDt);      // only affects the gameplay/level timer
            scoreDataModel.Tick(authoritativeDt);     // only affects combo timing inside score model

            // 3) Tick buffs with the same authoritative dt
            buffService?.Tick(authoritativeDt);
        }

        public void LoadCustomLevel(LevelData levelData, PreloadedLevelData preloadedLevelData, BackgroundData backgroundData, bool animateDock, SimpleCallback onLevelLoaded = null)
        {
            level = levelData;

            loadedLevelIndex = -1;
            firstTimeCompletedLevel = false;
            isCustomLevel = true;
            isBusy = true;

            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.PowerUpsUIController.OnLevelStarted(0);
            gameUI.ActivateTutorial();

            levelObject.SetActive(true);

            levelScaler.Recalculate();

            layersParentObject.transform.position = levelScaler.LevelFieldCenter;

            // Initing level representation
            levelRepresentation = new LevelRepresentation(level, layersParentObject);
            levelRepresentation.SpawnObjects(preloadedLevelData);

            LoadLevelData(level);
            
            RaycastController.Disable();

            levelSpawnAnimation.Play(levelRepresentation, () =>
            {
                isLevelLoaded = true;

                RaycastController.Enable();

                isBusy = false;

                onLevelLoaded?.Invoke();
            });

            if(animateDock)
                dock.PlayAppearAnimation();

            LoadBackground(backgroundData);
            MusicManager.Instance.PlayForLevel(level);
        }

        public static void CompleteCustomLevel()
        {
            if (levelRepresentation != null)
            {
                UnloadLevel();
            }

            isCustomLevel = false;
        }

        public void LoadLevel(int levelIndex, SimpleCallback onLevelLoaded = null)
        {
            if (levelRepresentation != null)
            {
                UnloadLevel();
            }

            // ── Endless / randomised mode ────────────────────────────────────
            // Once the player passes the last hand-crafted level, generate
            // procedural levels indefinitely using RandomisedLevelData.
            if (ShouldUseEndlessMode(levelIndex))
            {
                LoadRandomisedLevel(levelIndex, onLevelLoaded);
                return;
            }
            // ────────────────────────────────────────────────────────────────

            isEndlessMode = false;
            activeRandomisedResult = null;

            int realLevelIndex;
            if (levelSave.IsPlayingRandomLevel && levelIndex == levelSave.DisplayLevelIndex && levelSave.RealLevelIndex != -1)
            {
                realLevelIndex = levelSave.RealLevelIndex;
            }
            else
            {
                realLevelIndex = database.GetRandomLevelIndex(levelIndex, levelSave.LastPlayerLevelIndex, false);

                levelSave.LastPlayerLevelIndex = realLevelIndex;
                levelSave.RealLevelIndex = realLevelIndex;

                if(realLevelIndex != levelIndex)
                {
                    levelSave.IsPlayingRandomLevel = true;
                }
            }

            SaveController.Save();

            levelSave.DisplayLevelIndex = levelIndex;
            loadedLevelIndex = levelIndex;
            firstTimeCompletedLevel = false;
            isBusy = true;

            level = database.GetLevel(realLevelIndex);

            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.PowerUpsUIController.OnLevelStarted(levelIndex);
            gameUI.UpdateLevelNumber(levelIndex + 1);

            levelObject.SetActive(true);

            levelScaler.Recalculate();
            layersParentObject.transform.position = levelScaler.LevelFieldCenter;
            
            LoadLevelData(level);

            // Preparing objects to be placed on the level
            TileData[] availableObjects = database.AvailableForLevel(level);

            // Initing level representation
            levelRepresentation = new LevelRepresentation(level, layersParentObject);
            levelRepresentation.SpawnObjects(availableObjects);

            RaycastController.Disable();

            levelSpawnAnimation.Play(levelRepresentation, () =>
            {
                isLevelLoaded = true;

                RaycastController.Enable();

                isBusy = false;

                onLevelLoaded?.Invoke();
            });

            
            dock.PlayAppearAnimation();
            
            LoadBackground();
            
            MusicManager.Instance.PlayForLevel(level);

            Tween.NextFrame(() =>
            {
                SavePresets.CreateSave("Level " + (levelIndex + 1).ToString("0000"), "Levels");
            });
        }

        // ── Endless mode ─────────────────────────────────────────────────────

        /// <summary>
        /// Loads a procedurally-configured level once the player has cleared all
        /// hand-crafted levels.  A random <see cref="LevelData"/> from the database
        /// is chosen as the board geometry; <see cref="RandomisedLevelData"/> then
        /// randomises every other gameplay parameter.
        /// </summary>
        private void LoadRandomisedLevel(int displayLevelIndex, SimpleCallback onLevelLoaded = null)
        {
            isEndlessMode  = true;
            isBusy         = true;
            firstTimeCompletedLevel = false;
            loadedLevelIndex = displayLevelIndex;

            // Pick a random hand-crafted level to use as the board template
            int sourceLevelIndex = Random.Range(0, database.AmountOfLevels);
            LevelData sourceLevelData = database.GetLevel(sourceLevelIndex);

            // Generate all randomised parameters
            activeRandomisedResult = randomisedLevelConfig.Randomise(database, sourceLevelData);

            // The static 'level' field is set to the source so that helpers that
            // read LevelController.Level (e.g. EvenLayerSize, OddLayerSize) work.
            level = sourceLevelData;

            // Update save / UI
            levelSave.DisplayLevelIndex  = displayLevelIndex;
            levelSave.IsPlayingRandomLevel = false; // endless mode has its own flag
            SaveController.MarkAsSaveIsRequired();

            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.PowerUpsUIController.OnLevelStarted(displayLevelIndex);
            gameUI.UpdateLevelNumber(displayLevelIndex + 1);

            levelObject.SetActive(true);

            levelScaler.Recalculate();
            layersParentObject.transform.position = levelScaler.LevelFieldCenter;

            // Configure gameplay subsystems using the randomised result
            LoadRandomisedLevelData(activeRandomisedResult);

            // Build tile selection from the randomised pool, respecting elementsPerLevel
            TileData[] tilePool = activeRandomisedResult.TilePool;
            int elementsCount   = Mathf.Clamp(activeRandomisedResult.ElementsPerLevel, 1, tilePool.Length);

            // Shuffle and trim to the chosen element count
            List<TileData> tileList = new List<TileData>(tilePool);
            tileList.Shuffle();
            if (tileList.Count > elementsCount)
                tileList.RemoveRange(elementsCount, tileList.Count - elementsCount);

            TileData[] availableObjects = tileList.ToArray();

            // Spawn using the source level's geometry
            levelRepresentation = new LevelRepresentation(sourceLevelData, layersParentObject);
            levelRepresentation.SpawnObjects(availableObjects);

            RaycastController.Disable();

            levelSpawnAnimation.Play(levelRepresentation, () =>
            {
                isLevelLoaded = true;
                RaycastController.Enable();
                isBusy = false;
                onLevelLoaded?.Invoke();
            });

            dock.PlayAppearAnimation();
            LoadBackground();

            // PlayForLevel reads level.PlaylistType internally.  For randomised levels
            // the source LevelData's playlist type is used as a safe fallback; the
            // randomised PlaylistType is available on activeRandomisedResult for any
            // future MusicManager overload that accepts it explicitly.
            MusicManager.Instance.PlayForLevel(level);
        }

        /// <summary>
        /// Mirrors <see cref="LoadLevelData"/> but reads from a
        /// <see cref="RandomisedLevelResult"/> instead of a <see cref="LevelData"/>.
        /// Deck lists are read from the result's dedicated fields rather than from
        /// <c>toggle.List</c>, which is inspector-only and cannot be set in code.
        /// </summary>
        private void LoadRandomisedLevelData(RandomisedLevelResult result)
        {
            scoreDataModel.ResetForLevel();

            if (result.UsesCards)
            {
                cardLogicController.EnableSelectionLoop();
                buffService.SubscribeToMatchResolved();
            }

            if (result.TimerToggle.Enabled)
            {
                GameplayTimer.SetMaxTime(result.TimerToggle.Value);
                GameplayTimer.Start();
                if (result.TimerDeckList != null && result.TimerDeckList.Count > 0)
                {
                    cardLogicController.AddToActiveDeck(result.TimerDeckList.ToArray());
                }
            }

            if (result.ScoreToggle.Enabled)
            {
                scoreDataModel.SetTargetScoreExists(true);
                scoreDataModel.SetTargetScore(result.ScoreToggle.Value);
                if (result.ScoreDeckList != null && result.ScoreDeckList.Count > 0)
                {
                    cardLogicController.AddToActiveDeck(result.ScoreDeckList.ToArray());
                }
            }

            // Cards-only mode: no timer, no score — apply the card deck directly
            if (result.UsesCards
                && !result.TimerToggle.Enabled
                && !result.ScoreToggle.Enabled
                && result.CardDeckList != null
                && result.CardDeckList.Count > 0)
            {
                cardLogicController.AddToActiveDeck(result.CardDeckList.ToArray());
            }

            // Randomised levels always use the default slot count
            dock.ApplyDefaultSlotCount();
        }

        // ────────────────────────────────────────────────────────────────────

        private void LoadBackground(BackgroundData backgroundData = null)
        {
            if (Background != null)
                Destroy(Background.gameObject);

            if(backgroundData == null)
                backgroundData = database.GetLastAvailableBackgroundData();

            if (backgroundData != null)
            {
                Background = Instantiate(backgroundData.BackgroundPrefab).GetComponent<BackgroundBehavior>();
            }
        }
        
        //Section for data driven level type control
        private void LoadLevelData(LevelData levelData)
        {
            // Always reset score state first so no previous level's subscriptions or
            // data carry over — even when this level does not use scoring.
            scoreDataModel.ResetForLevel();
            
            var timer = level.GameplayTimer;
            var scoreTarget = level.ScoreTarget;
            
            if (level.UsesCards)
            {
                cardLogicController.EnableSelectionLoop();
                buffService.SubscribeToMatchResolved();
            }
            
            if(timer.Enabled)
            {
                GameplayTimer.SetMaxTime(timer.Value);
                GameplayTimer.Start();
                if (timer.List != null && timer.List.Count > 0)
                {
                    if (level.OverrideDefaultDeck)
                    {
                        cardLogicController.OverrideActiveDeck(timer.List.ToArray());
                    }
                    else
                    {
                        cardLogicController.AddToActiveDeck(timer.List.ToArray());
                    }
                    
                }
            }
            
            if (scoreTarget.Enabled)
            {
                Debug.Log("Score enabled");
                scoreDataModel.SetTargetScoreExists(scoreTarget.Enabled);
                scoreDataModel.SetTargetScore(scoreTarget.Value);
                if (scoreTarget.List != null &&  scoreTarget.List.Count > 0)
                {
                    Debug.Log("List exists");
                    if (level.OverrideDefaultDeck)
                    {
                        Debug.Log("List override");
                        cardLogicController.OverrideActiveDeck(scoreTarget.List.ToArray());
                    }
                    else
                    {
                        Debug.Log("List add");
                        cardLogicController.AddToActiveDeck(scoreTarget.List.ToArray());
                    }
                }
            }
            
            if (level.NonDefaultStartingSlots.Enabled)
            {
                dock.SetSlotCount(level.NonDefaultStartingSlots.Value);
            }
            else
            {   //This is duplicate method call, but leaving here for insurance
                dock.ApplyDefaultSlotCount();
            }
            
        }

        public static void OnTileSubmitted(TileBehavior tileBehavior)
        {
            if (!GameController.IsGameActive) return;

            TileEffect effect = tileBehavior.Effect;
            if (effect != null)
            {
                effect.OnTileSubmitted();
            }

            List<TileBehavior> activeTiles = levelRepresentation.Tiles;
            foreach (TileBehavior tiles in activeTiles)
            {
                effect = tiles.Effect;
                if (effect != null)
                {
                    effect.OnAnyTileSubmitted();
                }
            }
        }

        public static void UnloadLevel()
        {
            PUController.ResetBehaviors();

            if (levelRepresentation != null)
            {
                levelRepresentation.Clear();
                levelRepresentation = null;
            }
            DisableSubsystems();
            MusicManager.Instance.StopMusic();
            instance.levelSpawnAnimation.Clear();
            instance.dock.DisposeQuickly();
            instance.dock.HideSlots();
            
        }

        public static void DisableSubsystems()
        {
            cardLogicController.DisableSelectionLoop(true);
            GameplayTimer.Reset();
            buffService?.ClearAllBuffs();
            buffService?.UnsubscribeFromMatchResolved();
            instance.scoreDataModel.StopAll();
        }

        /// <summary>
        /// Unified method to complete the current level as a win.
        /// Handles save progression, timer cleanup, and GameController notification.
        /// </summary>
        private void WinLevel()
        {
            if (!GameController.IsGameActive) return;
            if (isCustomLevel) return;

            RaycastController.Disable();

            if (isEndlessMode)
            {
                // In endless mode we keep incrementing the display index so the UI
                // shows ever-increasing level numbers, but MaxReachedLevelIndex is
                // clamped to the last hand-crafted level to avoid save corruption.
                levelSave.DisplayLevelIndex++;
                levelSave.MaxReachedLevelIndex = database.AmountOfLevels - 1;

                // Endless levels are never "first time completed" for reward purposes
                firstTimeCompletedLevel = false;
            }
            else
            {
                levelSave.IsPlayingRandomLevel = false;
                levelSave.DisplayLevelIndex++;

                if (levelSave.DisplayLevelIndex > levelSave.MaxReachedLevelIndex)
                {
                    levelSave.MaxReachedLevelIndex = levelSave.DisplayLevelIndex;
                    firstTimeCompletedLevel = true;
                }
            }

            SaveController.MarkAsSaveIsRequired();
            GameController.OnLevelCompleted();
            AudioController.PlaySound(AudioController.AudioClips.levelComplete);
        }

        /// <summary>
        /// Unified method to end the current level as a loss.
        /// Handles timer cleanup and GameController notification.
        /// </summary>
        private void LoseLevel()
        {
            if (!GameController.IsGameActive) return;

            
            GameController.OnLevelFailed();
            AudioController.PlaySound(AudioController.AudioClips.levelFailed);
        }

        public void OnMatchCompleted()
        {
            if (isCustomLevel) return;

            if (levelRepresentation.Tiles.Count == 0 && dock.IsEmpty)
            {
                DisableSubsystems();

                // When in endless mode, read score-target state from the randomised
                // result rather than from LevelData (which holds the source template).
                bool hasScoreTarget = isEndlessMode
                    ? (activeRandomisedResult != null && activeRandomisedResult.ScoreToggle.Enabled)
                    : level.ScoreTarget.Enabled;

                int scoreTarget = isEndlessMode && activeRandomisedResult != null
                    ? activeRandomisedResult.ScoreToggle.Value
                    : (level.ScoreTarget.Enabled ? level.ScoreTarget.Value : 0);

                // If a score target exists, emptying the board does NOT automatically win —
                // the score must be met. Winning via score is handled by OnScoreTargetReached.
                if (hasScoreTarget)
                {
                    if (scoreDataModel.CurrentScore < scoreTarget)
                    {
                        LoseLevel();
                    }
                    // Score target not yet reached but board is empty: do nothing here —
                    // OnScoreTargetReached will call WinLevel() if/when the target is met.
                }
                else
                {
                    // No score target — clearing the board wins the level.
                    WinLevel();
                }
            }
        }

        public void OnSlotsFilled()
        {
            DisableSubsystems();
            LoseLevel();
        }

        private void OnTimerFinished()
        {
            DisableSubsystems();
            LoseLevel();
        }

        private void OnScoreTargetReached()
        {
            Debug.Log("OnScoreTargetReached");
            DisableSubsystems();
            WinLevel();
        }

        //helper method,to know when all matches are complete...
        public static bool AreAllMatchesCompleted()
        {
            return LevelRepresentation != null && LevelRepresentation.Tiles.Count == 0 && Dock.IsEmpty;
        }

        

        public static bool SubmitIsAllowed()
        {
            return !instance.dock.IsFilled;
        }

        public static TileBehavior SpawnDockTile(int tileID)
        {
            TileData tileData = Database.GetTile(tileID);
            ElementPosition elementPosition = new ElementPosition(-1, -1);

            TileBehavior tileBehavior = tileData.Pool.GetPooledObject().GetComponent<TileBehavior>();
            tileBehavior.Init(tileData, elementPosition);
            tileBehavior.transform.localScale = Vector3.one;
            tileBehavior.SetScale(Vector2.one * LevelScaler.SlotSize);
            tileBehavior.MarkAsSubmitted();

            Dock.SubmitToSlot(tileBehavior, true);

            return tileBehavior;
        }

        public static void SubmitElement(TileBehavior tileBehavior)
        {
            tileBehavior.MarkAsSubmitted();

            instance.dock.SubmitToSlot(tileBehavior, false);

            levelRepresentation.RemoveObject(tileBehavior);
            levelRepresentation.UpdateStates(true);
        }

        public static void RevertElement(TileBehavior tileBehavior)
        {
            tileBehavior.ResetSubmitState();

            levelRepresentation.AddObject(tileBehavior);
            levelRepresentation.UpdateStates(true);
        }

        public static void RevertElements(List<TileBehavior> tileBehaviors)
        {
            foreach (TileBehavior tileBehavior in tileBehaviors)
            {
                tileBehavior.ResetSubmitState();

                levelRepresentation.AddObject(tileBehavior);
            }

            levelRepresentation.UpdateStates(true);
        }

        public static void SubmitElements(List<TileBehavior> tileBehaviors)
        {
            for (int i = 0; i < tileBehaviors.Count; i++)
            {
                var tileBehavior = tileBehaviors[i];

                tileBehavior.MarkAsSubmitted();

                instance.dock.SubmitToSlot(tileBehavior, false);

                levelRepresentation.RemoveObject(tileBehavior);
            }

            levelRepresentation.UpdateStates(true);
        }

        public static TileEffect GetTileEffect(TileEffectType tileEffectType)
        {
            if (effectsLink.ContainsKey(tileEffectType))
                return effectsLink[tileEffectType];

            return null;
        }

        public static List<TileBehavior> GetActiveTiles(bool ignoreEffects)
        {
            List<TileBehavior> tempTiles = new List<TileBehavior>();
            List<TileBehavior> activeTiles = levelRepresentation.Tiles;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                if (!activeTiles[i].IsSubmitted)
                {
                    if (ignoreEffects)
                    {
                        if (activeTiles[i].Effect == null)
                        {
                            tempTiles.Add(activeTiles[i]);
                        }
                    }
                    else
                    {
                        tempTiles.Add(activeTiles[i]);
                    }
                }
            }

            return tempTiles;
        }

        public static List<TileBehavior> GetClickableTiles()
        {
            List<TileBehavior> tempTiles = new List<TileBehavior>();
            List<TileBehavior> activeTiles = levelRepresentation.Tiles;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                if (!activeTiles[i].IsSubmitted && activeTiles[i].IsClickable)
                {
                    tempTiles.Add(activeTiles[i]);
                }
            }

            return tempTiles;
        }

        public static List<TileBehavior> GetTilesByType(TileData tileData, int amount = int.MaxValue)
        {
            List<TileBehavior> tempTiles = new List<TileBehavior>();
            List<TileBehavior> activeTiles = levelRepresentation.Tiles;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                if (!activeTiles[i].IsSubmitted)
                {
                    if (activeTiles[i].TileData == tileData)
                    {
                        tempTiles.Add(activeTiles[i]);

                        if (tempTiles.Count >= amount)
                            break;
                    }
                }
            }

            return tempTiles;
        }

        public static List<TileBehavior> GetNeighbourTiles(ElementPosition elementPosition)
        {
            List<TileBehavior> neighbourTiles = new List<TileBehavior>();

            ElementPosition[] neighbourPositions = new ElementPosition[] { elementPosition.UpNeighbourPos, elementPosition.RightNeighbourPos, elementPosition.BottomNeighbourPos, elementPosition.LeftNeighbourPos };
            for(int i = 0; i < neighbourPositions.Length; i++)
            {
                if(levelRepresentation.IsTileExists(neighbourPositions[i]))
                {
                    neighbourTiles.Add(levelRepresentation.Layers[neighbourPositions[i]].Tile);
                }
            }

            return neighbourTiles;
        }

        public static TileBehavior GetTile(ElementPosition elementPosition)
        {
            if (levelRepresentation.IsTileExists(elementPosition))
            {
                return levelRepresentation.Layers[elementPosition].Tile;
            }

            return null;
        }

        public static bool HasNeighbourTiles(ElementPosition elementPosition)
        {
            ElementPosition[] neighbourPositions = new ElementPosition[] { elementPosition.UpNeighbourPos, elementPosition.RightNeighbourPos, elementPosition.BottomNeighbourPos, elementPosition.LeftNeighbourPos };
            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                if (levelRepresentation.IsTileExists(neighbourPositions[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static int GetCurrentLevelReward()
        {
            if (Level != null)
            {
                if (firstTimeCompletedLevel)
                {
                    return level.CoinsReward;
                }
                else
                {
                    return (int)(level.CoinsReward * 0.25f);
                }
            }

            return 5;
        }

        public static bool ReturnTiles(int count, SimpleCallback callback)
        {
            List<TileBehavior> removedTiles = DockBehavior.RemoveObjects(count).ConvertAll((slotable) => (TileBehavior)slotable);
            if (removedTiles.IsNullOrEmpty())
            {
                callback?.Invoke();
                return false;
            } 

            int revertedTiles = 0;
            foreach (TileBehavior tile in removedTiles)
            {
                Vector3 returnPosition = LevelScaler.GetPosition(tile.ElementPosition);

                Transform parentTransform = tile.transform.parent;
                if (parentTransform != null)
                {
                    returnPosition = parentTransform.TransformPoint(returnPosition);
                }

                tile.SubmitMove(returnPosition, Vector3.one * LevelScaler.TileSize, () =>
                {
                    revertedTiles++;

                    callback?.Invoke();

                    tile.SetPosition(tile.ElementPosition);

                    if (revertedTiles >= removedTiles.Count)
                    {
                        RevertElements(removedTiles);
                    }
                });
            }

            return true;
        }

        public static bool IsLevelCompletable()
        {
            List<TileBehavior> tiles = levelRepresentation.Tiles;
            foreach(TileBehavior tile in tiles)
            {
                if (tile.IsClickable)
                {
                    return true;
                }
            }

            return false;
        }

        public static void SetBusyState(bool state)
        {
            isBusy = state;
        }

        public static void ClampMaxReachedLevel()
        {
            levelSave.MaxReachedLevelIndex = Mathf.Clamp(levelSave.MaxReachedLevelIndex, 0, Database.AmountOfLevels - 1);
        }

        public static void Revive()
        {
            RaycastController.Enable();

            ReturnTiles(3, null);
        }
    }
}