using SnakeRoguelite.Gameplay.Run;
using SnakeRoguelite.Meta;
using SnakeRoguelite.Gameplay.Snake;
using UnityEngine;

namespace SnakeRoguelite.UI
{
    public sealed class PrototypeHudOverlay : MonoBehaviour
    {
        [SerializeField] private PrototypeRunController prototypeRunController;
        [SerializeField] private SnakeController snakeController;
        [SerializeField] private PrototypeMetaProgressionController prototypeMetaProgressionController;

        private GUIStyle _hudStyle;

        private void OnGUI()
        {
            if (prototypeRunController == null || snakeController == null)
            {
                return;
            }

            EnsureStyle();

            var runSession = prototypeRunController.RunSession;
            if (runSession == null)
            {
                return;
            }

            var state = runSession.State;
            var waveIndex = state.CurrentWaveIndex + 1;
            var waveTimerText = prototypeRunController.CurrentWaveDurationSeconds > 0f
                ? $"{prototypeRunController.CurrentWaveElapsedSeconds:0.0}/{prototypeRunController.CurrentWaveDurationSeconds:0.0}s"
                : "-";
            var hudText =
                $"Phase: {state.Phase}\n" +
                $"Wave: {waveIndex}\n" +
                $"Health: {snakeController.CurrentHealth}/{snakeController.MaxHealth}\n" +
                $"Segments: {snakeController.SegmentCount}\n" +
                $"Level: {state.CurrentLevel}\n" +
                $"XP: {state.CurrentLevelXp}/{state.NextLevelXpRequirement}\n" +
                $"Wave Time: {waveTimerText}\n" +
                $"Live/Queued: {prototypeRunController.ActiveEnemyCount}/{prototypeRunController.PendingEnemyCount}\n" +
                $"Pickups: {prototypeRunController.ActivePickupCount}\n" +
                $"Meta Shards: {GetMetaShardText()}\n" +
                $"Powers: {runSession.SelectedPowers.Count}\n" +
                $"Last: {GetLastPowerName(runSession)}";

            GUI.Box(new Rect(16f, 16f, 280f, 240f), string.Empty);
            GUI.Label(new Rect(28f, 26f, 252f, 216f), hudText, _hudStyle);
        }

        private void EnsureStyle()
        {
            if (_hudStyle != null)
            {
                return;
            }

            _hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true
            };
        }

        private static string GetLastPowerName(RunSession runSession)
        {
            if (runSession.SelectedPowers.Count == 0)
            {
                return "-";
            }

            return runSession.SelectedPowers[runSession.SelectedPowers.Count - 1].DisplayName;
        }

        private string GetMetaShardText()
        {
            if (prototypeMetaProgressionController == null)
            {
                return "-";
            }

            return prototypeMetaProgressionController.CurrencyBalance.ToString();
        }
    }
}
