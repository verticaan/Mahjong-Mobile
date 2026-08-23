using System;
using TMPro;
using UnityEngine;

namespace Watermelon
{
    public class TimerVisualiser : MonoBehaviour
    {
        [SerializeField] TMP_Text timerText;
        private GameplayTimer timer;

        [SerializeField] SlicedFilledImage fillImage;

        public void Show(GameplayTimer timer)
        {
            if (this.timer != null)
                this.timer.OnTimeSpanChanged -= OnTimeChanged;

            this.timer = timer;
            gameObject.SetActive(true);

            timer.OnTimeSpanChanged += OnTimeChanged;
            OnTimeChanged(timer.CurrentTimeSpan);
        }

        private void OnDestroy()
        {
            if (timer != null)
                timer.OnTimeSpanChanged -= OnTimeChanged;
        }

        public void Hide()
        {
            gameObject.SetActive(false);

            if (timer != null)
            {
                timer.OnTimeSpanChanged -= OnTimeChanged;
                timer = null;
            }
        }

        public void SetFreezeFillAmount(float t)
        {
            fillImage.fillAmount = t;
        }

        public void OnTimeChanged(TimeSpan timeSpan)
        {
            timerText.text = string.Format("{0:mm\\:ss}", timeSpan);
        }
    }
}
