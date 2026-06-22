using SnakeRoguelite.Gameplay.Enemies;
using SnakeRoguelite.Gameplay.Run;
using UnityEngine;

namespace SnakeRoguelite.UI
{
    public sealed class PrototypeBossOverlay : MonoBehaviour
    {
        [SerializeField] private PrototypeRunController prototypeRunController;

        private GUIStyle _titleStyle;
        private GUIStyle _valueStyle;
        private Texture2D _whiteTexture;

        private void OnGUI()
        {
            if (prototypeRunController == null)
            {
                return;
            }

            var runSession = prototypeRunController.RunSession;
            var boss = prototypeRunController.ActiveBoss;
            if (runSession == null ||
                runSession.State.Phase != RunPhase.Boss ||
                boss == null ||
                !boss.IsAlive)
            {
                return;
            }

            EnsureStyles();

            var panelWidth = Mathf.Min(Screen.width - 120f, 560f);
            var panelRect = new Rect((Screen.width - panelWidth) * 0.5f, 24f, panelWidth, 72f);
            GUI.Box(panelRect, string.Empty);

            GUI.Label(
                new Rect(panelRect.x + 18f, panelRect.y + 10f, panelRect.width - 36f, 22f),
                "Boss Encounter",
                _titleStyle);

            var barRect = new Rect(panelRect.x + 18f, panelRect.y + 38f, panelRect.width - 36f, 18f);
            DrawBar(barRect, boss.HealthNormalized, new Color(0.95f, 0.25f, 0.25f), new Color(0.16f, 0.16f, 0.18f));

            GUI.Label(
                new Rect(panelRect.x + 18f, panelRect.y + 54f, panelRect.width - 36f, 18f),
                $"{Mathf.CeilToInt(boss.CurrentHealth)} / {Mathf.CeilToInt(boss.MaxHealth)}",
                _valueStyle);
        }

        private void DrawBar(Rect rect, float normalized, Color fillColor, Color backgroundColor)
        {
            GUI.color = backgroundColor;
            GUI.DrawTexture(rect, _whiteTexture);

            var fillRect = rect;
            fillRect.width *= Mathf.Clamp01(normalized);
            GUI.color = fillColor;
            GUI.DrawTexture(fillRect, _whiteTexture);

            GUI.color = Color.white;
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            _whiteTexture = Texture2D.whiteTexture;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleRight
            };
        }
    }
}
