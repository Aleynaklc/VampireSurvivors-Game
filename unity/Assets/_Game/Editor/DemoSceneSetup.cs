using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SnakeRoguelite.Core;
using SnakeRoguelite.Audio;
using SnakeRoguelite.Gameplay.Run;
using SnakeRoguelite.Gameplay.Powers;
using SnakeRoguelite.Gameplay.Snake;
using SnakeRoguelite.Gameplay.Enemies;
using SnakeRoguelite.Gameplay.Waves;
using SnakeRoguelite.Meta;
using SnakeRoguelite.Telemetry;
using SnakeRoguelite.UI;

namespace SnakeRoguelite.Editor
{
    public class DemoSceneSetup
    {
        [MenuItem("Snake Roguelite/Setup Prototype Demo Scene")]
        public static void SetupScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // Create a new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // 1. Data Assets Creation
            string dataPath = "Assets/_Game/Data";
            string prefabsPath = "Assets/_Game/Prefabs";
            
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Data"))
                AssetDatabase.CreateFolder("Assets/_Game", "Data");
                
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Game", "Prefabs");

            // Create RunConfig
            RunConfig runConfig = GetOrCreateAsset<RunConfig>(dataPath, "RunConfig");
            
            // Create Powers
            PowerDefinition tailSpike = CreatePower(dataPath, "Tail Spike", PowerEffectType.TailSpike);
            PowerDefinition bodyShield = CreatePower(dataPath, "Body Shield", PowerEffectType.BodyShield);
            PowerDefinition frenzyCoil = CreatePower(dataPath, "Frenzy Coil", PowerEffectType.FrenzyCoil);
            PowerDefinition hungerScale = CreatePower(dataPath, "Hunger Scale", PowerEffectType.HungerScale);
            PowerDefinition sparkMolt = CreatePower(dataPath, "Spark Molt", PowerEffectType.SparkMolt);
            PowerDefinition magnetTail = CreatePower(dataPath, "Magnet Tail", PowerEffectType.MagnetTail);
            PowerDefinition overload = CreatePower(dataPath, "Overload", PowerEffectType.Overload);
            
            // Create PowerLibrary
            PowerLibrary powerLibrary = GetOrCreateAsset<PowerLibrary>(dataPath, "PowerLibrary");
            SerializedObject soLib = new SerializedObject(powerLibrary);
            SetListProperty(soLib, "Powers", new[] { tailSpike, bodyShield, frenzyCoil, hungerScale, sparkMolt, magnetTail, overload });
            soLib.ApplyModifiedProperties();

            PrototypeRelicDefinition coiledStart = CreateRelic(
                dataPath,
                "Coiled Start",
                RelicEffectType.ExtraStartingSegments,
                true,
                0,
                3f,
                0f,
                0f);
            PrototypeRelicDefinition shardCharm = CreateRelic(
                dataPath,
                "Shard Charm",
                RelicEffectType.MetaShardBonus,
                false,
                10,
                4f,
                0f,
                0f);
            PrototypeRelicDefinition pullScale = CreateRelic(
                dataPath,
                "Pull Scale",
                RelicEffectType.StartingMagnet,
                false,
                14,
                3.2f,
                0f,
                0f);
            PrototypeRelicDefinition ironEgg = CreateRelic(
                dataPath,
                "Iron Egg",
                RelicEffectType.OpeningBodyShield,
                false,
                20,
                0.05f,
                3f,
                0.25f);

            PrototypeRelicLibrary relicLibrary = GetOrCreateAsset<PrototypeRelicLibrary>(dataPath, "PrototypeRelicLibrary");
            SerializedObject soRelicLib = new SerializedObject(relicLibrary);
            SetListProperty(soRelicLib, "Relics", new[] { coiledStart, shardCharm, pullScale, ironEgg });
            soRelicLib.ApplyModifiedProperties();

            // Create WaveDefinition
            WaveDefinition wave1 = GetOrCreateAsset<WaveDefinition>(dataPath, "Wave1");
            SerializedObject soWave = new SerializedObject(wave1);
            SetProperty(soWave, "Index", 1);
            SetProperty(soWave, "DurationSeconds", 90f);
            SetProperty(soWave, "ChaserCount", 6);
            SetProperty(soWave, "DasherCount", 1);
            SetProperty(soWave, "TankCount", 1);
            soWave.ApplyModifiedProperties();
            
            // Create WaveCatalog
            WaveCatalog waveCatalog = GetOrCreateAsset<WaveCatalog>(dataPath, "WaveCatalog");
            SerializedObject soCat = new SerializedObject(waveCatalog);
            SetListProperty(soCat, "Waves", new[] { wave1 });
            soCat.ApplyModifiedProperties();

            // 2. Prefabs Creation
            ChaserEnemy chaserPrefab = CreateEnemyPrefab<ChaserEnemy>(prefabsPath, "ChaserEnemy", PrimitiveType.Capsule);
            DasherEnemy dasherPrefab = CreateEnemyPrefab<DasherEnemy>(prefabsPath, "DasherEnemy", PrimitiveType.Cube);
            TankEnemy tankPrefab = CreateEnemyPrefab<TankEnemy>(prefabsPath, "TankEnemy", PrimitiveType.Cylinder);
            PrototypeBossEnemy bossPrefab = CreateEnemyPrefab<PrototypeBossEnemy>(prefabsPath, "PrototypeBossEnemy", PrimitiveType.Sphere);

            // 3. Scene Objects
            GameObject mainCameraObj = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera));
            mainCameraObj.name = "Main Camera";
            mainCameraObj.tag = "MainCamera";

            GameObject gameRoot = new GameObject("GameRoot");
            GameObject snakeObj = new GameObject("Snake");
            GameObject enemyRoot = new GameObject("EnemyRoot");
            GameObject uiRoot = new GameObject("UIRoot");

            // Attach Components
            GameBootstrap bootstrap = gameRoot.AddComponent<GameBootstrap>();
            PrototypeRunController runController = gameRoot.AddComponent<PrototypeRunController>();
            PrototypePowerController powerController = gameRoot.AddComponent<PrototypePowerController>();
            PrototypeMetaProgressionController metaController = gameRoot.AddComponent<PrototypeMetaProgressionController>();
            
            SnakeController snakeController = snakeObj.AddComponent<SnakeController>();

            PrototypeDraftOverlay draftOverlay = uiRoot.AddComponent<PrototypeDraftOverlay>();
            PrototypeHudOverlay hudOverlay = uiRoot.AddComponent<PrototypeHudOverlay>();
            PrototypeBossOverlay bossOverlay = uiRoot.AddComponent<PrototypeBossOverlay>();
            PrototypeRunEndOverlay runEndOverlay = uiRoot.AddComponent<PrototypeRunEndOverlay>();
            PrototypeFeedbackController feedbackController = uiRoot.AddComponent<PrototypeFeedbackController>();
            PrototypeAudioController audioController = uiRoot.AddComponent<PrototypeAudioController>();
            PrototypeTelemetryController telemetryController = uiRoot.AddComponent<PrototypeTelemetryController>();

            // 4. Component Assignments via SerializedObject
            // GameBootstrap
            SerializedObject soBoot = new SerializedObject(bootstrap);
            SetProperty(soBoot, "runConfig", runConfig);
            SetProperty(soBoot, "powerLibrary", powerLibrary);
            SetProperty(soBoot, "waveCatalog", waveCatalog);
            soBoot.ApplyModifiedProperties();

            // PrototypeRunController
            SerializedObject soRun = new SerializedObject(runController);
            SetProperty(soRun, "gameBootstrap", bootstrap);
            SetProperty(soRun, "snakeController", snakeController);
            SetProperty(soRun, "powerController", powerController);
            SetProperty(soRun, "metaProgressionController", metaController);
            SetProperty(soRun, "chaserEnemyPrefab", chaserPrefab);
            SetProperty(soRun, "dasherEnemyPrefab", dasherPrefab);
            SetProperty(soRun, "tankEnemyPrefab", tankPrefab);
            SetProperty(soRun, "bossEnemyPrefab", bossPrefab);
            SetProperty(soRun, "enemyRoot", enemyRoot.transform);
            SetProperty(soRun, "autoAdvanceWaves", true);
            SetProperty(soRun, "autoResolveDrafts", false);
            soRun.ApplyModifiedProperties();

            SerializedObject soMeta = new SerializedObject(metaController);
            SetProperty(soMeta, "gameBootstrap", bootstrap);
            SetProperty(soMeta, "prototypeRunController", runController);
            SetProperty(soMeta, "powerLibrary", powerLibrary);
            SetProperty(soMeta, "relicLibrary", relicLibrary);
            soMeta.ApplyModifiedProperties();

            // PrototypePowerController
            SerializedObject soPower = new SerializedObject(powerController);
            SetProperty(soPower, "gameBootstrap", bootstrap);
            SetProperty(soPower, "snakeController", snakeController);
            SetProperty(soPower, "runController", runController);
            soPower.ApplyModifiedProperties();

            // SnakeController
            SerializedObject soSnake = new SerializedObject(snakeController);
            SetProperty(soSnake, "gameplayCamera", mainCameraObj.GetComponent<Camera>());
            soSnake.ApplyModifiedProperties();

            // UI Overlays
            SerializedObject soDraft = new SerializedObject(draftOverlay);
            SetProperty(soDraft, "prototypeRunController", runController);
            soDraft.ApplyModifiedProperties();

            SerializedObject soHud = new SerializedObject(hudOverlay);
            SetProperty(soHud, "prototypeRunController", runController);
            SetProperty(soHud, "snakeController", snakeController);
            SetProperty(soHud, "prototypeMetaProgressionController", metaController);
            soHud.ApplyModifiedProperties();

            SerializedObject soRunEnd = new SerializedObject(runEndOverlay);
            SetProperty(soRunEnd, "prototypeRunController", runController);
            SetProperty(soRunEnd, "prototypeTelemetryController", telemetryController);
            SetProperty(soRunEnd, "prototypeMetaProgressionController", metaController);
            soRunEnd.ApplyModifiedProperties();

            SerializedObject soBoss = new SerializedObject(bossOverlay);
            SetProperty(soBoss, "prototypeRunController", runController);
            soBoss.ApplyModifiedProperties();

            SerializedObject soFeed = new SerializedObject(feedbackController);
            SetProperty(soFeed, "prototypeRunController", runController);
            SetProperty(soFeed, "snakeController", snakeController);
            SetProperty(soFeed, "gameplayCamera", mainCameraObj.GetComponent<Camera>());
            soFeed.ApplyModifiedProperties();

            SerializedObject soAudio = new SerializedObject(audioController);
            SetProperty(soAudio, "gameBootstrap", bootstrap);
            SetProperty(soAudio, "prototypeRunController", runController);
            SetProperty(soAudio, "snakeController", snakeController);
            soAudio.ApplyModifiedProperties();

            SerializedObject soTelemetry = new SerializedObject(telemetryController);
            SetProperty(soTelemetry, "gameBootstrap", bootstrap);
            SetProperty(soTelemetry, "prototypeRunController", runController);
            SetProperty(soTelemetry, "snakeController", snakeController);
            SetProperty(soTelemetry, "prototypeMetaProgressionController", metaController);
            soTelemetry.ApplyModifiedProperties();
            
            AssetDatabase.SaveAssets();

            Debug.Log("Prototype Scene successfully set up! You can now save it and press Play.");
        }

        private static T GetOrCreateAsset<T>(string path, string name) where T : ScriptableObject
        {
            string fullPath = $"{path}/{name}.asset";
            T asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, fullPath);
            }
            return asset;
        }

        private static PowerDefinition CreatePower(string path, string name, PowerEffectType effectType)
        {
            PowerDefinition power = GetOrCreateAsset<PowerDefinition>(path, name);
            SerializedObject so = new SerializedObject(power);
            SetProperty(so, "PowerId", name.Replace(" ", ""));
            SetProperty(so, "DisplayName", name);
            SetProperty(so, "EffectType", (int)effectType);
            ConfigurePowerMeta(so, name);
            so.ApplyModifiedProperties();
            return power;
        }

        private static PrototypeRelicDefinition CreateRelic(
            string path,
            string name,
            RelicEffectType effectType,
            bool unlockedByDefault,
            int unlockCost,
            float primaryValue,
            float secondaryValue,
            float tertiaryValue)
        {
            PrototypeRelicDefinition relic = GetOrCreateAsset<PrototypeRelicDefinition>(path, name);
            SerializedObject so = new SerializedObject(relic);
            SetProperty(so, "RelicId", name.Replace(" ", ""));
            SetProperty(so, "DisplayName", name);
            SetProperty(so, "Description", GetRelicDescription(effectType));
            SetProperty(so, "IsUnlockedByDefault", unlockedByDefault);
            SetProperty(so, "UnlockCost", unlockCost);
            SetProperty(so, "EffectType", (int)effectType);
            SetProperty(so, "PrimaryValue", primaryValue);
            SetProperty(so, "SecondaryValue", secondaryValue);
            SetProperty(so, "TertiaryValue", tertiaryValue);
            so.ApplyModifiedProperties();
            return relic;
        }

        private static string GetRelicDescription(RelicEffectType effectType)
        {
            return effectType switch
            {
                RelicEffectType.ExtraStartingSegments => "Start each run with extra body length.",
                RelicEffectType.StartingMagnet => "Pull XP pickups from farther away.",
                RelicEffectType.OpeningBodyShield => "Gain length-based damage reduction from the start.",
                RelicEffectType.MetaShardBonus => "Earn extra Meta Shards after every run.",
                _ => "Prototype relic effect."
            };
        }

        private static void ConfigurePowerMeta(SerializedObject so, string name)
        {
            bool unlockedByDefault;
            int unlockCost;

            switch (name)
            {
                case "Tail Spike":
                case "Body Shield":
                case "Hunger Scale":
                case "Spark Molt":
                    unlockedByDefault = true;
                    unlockCost = 0;
                    break;

                case "Magnet Tail":
                    unlockedByDefault = false;
                    unlockCost = 12;
                    break;

                case "Frenzy Coil":
                    unlockedByDefault = false;
                    unlockCost = 18;
                    break;

                case "Overload":
                    unlockedByDefault = false;
                    unlockCost = 24;
                    break;

                default:
                    unlockedByDefault = true;
                    unlockCost = 0;
                    break;
            }

            SetProperty(so, "IsUnlockedByDefault", unlockedByDefault);
            SetProperty(so, "UnlockCost", unlockCost);
        }

        private static T CreateEnemyPrefab<T>(string path, string name, PrimitiveType primitiveType) where T : Component
        {
            string fullPath = $"{path}/{name}.prefab";
            T prefab = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (prefab == null)
            {
                GameObject obj = GameObject.CreatePrimitive(primitiveType);
                obj.name = name;
                obj.AddComponent<T>();
                prefab = PrefabUtility.SaveAsPrefabAsset(obj, fullPath).GetComponent<T>();
                GameObject.DestroyImmediate(obj);
            }
            return prefab;
        }

        private static void SetProperty(SerializedObject so, string propertyName, object value)
        {
            SerializedProperty prop = so.FindProperty($"<{propertyName}>k__BackingField");
            if (prop == null) prop = so.FindProperty(propertyName);

            if (prop != null)
            {
                if (value is int i && prop.propertyType == SerializedPropertyType.Enum) prop.enumValueIndex = i;
                else if (value is int i) prop.intValue = i;
                else if (value is float f) prop.floatValue = f;
                else if (value is bool b) prop.boolValue = b;
                else if (value is string s) prop.stringValue = s;
                else if (value is UnityEngine.Object obj) prop.objectReferenceValue = obj;
            }
            else
            {
                Debug.LogWarning($"Property {propertyName} not found on {so.targetObject.GetType().Name}");
            }
        }

        private static void SetListProperty<T>(SerializedObject so, string propertyName, T[] items) where T : UnityEngine.Object
        {
            SerializedProperty prop = so.FindProperty($"<{propertyName}>k__BackingField");
            if (prop == null) prop = so.FindProperty(propertyName);

            if (prop != null && prop.isArray)
            {
                prop.ClearArray();
                for (int i = 0; i < items.Length; i++)
                {
                    prop.InsertArrayElementAtIndex(i);
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                }
            }
        }
    }
}
