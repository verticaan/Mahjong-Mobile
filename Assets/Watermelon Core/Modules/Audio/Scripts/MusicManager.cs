using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class MusicManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────

        private static MusicManager instance;
        public static MusicManager Instance => instance;

        // ── Inspector ────────────────────────────────────────────────────────

        [Header("Audio Source")]
        [SerializeField] private MusicSource musicSource;

        [Header("Main Menu")]
        [Tooltip("Playlist used exclusively on the main menu screen.")]
        [SerializeField] private MusicPlaylist menuPlaylist;

        [Header("Fallback")]
        [Tooltip("Played when a level's playlist type has no matching entry.")]
        [SerializeField] private MusicPlaylist fallbackPlaylist;

        [Header("Playlists")]
        [SerializeField] private List<MusicPlaylist> playlists = new();

        // ── Runtime state ────────────────────────────────────────────────────

        private MusicPlaylist activePlaylist;
        private PlaylistTrack activeTrack;
        private int activeTrackIndex = -1;
        private float targetVolume;

        public MusicPlaylist ActivePlaylist => activePlaylist;
        public PlaylistTrack ActiveTrack => activeTrack;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            targetVolume = musicSource != null
                ? musicSource.AudioSource.volume
                : 1f;
        }

        private void Start()
        {
            PreloadAll();
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // ── Preloading ───────────────────────────────────────────────────────

        /// <summary>
        /// Loads audio data for all playlists into memory.
        /// Called automatically on Start.
        /// </summary>
        public void PreloadAll()
        {
            foreach (MusicPlaylist playlist in playlists)
                playlist?.Preload();

            menuPlaylist?.Preload();
            fallbackPlaylist?.Preload();
        }

        // ── Playback ─────────────────────────────────────────────────────────

        /// <summary>
        /// Plays a random looping track from the main menu playlist.
        /// Call from your main menu controller on show/open.
        /// </summary>
        public void PlayMenuMusic()
        {
            if (menuPlaylist == null)
            {
                Debug.LogWarning("[MusicManager] No menu playlist assigned.");
                return;
            }

            activePlaylist = menuPlaylist;
            PlayRandomTrack();
        }

        /// <summary>
        /// Call from your level controller's setup phase.
        /// Finds the playlist matching the level's <see cref="LevelPlaylistType"/>
        /// and begins playback of a random looping track.
        /// </summary>
        public void PlayForLevel(LevelPlaylistType levelMusic)
        {
            MusicPlaylist chosen = GetPlaylist(levelMusic);

            if (chosen == null)
            {
                Debug.LogWarning($"[MusicManager] No playlist found for type " +
                                 $"'{levelMusic}' — using fallback.");
                chosen = fallbackPlaylist;
            }

            if (chosen == null)
            {
                Debug.LogWarning("[MusicManager] No fallback playlist set. " +
                                 "No music will play.");
                return;
            }

            activePlaylist = chosen;
            PlayRandomTrack();
        }

        /// <summary>
        /// Picks a new random track from the active playlist, avoiding the current one.
        /// </summary>
        public void PlayRandomTrack()
        {
            if (activePlaylist == null)
            {
                Debug.LogWarning("[MusicManager] No active playlist. " +
                                 "Call PlayForLevel or PlayMenuMusic first.");
                return;
            }

            PlaylistTrack track = activePlaylist.GetRandomTrack(
                out int chosenIndex, excludeIndex: activeTrackIndex);

            if (!track.IsValid)
            {
                Debug.LogWarning($"[MusicManager] Playlist '{activePlaylist.PlaylistType}' " +
                                 "returned an invalid track.");
                return;
            }

            activeTrackIndex = chosenIndex;
            PlayTrack(track);
        }

        /// <summary>
        /// Jumps to a specific track index in the active playlist.
        /// </summary>
        public void PlaySpecificTrack(int index)
        {
            if (activePlaylist == null)
            {
                Debug.LogWarning("[MusicManager] No active playlist. " +
                                 "Call PlayForLevel or PlayMenuMusic first.");
                return;
            }

            PlaylistTrack track = activePlaylist.GetTrack(index);
            if (!track.IsValid)
            {
                Debug.LogWarning($"[MusicManager] Track index {index} is invalid in " +
                                 $"playlist '{activePlaylist.PlaylistType}'.");
                return;
            }

            activeTrackIndex = index;
            PlayTrack(track);
        }

        /// <summary>
        /// Fades out and stops all playback. Call from your level controller's teardown.
        /// </summary>
        public void StopMusic(float fadeDuration = 0.3f)
        {
            if (musicSource == null) return;

            musicSource.Fade(0f, fadeDuration, onComplete: () =>
            {
                musicSource.AudioSource.Stop();
                musicSource.AudioSource.clip = null;
            });

            activePlaylist = null;
            activeTrack = default;
            activeTrackIndex = -1;
        }

        /// <summary>Pauses without clearing active state.</summary>
        public void PauseMusic() => musicSource?.AudioSource.Pause();

        /// <summary>Resumes a paused track.</summary>
        public void ResumeMusic() => musicSource?.AudioSource.UnPause();

        // ── Private helpers ──────────────────────────────────────────────────

        private MusicPlaylist GetPlaylist(LevelPlaylistType type)
        {
            foreach (MusicPlaylist playlist in playlists)
                if (playlist != null && playlist.PlaylistType == type)
                    return playlist;

            return null;
        }

        private void PlayTrack(PlaylistTrack track)
        {
            if (musicSource == null)
            {
                Debug.LogError("[MusicManager] No MusicSource assigned.");
                return;
            }

            activeTrack = track;

            float fadeDuration = track.GetFadeDuration(activePlaylist.FadeDuration);
            float halfFade = fadeDuration * 0.5f;
            AudioSource audioSource = musicSource.AudioSource;
            bool isFirstPlay = !audioSource.isPlaying;

            if (isFirstPlay)
            {
                audioSource.volume = 0f;
                audioSource.clip = track.Clip;
                audioSource.loop = true;
                audioSource.Play();
                musicSource.Fade(targetVolume, fadeDuration);
            }
            else
            {
                musicSource.Fade(0f, halfFade, onComplete: () =>
                {
                    audioSource.clip = track.Clip;
                    audioSource.loop = true;
                    audioSource.Play();
                    musicSource.Fade(targetVolume, halfFade);
                });
            }
        }
    }
}