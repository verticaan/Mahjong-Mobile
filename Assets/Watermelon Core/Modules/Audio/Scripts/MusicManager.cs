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

        [Header("Fallback")]
        [Tooltip("Played when a level has no playlists assigned.")]
        [SerializeField] private MusicPlaylist fallbackPlaylist;

        [Header("Playlists")]
        [Tooltip("All playlists in the game. Every clip will be loaded into memory " +
                 "at startup. Ensure all music clips are set to Compressed In Memory " +
                 "in their AudioClip import settings.")]
        [SerializeField] private MusicPlaylist[] allPlaylists;

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
        /// Loads audio data for every playlist into memory.
        /// Called automatically on Start. Safe to call again if playlists are added at runtime.
        /// </summary>
        public void PreloadAll()
        {
            if (allPlaylists == null) return;

            foreach (MusicPlaylist playlist in allPlaylists)
                playlist?.Preload();

            fallbackPlaylist?.Preload();
        }

        // ── Playback ─────────────────────────────────────────────────────────

        /// <summary>
        /// Call from your level controller's setup phase.
        /// Picks one playlist at random if multiple are assigned to the level,
        /// then picks a random track from it and loops it.
        /// </summary>
        public void PlayForLevel(LevelData levelData)
        {
            if (levelData == null)
            {
                Debug.LogWarning("[MusicManager] PlayForLevel called with null LevelData.");
                return;
            }

            MusicPlaylist chosen = ChoosePlaylist(levelData.MusicPlaylists);

            if (chosen == null)
            {
                Debug.LogWarning("[MusicManager] LevelData has no playlists assigned — " +
                                 "using fallback.");
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
                                 "Call PlayForLevel first.");
                return;
            }

            PlaylistTrack track = activePlaylist.GetRandomTrack(
                out int chosenIndex, excludeIndex: activeTrackIndex);

            if (!track.IsValid)
            {
                Debug.LogWarning($"[MusicManager] Playlist '{activePlaylist.name}' " +
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
                                 "Call PlayForLevel first.");
                return;
            }

            PlaylistTrack track = activePlaylist.GetTrack(index);
            if (!track.IsValid)
            {
                Debug.LogWarning($"[MusicManager] Track index {index} is invalid in " +
                                 $"playlist '{activePlaylist.name}'.");
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

        private MusicPlaylist ChoosePlaylist(MusicPlaylist[] playlists)
        {
            if (playlists == null || playlists.Length == 0) return null;
            if (playlists.Length == 1) return playlists[0];
            return playlists[Random.Range(0, playlists.Length)];
        }
    }
}