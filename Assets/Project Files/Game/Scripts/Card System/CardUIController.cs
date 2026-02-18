using MoreMountains.Feedbacks;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class CardUIController : MonoBehaviour
    {
        
        [LineSpacer("Card Prefab")]
        [SerializeField] private CardUI cardPrefab;
        
        [LineSpacer("Sounds")]
        [SerializeField] AudioClip selectSound;
        [SerializeField] AudioClip confirmSound;
        [SerializeField] AudioClip discardSound;
        
        [LineSpacer("UI Controls")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button discardButton;
        
        [LineSpacer("Transforms")]
        [SerializeField] Transform leftCardSpawnPosition;
        [SerializeField] Transform rightCardSpawnPosition;
        [SerializeField] Transform leftCardSelectPosition;
        [SerializeField] Transform rightCardSelectPosition;

        [LineSpacer("BackgroundTint")]
        [SerializeField] GameObject cardBackgroundTint; 

        private CardUI leftCardUI;
        private CardUI rightCardUI;
        
        private CardUI selectedCard;

        private Action<CardDataSO> onConfirmed;

        /// <summary>
        /// Invoked when the player discards both cards without picking one.
        /// </summary>
        private Action onDiscarded;

        //Animation after card has been spawned.
        public MMF_Player LeftCardUIAnimation;
        public MMF_Player RightCardUIAnimation;

        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmSelection);

            if (discardButton != null)
                discardButton.onClick.AddListener(DiscardHand);

            CreateCardInstances();
            CloseAll();
        }

        private void CreateCardInstances()
        {
            if (leftCardUI != null && rightCardUI != null) return;

            leftCardUI = Instantiate(cardPrefab, leftCardSpawnPosition.transform);
            rightCardUI = Instantiate(cardPrefab, rightCardSpawnPosition.transform);

            leftCardUI.gameObject.SetActive(false);
            rightCardUI.gameObject.SetActive(false);
        }
        
        public void ShowTwoCards(CardDataSO leftCard, CardDataSO rightCard,
                                  Action<CardDataSO> onConfirmedCallback,
                                  Action onDiscardedCallback = null)
        {
            onConfirmed  = onConfirmedCallback;
            onDiscarded  = onDiscardedCallback;

            // init visuals + controller wiring
            leftCardUI.Init(leftCard, this, leftCardSpawnPosition, leftCardSelectPosition);
            rightCardUI.Init(rightCard, this, rightCardSpawnPosition, rightCardSelectPosition);

            // position at spawn
            leftCardUI.transform.position  = leftCardSpawnPosition.position;
            rightCardUI.transform.position = rightCardSpawnPosition.position;

            leftCardUI.gameObject.SetActive(true);
            rightCardUI.gameObject.SetActive(true);

            ClearSelection();

            // Block gameplay raycasts for the entire duration of the selection.
            RaycastController.Disable();

            // Show background tint for the full duration of card selection
            if (cardBackgroundTint != null)
                cardBackgroundTint.SetActive(true);

            // Restore Initial values then Play Animation before card is shown
            LeftCardUIAnimation.RestoreInitialValues();
            RightCardUIAnimation.RestoreInitialValues();
            RightCardUIAnimation.PlayFeedbacks();
            LeftCardUIAnimation.PlayFeedbacks();
        }
        
        private void Select(CardUI ui)
        {
            // unselect previous
            if (selectedCard != null)
                selectedCard.SetSelected(false);

            selectedCard = ui;
            selectedCard.SetSelected(true);

            SetConfirmInteractable(true);

            if (confirmButton != null)
                confirmButton.gameObject.SetActive(true);

            if (discardButton != null)
                discardButton.gameObject.SetActive(true);
        }
        
        private void ClearSelection()
        {
            if (selectedCard != null)
                selectedCard.SetSelected(false);

            selectedCard = null;

            SetConfirmInteractable(false);

            // Hide both buttons when nothing is selected — tint stays until CloseAll
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(false);

            if (discardButton != null)
                discardButton.gameObject.SetActive(false);
        }

        
        public void OnCardClicked(CardUI clicked)
        {
            if (selectedCard == clicked)
            {
                ClearSelection();
                return;
            }

            Select(clicked);
        }
        
        private void ConfirmSelection()
        {
            if (selectedCard == null)
                return;

            // TODO: play confirmSound

            var chosen = selectedCard.CardData;

            SetConfirmInteractable(false);

            onConfirmed?.Invoke(chosen);
        }

        /// <summary>
        /// Discards the current hand without picking either card.
        /// Hides the cards and notifies the logic controller.
        /// </summary>
        private void DiscardHand()
        {
            // TODO: play discardSound

            var callback = onDiscarded;

            // Close UI first so the callback doesn't see stale state.
            CloseAll();

            callback?.Invoke();
        }

        public void CloseAll()
        {
            onConfirmed = null;
            onDiscarded = null;
            ClearSelection();

            // Selection is over — restore gameplay raycasts.
            RaycastController.Enable();

            // Hide the background tint now that selection is fully over
            if (cardBackgroundTint != null)
                cardBackgroundTint.SetActive(false);

            if (leftCardUI  != null) leftCardUI.gameObject.SetActive(false);
            if (rightCardUI != null) rightCardUI.gameObject.SetActive(false);
        }
        
        
        private void SetConfirmInteractable(bool value)
        {
            if (confirmButton != null)
                confirmButton.interactable = value;
        }
        
    }
}