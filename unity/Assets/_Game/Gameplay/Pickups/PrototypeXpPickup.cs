using System;
using SnakeRoguelite.Gameplay.Snake;
using UnityEngine;

namespace SnakeRoguelite.Gameplay.Pickups
{
    public sealed class PrototypeXpPickup : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float collectRadius = 0.65f;
        [SerializeField, Min(0.5f)] private float baseAttractRadius = 1.2f;
        [SerializeField, Min(0.5f)] private float attractionSpeed = 9f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.12f;
        [SerializeField, Min(0f)] private float bobFrequency = 4f;
        [SerializeField] private Color pickupColor = new(0.48f, 0.9f, 1f);
        [SerializeField, Min(0.05f)] private float visualScale = 0.28f;

        private SnakeController _targetSnake;
        private Vector3 _anchorPosition;
        private bool _isPaused;
        private bool _isCollected;
        private bool _isMagnetized;
        private Renderer _renderer;

        public event Action<PrototypeXpPickup> Collected;

        public int ExperienceAmount { get; private set; }

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer == null)
            {
                var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "XpPickupVisual";
                visual.transform.SetParent(transform, false);
                visual.transform.localScale = Vector3.one * visualScale;

                var collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                _renderer = visual.GetComponent<Renderer>();
            }

            if (_renderer != null)
            {
                _renderer.material.color = pickupColor;
            }

            _anchorPosition = transform.position;
        }

        private void Update()
        {
            if (_isPaused || _isCollected || _targetSnake == null || !_targetSnake.IsAlive)
            {
                return;
            }

            var targetPosition = _targetSnake.HeadPosition;
            targetPosition.y = transform.position.y;

            var delta = targetPosition - transform.position;
            delta.y = 0f;
            var distance = delta.magnitude;
            var magnetRadius = Mathf.Max(baseAttractRadius, _targetSnake.PickupMagnetRadius);
            if (distance <= magnetRadius)
            {
                _isMagnetized = true;
            }

            if (_isMagnetized)
            {
                var direction = distance <= 0.0001f ? Vector3.zero : (delta / distance);
                transform.position += direction * (attractionSpeed * Time.deltaTime);
            }
            else
            {
                var bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
                var position = _anchorPosition;
                position.y += bobOffset;
                transform.position = position;
            }

            var collectDistance = FlatDistance(transform.position, _targetSnake.HeadPosition);
            if (collectDistance > collectRadius)
            {
                return;
            }

            _isCollected = true;
            Collected?.Invoke(this);
            Destroy(gameObject);
        }

        public void Initialize(SnakeController snakeController, int experienceAmount)
        {
            _targetSnake = snakeController;
            ExperienceAmount = Mathf.Max(0, experienceAmount);
            _anchorPosition = transform.position;
        }

        public void SetPaused(bool isPaused)
        {
            if (_isPaused == isPaused)
            {
                return;
            }

            _isPaused = isPaused;
            if (!isPaused)
            {
                _anchorPosition = transform.position;
            }
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            var delta = a - b;
            delta.y = 0f;
            return delta.magnitude;
        }
    }
}
