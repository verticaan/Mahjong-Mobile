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
        public static int DisplayedLevelIndex  => levelSave.DisplayLevelIndex;

        private static int loadedLevelIndex;

        public static GameObject LevelObject => instance.levelObject;

        private static LevelRepresentation levelRepresentation;
        public static LevelRepresentation LevelRepresentation => levelRepresentation;

        private static Dictionary<TileEffectType, TileEffect> effectsLink;

        public static Vector2Int EvenLayerSize => new Vector2Int(Level.GetLayer(Level.AmountOfLayers - 1).GetRow(0).AmountOfCells, Level.GetLayer(Level.AmountOfLayers - 1).AmountOfRows);
        public static Vector2Int OddLayerSize  => new Vector2Int(Level.GetLayer(Level.AmountOfLayers - 2).GetRow(0).AmountOfCells, Level.GetLayer(Level.AmountOfLayers - 2).AmountOfRows);
        public static bool IsEvenLayerBigger   => EvenLayerSize.x > OddLayerSize.x;

        public static int CurrentReward => GetCurrentLevelReward();
        
        public static DockBehavior Dock => instance.dock;

        public static BackgroundBehavior Background { get; private set; }

        private static bool isCustomLevel;
        public static bool IsCustomLevel => isCustomLevel;

        public static bool IsRaycastEnabled { get; set; } = true;

        private static bool firstTimeCompletedLevel = false;

        private static bool isBusy;
        public static bool IsBusy => isBusy;

        // ── Endless / randomised mode ─────────────────────────────────────────

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
        /// <see cref="LoadRandomisedLevel"/> to configure gameplay subsystems.
        /// </summary>
        private static RandomisedLevelResult activeRandomisedResult;

        public static GameplayTimer GameplayTimer { get; private set; }

        /// <summary>
        /// Returns true when <paramref name="levelIndex"/> is beyond the last
        /// hand-crafted level and a <see cref="RandomisedLevelData"/> asset is assigned.
        /// </summary>
        private bool ShouldUseEndlessMode(int levelIndex)
            => levelIndex >= database.AmountOfLevels && randomisedLevelConfig != null;

        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            instance = this;
        }

        public void Init()
        {
            LevelScaler levelScaler = GetComponent<LevelScaler>();
            levelScaler.Init();

            GameplayTimer = new GameplayTimer();
            buffService   = new CardBuffService();
            GameplayTimer.OnTimerFinished       += OnTimerFinished;
            scoreDataModel.OnScoreTargetReached += OnScoreTargetReached;
            scoreDataModel.Init();
            database.Init();
            dock.Init(this);
            levelSave           = SaveController.GetSaveObject<LevelSave>("level");
            cardLogicController = gameObject.GetComponent<CardLogicController>();
            
            RaycastController raycastController = gameObject.AddComponent<RaycastController>();
            raycastController.Init();

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
            effectsLink         = null;

            loadedLevelIndex = -1;
            isLevelLoaded    = false;
            isBusy           = false;

            isEndlessMode          = false;
            activeRandomisedResult = null;
        }
        
        private void Update()
        {
            float dt          = Time.deltaTime;
            bool  timeBlocked = UIController.IsPopupOpened || !UIController.focusedOnGame || CardLogicController.IsChoosing;
            float authoritativeDt = timeBlocked ? 0f : dt;

            GameplayTimer.Tick(authoritativeDt);
            scoreDataModel.Tick(authoritativeDt);
            buffService?.Tick(authoritativeDt);
        }

        // ── Custom level ──────────────────────────────────────────────────────

        public void LoadCustomLevel(LevelData levelData, PreloadedLevelData preloadedLevelData, BackgroundData backgroundData, bool animateDock, SimpleCallback onLevelLoaded = null)
        {
            level = levelData;

            loadedLevelIndex        = -1;
            firstTimeCompletedLevel = false;
            isCustomLevel           = true;
            isBusy                  = true;

            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.PowerUpsUIController.OnLevelStarted(0);
            gameUI.ActivateTutorial();

            levelObject.SetActive(true);
            levelScaler.Recalculate();
            layersParentObject.transform.position = levelScaler.LevelFieldCenter;

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

            if (animateDock)
                dock.PlayAppearAnimation();

            LoadBackground(backgroundData);
            MusicManager.Instance.PlayForLevel(level.PlaylistType);
        }

        public static void CompleteCustomLevel()
        {
            if (levelRepresentation != null)
                UnloadLevel();

            isCustomLevel = false;
        }

        // ── Normal level ──────────────────────────────────────────────────────

        public void LoadLevel(int levelIndex, SimpleCallback onLevelLoaded = null)
        {
            if (levelRepresentation != null)
                UnloadLevel();

            if (ShouldUseEndlessMode(levelIndex))
            {
                LoadRandomisedLevel(levelIndex, onLevelLoaded);
                return;
            }

            isEndlessMode          = false;
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
                levelSave.RealLevelIndex       = realLevelIndex;

                if (realLevelIndex != levelIndex)
                    levelSave.IsPlayingRandomLevel = true;
            }

            SaveController.Save();

            levelSave.DisplayLevelIndex = levelIndex;
            loadedLevelIndex            = levelIndex;
            firstTimeCompletedLevel     = false;
            isBusy                      = true;

            level = database.GetLevel(realLevelIndex);

            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.PowerUpsUIController.OnLevelStarted(levelIndex);
            gameUI.UpdateLevelNumber(levelIndex + 1);

            levelObject.SetActive(true);
            levelScaler.Recalculate();
            layersParentObject.transform.position = levelScaler.LevelFieldCenter;
            
            LoadLevelData(level);

            TileData[] availableObjects = database.AvailableForLevel(level);

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
            MusicManager.Instance.PlayForLevel(level.PlaylistType);

            Tween.NextFrame(() =>
            {
                SavePresets.CreateSave("Level " + (levelIndex + 1).ToString("0000"), "Levels");
            });
        }

        // ── Endless / randomised level ────────────────────────────────────────

        /// <summary>
        /// Loads a procedurally-configured level once the player has cleared all
        /// hand-crafted levels. A random <see cref="LevelData"/> supplies the board
        /// geometry; <see cref="RandomisedLevelData"/> randomises every gameplay param.
        /// </summary>
        private void LoadRandomisedLevel(int displayLevelIndex, SimpleCallback onLevelLoaded = null)
        {
            isEndlessMode           = true;
            isBusy                  = true;
            firstTimeCompletedLevel = false;
            loadedLevelIndex        = displayLevelIndex;

            // Pick a random hand-crafted level as the board template
            LevelData sourceLevelData = database.GetLevel(Random.Range(0, database.AmountOfLevels));

            // Generate all randomised gameplay parameters
            activeRandomisedResult = randomisedLevelConfig.Randomise(sourceLevelData);

            // Keep the static 'level' field pointing at the source so helpers
            // that read LevelController.Level (EvenLayerSize, OddLayerSize, …) work
            level = sourceLevelData;

            levelSave.DisplayLevelIndex    = displayLevelIndex;
            levelSave.IsPlayingRandomLevel = false;
            SaveController.MarkAsSaveIsRequired();

            UIGame gameUI = UIController.GetPage<UIGame>();
            gameUI.PowerUpsUIController.OnLevelStarted(displayLevelIndex);
            gameUI.UpdateLevelNumber(displayLevelIndex + 1);

            levelObject.SetActive(true);
            levelScaler.Recalculate();
            layersParentObject.transform.position = levelScaler.LevelFieldCenter;

            ApplyGameplayConfig(activeRandomisedResult);

            TileData[] availableObjects  = database.AvailableForLevel(sourceLevelData);
            LevelData  strippedLevelData = StripEffects(sourceLevelData);
            levelRepresentation = new LevelRepresentation(strippedLevelData, layersParentObject);
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

            // Use the playlist derived from the randomised flags, not from the
            // source level template (which would give the wrong music context)
            MusicManager.Instance.PlayForLevel(activeRandomisedResult.PlaylistType);
        }

        // ── Gameplay configuration ────────────────────────────────────────────

        /// <summary>
        /// Configures all gameplay subsystems for a hand-crafted <see cref="LevelData"/>.
        /// Deck application order: timer list → score list, add or override per
        /// <see cref="LevelData.OverrideDefaultDeck"/>.
        /// </summary>
        private void LoadLevelData(LevelData levelData)
        {
            scoreDataModel.ResetForLevel();

            if (levelData.UsesCards)
            {
                cardLogicController.EnableSelectionLoop();
                buffService.SubscribeToMatchResolved();
            }

            var timer       = levelData.GameplayTimer;
            var scoreTarget = levelData.ScoreTarget;

            if (timer.Enabled)
            {
                GameplayTimer.SetMaxTime(timer.Value);
                GameplayTimer.Start();
                ApplyDeckList(timer.List, levelData.OverrideDefaultDeck);
            }

            if (scoreTarget.Enabled)
            {
                scoreDataModel.SetTargetScoreExists(true);
                scoreDataModel.SetTargetScore(scoreTarget.Value);
                ApplyDeckList(scoreTarget.List, levelData.OverrideDefaultDeck);
            }

            if (levelData.NonDefaultStartingSlots.Enabled)
                dock.SetSlotCount(levelData.NonDefaultStartingSlots.Value);
            else
                dock.ApplyDefaultSlotCount();
        }

        /// <summary>
        /// Configures all gameplay subsystems from a <see cref="RandomisedLevelResult"/>.
        /// Deck layering mirrors <see cref="LoadLevelData"/>:
        ///   1. Default deck — applied first whenever cards are active.
        ///   2. Timer deck   — applied on top when a timer is active.
        ///   3. Score deck   — applied on top when a score target is active.
        /// Timer and score can both be active simultaneously; their decks compose freely.
        /// </summary>
        private void ApplyGameplayConfig(RandomisedLevelResult result)
        {
            scoreDataModel.ResetForLevel();

            if (result.UsesCards)
            {
                cardLogicController.EnableSelectionLoop();
                buffService.SubscribeToMatchResolved();

                // 1. Base deck — always first when cards are on
                ApplyDeckList(result.DefaultDeckList, overrideDefault: false);
            }

            if (result.HasTimer)
            {
                GameplayTimer.SetMaxTime(result.TimerSeconds.Value);
                GameplayTimer.Start();

                // 2. Timer deck layered on top
                ApplyDeckList(result.TimerDeckList, overrideDefault: false);
            }

            if (result.HasScoreTarget)
            {
                scoreDataModel.SetTargetScoreExists(true);
                scoreDataModel.SetTargetScore(result.ScoreTarget.Value);

                // 3. Score deck layered on top
                ApplyDeckList(result.ScoreDeckList, overrideDefault: false);
            }

            // Randomised levels always use the default slot count
            dock.ApplyDefaultSlotCount();
        }

        /// <summary>
        /// Adds or overrides the active card deck, guarded against null/empty input.
        /// </summary>
        private void ApplyDeckList(List<CardDeckSO> decks, bool overrideDefault)
        {
            if (decks == null || decks.Count == 0) return;

            if (overrideDefault)
                cardLogicController.OverrideActiveDeck(decks.ToArray());
            else
                cardLogicController.AddToActiveDeck(decks.ToArray());
        }

        // ── Background ────────────────────────────────────────────────────────

        private void LoadBackground(BackgroundData backgroundData = null)
        {
            if (Background != null)
                Destroy(Background.gameObject);

            if (backgroundData == null)
                backgroundData = database.GetLastAvailableBackgroundData();

            if (backgroundData != null)
                Background = Instantiate(backgroundData.BackgroundPrefab).GetComponent<BackgroundBehavior>();
        }

        // ── Level events ──────────────────────────────────────────────────────

        public void OnMatchCompleted()
        {
            if (isCustomLevel) return;
            if (levelRepresentation.Tiles.Count != 0 || !dock.IsEmpty) return;

            // Read score-target state from the active source (randomised or normal level)
            bool hasScoreTarget = isEndlessMode
                ? activeRandomisedResult?.HasScoreTarget ?? false
                : level.ScoreTarget.Enabled;

            int scoreTarget = isEndlessMode
                ? activeRandomisedResult?.ScoreTarget ?? 0
                : (level.ScoreTarget.Enabled ? level.ScoreTarget.Value : 0);

            if (hasScoreTarget)
            {
                // Board empty but score not yet met → lose
                // Score-target win is handled by OnScoreTargetReached
                if (scoreDataModel.CurrentScore < scoreTarget)
                    LoseLevel();
            }
            else
            {
                WinLevel();
            }
        }

        public void OnSlotsFilled()
        {
            LoseLevel();
        }

        private void OnTimerFinished()
        {
            LoseLevel();
        }

        private void OnScoreTargetReached()
        {
            Debug.Log("OnScoreTargetReached");
            WinLevel();
        }

        public static void OnTileSubmitted(TileBehavior tileBehavior)
        {
            if (!GameController.IsGameActive) return;

            TileEffect effect = tileBehavior.Effect;
            if (effect != null)
                effect.OnTileSubmitted();

            List<TileBehavior> activeTiles = levelRepresentation.Tiles;
            foreach (TileBehavior tiles in activeTiles)
            {
                effect = tiles.Effect;
                if (effect != null)
                    effect.OnAnyTileSubmitted();
            }
        }

        // ── Win / lose ────────────────────────────────────────────────────────

        /// <summary>
        /// Unified method to complete the current level as a win.
        /// </summary>
        private void WinLevel()
        {
            if (!GameController.IsGameActive) return;
            if (isCustomLevel) return;

            RaycastController.Disable();

            if (isEndlessMode)
            {
                levelSave.DisplayLevelIndex++;
                levelSave.MaxReachedLevelIndex = database.AmountOfLevels - 1;
                firstTimeCompletedLevel        = false;
            }
            else
            {
                levelSave.IsPlayingRandomLevel = false;
                levelSave.DisplayLevelIndex++;

                if (levelSave.DisplayLevelIndex > levelSave.MaxReachedLevelIndex)
                {
                    levelSave.MaxReachedLevelIndex = levelSave.DisplayLevelIndex;
                    firstTimeCompletedLevel        = true;
                }
            }

            SaveController.MarkAsSaveIsRequired();
            ResetSubsystems();
            GameController.OnLevelCompleted();
            AudioController.PlaySound(AudioController.AudioClips.levelComplete);
        }

        /// <summary>
        /// Unified method to end the current level as a loss.
        /// </summary>
        private void LoseLevel()
        {
            if (!GameController.IsGameActive) return;
            PauseSubsystems();
            GameController.OnLevelFailed();
            AudioController.PlaySound(AudioController.AudioClips.levelFailed);
        }

        // ── Unload ────────────────────────────────────────────────────────────

        public static void UnloadLevel()
        {
            PUController.ResetBehaviors();

            if (levelRepresentation != null)
            {
                levelRepresentation.Clear();
                levelRepresentation = null;
            }

            ResetSubsystems();
            MusicManager.Instance.StopMusic();
            instance.levelSpawnAnimation.Clear();
            instance.dock.DisposeQuickly();
            instance.dock.HideSlots();
        }

        public static void ResetSubsystems()
        {
            cardLogicController.DisableSelectionLoop(true);
            GameplayTimer.Reset();
            buffService?.ClearAllBuffs();
            buffService?.UnsubscribeFromMatchResolved();
            ScoreDataModel.StopAll();
        }

        public static void ResumeSubsystems()
        {
            GameplayTimer.Resume();
            ScoreDataModel.ResumeComboTimer();
        }
        
        public static void PauseSubsystems()
        {
            GameplayTimer.Pause();
            ScoreDataModel.PauseComboTimer();
        }
        
        public static void DisableSubsystems()
        {
            
        }

        // ── Tile helpers ──────────────────────────────────────────────────────

        public static bool AreAllMatchesCompleted()
            => LevelRepresentation != null && LevelRepresentation.Tiles.Count == 0 && Dock.IsEmpty;

        public static bool SubmitIsAllowed()
            => !instance.dock.IsFilled;

        public static void SubmitElement(TileBehavior tileBehavior)
        {
            tileBehavior.MarkAsSubmitted();
            instance.dock.SubmitToSlot(tileBehavior, false);
            levelRepresentation.RemoveObject(tileBehavior);
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
                    returnPosition = parentTransform.TransformPoint(returnPosition);

                tile.SubmitMove(returnPosition, Vector3.one * LevelScaler.TileSize, () =>
                {
                    revertedTiles++;
                    callback?.Invoke();
                    tile.SetPosition(tile.ElementPosition);

                    if (revertedTiles >= removedTiles.Count)
                        RevertElements(removedTiles);
                });
            }

            return true;
        }

        public static TileBehavior SpawnDockTile(int tileID)
        {
            TileData        tileData        = Database.GetTile(tileID);
            ElementPosition elementPosition = new ElementPosition(-1, -1);

            TileBehavior tileBehavior = tileData.Pool.GetPooledObject().GetComponent<TileBehavior>();
            tileBehavior.Init(tileData, elementPosition);
            tileBehavior.transform.localScale = Vector3.one;
            tileBehavior.SetScale(Vector2.one * LevelScaler.SlotSize);
            tileBehavior.MarkAsSubmitted();

            Dock.SubmitToSlot(tileBehavior, true);

            return tileBehavior;
        }

        public static TileEffect GetTileEffect(TileEffectType tileEffectType)
        {
            if (effectsLink.ContainsKey(tileEffectType))
                return effectsLink[tileEffectType];

            return null;
        }

        public static List<TileBehavior> GetActiveTiles(bool ignoreEffects)
        {
            List<TileBehavior> tempTiles   = new List<TileBehavior>();
            List<TileBehavior> activeTiles = levelRepresentation.Tiles;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                if (!activeTiles[i].IsSubmitted)
                {
                    if (ignoreEffects)
                    {
                        if (activeTiles[i].Effect == null)
                            tempTiles.Add(activeTiles[i]);
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
            List<TileBehavior> tempTiles   = new List<TileBehavior>();
            List<TileBehavior> activeTiles = levelRepresentation.Tiles;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                if (!activeTiles[i].IsSubmitted && activeTiles[i].IsClickable)
                    tempTiles.Add(activeTiles[i]);
            }

            return tempTiles;
        }

        public static List<TileBehavior> GetTilesByType(TileData tileData, int amount = int.MaxValue)
        {
            List<TileBehavior> tempTiles   = new List<TileBehavior>();
            List<TileBehavior> activeTiles = levelRepresentation.Tiles;

            for (int i = 0; i < activeTiles.Count; i++)
            {
                if (!activeTiles[i].IsSubmitted && activeTiles[i].TileData == tileData)
                {
                    tempTiles.Add(activeTiles[i]);

                    if (tempTiles.Count >= amount)
                        break;
                }
            }

            return tempTiles;
        }

        public static List<TileBehavior> GetNeighbourTiles(ElementPosition elementPosition)
        {
            List<TileBehavior> neighbourTiles = new List<TileBehavior>();

            ElementPosition[] neighbourPositions = new ElementPosition[]
            {
                elementPosition.UpNeighbourPos,
                elementPosition.RightNeighbourPos,
                elementPosition.BottomNeighbourPos,
                elementPosition.LeftNeighbourPos
            };

            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                if (levelRepresentation.IsTileExists(neighbourPositions[i]))
                    neighbourTiles.Add(levelRepresentation.Layers[neighbourPositions[i]].Tile);
            }

            return neighbourTiles;
        }

        public static TileBehavior GetTile(ElementPosition elementPosition)
        {
            if (levelRepresentation.IsTileExists(elementPosition))
                return levelRepresentation.Layers[elementPosition].Tile;

            return null;
        }

        public static bool HasNeighbourTiles(ElementPosition elementPosition)
        {
            ElementPosition[] neighbourPositions = new ElementPosition[]
            {
                elementPosition.UpNeighbourPos,
                elementPosition.RightNeighbourPos,
                elementPosition.BottomNeighbourPos,
                elementPosition.LeftNeighbourPos
            };

            for (int i = 0; i < neighbourPositions.Length; i++)
            {
                if (levelRepresentation.IsTileExists(neighbourPositions[i]))
                    return true;
            }

            return false;
        }

        public static bool IsLevelCompletable()
        {
            List<TileBehavior> tiles = levelRepresentation.Tiles;
            foreach (TileBehavior tile in tiles)
            {
                if (tile.IsClickable)
                    return true;
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

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns a transient runtime-only copy of <paramref name="source"/> with
        /// every cell's <see cref="CellData.Effect"/> cleared to
        /// <see cref="TileEffectType.None"/>.
        ///
        /// Used by <see cref="LoadRandomisedLevel"/> so that the board geometry is
        /// reused as-is, but hand-crafted tile effects do not carry over into
        /// procedurally-generated levels. The copy is a plain
        /// <see cref="ScriptableObject.CreateInstance"/> and is never saved to disk;
        /// it will be garbage-collected when the level unloads.
        /// </summary>
        private static LevelData StripEffects(LevelData source)
        {
            LevelData copy = ScriptableObject.CreateInstance<LevelData>();

            int layerCount = source.AmountOfLayers;
            var layers     = new Layer[layerCount];

            for (int l = 0; l < layerCount; l++)
            {
                Layer sourceLayer = source.GetLayer(l);
                int   rowCount    = sourceLayer.AmountOfRows;
                var   rows        = new LayerRow[rowCount];

                for (int r = 0; r < rowCount; r++)
                {
                    LayerRow sourceRow  = sourceLayer.GetRow(r);
                    int      cellCount  = sourceRow.AmountOfCells;
                    var      cells      = new CellData[cellCount];

                    for (int c = 0; c < cellCount; c++)
                    {
                        CellData sourceCell = sourceRow.GetCell(c);
                        cells[c] = new CellData
                        {
                            IsFilled = sourceCell.IsFilled,
                            Effect   = TileEffectType.None   // strip effect
                        };
                    }

                    rows[r] = new LayerRow(cells);
                }

                layers[l] = new Layer(rows);
            }

            copy.InitFromLayers(layers, source.BottomLayerWidth, source.BottomLayerHeight);
            return copy;
        }

        private static int GetCurrentLevelReward()
        {
            if (Level != null)
                return firstTimeCompletedLevel ? level.CoinsReward : (int)(level.CoinsReward * 0.25f);

            return 5;
        }
    }
}