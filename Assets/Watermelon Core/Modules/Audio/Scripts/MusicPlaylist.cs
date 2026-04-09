using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(menuName = "Watermelon/Audio/Music Playlist", fileName = "MusicPlaylist")]
    public class MusicPlaylist : ScriptableObject
    {
        [Tooltip("Default cross-fade duration in seconds when switching tracks.")]
        [SerializeField] [Min(0f)] private float fadeDuration = 0.3f;
        public float FadeDuration => fadeDuration;

        [SerializeField] private List<PlaylistTrack> tracks = new();
        public IReadOnlyList<PlaylistTrack> Tracks => tracks;

        /// <summary>
        /// Returns a random track from the playlist.
        /// Avoids repeating <paramref name="excludeIndex"/> if more than one track exists.
        /// </summary>
        public PlaylistTrack GetRandomTrack(out int chosenIndex, int excludeIndex = -1)
        {
            if (tracks == null || tracks.Count == 0)
            {
                chosenIndex = -1;
                return default;
            }

            if (tracks.Count == 1)
            {
                chosenIndex = 0;
                return tracks[0];
            }

            int index;
            do { index = Random.Range(0, tracks.Count); }
            while (index == excludeIndex);

            chosenIndex = index;
            return tracks[index];
        }

        /// <summary>Returns the track at <paramref name="index"/>, clamped to valid range.</summary>
        public PlaylistTrack GetTrack(int index)
        {
            if (tracks == null || tracks.Count == 0) return default;
            return tracks[Mathf.Clamp(index, 0, tracks.Count - 1)];
        }

        /// <summary>
        /// Calls LoadAudioData on every clip in this playlist that is not yet loaded.
        /// Safe to call multiple times — skips clips already in memory.
        /// </summary>
        public void Preload()
        {
            if (tracks == null) return;

            foreach (PlaylistTrack track in tracks)
            {
                if (track.Clip == null) continue;

                if (track.Clip.loadState == AudioDataLoadState.Unloaded ||
                    track.Clip.loadState == AudioDataLoadState.Failed)
                {
                    track.Clip.LoadAudioData();
                }
            }
        }

#if UNITY_EDITOR
        public bool Validate(out string error)
        {
            if (tracks == null || tracks.Count == 0)
            {
                error = $"Playlist '{name}' has no tracks assigned.";
                return false;
            }

            for (int i = 0; i < tracks.Count; i++)
            {
                if (tracks[i].Clip == null)
                {
                    error = $"Playlist '{name}': track at index {i} has no AudioClip.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
#endif
    }

    [System.Serializable]
    public struct PlaylistTrack
    {
        [SerializeField] private AudioClip clip;
        public AudioClip Clip => clip;

        [Tooltip("Override the playlist fade duration for this track. " +
                 "Leave at 0 to use the playlist default.")]
        [SerializeField] [Min(0f)] private float fadeDurationOverride;

        public bool IsValid => clip != null;

        public float GetFadeDuration(float playlistDefault)
            => fadeDurationOverride > 0f ? fadeDurationOverride : playlistDefault;
    }
}