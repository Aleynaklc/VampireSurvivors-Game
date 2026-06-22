using SnakeRoguelite.Gameplay.Powers;
using SnakeRoguelite.Gameplay.Run;
using UnityEngine;

namespace SnakeRoguelite.UI
{
    public sealed class PrototypeDraftOverlay : MonoBehaviour
    {
        [SerializeField] private PrototypeRunController prototypeRunController;

        private GUIStyle _titleStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _metaStyle;
        private GUIStyle _badgeStyle;

        private void OnGUI()
        {
            if (prototypeRunController == null || !prototypeRunController.IsAwaitingDraftSelection)
            {
                return;
            }

            EnsureStyles();

            var width = Mathf.Min(880f, Screen.width - 80f);
            var panelRect = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height * 0.18f,
                width,
                Screen.height * 0.64f);

            GUI.Box(panelRect, string.Empty);
            GUI.Label(
                new Rect(panelRect.x + 24f, panelRect.y + 18f, panelRect.width - 48f, 40f),
                "Choose a Power",
                _titleStyle);

            var choices = prototypeRunController.CurrentDraftChoices;
            var buttonWidth = (panelRect.width - 64f) / Mathf.Max(1, choices.Count);
            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var buttonRect = new Rect(
                    panelRect.x + 20f + (buttonWidth * i),
                    panelRect.y + 80f,
                    buttonWidth - 12f,
                    panelRect.height - 110f);

                if (GUI.Button(buttonRect, string.Empty, _buttonStyle))
                {
                    prototypeRunController.SelectDraftChoice(i);
                }

                GUI.Label(
                    new Rect(buttonRect.x + 14f, buttonRect.y + 16f, buttonRect.width - 28f, 28f),
                    choice.DisplayName,
                    _titleStyle);

                GUI.Label(
                    new Rect(buttonRect.x + 14f, buttonRect.y + 48f, buttonRect.width - 28f, 24f),
                    FormatMeta(choice),
                    _metaStyle);

                if (choice.IsRunDefining)
                {
                    GUI.Label(
                        new Rect(buttonRect.x + 14f, buttonRect.y + 78f, buttonRect.width - 28f, 22f),
                        "Run Defining",
                        _badgeStyle);
                }

                GUI.Label(
                    new Rect(buttonRect.x + 14f, buttonRect.y + 108f, buttonRect.width - 28f, buttonRect.height - 124f),
                    choice.Description,
                    _bodyStyle);
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
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                wordWrap = true
            };

            _metaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal =
                {
                    textColor = new Color(0.88f, 0.9f, 1f)
                }
            };

            _badgeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                normal =
                {
                    textColor = new Color(1f, 0.82f, 0.32f)
                }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 18,
                wordWrap = true,
                padding = new RectOffset(12, 12, 12, 12)
            };
        }

        private static string FormatMeta(PowerDefinition power)
        {
            var rarity = power.Rarity switch
            {
                PowerRarity.Common => "Common",
                PowerRarity.Rare => "Rare",
                PowerRarity.Legendary => "Legendary",
                _ => "Common"
            };

            var tag = power.PrimaryTag == PowerTag.None
                ? "Neutral"
                : power.PrimaryTag.ToString();

            return $"{rarity}  |  {tag}";
        }
    }
}
