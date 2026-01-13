using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Audio Clips", menuName = "Data/Core/Audio Clips")]
    public class AudioClips : ScriptableObject
    {
        [BoxGroup("UI", "UI")]
        public AudioClip buttonSound;
        [BoxGroup("UI")]
        public AudioClip levelComplete;
        [BoxGroup("UI")]
        public AudioClip levelFailed;

        [BoxGroup("Gameplay", "Gameplay")]
        public List<AudioClip> tileClick;
        [BoxGroup("Gameplay")]
        public AudioClip tileClickBlocked;
        [BoxGroup("Gameplay")]
        public List<AudioClip> mergeSound;

        [BoxGroup("Effects", "Effects")]
        public AudioClip iceCrackSound;
        [BoxGroup("Effects")]
        public AudioClip crateCrackSound;
        [BoxGroup("Effects")]
        public AudioClip rubberSound;

        [BoxGroup("Jazz", "Jazz")]
        public List<AudioClip> jazzChordTones;   // C, E, G

        [BoxGroup("Jazz")]
        public List<AudioClip> jazzColorTones;   // D, A

        [BoxGroup("Jazz")]
        public List<AudioClip> jazzTensionTones; // E♭

        [BoxGroup("Jazz Chords", "Jazz Chords")]
        public List<AudioClip> jazzTonicChords;      // C6, Cmaj7

        [BoxGroup("Jazz Chords")]
        public List<AudioClip> jazzSubdominantChords; // Dm7, F9

        [BoxGroup("Jazz Chords")]
        public List<AudioClip> jazzDominantChords;    // G7, G13

    }
}

// -----------------
// Audio Controller v 0.4
// -----------------