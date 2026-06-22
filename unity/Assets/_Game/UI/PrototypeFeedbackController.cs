using System.Collections.Generic;
using SnakeRoguelite.Gameplay.Run;
using SnakeRoguelite.Gameplay.Snake;
using UnityEngine;

namespace SnakeRoguelite.UI
{
    public sealed class PrototypeFeedbackController : MonoBehaviour
    {
        private sealed class DamagePopup
        {
            public Vector3 WorldPosition;
            public string Text;
            public Color Color;
            public float Age;
            public float Lifetime;
        }

        [Header("References")]
        [SerializeField] private PrototypeRunController prototypeRunController;
        [SerializeField] private SnakeController snakeController;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private Transform shakeTarget;

        [Header("Shake")]
        [SerializeField, Min(0.01f)] private float minorShakeAmplitude = 0.08f;
        [SerializeField, Min(0.01f)] private float majorShakeAmplitude = 0.2f;
        [SerializeField, Min(0.5f)] private float shakeDamping = 10f;

        [Header("Damage Popups")]
        [SerializeField, Min(0.2f)] private float popupLifetimeSeconds = 0.65f;
        [SerializeField] private Color enemyDamageColor = new(1f, 0.95f, 0.75f);
        [SerializeField] private Color enemyKillColor = new(1f, 0.72f, 0.28f);
        [SerializeField] private Color snakeDamageColor = new(1f, 0.35f, 0.35f);
        [SerializeField] private Color xpPickupColor = new(0.48f, 0.9f, 1f);

        private readonly List<DamagePopup> _popups = new();
        private GUIStyle _popupStyle;
        private Vector3 _baseShakeLocalPosition;
        private float _shakeIntensity;
        private bool _subscribed;

        private void Awake()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (shakeTarget == null && gameplayCamera != null)
            {
                shakeTarget = gameplayCamera.transform;
            }

            if (shakeTarget != null)
            {
                _baseShakeLocalPosition = shakeTarget.localPosition;
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ResetShake();
        }

        private void Update()
        {
            TickPopups(Time.deltaTime);
            TickShake(Time.deltaTime);
        }

        private void LateUpdate()
        {
            ApplyShakeOffset();
        }

        private void OnGUI()
        {
            if (gameplayCamera == null || _popups.Count == 0)
            {
                return;
            }

            EnsurePopupStyle();

            for (var i = 0; i < _popups.Count; i++)
            {
                var popup = _popups[i];
                var progress = Mathf.Clamp01(popup.Age / popup.Lifetime);
                var worldOffset = popup.WorldPosition + (Vector3.up * progress * 1.4f);
                var screenPosition = gameplayCamera.WorldToScreenPoint(worldOffset);
                if (screenPosition.z <= 0f)
                {
                    continue;
                }

                var alpha = 1f - progress;
                var color = popup.Color;
                color.a = alpha;
                _popupStyle.normal.textColor = color;

                var rect = new Rect(
                    screenPosition.x - 40f,
                    Screen.height - screenPosition.y - 20f,
                    80f,
                    24f);

                GUI.Label(rect, popup.Text, _popupStyle);
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            if (prototypeRunController != null)
            {
                prototypeRunController.EnemyDamageFeedback += OnEnemyDamageFeedback;
                prototypeRunController.BossDamageFeedback += OnBossDamageFeedback;
                prototypeRunController.XpPickupCollected += OnXpPickupCollected;
            }

            if (snakeController != null)
            {
                snakeController.DamageTaken += OnSnakeDamageTaken;
            }

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (prototypeRunController != null)
            {
                prototypeRunController.EnemyDamageFeedback -= OnEnemyDamageFeedback;
                prototypeRunController.BossDamageFeedback -= OnBossDamageFeedback;
                prototypeRunController.XpPickupCollected -= OnXpPickupCollected;
            }

            if (snakeController != null)
            {
                snakeController.DamageTaken -= OnSnakeDamageTaken;
            }

            _subscribed = false;
        }

        private void OnEnemyDamageFeedback(Vector3 worldPosition, float damage, bool wasKilled)
        {
            AddPopup(worldPosition, Mathf.RoundToInt(damage).ToString(), wasKilled ? enemyKillColor : enemyDamageColor);
            AddShake(wasKilled ? majorShakeAmplitude : minorShakeAmplitude);
        }

        private void OnBossDamageFeedback(Vector3 worldPosition, float damage, bool wasKilled)
        {
            AddPopup(worldPosition, Mathf.RoundToInt(damage).ToString(), wasKilled ? enemyKillColor : enemyDamageColor);
            AddShake(wasKilled ? majorShakeAmplitude * 1.25f : minorShakeAmplitude * 1.4f);
        }

        private void OnSnakeDamageTaken(int damageAmount, int currentHealth)
        {
            if (snakeController == null)
            {
                return;
            }

            AddPopup(snakeController.HeadPosition, $"-{damageAmount}", snakeDamageColor);
            AddShake(majorShakeAmplitude);
        }

        private void OnXpPickupCollected(Vector3 worldPosition, int amount)
        {
            AddPopup(worldPosition, $"+{amount} XP", xpPickupColor);
        }

        private void AddPopup(Vector3 worldPosition, string text, Color color)
        {
            _popups.Add(new DamagePopup
            {
                WorldPosition = worldPosition,
                Text = text,
                Color = color,
                Age = 0f,
                Lifetime = popupLifetimeSeconds
            });
        }

        private void AddShake(float amplitude)
        {
            _shakeIntensity = Mathf.Max(_shakeIntensity, amplitude);
        }

        private void TickPopups(float deltaTime)
        {
            for (var i = _popups.Count - 1; i >= 0; i--)
            {
                var popup = _popups[i];
                popup.Age += deltaTime;
                if (popup.Age >= popup.Lifetime)
                {
                    _popups.RemoveAt(i);
                }
            }
        }

        private void TickShake(float deltaTime)
        {
            if (_shakeIntensity <= 0f)
            {
                _shakeIntensity = 0f;
                return;
            }

            _shakeIntensity = Mathf.Lerp(_shakeIntensity, 0f, shakeDamping * deltaTime);
            if (_shakeIntensity < 0.005f)
            {
                _shakeIntensity = 0f;
            }
        }

        private void ApplyShakeOffset()
        {
            if (shakeTarget == null)
            {
                return;
            }

            if (_shakeIntensity <= 0f)
            {
                shakeTarget.localPosition = _baseShakeLocalPosition;
                return;
            }

            var randomOffset = Random.insideUnitSphere * _shakeIntensity;
            randomOffset.z = 0f;
            shakeTarget.localPosition = _baseShakeLocalPosition + randomOffset;
        }

        private void ResetShake()
        {
            if (shakeTarget != null)
            {
                shakeTarget.localPosition = _baseShakeLocalPosition;
            }
        }

        private void EnsurePopupStyle()
        {
            if (_popupStyle != null)
            {
                return;
            }

            _popupStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
    }
}
