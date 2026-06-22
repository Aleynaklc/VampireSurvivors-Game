using SnakeRoguelite.Gameplay.Powers;
using SnakeRoguelite.Gameplay.Run;
using SnakeRoguelite.Gameplay.Waves;
using UnityEngine;

namespace SnakeRoguelite.Core
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private RunConfig runConfig;
        [SerializeField] private PowerLibrary powerLibrary;
        [SerializeField] private WaveCatalog waveCatalog;

        private RunSession _runSession;

        public RunSession RunSession => _runSession;

        private void Awake()
        {
            _runSession = new RunSession(runConfig, powerLibrary, waveCatalog);
        }

        public void StartRun()
        {
            _runSession.StartNewRun();
        }
    }
}
