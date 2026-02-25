using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.GPUSort;
using Random = UnityEngine.Random;

namespace Watermelon
{

    public class CardlevelTutorial : BaseTutorial, ITutorial
    {
        [SerializeField] BackgroundData backgroundData;

        [Header("Step I")]
        [SerializeField] LevelData cardLevelData;
        [SerializeField] PreloadedLevelData firstPreloadedLevelData;
        [SerializeField] string firstStepTitle = "Welcome";
        [SerializeField] string firstStepMessage = "This is the card level tutorial. Let's get started!";

        [Header("Step II")]
        [SerializeField] string secondStepTitle = "Great";
        [SerializeField] string secondStepMessage = "You selected a card. Now watch what happens next...";

        [Header("Step II")]
        [SerializeField] string thirdStepTitle = "Awesome";
        [SerializeField] string thirdStepMessage = "Now watch how the card affects the Game";

        [Header("Finish")]
        [SerializeField] string finishTitle = "Good job!";


        //[SerializeField] private int cardLevelIndex;

        private static CardLogicController cardLogicController;

        public override bool IsFinished => saveData.isFinished;

        //public static int CardLevelIndex { get; private set; }


        private bool isActive;
        public override bool IsActive => isActive;

        private int progress;
        public override int Progress => progress;

        private TutorialBaseSave saveData;

        private UIGame gameUI;


        public override void Init()
        {
            //CardLevelIndex = cardLevelIndex;

            TutorialController.RegisterTutorial(this);

            cardLogicController = gameObject.GetComponent<CardLogicController>();
            
            cardLogicController.OnCardsShown += OnCardsShownTutorialStep;
            cardLogicController.OnCardSelected += OnCardSelectedTutorialStep;
            cardLogicController.OnCardDiscarded += OnCardDiscardedTutorialStep;

            saveData = SaveController.GetSaveObject<TutorialBaseSave>(string.Format(ITutorial.SAVE_IDENTIFIER, TutorialID.ToString()));

            Debug.Log("Tutorial save data loaded: " + saveData);

            gameUI = UIController.GetPage<UIGame>();
        }

        public override void StartTutorial()
        {
            if (isActive) return;

            isActive = true;
            progress = 0;

            Debug.Log("Starting tutorial: " + TutorialID);

            LevelController.UnloadLevel();

            UIController.HidePage<UIMainMenu>();

            gameUI.SetTutorialText(firstStepTitle, firstStepMessage);

            StartCoroutine(ShowSecondTutorialStepAfterDelay(3f));

            DockBehavior.MatchCombined += OnMatchCombined;

            GameController.LoadCustomLevel(cardLevelData, firstPreloadedLevelData, backgroundData, true, () =>
            {
                gameUI.ActivateTutorial();

            });

            AdsManager.DisableBanner();
        }

        public override void FinishTutorial()
        {

            saveData.isFinished = true;

            isActive = false;
            Debug.Log("Finishing tutorial: " + TutorialID);

            AdsManager.EnableBanner();
            
            UIController.ShowPage<UIGame>();

            cardLogicController.OnCardsShown -= OnCardsShownTutorialStep;
            cardLogicController.OnCardSelected -= OnCardSelectedTutorialStep;

        }

        public override void Unload()
        {
        }

        private IEnumerator ShowSecondTutorialStepAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameUI.SetTutorialText("Show cards", "Make 3 Correct matches to activate cards");
        }

        private IEnumerator FinishTutorialStepAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            gameUI.SetTutorialText("Great", "Complete the level to continue");
        }



        private void OnCardsShownTutorialStep(CardDataSO left, CardDataSO right)
        {
            gameUI.SetTutorialText(secondStepTitle, secondStepMessage);
        }

        private void OnCardSelectedTutorialStep(CardDataSO selectedCard)
        {
            gameUI.SetTutorialText(thirdStepTitle, thirdStepMessage);
            StartCoroutine(FinishTutorialStepAfterDelay(3f));
        }

        private void OnCardDiscardedTutorialStep(CardDataSO discardedCard)
        {

            gameUI.SetTutorialText("Cards Discarded", "Discarding cards affects the card quality");
            StartCoroutine(FinishTutorialStepAfterDelay(3f));
        }

        public void CompleteTutorial()
        {
            FinishTutorial();
            LevelController.CompleteCustomLevel();
            gameUI.DisableTutorial();
            GameController.LoadLevel(5, () => { });
        }

        private void OnMatchCombined(List<ISlotable> tiles)
        {
            if (LevelController.LevelRepresentation.Tiles.IsNullOrEmpty())
                {
                    gameUI.SetTutorialText(finishTitle, "");

                    Tween.DelayedCall(1.0f, () =>
                    {
                        CompleteTutorial();
                    });
                }
        }

        public void OnSkipButtonClicked()
        {
            if (isActive && !saveData.isFinished)
            {
                CompleteTutorial();
            }
        }

    }
}
