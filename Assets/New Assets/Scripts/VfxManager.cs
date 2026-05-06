using Coffee.UIExtensions;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Audio;
using Watermelon;

public class VfxManager : MonoBehaviour
{
    [SerializeField] private UIParticleAttractor uIParticleAttractor;

    [Header("Particles")]
    
    [SerializeField] private ParticleSystem attractorParticle;

    [SerializeField] private UIParticle comboParticle;

    [Header("References")]
    [SerializeField] private ScoreDataModel scoreData;

    [SerializeField] private MMFeedbacks combofeedback;

    private void OnEnable()
    {
        if (scoreData != null)
            scoreData.OnScoreAdded += HandleScoreAdded;

        if (uIParticleAttractor != null)
            uIParticleAttractor.onAttracted.AddListener(HandleAttraction);
    }

    private void OnDisable()
    {
        if (scoreData != null)
            scoreData.OnScoreAdded -= HandleScoreAdded;

        if (uIParticleAttractor != null)
            uIParticleAttractor.onAttracted.RemoveListener(HandleAttraction);
    }


    private void HandleScoreAdded(int amount)
    {
        int particles = Mathf.CeilToInt(5 + Mathf.Sqrt(amount) * 2f);
        attractorParticle.Emit(particles);
        Debug.Log(particles);

        combofeedback.PlayFeedbacks();
    }

    private void HandleAttraction()
    {
        //scoreData.ApplyScoreToUI();
        Debug.Log("Attraction Ended");
    }

}
