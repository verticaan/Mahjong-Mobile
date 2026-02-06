using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Watermelon
{

    public class CardlevelTutorial : BaseTutorial, ITutorial
    {

        [SerializeField] BackgroundData backgroundData;

        [SerializeField] LevelData cardLevelData;
        [SerializeField] PreloadedLevelData firstPreloadedLevelData;

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

            cardLogicController.EnableSelectionLoop();

            UIController.HidePage<UIMainMenu>();

            GameController.LoadCustomLevel(cardLevelData, firstPreloadedLevelData, backgroundData, true, () =>
            { });

            AdsManager.DisableBanner();
        }

        public override void FinishTutorial()
        {

            saveData.isFinished = true;

            isActive = false;
            Debug.Log("Finishing tutorial: " + TutorialID);

            AdsManager.EnableBanner();
            
            UIController.ShowPage<UIGame>();

        }

        public override void Unload()
        {
        }

        public void OnSkipButtonClicked()
        {
            if (isActive && !saveData.isFinished)
            {
                FinishTutorial();

                LevelController.CompleteCustomLevel();

                gameUI.DisableTutorial();

                GameController.LoadLevel(12, () => {});
            }
        }

    }
}
