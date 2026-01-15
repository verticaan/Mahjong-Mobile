using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class CardShuffle : CardBehaviorBase
    {
        [LineSpacer("Settings")]
        [SerializeField]
        private float scaleTime = 0.5f;
        public float ScaleTime => scaleTime;
        [SerializeField]
        private float scaleMinDelay = 0.05f;
        public float ScaleMinDelay => scaleMinDelay;
        [SerializeField]
        private float scaleMaxDelay = 0.4f;
        public float ScaleMaxDelay => scaleMaxDelay;
        [SerializeField]
        private Ease.Type scaleEasingType = Ease.Type.BackOut;
        public Ease.Type ScaleEasingType => scaleEasingType;
        
        private TweenCase delayTweenCase;

        public override void Init()
        {
            throw new System.NotImplementedException();
        }

        public override bool Activate()
        {
            if (!LevelController.IsBusy)
            {
                LevelRepresentation levelRepresentation = LevelController.LevelRepresentation;

                List<TileBehavior> activeTiles = levelRepresentation.Tiles;
                if (activeTiles != null)
                {
                    if (activeTiles.Count > 1)
                    {
                        List<TileBehavior> allowedToShuffleTiles = new List<TileBehavior>(activeTiles);

                        for (int i = allowedToShuffleTiles.Count - 1; i >= 0; i--)
                        {
                            if (!allowedToShuffleTiles[i].IsShuffleAllowed())
                            {
                                allowedToShuffleTiles.RemoveAt(i);
                            }
                        }

                        if (allowedToShuffleTiles.Count > 1)
                        {
                            LevelController.SetBusyState(true);

                            RaycastController.Disable();

                            ElementPosition[] shuffleElements = new ElementPosition[allowedToShuffleTiles.Count];
                            for (int i = 0; i < shuffleElements.Length; i++)
                            {
                                shuffleElements[i] = allowedToShuffleTiles[i].ElementPosition;
                            }

                            shuffleElements.Shuffle();

                            // Reset tiles scale
                            for (int i = 0; i < activeTiles.Count; i++)
                            {
                                activeTiles[i].transform.localScale = Vector3.zero;
                            }

                            // Reposition shuffled tiles
                            for (int i = 0; i < shuffleElements.Length; i++)
                            {
                                allowedToShuffleTiles[i].transform.localScale = Vector3.zero;
                                allowedToShuffleTiles[i].transform.localPosition = LevelScaler.GetPosition(shuffleElements[i]);
                                allowedToShuffleTiles[i].SetPosition(shuffleElements[i]);
                            }

                            levelRepresentation.RelinkTiles();

                            OptimisedTilesScaleTweenCase optimisedShuffleTweenCase = new OptimisedTilesScaleTweenCase(activeTiles, ScaleTime, ScaleMinDelay, ScaleMaxDelay);
                            optimisedShuffleTweenCase.SetEasing(ScaleEasingType);
                            optimisedShuffleTweenCase.OnComplete(() =>
                            {
                                RaycastController.Enable();

                                LevelController.SetBusyState(false);
                            });

                            optimisedShuffleTweenCase.StartTween();

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public override void ResetBehavior()
        {

            delayTweenCase.KillActive();
        }
    }
}