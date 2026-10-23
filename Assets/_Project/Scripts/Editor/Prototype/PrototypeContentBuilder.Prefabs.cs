using FeverDream.AlterEgo;
using FeverDream.Aura;
using FeverDream.Combat;
using FeverDream.Core;
using FeverDream.Enemies;
using FeverDream.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FeverDream.EditorTools.Prototype
{
    public static partial class PrototypeContentBuilder
    {
        private sealed class PrefabSet
        {
            public GameObject Player;
            public GameObject AlterEgo;
            public GameObject Hud;
            public GameObject AlarmClockWorker;
            public GameObject ToiletLidShield;
            public GameObject ClownThrower;
            public GameObject Pie;
            public GameObject GrapplePoint;
            public GameObject Checkpoint;
            public GameObject KillVolume;
            public GameObject Goal;
        }

        private static PrefabSet CreatePrefabs(Palette palette, DataSet data, bool overwrite)
        {
            var set = new PrefabSet();
            set.Pie = Prefab($"{PrefabsFolder}/Enemies/PF_Projectile_Pie.prefab", overwrite, () => BuildPie(palette), null);
            set.Player = Prefab($"{PrefabsFolder}/Characters/PF_Player.prefab", overwrite, () => BuildPlayer(palette, data), root => LinkPlayerData(root, data, false));
            set.AlterEgo = Prefab($"{PrefabsFolder}/Characters/PF_AlterEgo_Pigeon.prefab", overwrite, () => BuildAlterEgo(data),
                root => SetReferenceIfMissing(root.GetComponent<AlterEgoCommentator>(), "lines", data.AlterEgo));
            set.AlarmClockWorker = Prefab($"{PrefabsFolder}/Enemies/PF_Enemy_AlarmClockWorker.prefab", overwrite, () => BuildAlarmClockWorker(palette, data),
                root => SetReferenceIfMissing(root.GetComponent<EnemyBase>(), "settings", data.AlarmClockWorker));
            set.ToiletLidShield = Prefab($"{PrefabsFolder}/Enemies/PF_Enemy_ToiletLidShield.prefab", overwrite, () => BuildToiletLidShield(palette, data),
                root => SetReferenceIfMissing(root.GetComponent<EnemyBase>(), "settings", data.ToiletLidShield));
            set.ClownThrower = Prefab($"{PrefabsFolder}/Enemies/PF_Enemy_ClownThrower.prefab", overwrite, () => BuildClownThrower(palette, data, set.Pie),
                root =>
                {
                    SetReferenceIfMissing(root.GetComponent<EnemyBase>(), "settings", data.ClownThrower);
                    SetReferenceIfMissing(root.GetComponent<ClownThrower>(), "projectilePrefab", set.Pie.GetComponent<PieProjectile>());
                });
            set.GrapplePoint = Prefab($"{PrefabsFolder}/Gameplay/PF_GrapplePoint.prefab", overwrite, () => BuildGrapplePoint(palette), null);
            set.Checkpoint = Prefab($"{PrefabsFolder}/Gameplay/PF_Checkpoint.prefab", overwrite, () => BuildCheckpoint(palette), null);
            set.KillVolume = Prefab($"{PrefabsFolder}/Gameplay/PF_KillVolume.prefab", overwrite, BuildKillVolume, null);
            set.Goal = Prefab($"{PrefabsFolder}/Gameplay/PF_Goal_DreamToilet.prefab", overwrite, () => BuildGoal(palette), null);
            set.Hud = Prefab($"{PrefabsFolder}/UI/PF_HUD.prefab", overwrite, BuildHud, null);
            return set;
        }

        /// <summary>
        /// Builds the prefab when it is missing or when overwriting. An existing prefab is kept as it is, except that
        /// <paramref name="relink"/> re-assigns data references that are empty or point at a deleted asset.
        /// </summary>
        private static GameObject Prefab(string path, bool overwrite, System.Func<GameObject> build, System.Action<GameObject> relink)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && !overwrite)
            {
                if (relink != null)
                {
                    var contents = PrefabUtility.LoadPrefabContents(path);
                    relink(contents);
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    PrefabUtility.UnloadPrefabContents(contents);
                }
                return existing;
            }
            if (existing == null && !overwrite)
            {
                RecreatedPrefabs.Add(System.IO.Path.GetFileNameWithoutExtension(path));
            }
            var instance = build();
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        /// <summary>Data references of the player. With <paramref name="force"/> they are always set, otherwise only when missing.</summary>
        private static void LinkPlayerData(GameObject root, DataSet data, bool force)
        {
            System.Action<Object, string, Object> link = force ? SetReference : SetReferenceIfMissing;
            link(root.GetComponent<PlayerInputReader>(), "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            link(root.GetComponent<PlayerMovement>(), "settings", data.Movement);
            link(root.GetComponent<PlayerCamera>(), "settings", data.Camera);
            link(root.GetComponent<PlayerDamageReceiver>(), "settings", data.Combat);
            link(root.GetComponent<PlayerRespawner>(), "settings", data.Combat);
            link(root.GetComponent<PlayerDodge>(), "settings", data.Combat);
            link(root.GetComponent<AuraWallet>(), "settings", data.Aura);
            link(root.GetComponent<ComboTracker>(), "settings", data.Aura);
            link(root.GetComponent<TsunagiWeapon>(), "form", data.Katana);
        }

        private static GameObject BuildPlayer(Palette palette, DataSet data)
        {
            var root = new GameObject("PF_Player") { tag = "Player", layer = PlayerLayer };
            var controller = root.AddComponent<CharacterController>();
            controller.radius = 0.35f;
            controller.height = 1.8f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 50f;
            controller.skinWidth = 0.04f;
            controller.minMoveDistance = 0f;

            var cameraRoot = Child(root.transform, "CameraRoot", new Vector3(0f, 1.65f, 0f));
            var cameraObject = Child(cameraRoot, "PlayerCamera", Vector3.zero).gameObject;
            cameraObject.tag = "MainCamera";
            var viewCamera = cameraObject.AddComponent<Camera>();
            viewCamera.nearClipPlane = 0.03f;
            viewCamera.fieldOfView = 80f;
            cameraObject.AddComponent<AudioListener>();
            var cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            var weaponHolder = Child(cameraObject.transform, "WeaponHolder", new Vector3(0.32f, -0.32f, 0.5f));
            var katana = Child(weaponHolder, "Tsunagi_Katana", Vector3.zero);
            Primitive(PrimitiveType.Cube, "Blade", katana, new Vector3(0f, 0f, 0.55f), new Vector3(0.035f, 0.07f, 1f), palette.Tsunagi, castShadows: false);
            Primitive(PrimitiveType.Cube, "Guard", katana, new Vector3(0f, 0f, 0.03f), new Vector3(0.12f, 0.12f, 0.03f), palette.Dark, castShadows: false);
            Primitive(PrimitiveType.Cube, "Grip", katana, new Vector3(0f, 0f, -0.12f), new Vector3(0.045f, 0.045f, 0.28f), palette.Dark, castShadows: false);
            var trail = Child(katana, "TrailTip", new Vector3(0f, 0f, 1.05f)).gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.12f;
            trail.startWidth = 0.09f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.02f;
            trail.sharedMaterial = palette.TsunagiTrail;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.emitting = false;
            var swing = weaponHolder.gameObject.AddComponent<WeaponSwingAnimator>();

            var ropeObject = Child(root.transform, "GrappleRope", Vector3.zero).gameObject;
            var line = ropeObject.AddComponent<LineRenderer>();
            line.startWidth = 0.035f;
            line.endWidth = 0.035f;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.sharedMaterial = palette.Chain;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.enabled = false;
            var rope = ropeObject.AddComponent<GrappleRope>();

            root.AddComponent<PlayerContext>();
            root.AddComponent<PlayerInputReader>();
            var movement = root.AddComponent<PlayerMovement>();
            var playerCamera = root.AddComponent<PlayerCamera>();
            root.AddComponent<Health>();
            root.AddComponent<PlayerDamageReceiver>();
            root.AddComponent<PlayerRespawner>();
            root.AddComponent<AuraWallet>();
            root.AddComponent<ComboTracker>();
            var weapon = root.AddComponent<TsunagiWeapon>();
            root.AddComponent<PlayerDodge>();

            LinkPlayerData(root, data, true);
            SetReference(movement, "viewPoint", cameraRoot);
            SetReference(playerCamera, "cameraRoot", cameraRoot);
            SetReference(playerCamera, "viewCamera", viewCamera);
            SetReference(weapon, "aimOrigin", cameraObject.transform);
            SetMask(weapon, "hitMask", (1 << EnemyLayer) | (1 << ProjectileLayer));
            SetMask(weapon, "obstacleMask", 1 << EnvironmentLayer);
            SetReference(swing, "weapon", weapon);
            SetReference(swing, "trail", trail);
            SetReference(rope, "movement", movement);
            SetReference(rope, "origin", weaponHolder);
            return root;
        }

        private static GameObject BuildAlterEgo(DataSet data)
        {
            var root = new GameObject("PF_AlterEgo_Pigeon");
            var commentator = root.AddComponent<AlterEgoCommentator>();
            SetReference(commentator, "lines", data.AlterEgo);
            return root;
        }

        private static GameObject EnemyRoot(string name, out Transform visual)
        {
            var root = new GameObject(name) { layer = EnemyLayer };
            var body = root.AddComponent<Rigidbody>();
            body.mass = 60f;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 1f, 0f);
            capsule.height = 2f;
            capsule.radius = 0.45f;
            root.AddComponent<Health>();
            visual = Child(root.transform, "Visual", Vector3.zero);
            return root;
        }

        private static void SetupEnemy(EnemyBase enemy, EnemySettings settings, Transform telegraphPart)
        {
            var feedback = enemy.gameObject.AddComponent<EnemyFeedback>();
            SetReference(enemy, "settings", settings);
            SetMask(enemy, "environmentMask", 1 << EnvironmentLayer);
            SetReference(enemy, "feedback", feedback);
            if (telegraphPart != null)
            {
                SetReference(feedback, "telegraphPart", telegraphPart);
            }
        }

        private static GameObject BuildAlarmClockWorker(Palette palette, DataSet data)
        {
            var root = EnemyRoot("PF_Enemy_AlarmClockWorker", out var visual);
            Primitive(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 0.85f, 0f), new Vector3(0.8f, 0.85f, 0.8f), palette.OfficeSuit);
            Primitive(PrimitiveType.Cube, "Tie", visual, new Vector3(0f, 1.2f, 0.38f), new Vector3(0.12f, 0.5f, 0.04f), palette.ClownNose);
            var head = Child(visual, "AlarmClockHead", new Vector3(0f, 1.95f, 0f));
            Primitive(PrimitiveType.Cylinder, "Clock", head, Vector3.zero, new Vector3(0.6f, 0.14f, 0.6f), palette.AlarmClock, new Vector3(90f, 0f, 0f));
            Primitive(PrimitiveType.Sphere, "Bell_Left", head, new Vector3(-0.22f, 0.32f, 0f), Vector3.one * 0.2f, palette.Brass);
            Primitive(PrimitiveType.Sphere, "Bell_Right", head, new Vector3(0.22f, 0.32f, 0f), Vector3.one * 0.2f, palette.Brass);
            Primitive(PrimitiveType.Cube, "Stapler", visual, new Vector3(0.5f, 1f, 0.35f), new Vector3(0.14f, 0.16f, 0.5f), palette.Dark);
            SetupEnemy(root.AddComponent<AlarmClockWorker>(), data.AlarmClockWorker, head);
            return root;
        }

        private static GameObject BuildToiletLidShield(Palette palette, DataSet data)
        {
            var root = EnemyRoot("PF_Enemy_ToiletLidShield", out var visual);
            Primitive(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 0.85f, 0f), new Vector3(0.9f, 0.85f, 0.9f), palette.Porcelain);
            Primitive(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 1.95f, 0f), Vector3.one * 0.6f, palette.Porcelain);
            var lidPivot = Child(visual, "LidPivot", new Vector3(0f, 0.95f, 0.55f));
            Primitive(PrimitiveType.Cube, "Lid", lidPivot, new Vector3(0f, 0.15f, 0f), new Vector3(0.95f, 1.3f, 0.08f), palette.ToiletLid);
            Primitive(PrimitiveType.Cube, "Hinge", lidPivot, new Vector3(0f, -0.5f, 0f), new Vector3(0.9f, 0.08f, 0.12f), palette.Dark);
            var shield = root.AddComponent<ToiletLidShield>();
            SetupEnemy(shield, data.ToiletLidShield, null);
            SetReference(shield, "lid", lidPivot);
            return root;
        }

        private static GameObject BuildClownThrower(Palette palette, DataSet data, GameObject piePrefab)
        {
            var root = EnemyRoot("PF_Enemy_ClownThrower", out var visual);
            Primitive(PrimitiveType.Capsule, "Body", visual, new Vector3(0f, 0.85f, 0f), new Vector3(0.8f, 0.85f, 0.8f), palette.ClownSuit);
            Primitive(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 1.9f, 0f), Vector3.one * 0.55f, palette.ClownFace);
            Primitive(PrimitiveType.Sphere, "Nose", visual, new Vector3(0f, 1.9f, 0.3f), Vector3.one * 0.16f, palette.ClownNose);
            Primitive(PrimitiveType.Sphere, "Hair_Left", visual, new Vector3(-0.32f, 2f, 0f), Vector3.one * 0.3f, palette.ClownHair);
            Primitive(PrimitiveType.Sphere, "Hair_Right", visual, new Vector3(0.32f, 2f, 0f), Vector3.one * 0.3f, palette.ClownHair);
            var throwPoint = Child(root.transform, "ThrowPoint", new Vector3(0.45f, 1.6f, 0.35f));
            var clown = root.AddComponent<ClownThrower>();
            SetupEnemy(clown, data.ClownThrower, null);
            SetReference(clown, "projectilePrefab", piePrefab.GetComponent<PieProjectile>());
            SetReference(clown, "throwPoint", throwPoint);
            return root;
        }

        private static GameObject BuildPie(Palette palette)
        {
            var root = new GameObject("PF_Projectile_Pie") { layer = ProjectileLayer };
            var body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = 0.25f;
            sphere.isTrigger = true;
            root.AddComponent<PieProjectile>();
            Primitive(PrimitiveType.Cylinder, "Pie", root.transform, Vector3.zero, new Vector3(0.45f, 0.06f, 0.45f), palette.Pie, new Vector3(90f, 0f, 0f));
            return root;
        }

        private static GameObject BuildGrapplePoint(Palette palette)
        {
            var root = new GameObject("PF_GrapplePoint") { layer = GrapplePointLayer };
            root.AddComponent<GrapplePoint>();
            Primitive(PrimitiveType.Sphere, "Anchor", root.transform, Vector3.zero, Vector3.one * 0.7f, palette.Grapple);
            Primitive(PrimitiveType.Cylinder, "Ring", root.transform, Vector3.zero, new Vector3(1.4f, 0.02f, 1.4f), palette.Grapple, new Vector3(90f, 0f, 0f));
            return root;
        }

        private static GameObject BuildCheckpoint(Palette palette)
        {
            var root = new GameObject("PF_Checkpoint");
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            trigger.size = new Vector3(6f, 3f, 2f);
            root.AddComponent<Checkpoint>();
            Primitive(PrimitiveType.Cube, "Pad", root.transform, new Vector3(0f, 0.03f, 0f), new Vector3(2f, 0.05f, 0.8f), palette.Checkpoint);
            return root;
        }

        private static GameObject BuildKillVolume()
        {
            var root = new GameObject("PF_KillVolume");
            root.AddComponent<BoxCollider>().isTrigger = true;
            root.AddComponent<KillVolume>();
            return root;
        }

        private static GameObject BuildGoal(Palette palette)
        {
            var root = new GameObject("PF_Goal_DreamToilet");
            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.5f, 0f);
            trigger.size = new Vector3(3f, 3f, 3f);
            root.AddComponent<LevelGoal>();
            Primitive(PrimitiveType.Cube, "Base", root.transform, new Vector3(0f, 0.08f, 0f), new Vector3(1.4f, 0.16f, 2f), palette.Goal);
            Primitive(PrimitiveType.Cylinder, "Hole", root.transform, new Vector3(0f, 0.17f, 0.25f), new Vector3(0.45f, 0.01f, 0.6f), palette.Dark);
            Primitive(PrimitiveType.Cube, "FootRest_Left", root.transform, new Vector3(-0.35f, 0.18f, -0.35f), new Vector3(0.3f, 0.04f, 0.6f), palette.Goal);
            Primitive(PrimitiveType.Cube, "FootRest_Right", root.transform, new Vector3(0.35f, 0.18f, -0.35f), new Vector3(0.3f, 0.04f, 0.6f), palette.Goal);
            var glow = Child(root.transform, "Glow", new Vector3(0f, 2f, 0f)).gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(1f, 0.85f, 0.5f);
            glow.intensity = 4f;
            glow.range = 9f;
            return root;
        }
    }
}
