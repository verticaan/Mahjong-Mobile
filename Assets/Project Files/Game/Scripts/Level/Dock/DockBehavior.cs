using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Watermelon
{
    [System.Serializable]
    public class DockBehavior : MonoBehaviour
    {
        [Header("Slots")]
        [Min(1)]
        [SerializeField] int defaultSlotCount = 6;
        [Min(1)]
        [SerializeField] int minSlotCount = 4;
        [Min(1)]
        [SerializeField] int maxSlotCount = 8;

        private static DockBehavior instance;

        [SerializeField] GameObject trailPrefab;
        [SerializeField] AnimationCurve positionYCurve;
        [SerializeField] GameObject slotPrefab;

        [SerializeField] private ScoreDataModel scoreDataModel;

        private static List<SlotBehavior> slots;

        private Pool trailPool;
        private LevelController levelController;
        private Vector3 defaultContainerPosition;

        private ISlotable lastPickedObject;

        public bool IsFilled => slots[^1].IsOccupied;
        public bool IsEmpty => !slots[0].IsOccupied;

        private TweenCase delayTweenCase;
        private int addedDepth = 0;

        private int NonTempSlotCount
        {
            get
            {
                if (slots == null) return 0;
                int counter = 0;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (!slots[i].IsTemp) counter++;
                }
                return counter;
            }
        }

        public static ISlotable LastPickedObject => instance.lastPickedObject;
        public static AnimationCurve PositionYCurve => instance.positionYCurve;

        public static event Action<ISlotable> ElementAdded;
        public static event Action<List<ISlotable>> MatchCombined;

        public void Init(LevelController levelController)
        {
            instance = this;

            this.levelController = levelController;

            defaultContainerPosition = transform.position;

            lastPickedObject = null;

            trailPool = new Pool(trailPrefab, $"Trail_{trailPrefab.name}");

            minSlotCount = Mathf.Max(1, minSlotCount);
            maxSlotCount = Mathf.Max(minSlotCount, maxSlotCount);
            defaultSlotCount = Mathf.Clamp(defaultSlotCount, minSlotCount, maxSlotCount);

            slots = new List<SlotBehavior>();
            transform.GetComponentsInChildren(slots);

            slots.Sort((slot1, slot2) => (int)((slot2.Position.x - slot1.Position.x) * 100));

            // Ensure the dock starts with the configured default number of slots.
            // NOTE: This is intended for level-load initialization when the dock is empty.
            if (slots.Count > defaultSlotCount)
            {
                for (int i = slots.Count - 1; i >= defaultSlotCount; i--)
                {
                    var slot = slots[i];
                    slot.Clear();
                    Destroy(slot.gameObject);
                    slots.RemoveAt(i);
                }
            }
            else if (slots.Count < defaultSlotCount)
            {
                int toAdd = defaultSlotCount - slots.Count;
                Vector3 basePos = slots.Count > 0 ? slots[0].transform.position : transform.position;

                for (int i = 0; i < toAdd; i++)
                {
                    var newSlot = Instantiate(slotPrefab).GetComponent<SlotBehavior>();
                    newSlot.transform.position = basePos;
                    slots.Add(newSlot);
                }
            }

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                var position = slot.transform.position.SetX(GetSlotX(i, NonTempSlotCount));
                var scale = Vector3.one * LevelScaler.SlotSize;

                slot.Init(i, position, scale);
            }
        }

        private float GetSlotX(int index, int count)
        {
            return -LevelScaler.SlotSize.x * count / 2f + (index + 0.5f) * LevelScaler.SlotSize.x;
        }

        private void RepositionNonTempSlots(int newCount)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot.IsTemp) continue;

                slot.ChangePosition(slot.transform.position.SetX(GetSlotX(i, newCount)));
            }
        }

        public void Unload()
        {
            if (trailPool != null)
            {
                PoolManager.DestroyPool(trailPool);

                trailPool = null;
            }
        }

        public void PlayAppearAnimation()
        {
            HideSlots();

            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].DOScale(1f, 0.3f, Random.Range(0f, 0.2f)).SetEasing(Ease.Type.CubicOut);
            }
        }

        public void HideSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].transform.localScale = Vector3.zero;
            }

            // Reverse back to default slot count when the dock is being hidden.
            if (NonTempSlotCount > defaultSlotCount)
            {
                for (int i = slots.Count - 1; i >= 0 && NonTempSlotCount > defaultSlotCount; i--)
                {
                    var slot = slots[i];
                    if (slot.IsTemp) continue;

                    slot.Clear();
                    Destroy(slot.gameObject);
                    slots.RemoveAt(i);
                }

                RepositionNonTempSlots(NonTempSlotCount);
            }
        }

        public void DisposeQuickly()
        {
            delayTweenCase.KillActive();

            lastPickedObject = null;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                slot.Clear();
            }

            for (int i = 0; i < addedDepth * 3; i++)
            {
                var slot = slots[slots.Count - 1];
                slots.RemoveAt(slots.Count - 1);

                slot.Clear();
                Destroy(slot.gameObject);
            }
            ResetScoreSystem();
            addedDepth = 0;
        }

        public void DisableRevert()
        {
            lastPickedObject = null;
        }

        private void RemoveMatch(List<ISlotable> charactersToRemove)
        {
            lastPickedObject = null;

            var slotsToRemove = new List<SlotCase>();
            for (int i = 0; i < slots.Count; i++)
            {
                var slotCase = slots[i].SlotCase;

                if (slotCase != null && charactersToRemove.Contains(slotCase.Behavior))
                {
                    slotsToRemove.Add(slotCase);
                    slotCase.IsBeingRemoved = true;
                }
            }

            for (int i = 0; i < slotsToRemove.Count; i++)
            {
                var slotCase = slotsToRemove[i];
                slotCase.Behavior.MatchAnimation(i * 0.05f);
            }

            AudioController.PlayJazzChord(0.7f);

            //AudioController.PlaySound(AudioController.AudioClips.mergeSound);

            Tween.DelayedCall(0.4f, () =>
            {
                for (int i = 0; i < slotsToRemove.Count; i++)
                {
                    var slotCase = slotsToRemove[i];
                    var element = slotCase.Behavior;

                    slotCase.IsBeingRemoved = false;

                    element.Clear();

                    for (int j = 0; j < slots.Count; j++)
                    {
                        var slot = slots[j];

                        if (slot.SlotCase == slotCase)
                        {
                            slot.RemoveSlot();
                            break;
                        }
                    }
                }

                ShiftAllLeft();

                if (addedDepth > 0)
                {
                    addedDepth--;
                    for (int i = 0; i < 3; i++)
                    {
                        var tempSlot = slots[^1];
                        slots.RemoveAt(slots.Count - 1);

                        tempSlot.Clear();
                        Destroy(tempSlot.gameObject);
                    }
                }
                levelController.OnMatchCompleted();
            });
        }

        private bool CheckMatch(bool remove = true)
        {
            if (IsEmpty) return false;

            return CheckDockMatch(remove);
        }

        public static List<ISlotable> GetHintSlots()
        {
            SlotBehavior[] elementsArray = slots.FindAll(x => x.IsOccupied).GroupBy(x => x.SlotCase.Behavior.UniqueElementID).OrderByDescending(g => g.Count()).SelectMany(g => g).ToArray();

            if (!elementsArray.IsNullOrEmpty())
            {
                List<ISlotable> tempSlotElements = new List<ISlotable>();
                tempSlotElements.Add(elementsArray[0].SlotCase.Behavior);

                for (int i = 1; i < elementsArray.Length; i++)
                {
                    if (elementsArray[i].SlotCase.Behavior.IsSameType(elementsArray[0].SlotCase.Behavior))
                    {
                        tempSlotElements.Add(elementsArray[i].SlotCase.Behavior);
                    }
                }

                return tempSlotElements;
            }

            return null;
        }

        private bool CheckDockMatch(bool remove = true)
        {
            int counter = 1;
            var comparableRefference = slots[0].SlotCase.Behavior;
            var list = new List<ISlotable> { slots[0].SlotCase.Behavior };

            for (int i = 1; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (!slot.IsOccupied) return false;

                var slotCase = slot.SlotCase;
                var element = slotCase.Behavior;

                if (counter == 0)
                {
                    comparableRefference = element;
                }

                if (element.IsSameType(comparableRefference) && !slotCase.IsBeingRemoved && (!slot.SlotCase.IsMoving || !remove))
                {
                    counter++;
                    list.Add(element);

                    if (counter == 3)
                    {
                        if (remove)
                        {
                            UpdateScoresAfterMatch();
                            RemoveMatch(list);
                            MatchCombined?.Invoke(list);
                        }
                        return true;
                    }
                }
                else if (!slotCase.IsBeingRemoved)
                {
                    counter = 1;
                    comparableRefference = element;
                    list = new List<ISlotable> { element };
                }
                else
                {
                    counter = 0;
                    list = new List<ISlotable>();
                }
            }

            return false;
        }

        private int CalculateIndexSlots(ISlotable tileBehavior)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (!slot.IsOccupied) return i;

                if (tileBehavior.IsSameType(slot.SlotCase.Behavior))
                {
                    for (int j = i + 1; j < slots.Count; j++)
                    {
                        var nextSlot = slots[j];

                        if (!nextSlot.IsOccupied) return j;

                        if (!slot.IsOccupied || !tileBehavior.IsSameType(nextSlot.SlotCase.Behavior))
                        {
                            return j;
                        }
                    }
                }
            }

            return -1;
        }

        public static int GetSlotsAvailable()
        {
            int counter = 0;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (!slots[i].IsOccupied && !slots[i].IsTemp) counter++;
            }

            return counter;
        }

        public void SetOverlayPosition()
        {
            transform.position = defaultContainerPosition.SetZ(1.0f);
        }

        public void SetDefaultPosition()
        {
            transform.position = defaultContainerPosition;
        }

        public bool SubmitToSlot(ISlotable element, bool instant)
        {
            int index = CalculateIndexSlots(element);

            if (index == -1)
            {
                levelController.OnSlotsFilled();
                return true;
            }

            SlotCase slotCase = new SlotCase(element);
            slotCase.AddTrail(trailPool.GetPooledObject());

            slotCase.IsMoving = true;
            slotCase.MoveType = DockMoveType.Submit;

            if (slots[index].IsOccupied)
            {
                Insert(slotCase, index, instant);
            }
            else
            {
                slots[index].Assign(slotCase, instant);
            }

            lastPickedObject = element;

            ElementAdded?.Invoke(element);

            if (CheckMatch(false))
            {
                if (addedDepth < 2)
                {
                    addedDepth++;
                    for (int i = 0; i < 3; i++)
                    {
                        slots.Add(SlotBehavior.GetTempSlot(slots[^1], slots[^2]));
                    }
                }

            }
            else if (IsFilled)
            {
                levelController.OnSlotsFilled();
            }

            return true;
        }

        public static List<ISlotable> RemoveObjects(int count)
        {
            List<ISlotable> removedTiles = new List<ISlotable>();

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                SlotBehavior slot = slots[i];
                if (slot.IsOccupied)
                {
                    ISlotable tileBehavior = slot.SlotCase.Behavior;

                    slot.SlotCase.Clear(false);
                    slot.RemoveSlot();

                    if (instance.lastPickedObject == tileBehavior)
                        instance.lastPickedObject = null;

                    removedTiles.Add(tileBehavior);

                    if (removedTiles.Count >= count)
                        break;
                }
            }

            instance.ShiftAllLeft();

            return removedTiles;
        }

        public static ISlotable RemoveLastPicked()
        {
            if (instance.lastPickedObject == null) return null;

            var objToReturn = instance.lastPickedObject;
            instance.lastPickedObject = null;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (slot.IsOccupied && slot.SlotCase.Behavior == objToReturn)
                {
                    slot.SlotCase.Clear(false);
                    slot.RemoveSlot();

                    instance.ShiftAllLeft();

                    break;
                }
            }

            return instance.lastPickedObject;
        }

        public void Insert(SlotCase slotCase, int index, bool instant = false)
        {
            var freeCase = slotCase;

            for (int i = index; i < slots.Count; i++)
            {
                var slot = slots[i];

                var caseToShift = slot.RemoveSlot();

                if (freeCase != null)
                {
                    if (freeCase.IsMoving && freeCase.MoveType == DockMoveType.Submit)
                    {
                        slot.Assign(freeCase, instant);
                    }
                    else
                    {
                        slot.AssingFast(freeCase);
                    }
                }

                freeCase = caseToShift;
            }
        }
        public void ShiftAllLeft()
        {
            var lastIndex = -1;

            for (int i = 0; i < slots.Count - 1; i++)
            {
                var recepient = slots[i];

                if (recepient.IsOccupied) continue;

                bool found = false;
                for (int j = i + 1; j < slots.Count; j++)
                {
                    var donor = slots[j];

                    if (!donor.IsOccupied) continue;

                    var slotCase = donor.RemoveSlot();
                    if (slotCase.IsMoving && slotCase.MoveType == DockMoveType.Submit)
                    {
                        recepient.Assign(slotCase);
                    }
                    else
                    {
                        recepient.AssingFast(slotCase);
                    }

                    found = true;

                    break;
                }

                lastIndex = i;
                if (!found) break;
            }

            if (lastIndex == -1) return;

            for (int i = lastIndex; i < slots.Count; i++)
            {
                var slot = slots[i];
                slot.RestoreColor(Color.white);
            }
        }

        public void LateUpdate()
        {
            if (Time.frameCount % 15 == 0 && !IsEmpty)
            {
                if (!CheckMatch() && IsFilled)
                {
                    levelController.OnSlotsFilled();
                }
            }
        }

        public static void OnMovementEnded(SlotCase slotCase, DockMoveType moveType)
        {
            instance.OnMoveEnded(slotCase, moveType);
        }

        public void OnMoveEnded(SlotCase slotCase, DockMoveType moveType)
        {
            CheckMatch();

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (slot.IsOccupied && (slot.SlotCase.IsMoving || slot.SlotCase.IsBeingRemoved)) return;
            }

            ShiftAllLeft();

            for (int i = 0; i < addedDepth * 3; i++)
            {
                var slot = slots[slots.Count - 1];
                slots.RemoveAt(slots.Count - 1);

                slot.Clear();
                Destroy(slot.gameObject);
            }

            addedDepth = 0;
        }

        public void AddExtraSlot()
        {
            // Backwards-compatible API: add a single slot (if possible).
            TryAddSlots(1);
        }

        /// <summary>
        /// Current number of permanent (non-temp) slots.
        /// </summary>
        public int CurrentSlotCount => NonTempSlotCount;

        /// <summary>
        /// Sets the default starting slot count used during Init().
        /// </summary>
        public int DefaultSlotCount => defaultSlotCount;

        /// <summary>
        /// Sets the permanent slot count directly at runtime (clamped between minSlotCount and maxSlotCount).
        /// Intended to be called by an external controller on level load (before gameplay begins).
        /// Returns false if the request could not be completed safely (e.g. temp slots exist or occupied slots would be removed).
        /// </summary>
        public bool SetSlotCount(int desiredCount, bool instant = true)
        {
            minSlotCount = Mathf.Max(1, minSlotCount);
            maxSlotCount = Mathf.Max(minSlotCount, maxSlotCount);

            desiredCount = Mathf.Clamp(desiredCount, minSlotCount, maxSlotCount);

            // Do not allow structural changes while temp slots exist.
            if (addedDepth != 0) return false;

            int current = NonTempSlotCount;
            if (desiredCount == current) return true;

            if (desiredCount > current)
            {
                int toAdd = desiredCount - current;

                if (instant)
                {
                    for (int i = 0; i < toAdd; i++)
                    {
                        SpawnExtraSlotImmediate();
                    }

                    LevelController.IsRaycastEnabled = true;
                }
                else
                {
                    TryAddSlots(toAdd);
                }

                return true;
            }
            else
            {
                int toRemove = current - desiredCount;

                if (instant)
                {
                    for (int i = 0; i < toRemove; i++)
                    {
                        if (!TryRemoveLastExtraSlot())
                            return false;
                    }

                    return true;
                }

                return TryRemoveSlots(toRemove) == toRemove;
            }
        }

        /// <summary>
        /// Applies the configured default slot count.
        /// </summary>
        public bool ApplyDefaultSlotCount(bool instant = true)
        {
            return SetSlotCount(defaultSlotCount, instant);
        }

        /// <summary>
        /// Attempts to add a number of permanent (non-temp) slots, clamped between minSlotCount and maxSlotCount.
        /// Returns the amount actually added.
        /// </summary>
        public int TryAddSlots(int amount)
        {
            if (amount <= 0) return 0;

            minSlotCount = Mathf.Max(1, minSlotCount);
            maxSlotCount = Mathf.Max(minSlotCount, maxSlotCount);

            int canAdd = Mathf.Min(amount, maxSlotCount - NonTempSlotCount);
            if (canAdd <= 0) return 0;

            LevelController.IsRaycastEnabled = false;

            if (addedDepth == 0)
            {
                for (int i = 0; i < canAdd; i++)
                {
                    SpawnExtraSlot();
                }
            }
            else
            {
                StartCoroutine(WaitAndSpawnExtraSlotCoroutine(canAdd));
            }

            return canAdd;
        }

        /// <summary>
        /// Attempts to remove a number of permanent (non-temp) slots down to minSlotCount.
        /// Only empty slots can be removed. Returns the amount actually removed.
        /// </summary>
        public int TryRemoveSlots(int amount)
        {
            if (amount <= 0) return 0;
            if (addedDepth != 0) return 0;

            minSlotCount = Mathf.Max(1, minSlotCount);
            maxSlotCount = Mathf.Max(minSlotCount, maxSlotCount);

            int removable = Mathf.Min(amount, NonTempSlotCount - minSlotCount);
            if (removable <= 0) return 0;

            int removed = 0;
            for (int i = 0; i < removable; i++)
            {
                if (!TryRemoveLastExtraSlot()) break;
                removed++;
            }

            return removed;
        }

        private IEnumerator WaitAndSpawnExtraSlotCoroutine(int amount)
        {
            while (addedDepth != 0) yield return null;

            for (int i = 0; i < amount; i++)
            {
                SpawnExtraSlot();
            }
        }

        private void SpawnExtraSlot()
        {
            if (NonTempSlotCount >= maxSlotCount)
            {
                LevelController.IsRaycastEnabled = true;
                return;
            }

            int newCount = NonTempSlotCount + 1;
            RepositionNonTempSlots(newCount);

            var newSlot = Instantiate(slotPrefab).GetComponent<SlotBehavior>();

            var position = slots[0].transform.position.SetX(GetSlotX(newCount - 1, newCount));
            var scale = Vector3.one * LevelScaler.SlotSize;

            newSlot.Init(newCount - 1, position, scale);

            newSlot.transform.localScale = Vector3.zero;
            newSlot.transform.DOScale(1f, 0.1f, 0.025f);

            slots.Add(newSlot);

            Tween.DelayedCall(0.1f, () => LevelController.IsRaycastEnabled = true);
        }

        private void SpawnExtraSlotImmediate()
        {
            if (NonTempSlotCount >= maxSlotCount) return;

            int newCount = NonTempSlotCount + 1;
            RepositionNonTempSlots(newCount);

            var newSlot = Instantiate(slotPrefab).GetComponent<SlotBehavior>();

            var position = slots[0].transform.position.SetX(GetSlotX(newCount - 1, newCount));
            var scale = Vector3.one * LevelScaler.SlotSize;

            newSlot.Init(newCount - 1, position, scale);
            newSlot.transform.localScale = Vector3.one;

            slots.Add(newSlot);
        }

        private bool TryRemoveLastExtraSlot()
        {
            if (NonTempSlotCount <= minSlotCount) return false;

            for (int i = slots.Count - 1; i >= 0; i--)
            {
                var slot = slots[i];
                if (slot.IsTemp) continue;

                // Only remove empty slots to avoid unexpectedly deleting gameplay elements.
                if (slot.IsOccupied) return false;

                slot.Clear();
                Destroy(slot.gameObject);
                slots.RemoveAt(i);

                RepositionNonTempSlots(NonTempSlotCount);
                return true;
            }

            return false;
        }

        public int CountTiles(TileBehavior tile)
        {
            var counter = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (slot.IsOccupied)
                {
                    if (tile.IsSameType(slot.SlotCase.Behavior))
                    {
                        counter++;
                    }
                }
            }

            return counter;
        }
        
        private void UpdateScoresAfterMatch()
        {
            if (!scoreDataModel.TargetScoreExists) return;
            scoreDataModel.StartTimerFromList();
            int emptySlots = GetSlotsAvailable();
            scoreDataModel.AddRawScorePerSlot(emptySlots);
            scoreDataModel.IncreaseMultiplierPerMatch(1);
        }

        private void ResetScoreSystem()
        {
            scoreDataModel.ResetComboTimerIndex();
            scoreDataModel.StopAll();
        }

    }
}
