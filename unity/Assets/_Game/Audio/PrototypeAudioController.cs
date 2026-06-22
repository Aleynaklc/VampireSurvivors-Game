using System.Collections.Generic;
using SnakeRoguelite.Core;
using SnakeRoguelite.Gameplay.Powers;
using SnakeRoguelite.Gameplay.Run;
using SnakeRoguelite.Gameplay.Snake;
using UnityEngine;

namespace SnakeRoguelite.Audio
{
    public sealed class PrototypeAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameBootstrap gameBootstrap;
        [SerializeField] private PrototypeRunController prototypeRunController;
        [SerializeField] private SnakeController snakeController;
        [SerializeField] private AudioSource sfxSource;

        [Header("Core Clips")]
        [SerializeField] private AudioClip[] enemyHitClips;
        [SerializeField] private AudioClip[] enemyKillClips;
        [SerializeField] private AudioClip[] snakeHitClips;
        [SerializeField] private AudioClip[] growthClips;
        [SerializeField] private AudioClip[] pickupClips;
        [SerializeField] private AudioClip waveStartClip;
        [SerializeField] private AudioClip draftOpenClip;
        [SerializeField] private AudioClip powerPickClip;
        [SerializeField] private AudioClip levelUpClip;
        [SerializeField] private AudioClip bossStartClip;
        [SerializeField] private AudioClip runClearClip;
        [SerializeField] private AudioClip runFailClip;

        [Header("Mix")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 0.9f;
        [SerializeField, Range(0f, 1f)] private float enemyHitVolume = 0.42f;
        [SerializeField, Range(0f, 1f)] private float enemyKillVolume = 0.72f;
        [SerializeField, Range(0f, 1f)] private float snakeHitVolume = 0.82f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.78f;
        [SerializeField, Range(0f, 1f)] private float bigMomentVolume = 0.92f;
        [SerializeField, Min(0f)] private float enemyHitCooldownSeconds = 0.04f;
        [SerializeField, Min(0f)] private float snakeHitCooldownSeconds = 0.08f;
        [SerializeField, Min(0f)] private float growthCooldownSeconds = 0.05f;

        private bool _subscribed;
        private bool _runStateSubscribed;
        private float _nextEnemyHitTime;
        private float _nextSnakeHitTime;
        private float _nextGrowthTime;

        private void Awake()
        {
            if (sfxSource == null)
            {
                sfxSource = GetComponent<AudioSource>();
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;
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
        }

        private void Subscribe()
        {
            if (!_subscribed)
            {
                if (prototypeRunController != null)
                {
                    prototypeRunController.EnemyDamageFeedback += OnEnemyDamageFeedback;
                    prototypeRunController.BossDamageFeedback += OnBossDamageFeedback;
                    prototypeRunController.WaveStarted += OnWaveStarted;
                    prototypeRunController.DraftOpened += OnDraftOpened;
                    prototypeRunController.PowerSelected += OnPowerSelected;
                    prototypeRunController.BossStarted += OnBossStarted;
                    prototypeRunController.RunEnded += OnRunEnded;
                    prototypeRunController.XpPickupCollected += OnXpPickupCollected;
                }

                if (snakeController != null)
                {
                    snakeController.DamageTaken += OnSnakeDamageTaken;
                    snakeController.Grew += OnSnakeGrew;
                }

                _subscribed = true;
            }

            if (!_runStateSubscribed && gameBootstrap != null && gameBootstrap.RunSession != null)
            {
                gameBootstrap.RunSession.State.LevelChanged += OnLevelChanged;
                _runStateSubscribed = true;
            }
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
                prototypeRunController.WaveStarted -= OnWaveStarted;
                prototypeRunController.DraftOpened -= OnDraftOpened;
                prototypeRunController.PowerSelected -= OnPowerSelected;
                prototypeRunController.BossStarted -= OnBossStarted;
                prototypeRunController.RunEnded -= OnRunEnded;
                prototypeRunController.XpPickupCollected -= OnXpPickupCollected;
            }

            if (snakeController != null)
            {
                snakeController.DamageTaken -= OnSnakeDamageTaken;
                snakeController.Grew -= OnSnakeGrew;
            }

            if (_runStateSubscribed && gameBootstrap != null && gameBootstrap.RunSession != null)
            {
                gameBootstrap.RunSession.State.LevelChanged -= OnLevelChanged;
            }

            _subscribed = false;
            _runStateSubscribed = false;
        }

        private void OnEnemyDamageFeedback(Vector3 worldPosition, float damage, bool wasKilled)
        {
            if (wasKilled)
            {
                PlayRandomClip(enemyKillClips, enemyKillVolume, 0.96f, 1.08f);
                return;
            }

            if (Time.unscaledTime < _nextEnemyHitTime)
            {
                return;
            }

            _nextEnemyHitTime = Time.unscaledTime + enemyHitCooldownSeconds;
            PlayRandomClip(enemyHitClips, enemyHitVolume, 0.94f, 1.08f);
        }

        private void OnBossDamageFeedback(Vector3 worldPosition, float damage, bool wasKilled)
        {
            if (wasKilled)
            {
                PlayRandomClip(enemyKillClips, bigMomentVolume, 0.82f, 0.92f);
                return;
            }

            PlayRandomClip(enemyHitClips, enemyKillVolume, 0.78f, 0.9f);
        }

        private void OnSnakeDamageTaken(int damageAmount, int currentHealth)
        {
            if (Time.unscaledTime < _nextSnakeHitTime)
            {
                return;
            }

            _nextSnakeHitTime = Time.unscaledTime + snakeHitCooldownSeconds;
            PlayRandomClip(snakeHitClips, snakeHitVolume, 0.92f, 1.04f);
        }

        private void OnSnakeGrew(int amount)
        {
            if (amount <= 0 || Time.unscaledTime < _nextGrowthTime)
            {
                return;
            }

            _nextGrowthTime = Time.unscaledTime + growthCooldownSeconds;
            PlayRandomClip(growthClips, enemyKillVolume, 1f, 1.14f);
        }

        private void OnWaveStarted(int waveIndex)
        {
            PlayClip(waveStartClip, uiVolume, 0.98f + (waveIndex * 0.02f));
        }

        private void OnDraftOpened(IReadOnlyList<PowerDefinition> choices)
        {
            PlayClip(draftOpenClip, uiVolume, 1f);
        }

        private void OnPowerSelected(PowerDefinition power)
        {
            var pitch = 1f;
            if (power != null && power.Rarity == PowerRarity.Legendary)
            {
                pitch = 0.92f;
            }

            PlayClip(powerPickClip, uiVolume, pitch);
        }

        private void OnBossStarted()
        {
            PlayClip(bossStartClip, bigMomentVolume, 1f);
        }

        private void OnXpPickupCollected(Vector3 worldPosition, int amount)
        {
            var clips = pickupClips != null && pickupClips.Length > 0
                ? pickupClips
                : growthClips;
            PlayRandomClip(clips, enemyHitVolume, 1.02f, 1.16f);
        }

        private void OnRunEnded(bool cleared)
        {
            if (cleared)
            {
                PlayClip(runClearClip, bigMomentVolume, 1f);
                return;
            }

            PlayClip(runFailClip, bigMomentVolume, 0.96f);
        }

        private void OnLevelChanged(int level)
        {
            if (level <= 1)
            {
                return;
            }

            PlayClip(levelUpClip, uiVolume, Mathf.Clamp(1f + (level * 0.01f), 1f, 1.16f));
        }

        private void PlayRandomClip(AudioClip[] clips, float volume, float pitchMin, float pitchMax)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            var clip = clips[Random.Range(0, clips.Length)];
            var pitch = Random.Range(pitchMin, pitchMax);
            PlayClip(clip, volume, pitch);
        }

        private void PlayClip(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume * masterVolume));
        }
    }
}
