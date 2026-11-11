using FeverDream.AlterEgo;
using FeverDream.Aura;
using FeverDream.Combat;
using FeverDream.Core;
using FeverDream.Enemies;
using FeverDream.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FeverDream.EditorTools.Setup
{
    /// <summary>
    /// Restores the project's data setup on a fresh checkout or after something was deleted: layer names and collision rules,
    /// the SO_ data assets with their first-pass values and the dream volume profile. It only creates what is missing and
    /// never touches existing assets, prefabs or scenes; those are edited by hand.
    /// </summary>
    public static class ProjectDataSetup
    {
        private const string ProjectRoot = "Assets/_Project";
        private const string DataFolder = ProjectRoot + "/Data";
        private const string VolumeProfilePath = ProjectRoot + "/Art/PostProcessing/VP_Dream.asset";

        private const int PlayerLayer = 6;
        private const int EnemyLayer = 7;
        private const int EnvironmentLayer = 8;
        private const int GrapplePointLayer = 9;
        private const int ProjectileLayer = 10;

        [MenuItem("Fever Dream/Setup/Create Missing Data", priority = 0)]
        private static void CreateMissingFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Project data setup: leave Play mode first.");
                return;
            }
            ConfigureLayers();
            CreateData();
            CreateVolumeProfile();
            AssetDatabase.SaveAssets();
            Debug.Log("Project data setup: done. Existing assets were left as they are.");
        }

        /// <summary>Layer names and the collision rules documented in Docs/PROJECT_STRUCTURE.md. Never renames a layer someone else uses.</summary>
        private static void ConfigureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            NameLayer(layers, PlayerLayer, "Player");
            NameLayer(layers, EnemyLayer, "Enemy");
            NameLayer(layers, EnvironmentLayer, "Environment");
            NameLayer(layers, GrapplePointLayer, "GrapplePoint");
            NameLayer(layers, ProjectileLayer, "Projectile");
            tagManager.ApplyModifiedPropertiesWithoutUndo();

            for (int layer = 0; layer < 32; layer++)
            {
                // Projectiles only meet the player and the level; grapple points are query-only.
                Physics.IgnoreLayerCollision(ProjectileLayer, layer, layer != PlayerLayer && layer != EnvironmentLayer);
                Physics.IgnoreLayerCollision(GrapplePointLayer, layer, true);
            }
        }

        private static void NameLayer(SerializedProperty layers, int index, string layerName)
        {
            var layer = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
            }
            else if (layer.stringValue != layerName)
            {
                Debug.LogWarning($"Project data setup: layer {index} is named '{layer.stringValue}', expected '{layerName}'. "
                    + "It was left as it is; the game code expects the layer numbers in Docs/PROJECT_STRUCTURE.md.");
            }
        }

        private static void CreateData()
        {
            LoadOrCreate<PlayerMovementSettings>($"{DataFolder}/Player/SO_PlayerMovement_Default.asset", null);
            LoadOrCreate<PlayerCameraSettings>($"{DataFolder}/Player/SO_PlayerCamera_Default.asset", null);
            LoadOrCreate<PlayerCombatSettings>($"{DataFolder}/Player/SO_PlayerCombat_Default.asset", null);
            LoadOrCreate<AuraSettings>($"{DataFolder}/Aura/SO_Aura_Default.asset", null);
            LoadOrCreate<TsunagiFormData>($"{DataFolder}/Combat/SO_Tsunagi_Katana.asset", FillKatana);
            LoadOrCreate<EnemySettings>($"{DataFolder}/Enemies/SO_Enemy_AlarmClockWorker.asset", s =>
            {
                s.displayName = "Alarm Saatli Ofis Çalışanı";
                s.maxHealth = 45f;
                s.moveSpeed = 3.6f;
                s.windupInterruptStaggerMultiplier = 3f;
            });
            LoadOrCreate<EnemySettings>($"{DataFolder}/Enemies/SO_Enemy_ToiletLidShield.asset", s =>
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
            });
            LoadOrCreate<EnemySettings>($"{DataFolder}/Enemies/SO_Enemy_ClownThrower.asset", s =>
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
                s.projectileGravity = -5f;
                s.projectileLifetime = 6f;
            });
            LoadOrCreate<AlterEgoLines>($"{DataFolder}/AlterEgo/SO_AlterEgo_Pigeon.asset", DefaultAlterEgoLines.Fill);
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

        private static void CreateVolumeProfile()
        {
            if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath) != null)
            {
                return;
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            EnsureFolder(VolumeProfilePath);
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(0.85f);
            bloom.scatter.Override(0.7f);
            bloom.tint.Override(new Color(0.9f, 0.86f, 1f));
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.34f);
            vignette.smoothness.Override(0.45f);
            vignette.color.Override(new Color(0.12f, 0.04f, 0.28f));
            var colors = profile.Add<ColorAdjustments>(true);
            colors.saturation.Override(18f);
            colors.contrast.Override(8f);
            var aberration = profile.Add<ChromaticAberration>(true);
            aberration.intensity.Override(0.08f);
            var splitToning = profile.Add<SplitToning>(true);
            splitToning.shadows.Override(new Color(0.4f, 0.43f, 0.64f));
            splitToning.highlights.Override(new Color(0.62f, 0.5f, 0.57f));
            splitToning.balance.Override(-10f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            foreach (var component in profile.components)
            {
                AssetDatabase.AddObjectToAsset(component, profile);
            }
            EditorUtility.SetDirty(profile);
            Debug.LogWarning($"Project data setup: {VolumeProfilePath} was recreated. Assign it to Volume_Global in the scenes that lost it.");
        }

        private static void LoadOrCreate<T>(string path, System.Action<T> initialize) where T : ScriptableObject
        {
            if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            {
                return;
            }
            var asset = ScriptableObject.CreateInstance<T>();
            initialize?.Invoke(asset);
            EnsureFolder(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        /// <summary>Git does not keep empty folders, so a folder whose only asset was deleted is gone on a fresh checkout.</summary>
        private static void EnsureFolder(string assetPath)
        {
            string folder = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }
            EnsureFolder(folder);
            AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(folder).Replace('\\', '/'), System.IO.Path.GetFileName(folder));
        }
    }
}
