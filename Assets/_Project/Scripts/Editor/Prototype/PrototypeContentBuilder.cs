using FeverDream.AlterEgo;
using FeverDream.Aura;
using FeverDream.Combat;
using FeverDream.Core;
using FeverDream.Enemies;
using FeverDream.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FeverDream.EditorTools.Prototype
{
    /// <summary>
    /// Generates the prototype's placeholder content: layers, materials, data assets, prefabs and the L_Proto_Dream level.
    /// "Build Missing" only creates what does not exist yet and re-links broken data references on existing prefabs;
    /// "Rebuild" regenerates prefabs and the level. Data assets (SO_) and materials are never overwritten,
    /// so tuning done in the inspector is kept.
    /// </summary>
    public static partial class PrototypeContentBuilder
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string MaterialsFolder = ProjectRoot + "/Art/Materials/Prototype";
        private const string VolumeProfilePath = ProjectRoot + "/Art/PostProcessing/VP_Dream.asset";
        private const string InputActionsPath = ProjectRoot + "/Input/IA_Player.inputactions";
        private const string DataFolder = ProjectRoot + "/Data";
        private const string PrefabsFolder = ProjectRoot + "/Prefabs";
        private const string LevelScenePath = ProjectRoot + "/Scenes/Levels/L_Proto_Dream.unity";

        private const int PlayerLayer = 6;
        private const int EnemyLayer = 7;
        private const int EnvironmentLayer = 8;
        private const int GrapplePointLayer = 9;
        private const int ProjectileLayer = 10;
        private const int IgnoreRaycastLayer = 2;

        [MenuItem("Fever Dream/Prototype/Build Missing Content", priority = 0)]
        private static void BuildMissingFromMenu() => Build(false);

        [MenuItem("Fever Dream/Prototype/Rebuild Prefabs and Level (overwrites)", priority = 1)]
        private static void RebuildFromMenu()
        {
            const string message = "Prototype prefabs and L_Proto_Dream are regenerated and manual changes to them are lost. "
                + "Open scenes are closed (you are asked to save them first). Data assets (SO_) and materials are kept.";
            if (EditorUtility.DisplayDialog("Rebuild prototype content", message, "Rebuild", "Cancel"))
            {
                Build(true);
            }
        }

        public static void Build(bool overwrite)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Prototype content: leave Play mode before building.");
                return;
            }

            // Building the level replaces the open scenes, so ask before anything is written.
            bool buildLevel = overwrite || AssetDatabase.LoadAssetAtPath<SceneAsset>(LevelScenePath) == null;
            if (buildLevel && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureLayers();
            var palette = new Palette();
            var data = CreateData();
            bool profileExisted = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath) != null;
            var volumeProfile = CreateVolumeProfile();
            var prefabs = CreatePrefabs(palette, data, overwrite);
            if (buildLevel)
            {
                BuildLevel(palette, prefabs, volumeProfile);
            }
            else if (!profileExisted)
            {
                Debug.LogWarning($"Prototype content: {VolumeProfilePath} was recreated. Assign it to Volume_Global in L_Proto_Dream or run Rebuild.");
            }
            AddLevelToBuildSettings();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Layer names and the collision rules documented in Docs/PROJECT_STRUCTURE.md.</summary>
        private static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            layers.GetArrayElementAtIndex(PlayerLayer).stringValue = "Player";
            layers.GetArrayElementAtIndex(EnemyLayer).stringValue = "Enemy";
            layers.GetArrayElementAtIndex(EnvironmentLayer).stringValue = "Environment";
            layers.GetArrayElementAtIndex(GrapplePointLayer).stringValue = "GrapplePoint";
            layers.GetArrayElementAtIndex(ProjectileLayer).stringValue = "Projectile";
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            for (int layer = 0; layer < 32; layer++)
            {
                // Projectiles only meet the player and the level; grapple points are query-only.
                Physics.IgnoreLayerCollision(ProjectileLayer, layer, layer != PlayerLayer && layer != EnvironmentLayer);
                Physics.IgnoreLayerCollision(GrapplePointLayer, layer, true);
            }
        }

        private static void AddLevelToBuildSettings()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(scene => scene.path == LevelScenePath))
            {
                return;
            }
            scenes.Add(new EditorBuildSettingsScene(LevelScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private sealed class Palette
        {
            public readonly Material Floor = Mat("M_Proto_Floor", new Color(0.36f, 0.33f, 0.42f));
            public readonly Material Wall = Mat("M_Proto_Wall", new Color(0.5f, 0.47f, 0.58f));
            public readonly Material WallRun = Mat("M_Proto_WallRun", new Color(0.25f, 0.5f, 0.95f), 0.15f);
            public readonly Material Ledge = Mat("M_Proto_Ledge", new Color(1f, 0.85f, 0.2f), 0.8f);
            public readonly Material SlideUnder = Mat("M_Proto_SlideUnder", new Color(1f, 0.5f, 0.2f));
            public readonly Material Grapple = Mat("M_Proto_Grapple", new Color(0.2f, 0.95f, 1f), 2f);
            public readonly Material Checkpoint = Mat("M_Proto_Checkpoint", new Color(0.3f, 1f, 0.5f), 1f);
            public readonly Material Goal = Mat("M_Proto_Goal", new Color(1f, 0.95f, 0.8f), 0.6f);
            public readonly Material Pie = Mat("M_Proto_Pie", new Color(0.98f, 0.85f, 0.7f), 0.1f);

            public readonly Material Fridge = Mat("M_Prop_Fridge", new Color(0.85f, 0.92f, 0.95f), 0f, 0.6f);
            public readonly Material Appliance = Mat("M_Prop_Appliance", new Color(0.9f, 0.9f, 0.88f), 0f, 0.5f);
            public readonly Material Cart = Mat("M_Prop_ShoppingCart", new Color(0.75f, 0.2f, 0.25f), 0f, 0.6f);
            public readonly Material Wood = Mat("M_Prop_Wood", new Color(0.55f, 0.38f, 0.25f));
            public readonly Material Crate = Mat("M_Prop_Crate", new Color(0.6f, 0.45f, 0.3f));

            public readonly Material Tsunagi = Mat("M_Weapon_Tsunagi", new Color(0.6f, 0.3f, 1f), 2.5f, 0.8f);
            public readonly Material TsunagiTrail = Mat("M_Weapon_TsunagiTrail", new Color(0.55f, 0.85f, 1f), 3f);
            public readonly Material Chain = Mat("M_Weapon_Chain", new Color(0.7f, 0.7f, 0.8f), 0.5f, 0.8f);
            public readonly Material Dark = Mat("M_Proto_Dark", new Color(0.08f, 0.07f, 0.1f));

            public readonly Material OfficeSuit = Mat("M_Enemy_OfficeSuit", new Color(0.18f, 0.2f, 0.28f));
            public readonly Material AlarmClock = Mat("M_Enemy_AlarmClock", new Color(0.95f, 0.9f, 0.75f));
            public readonly Material Brass = Mat("M_Enemy_Brass", new Color(0.9f, 0.7f, 0.2f), 0.2f, 0.8f);
            public readonly Material Porcelain = Mat("M_Enemy_Porcelain", new Color(0.82f, 0.88f, 0.95f), 0f, 0.8f);
            public readonly Material ToiletLid = Mat("M_Enemy_ToiletLid", Color.white, 0f, 0.9f);
            public readonly Material ClownSuit = Mat("M_Enemy_ClownSuit", new Color(0.9f, 0.2f, 0.35f));
            public readonly Material ClownFace = Mat("M_Enemy_ClownFace", new Color(1f, 0.97f, 0.95f));
            public readonly Material ClownNose = Mat("M_Enemy_ClownNose", new Color(1f, 0.05f, 0.05f), 0.5f);
            public readonly Material ClownHair = Mat("M_Enemy_ClownHair", new Color(0.2f, 0.9f, 0.4f));
        }

        private sealed class DataSet
        {
            public PlayerMovementSettings Movement;
            public PlayerCameraSettings Camera;
            public PlayerCombatSettings Combat;
            public AuraSettings Aura;
            public TsunagiFormData Katana;
            public EnemySettings AlarmClockWorker;
            public EnemySettings ToiletLidShield;
            public EnemySettings ClownThrower;
            public AlterEgoLines AlterEgo;
        }

        private static DataSet CreateData()
        {
            return new DataSet
            {
                Movement = LoadOrCreate<PlayerMovementSettings>($"{DataFolder}/Player/SO_PlayerMovement_Default.asset", null),
                Camera = LoadOrCreate<PlayerCameraSettings>($"{DataFolder}/Player/SO_PlayerCamera_Default.asset", null),
                Combat = LoadOrCreate<PlayerCombatSettings>($"{DataFolder}/Player/SO_PlayerCombat_Default.asset", null),
                Aura = LoadOrCreate<AuraSettings>($"{DataFolder}/Aura/SO_Aura_Default.asset", null),
                Katana = LoadOrCreate<TsunagiFormData>($"{DataFolder}/Combat/SO_Tsunagi_Katana.asset", FillKatana),
                AlarmClockWorker = LoadOrCreate<EnemySettings>($"{DataFolder}/Enemies/SO_Enemy_AlarmClockWorker.asset", s =>
                {
                    s.displayName = "Alarm Saatli Ofis Çalışanı";
                    s.maxHealth = 45f;
                    s.moveSpeed = 3.6f;
                    s.windupInterruptStaggerMultiplier = 3f;
                }),
                ToiletLidShield = LoadOrCreate<EnemySettings>($"{DataFolder}/Enemies/SO_Enemy_ToiletLidShield.asset", s =>
                {
                    s.displayName = "Klozet Kapaklı Kalkan";
                    s.maxHealth = 60f;
                    s.moveSpeed = 2.6f;
                    s.attackWindup = 0.9f;
                    s.attackCooldown = 1.8f;
                    s.attackDamage = 16f;
                    s.attackKnockback = 9f;
                    s.knockbackResistance = 0.5f;
                    s.launchResistance = 0.2f;
                    s.guardBreakDuration = 2.5f;
                    s.guardFrontArc = 0.3f;
                }),
                ClownThrower = LoadOrCreate<EnemySettings>($"{DataFolder}/Enemies/SO_Enemy_ClownThrower.asset", s =>
                {
                    s.displayName = "Turta Atan Palyaço";
                    s.maxHealth = 25f;
                    s.moveSpeed = 3.2f;
                    s.detectionRange = 26f;
                    s.attackRange = 18f;
                    s.attackWindup = 0.6f;
                    s.attackRecovery = 0.5f;
                    s.attackCooldown = 2.2f;
                    s.attackDamage = 10f;
                    s.attackKnockback = 5f;
                    s.strafeSwitchInterval = 1.6f;
                    s.aimLeadFactor = 0.5f;
                }),
                AlterEgo = LoadOrCreate<AlterEgoLines>($"{DataFolder}/AlterEgo/SO_AlterEgo_Pigeon.asset", PrototypeAlterEgoLines.Fill),
            };
        }

        private static void FillKatana(TsunagiFormData form)
        {
            form.formName = "Katana";
            form.groundChain = new[]
            {
                Move("katana_slash_1", "Kesik", SwingType.SlashRight, AttackFlags.None, 10f, 2.5f, 0f, 0.07f, 0.2f, 0.45f, 2.7f, 0.9f, 0.4f, 0.04f, 0.5f, 10f),
                Move("katana_slash_2", "Ters Kesik", SwingType.SlashLeft, AttackFlags.None, 10f, 2.5f, 0f, 0.07f, 0.2f, 0.45f, 2.7f, 0.9f, 0.4f, 0.04f, 0.5f, 10f),
                Move("katana_thrust", "Saplama", SwingType.Thrust, AttackFlags.None, 14f, 6f, 0f, 0.1f, 0.3f, 0.5f, 3.1f, 0.8f, 0.8f, 0.06f, 0.8f, 14f),
            };
            form.launcher = Move("katana_launcher", "Gökyüzü Kesiği", SwingType.Uppercut, AttackFlags.Heavy | AttackFlags.Launcher,
                12f, 1f, 12f, 0.14f, 0.3f, 0.5f, 2.6f, 1f, 0.3f, 0.07f, 1f, 12f);
            form.finisher = Move("katana_finisher", "Ruh Kesişi", SwingType.Spin, AttackFlags.Heavy | AttackFlags.Finisher,
                30f, 12f, 0f, 0.18f, 0.45f, 0.3f, 3.2f, 1.4f, 0.6f, 0.12f, 1.6f, 20f);
            form.finisherAfterLightHits = 3;
            form.airChain = new[]
            {
                Move("katana_air_1", "Hava Kesiği", SwingType.SlashRight, AttackFlags.None, 9f, 1.5f, 0f, 0.06f, 0.18f, 0.45f, 2.8f, 1f, 0f, 0.04f, 0.5f, 10f),
                Move("katana_air_2", "Ters Hava Kesiği", SwingType.SlashLeft, AttackFlags.None, 9f, 1.5f, 0f, 0.06f, 0.18f, 0.45f, 2.8f, 1f, 0f, 0.04f, 0.5f, 10f),
                Move("katana_air_3", "Hava Darbesi", SwingType.Overhead, AttackFlags.None, 13f, 4f, 0f, 0.09f, 0.25f, 0.45f, 2.8f, 1f, 0f, 0.06f, 0.8f, 13f),
            };
            form.airSlam = Move("katana_slam", "Çakılma", SwingType.Slam, AttackFlags.Heavy | AttackFlags.Slam,
                18f, 8f, 0f, 0.15f, 0.35f, 0.2f, 0f, 3.2f, 0f, 0.08f, 1.4f, 15f);
            form.slamDiveSpeed = 22f;
            form.hitStopTimeScale = 0.05f;
            form.lungeDuration = 0.08f;
            form.inputBufferTime = 0.25f;
        }

        private static ComboMove Move(string id, string displayName, SwingType swing, AttackFlags flags, float damage, float knockback,
            float launchForce, float startup, float recovery, float chainWindow, float range, float radius, float lunge,
            float hitStop, float cameraShake, float auraValue)
        {
            return new ComboMove
            {
                id = id,
                displayName = displayName,
                swing = swing,
                flags = flags,
                damage = damage,
                knockback = knockback,
                launchForce = launchForce,
                startup = startup,
                recovery = recovery,
                chainWindow = chainWindow,
                range = range,
                radius = radius,
                lunge = lunge,
                hitStop = hitStop,
                cameraShake = cameraShake,
                auraValue = auraValue,
            };
        }

        private static VolumeProfile CreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.7f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.32f);
            vignette.color.Override(new Color(0.2f, 0.05f, 0.3f));
            var colors = profile.Add<ColorAdjustments>(true);
            colors.saturation.Override(18f);
            colors.contrast.Override(8f);
            var aberration = profile.Add<ChromaticAberration>(true);
            aberration.intensity.Override(0.18f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            foreach (var component in profile.components)
            {
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        #region Helpers

        private static Material Mat(string name, Color color, float emission = 0f, float smoothness = 0.2f)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static T LoadOrCreate<T>(string path, System.Action<T> initialize) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }
            asset = ScriptableObject.CreateInstance<T>();
            initialize?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static SerializedProperty FindField(SerializedObject serialized, Object target, string field)
        {
            return serialized.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{field}'.");
        }

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            FindField(serialized, target, field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Assigns the reference only when it is empty or points at a deleted asset.</summary>
        private static void SetReferenceIfMissing(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = FindField(serialized, target, field);
            if (property.objectReferenceValue == null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetMask(Object target, string field, int mask)
        {
            var serialized = new SerializedObject(target);
            FindField(serialized, target, field).intValue = mask;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }

        /// <summary>Visual-only primitive: no collider, no shadow-casting surprises from first-person parts.</summary>
        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, Vector3 localEuler = default, bool castShadows = true)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = Quaternion.Euler(localEuler);
            primitive.transform.localScale = localScale;
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
            var renderer = primitive.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            if (!castShadows)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            return primitive;
        }

        #endregion
    }
}
