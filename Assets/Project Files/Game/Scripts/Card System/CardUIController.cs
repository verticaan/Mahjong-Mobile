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
        
        [LineSpacer("UI Controls")]
        [SerializeField] private Button confirmButton;
        
        [LineSpacer("Transforms")]
        [SerializeField] Transform leftCardSpawnPosition;
        [SerializeField] Transform rightCardSpawnPosition;
        [SerializeField] Transform leftCardSelectPosition;
        [SerializeField] Transform rightCardSelectPosition;
        
        private CardUI leftCardUI;
        private CardUI rightCardUI;
        
        private CardUI selectedCard;
        
        private Action<CardDataSO> onConfirmed;
        
        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(ConfirmSelection);

            CreateCardInstances();
            CloseAll();
        }

        private void CreateCardInstances()
        {
            if (leftCardUI != null && rightCardUI != null) return;

            leftCardUI = Instantiate(cardPrefab, leftCardSpawnPosition.transform);
            rightCardUI = Instantiate(cardPrefab, leftCardSpawnPosition.transform);

            leftCardUI.gameObject.SetActive(false);
            rightCardUI.gameObject.SetActive(false);
        }
        
        public void ShowTwoCards(CardDataSO leftCard, CardDataSO rightCard, Action<CardDataSO> onConfirmedCallback)
        {
            onConfirmed = onConfirmedCallback;

            // init visuals + controller wiring
            leftCardUI.Init(leftCard, this, leftCardSpawnPosition,leftCardSelectPosition);
            rightCardUI.Init(rightCard, this, rightCardSpawnPosition,rightCardSelectPosition);

            // position at spawn
            leftCardUI.transform.position = leftCardSpawnPosition.position;
            rightCardUI.transform.position = rightCardSpawnPosition.position;

            leftCardUI.gameObject.SetActive(true);
            rightCardUI.gameObject.SetActive(true);

            ClearSelection();

            // TODO: tween from spawn to idle if needed
        }
        
        private void Select(CardUI ui)
        {
            // unselect previous
            if (selectedCard != null)
                selectedCard.SetSelected(false);

            selectedCard = ui;
            selectedCard.SetSelected(true);

            

            SetConfirmInteractable(true);
        }
        
        private void ClearSelection()
        {
            if (selectedCard != null)
                selectedCard.SetSelected(false);

            
            selectedCard = null;

            SetConfirmInteractable(false);
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

            // lock UI if desired
            SetConfirmInteractable(false);

            onConfirmed?.Invoke(chosen);
        }

        public void CloseAll()
        {
            onConfirmed = null;
            ClearSelection();

            if (leftCardUI != null) leftCardUI.gameObject.SetActive(false);
            if (rightCardUI != null) rightCardUI.gameObject.SetActive(false);
        }
        
        
        private void SetConfirmInteractable(bool value)
        {
            if (confirmButton != null)
                confirmButton.interactable = value;
        }
        
    }
}