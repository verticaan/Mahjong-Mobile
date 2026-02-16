#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Watermelon
{
    public sealed class CardBuffDebugWindow : EditorWindow
    {
        private Vector2 scroll;
        private readonly List<CardBuffService.BuffDebugInfo> snapshot = new(64);
        private double nextRepaintTime;

        [MenuItem("Tools/Debug/Card Debug")]
        public static void OpenOrFocus()
        {
            var w = GetWindow<CardBuffDebugWindow>("Card Debug");
            w.minSize = new Vector2(520, 360);
            w.Show(false);
            w.Focus();
        }


        private void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("CARD DEBUG", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Live buffs + player quality (best used alongside Device Simulator).", EditorStyles.miniLabel);
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live data.", MessageType.Info);
                return;
            }

            // Hookups:
            var buffService = LevelController.BuffService;
            var playerQuality = Object.FindFirstObjectByType<PlayerQuality>();

            DrawPlayerQuality(playerQuality);
            EditorGUILayout.Space(8);
            DrawBuffs(buffService);

            // Auto-refresh while playing
            double t = EditorApplication.timeSinceStartup;
            if (t >= nextRepaintTime)
            {
                nextRepaintTime = t + 0.1;
                Repaint();
            }
        }

        private void DrawPlayerQuality(PlayerQuality pq)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Player Quality", EditorStyles.boldLabel);

                if (pq == null)
                {
                    EditorGUILayout.HelpBox("PlayerQuality not found in scene.", MessageType.Warning);
                    return;
                }

                int q = pq.Quality;

                var prev = GUI.color;
                if (q <= 25) GUI.color = Color.red;
                else if (q >= 75) GUI.color = Color.green;
                else GUI.color = Color.white;

                EditorGUILayout.LabelField($"Quality: {q}", EditorStyles.largeLabel);

                GUI.color = prev;
            }
        }

        private void DrawBuffs(CardBuffService service)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Active Buffs", EditorStyles.boldLabel);

                if (service == null)
                {
                    EditorGUILayout.HelpBox("LevelController.BuffService is null.", MessageType.Warning);
                    return;
                }

                service.GetDebugSnapshot(snapshot);
                EditorGUILayout.LabelField($"Count: {snapshot.Count}");

                EditorGUILayout.Space(4);

                using (var sv = new EditorGUILayout.ScrollViewScope(scroll))
                {
                    scroll = sv.scrollPosition;

                    if (snapshot.Count == 0)
                    {
                        EditorGUILayout.LabelField("No active buffs.");
                        return;
                    }

                    for (int i = 0; i < snapshot.Count; i++)
                    {
                        var b = snapshot[i];

                        string duration;
                        if (b.IsInfinite) duration = "∞ infinite";
                        else if (b.HasTurns && b.HasTime) duration = $"T:{b.RemainingTurns} | S:{b.RemainingTime:0.00}s";
                        else if (b.HasTurns) duration = $"T:{b.RemainingTurns}";
                        else if (b.HasTime) duration = $"S:{b.RemainingTime:0.00}s";
                        else duration = "none";

                        bool nearExpiry =
                            !b.IsInfinite && (
                                (b.HasTurns && b.RemainingTurns <= 1) ||
                                (b.HasTime && b.RemainingTime <= 1f)
                            );

                        var prev = GUI.color;
                        if (nearExpiry) GUI.color = Color.yellow;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField(b.Name, GUILayout.Width(260));
                            EditorGUILayout.LabelField($"stacks: {b.Stacks}", GUILayout.Width(90));
                            EditorGUILayout.LabelField(duration);
                        }

                        GUI.color = prev;
                    }
                }
            }
        }
    }
}
#endif
