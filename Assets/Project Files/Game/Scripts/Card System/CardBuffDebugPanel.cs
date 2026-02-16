using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Watermelon
{
    /// <summary>
    /// Runtime debug overlay for:
    /// - Active card buffs
    /// - Player quality
    ///
    /// Visibility is controlled by a Unity UI Button (editable in Inspector).
    /// </summary>
    public sealed class CardBuffDebugPanel : MonoBehaviour
    {
        [Header("UI Toggle (Unity Button)")]
        [SerializeField] private Button toggleButton;            // Assign in Inspector
        [SerializeField] private bool updateButtonLabel = true;
        [SerializeField] private string showLabel = "Show Debug";
        [SerializeField] private string hideLabel = "Hide Debug";

        [Header("Visibility")]
        [SerializeField] private bool visible = false;

        [Header("Layout (Runtime Overlay)")]
        [SerializeField] private Vector2 panelPos = new Vector2(10f, 10f);
        [SerializeField] private Vector2 panelSize = new Vector2(560f, 420f);

        [Header("Refresh")]
        [SerializeField] private float refreshRate = 0.1f;
        [SerializeField] private bool autoResolveReferences = true;

        [Header("Near Expiry Highlight")]
        [SerializeField] private bool highlightNearExpiry = true;
        [SerializeField] private int nearExpiryTurnsThreshold = 1;
        [SerializeField] private float nearExpirySecondsThreshold = 1f;

        [Header("Runtime Visibility Boost")]
        [SerializeField] private bool fullscreenDimBackground = true;
        [SerializeField, Range(1f, 4f)] private float uiScale = 2.2f;

        private readonly List<CardBuffService.BuffDebugInfo> snapshot = new(32);
        private Vector2 scroll;

        private CardBuffService buffService;
        private PlayerQuality playerQuality;

        private float nextRefreshTime;

        public bool Visible => visible;

        public void SetVisible(bool v)
        {
            if (visible == v) return;
            visible = v;
            RefreshButtonLabel();
        }

        public void ToggleVisible()
        {
            visible = !visible;
            RefreshButtonLabel();
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (toggleButton != null)
                toggleButton.onClick.AddListener(ToggleVisible);

            RefreshButtonLabel();
        }

        private void OnDisable()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(ToggleVisible);
        }

        private void Update()
        {
            if (!visible)
                return;

            if (autoResolveReferences && (buffService == null || playerQuality == null))
                ResolveReferences();

            if (Time.unscaledTime >= nextRefreshTime)
            {
                nextRefreshTime = Time.unscaledTime + Mathf.Max(0.02f, refreshRate);

                if (buffService != null)
                    buffService.GetDebugSnapshot(snapshot);
                else
                    snapshot.Clear();
            }
        }

        private void OnGUI()
        {
            if (!visible)
                return;

            DrawLargeOverlay();
        }

        private void DrawLargeOverlay()
        {
            if (fullscreenDimBackground)
                GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * uiScale);

            float inv = 1f / uiScale;
            float x = panelPos.x * inv;
            float yTop = panelPos.y * inv;
            float w = panelSize.x * inv;
            float h = panelSize.y * inv;

            var rect = new Rect(x, yTop, w, h);
            GUI.Box(rect, "DEBUG PANEL");

            float y = rect.y + 28f;

            DrawPlayerQuality(rect, ref y);
            DrawBuffSection(rect, ref y);

            GUI.matrix = oldMatrix;
        }

        #region Sections

        private void DrawPlayerQuality(Rect panelRect, ref float y)
        {
            GUI.Label(new Rect(panelRect.x + 10, y, panelRect.width - 20, 22),
                "PLAYER QUALITY");
            y += 24;

            if (playerQuality == null)
            {
                GUI.Label(new Rect(panelRect.x + 10, y, panelRect.width - 20, 22),
                    "PlayerQuality not found.");
                y += 28;
                return;
            }

            int q = playerQuality.Quality;

            Color prev = GUI.color;
            if (q <= 25) GUI.color = Color.red;
            else if (q >= 75) GUI.color = Color.green;
            else GUI.color = Color.white;

            GUI.Label(new Rect(panelRect.x + 10, y, panelRect.width - 20, 26),
                $"Current Quality: {q}");

            GUI.color = prev;

            y += 34;
        }

        private void DrawBuffSection(Rect panelRect, ref float y)
        {
            GUI.Label(new Rect(panelRect.x + 10, y, panelRect.width - 20, 22),
                $"ACTIVE BUFFS ({snapshot.Count})");
            y += 26;

            var scrollRect = new Rect(panelRect.x + 10, y, panelRect.width - 20,
                panelRect.height - (y - panelRect.y) - 10);

            float contentHeight = Mathf.Max(1, snapshot.Count) * 24f + 10f;
            var viewRect = new Rect(0, 0, scrollRect.width - 18f, contentHeight);

            scroll = GUI.BeginScrollView(scrollRect, scroll, viewRect);

            float rowY = 6f;

            for (int i = 0; i < snapshot.Count; i++)
            {
                var b = snapshot[i];
                string duration = FormatDuration(b);

                Color prev = GUI.color;
                if (highlightNearExpiry && IsNearExpiry(b))
                    GUI.color = Color.yellow;

                GUI.Label(new Rect(6, rowY, viewRect.width - 12, 22),
                    $"{PadRight(b.Name, 28)} | stacks:{b.Stacks,2} | {duration}");

                GUI.color = prev;

                rowY += 24f;
            }

            GUI.EndScrollView();
        }

        #endregion

        #region Helpers

        private string FormatDuration(CardBuffService.BuffDebugInfo b)
        {
            if (b.IsInfinite)
                return "∞ infinite";

            if (b.HasTurns && b.HasTime)
                return $"T:{b.RemainingTurns} | S:{b.RemainingTime:0.00}s";

            if (b.HasTurns)
                return $"T:{b.RemainingTurns}";

            if (b.HasTime)
                return $"S:{b.RemainingTime:0.00}s";

            return "none";
        }

        private bool IsNearExpiry(CardBuffService.BuffDebugInfo b)
        {
            if (b.IsInfinite)
                return false;

            bool nearTurns = b.HasTurns && b.RemainingTurns <= nearExpiryTurnsThreshold;
            bool nearTime = b.HasTime && b.RemainingTime <= nearExpirySecondsThreshold;

            return nearTurns || nearTime;
        }

        private static string PadRight(string s, int width)
        {
            if (string.IsNullOrEmpty(s))
                s = "<null>";

            if (s.Length >= width)
                return s.Substring(0, width);

            return s.PadRight(width);
        }

        private void RefreshButtonLabel()
        {
            if (!updateButtonLabel || toggleButton == null)
                return;

            string label = visible ? hideLabel : showLabel;

            // Prefer TextMeshPro label if present
            var tmp = toggleButton.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = label;
                return;
            }

            // Fallback to legacy uGUI Text
            var text = toggleButton.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (text != null)
                text.text = label;
        }

        #endregion

        #region Reference Resolution

        private void ResolveReferences()
        {
            if (playerQuality == null)
                playerQuality = FindFirstObjectByType<PlayerQuality>();

            buffService = LevelController.BuffService;
        }

        #endregion
    }
}
