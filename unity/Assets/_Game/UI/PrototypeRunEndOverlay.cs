using SnakeRoguelite.Gameplay.Run;
using SnakeRoguelite.Meta;
using SnakeRoguelite.Gameplay.Powers;
using SnakeRoguelite.Telemetry;
using UnityEngine;

namespace SnakeRoguelite.UI
{
    public sealed class PrototypeRunEndOverlay : MonoBehaviour
    {
        [SerializeField] private PrototypeRunController prototypeRunController;
        [SerializeField] private PrototypeTelemetryController prototypeTelemetryController;
        [SerializeField] private PrototypeMetaProgressionController prototypeMetaProgressionController;

        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _subheaderStyle;
        private GUIStyle _unlockBodyStyle;

        private void Awake()
        {
            if (prototypeRunController == null)
            {
                prototypeRunController = FindObjectOfType<PrototypeRunController>();
            }

            if (prototypeTelemetryController == null)
            {
                prototypeTelemetryController = FindObjectOfType<PrototypeTelemetryController>();
            }

            if (prototypeMetaProgressionController == null)
            {
                prototypeMetaProgressionController = FindObjectOfType<PrototypeMetaProgressionController>();
            }
        }

        private void OnGUI()
        {
            if (prototypeRunController == null)
            {
                return;
            }

            var runSession = prototypeRunController.RunSession;
            if (runSession == null)
            {
                return;
            }

            var phase = runSession.State.Phase;
            if (phase != RunPhase.Summary && phase != RunPhase.Failed)
            {
                return;
            }

            EnsureStyles();

            var isCompactLayout = Screen.width < 680f;
            var panelWidth = Mathf.Min(Screen.width - 32f, 760f);
            var targetHeight = isCompactLayout ? 560f : 420f;
            var panelHeight = Mathf.Min(Screen.height - 32f, targetHeight);
            var panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.Box(panelRect, string.Empty);

            var title = phase == RunPhase.Summary ? "Run Cleared" : "Run Failed";
            GUI.Label(
                new Rect(panelRect.x + 24f, panelRect.y + 24f, panelRect.width - 48f, 32f),
                title,
                _titleStyle);

            var summary =
                $"Level Reached: {runSession.State.CurrentLevel}\n" +
                $"Total XP: {runSession.State.CollectedXp}\n" +
                $"Powers Chosen: {runSession.SelectedPowers.Count}";

            if (prototypeTelemetryController != null &&
                prototypeTelemetryController.LastCompletedRun != null)
            {
                var runRecord = prototypeTelemetryController.LastCompletedRun;
                summary +=
                    $"\nDuration: {runRecord.DurationSeconds:0.0}s" +
                    $"\nWave Reached: {runRecord.WaveReached}" +
                    $"\nKills: {runRecord.Kills}" +
                    $"\nPeak Segments: {runRecord.PeakSegments}";

                if (phase == RunPhase.Failed)
                {
                    summary += $"\nFail Reason: {runRecord.FailReason}";
                }
            }

            if (prototypeMetaProgressionController != null)
            {
                var selectedRelic = prototypeMetaProgressionController.SelectedRelic;
                summary +=
                    $"\nMeta Shards: {prototypeMetaProgressionController.CurrencyBalance}" +
                    $"\nRun Reward: +{prototypeMetaProgressionController.LastRunReward}" +
                    $"\nRelic: {(selectedRelic != null ? selectedRelic.DisplayName : "-")}";
            }

            var summaryRect = isCompactLayout
                ? new Rect(panelRect.x + 24f, panelRect.y + 76f, panelRect.width - 48f, 160f)
                : new Rect(panelRect.x + 24f, panelRect.y + 76f, 280f, 240f);
            GUI.Label(summaryRect, summary, _bodyStyle);

            DrawRelicPanel(panelRect, isCompactLayout);
            DrawUnlockPanel(panelRect, isCompactLayout);

            var playAgainY = isCompactLayout ? panelRect.y + panelRect.height - 54f : panelRect.y + 356f;

            if (GUI.Button(
                new Rect(panelRect.x + (panelRect.width - 200f) * 0.5f, playAgainY, 200f, 42f),
                "Play Again",
                _buttonStyle))
            {
                prototypeRunController.RestartRun();
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };

            _subheaderStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };

            _unlockBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void DrawUnlockPanel(Rect panelRect, bool isCompactLayout)
        {
            if (prototypeMetaProgressionController == null)
            {
                return;
            }

            var candidates = prototypeMetaProgressionController.GetNextUnlockCandidates(3);
            var unlockRect = isCompactLayout
                ? new Rect(panelRect.x + 24f, panelRect.y + 286f, panelRect.width - 48f, 190f)
                : new Rect(panelRect.x + 326f, panelRect.y + 76f, panelRect.width - 350f, 248f);
            GUI.Box(unlockRect, string.Empty);
            GUI.Label(
                new Rect(unlockRect.x + 16f, unlockRect.y + 12f, unlockRect.width - 32f, 22f),
                "Next Unlocks",
                _subheaderStyle);

            if (candidates.Count == 0)
            {
                GUI.Label(
                    new Rect(unlockRect.x + 16f, unlockRect.y + 48f, unlockRect.width - 32f, 60f),
                    "Tum prototype power'lari acildi.",
                    _unlockBodyStyle);
                return;
            }

            var cardWidth = (unlockRect.width - 48f) / Mathf.Max(1, candidates.Count);
            for (var i = 0; i < candidates.Count; i++)
            {
                var power = candidates[i];
                if (power == null)
                {
                    continue;
                }

                var cardRect = new Rect(
                    unlockRect.x + 16f + (cardWidth * i),
                    unlockRect.y + 46f,
                    cardWidth - 8f,
                    isCompactLayout ? 130f : 182f);

                GUI.Box(cardRect, string.Empty);
                GUI.Label(
                    new Rect(cardRect.x + 10f, cardRect.y + 10f, cardRect.width - 20f, 22f),
                    power.DisplayName,
                    _subheaderStyle);

                GUI.Label(
                    new Rect(cardRect.x + 10f, cardRect.y + 36f, cardRect.width - 20f, isCompactLayout ? 50f : 88f),
                    $"{power.Rarity} | {power.PrimaryTag}\n{power.Description}",
                    _unlockBodyStyle);

                var canUnlock = prototypeMetaProgressionController.CurrencyBalance >= power.UnlockCost;
                var buttonLabel = canUnlock ? $"Unlock {power.UnlockCost}" : $"Need {power.UnlockCost}";
                if (GUI.Button(
                    new Rect(cardRect.x + 10f, cardRect.y + (isCompactLayout ? 92f : 136f), cardRect.width - 20f, 32f),
                    buttonLabel))
                {
                    if (canUnlock)
                    {
                        prototypeMetaProgressionController.TryUnlockPower(power);
                    }
                }
            }
        }

        private void DrawRelicPanel(Rect panelRect, bool isCompactLayout)
        {
            if (prototypeMetaProgressionController == null)
            {
                return;
            }

            var relicRect = isCompactLayout
                ? new Rect(panelRect.x + 24f, panelRect.y + 218f, panelRect.width - 48f, 60f)
                : new Rect(panelRect.x + 24f, panelRect.y + 284f, 280f, 72f);
            GUI.Box(relicRect, string.Empty);

            var selectedRelic = prototypeMetaProgressionController.SelectedRelic;
            var selectedName = selectedRelic != null ? selectedRelic.DisplayName : "-";
            GUI.Label(
                new Rect(relicRect.x + 10f, relicRect.y + 8f, relicRect.width - 20f, 20f),
                $"Relic: {selectedName}",
                _subheaderStyle);

            var unlockedRelics = prototypeMetaProgressionController.GetUnlockedRelics();
            var x = relicRect.x + 10f;
            var y = relicRect.y + 34f;
            for (var i = 0; i < unlockedRelics.Count && i < 2; i++)
            {
                var relic = unlockedRelics[i];
                if (relic == null)
                {
                    continue;
                }

                var isSelected = selectedRelic == relic;
                var label = isSelected ? "Equipped" : relic.DisplayName;
                if (GUI.Button(new Rect(x, y, 88f, 24f), label) && !isSelected)
                {
                    prototypeMetaProgressionController.SelectRelic(relic);
                }

                x += 94f;
            }

            var nextRelics = prototypeMetaProgressionController.GetNextRelicUnlockCandidates(1);
            if (nextRelics.Count == 0)
            {
                return;
            }

            var nextRelic = nextRelics[0];
            if (nextRelic == null)
            {
                return;
            }

            var canUnlock = prototypeMetaProgressionController.CurrencyBalance >= nextRelic.UnlockCost;
            var unlockLabel = canUnlock
                ? $"Unlock {nextRelic.UnlockCost}"
                : $"Need {nextRelic.UnlockCost}";
            if (GUI.Button(new Rect(relicRect.x + relicRect.width - 104f, y, 94f, 24f), unlockLabel) && canUnlock)
            {
                prototypeMetaProgressionController.TryUnlockRelic(nextRelic);
            }
        }
    }
}
