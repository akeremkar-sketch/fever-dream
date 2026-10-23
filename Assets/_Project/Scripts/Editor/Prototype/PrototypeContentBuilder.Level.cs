using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FeverDream.Editor.Prototype
{
    /// <summary>
    /// L_Proto_Dream: one small level that teaches every mechanic in order (Docs/PROTOTYPE_PLAN.md).
    /// The player runs along +Z. Floors are sized so their top surface sits at the listed height.
    /// </summary>
    public static partial class PrototypeContentBuilder
    {
        private static void BuildLevel(Palette palette, PrefabSet prefabs, VolumeProfile volumeProfile)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var setup = Group("--- SETUP ---", null);
            var environment = Group("--- ENVIRONMENT ---", null);
            var gameplay = Group("--- GAMEPLAY ---", null);
            var enemies = Group("--- ENEMIES ---", null);
            var ui = Group("--- UI ---", null);

            BuildSetup(setup, volumeProfile);
            BuildStart(Group("Section_01_Start", environment), palette);
            BuildParkourOne(Group("Section_02_Parkour", environment), palette);
            BuildArenaA(Group("Section_03_Arena_A", environment), palette);
            BuildParkourTwo(Group("Section_04_Parkour", environment), palette);
            BuildArenaB(Group("Section_05_Arena_B", environment), palette);
            BuildGoalRoom(Group("Section_06_Goal", environment), palette);

            Place(prefabs.Player, gameplay, "Player", new Vector3(0f, 0f, -3f));
            Place(prefabs.AlterEgo, gameplay, "AlterEgo_Pigeon", Vector3.zero);

            var checkpoints = Group("Checkpoints", gameplay);
            Place(prefabs.Checkpoint, checkpoints, "Checkpoint_01_AfterWallRun", new Vector3(0f, 0f, 52f));
            Place(prefabs.Checkpoint, checkpoints, "Checkpoint_02_BeforeArenaA", new Vector3(0f, 2.2f, 63f));
            Place(prefabs.Checkpoint, checkpoints, "Checkpoint_03_AfterArenaA", new Vector3(0f, 0f, 101f));
            Place(prefabs.Checkpoint, checkpoints, "Checkpoint_04_Landing", new Vector3(0f, 1f, 130f));
            Place(prefabs.Checkpoint, checkpoints, "Checkpoint_05_ArenaB", new Vector3(0f, 5.2f, 144f));

            var grapplePoints = Group("GrapplePoints", gameplay);
            Place(prefabs.GrapplePoint, grapplePoints, "GrapplePoint_01", new Vector3(0f, 6f, 109f));
            Place(prefabs.GrapplePoint, grapplePoints, "GrapplePoint_02", new Vector3(0f, 7.5f, 119f));
            Place(prefabs.GrapplePoint, grapplePoints, "GrapplePoint_03_ArenaB", new Vector3(0f, 12f, 162f));

            var killVolumes = Group("KillVolumes", gameplay);
            Place(prefabs.KillVolume, killVolumes, "KillVolume_Void", new Vector3(0f, -15f, 95f)).transform.localScale = new Vector3(300f, 10f, 420f);

            Place(prefabs.Goal, gameplay, "Goal_DreamToilet", new Vector3(0f, 5.2f, 192f));

            var arenaA = Group("Arena_A", enemies);
            Place(prefabs.AlarmClockWorker, arenaA, "Enemy_AlarmClockWorker_01", new Vector3(-4f, 0f, 84f), 180f);
            Place(prefabs.AlarmClockWorker, arenaA, "Enemy_AlarmClockWorker_02", new Vector3(4f, 0f, 86f), 180f);
            Place(prefabs.AlarmClockWorker, arenaA, "Enemy_AlarmClockWorker_03", new Vector3(0f, 0f, 90f), 180f);
            Place(prefabs.ToiletLidShield, arenaA, "Enemy_ToiletLidShield_01", new Vector3(0f, 0f, 94f), 180f);

            var arenaB = Group("Arena_B", enemies);
            Place(prefabs.AlarmClockWorker, arenaB, "Enemy_AlarmClockWorker_04", new Vector3(-5f, 5.2f, 158f), 180f);
            Place(prefabs.AlarmClockWorker, arenaB, "Enemy_AlarmClockWorker_05", new Vector3(5f, 5.2f, 160f), 180f);
            Place(prefabs.ToiletLidShield, arenaB, "Enemy_ToiletLidShield_02", new Vector3(0f, 5.2f, 166f), 180f);
            Place(prefabs.ClownThrower, arenaB, "Enemy_ClownThrower_01", new Vector3(-9f, 8.2f, 172f), 180f);
            Place(prefabs.ClownThrower, arenaB, "Enemy_ClownThrower_02", new Vector3(9f, 8.2f, 172f), 180f);

            Place(prefabs.Hud, ui, "HUD", Vector3.zero);

            EditorSceneManager.SaveScene(scene, LevelScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(LevelScenePath, true) };
        }

        private static void BuildSetup(Transform setup, VolumeProfile volumeProfile)
        {
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.transform.SetParent(setup, false);
            sun.transform.rotation = Quaternion.Euler(48f, -30f, 0f);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.92f, 0.98f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.transform.SetParent(setup, false);
            volume.isGlobal = true;
            volume.sharedProfile = volumeProfile;

            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.45f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.35f, 0.3f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.12f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogColor = new Color(0.45f, 0.33f, 0.6f);
        }

        // Spawn room with absurd giant household props. Floor top y = 0, z -6..20.
        private static void BuildStart(Transform section, Palette p)
        {
            Block(section, "Floor_Start", new Vector3(0f, -0.5f, 7f), new Vector3(14f, 1f, 26f), p.Floor);
            Block(section, "Wall_Start_Left", new Vector3(-7.5f, 3f, 7f), new Vector3(1f, 6f, 26f), p.Wall);
            Block(section, "Wall_Start_Right", new Vector3(7.5f, 3f, 7f), new Vector3(1f, 6f, 26f), p.Wall);
            Block(section, "Wall_Start_Back", new Vector3(0f, 3f, -6.5f), new Vector3(16f, 6f, 1f), p.Wall);
            Block(section, "Wall_Start_Front_Left", new Vector3(-5f, 3f, 20.5f), new Vector3(4f, 6f, 1f), p.Wall);
            Block(section, "Wall_Start_Front_Right", new Vector3(5f, 3f, 20.5f), new Vector3(4f, 6f, 1f), p.Wall);
            Block(section, "Prop_GiantFridge_01", new Vector3(-4.5f, 3f, 12f), new Vector3(3f, 6f, 2.5f), p.Fridge);
            Block(section, "Prop_WashingMachine_01", new Vector3(4.8f, 1.25f, 4f), new Vector3(2.5f, 2.5f, 2.5f), p.Appliance);
            Block(section, "Prop_WashingMachine_02", new Vector3(4.8f, 3.75f, 4f), new Vector3(2.5f, 2.5f, 2.5f), p.Appliance);
            Cylinder(section, "Prop_GiantMug_01", new Vector3(-4.5f, 1.2f, 2f), new Vector3(2f, 1.2f, 2f), p.Appliance, Vector3.zero);
        }

        // Slide under the sofa, wall run over the gap on the right, climb the 2.2 m ledge.
        private static void BuildParkourOne(Transform section, Palette p)
        {
            Block(section, "Floor_Corridor", new Vector3(0f, -0.5f, 27f), new Vector3(6f, 1f, 14f), p.Floor);
            Block(section, "Wall_Corridor_Left", new Vector3(-3.5f, 3f, 27f), new Vector3(1f, 6f, 14f), p.Wall);
            Block(section, "Wall_Corridor_Right", new Vector3(3.5f, 3f, 27f), new Vector3(1f, 6f, 14f), p.Wall);
            Block(section, "Obstacle_SlideUnder_Sofa", new Vector3(0f, 2.6f, 26f), new Vector3(6f, 3f, 2f), p.SlideUnder);
            Block(section, "Decal_WallRunLane", new Vector3(2.2f, 0.01f, 30.5f), new Vector3(0.4f, 0.02f, 7f), p.WallRun, false);

            // Reaches below the floor line so a wall run that dips slightly never loses the wall.
            Block(section, "WallRun_Right_01", new Vector3(3.5f, 0f, 43f), new Vector3(1f, 12f, 18f), p.WallRun);

            Block(section, "Floor_AfterGap", new Vector3(0f, -0.5f, 58f), new Vector3(6f, 1f, 16f), p.Floor);
            Block(section, "Wall_AfterGap_Left", new Vector3(-3.5f, 3f, 58f), new Vector3(1f, 6f, 16f), p.Wall);
            Block(section, "Wall_AfterGap_Right", new Vector3(3.5f, 3f, 59f), new Vector3(1f, 6f, 14f), p.Wall);
            Block(section, "Ledge_Step_01", new Vector3(0f, 1.1f, 62f), new Vector3(6f, 2.2f, 8f), p.Floor);
            Block(section, "Decal_LedgeEdge_01", new Vector3(0f, 2.21f, 58.1f), new Vector3(6f, 0.02f, 0.2f), p.Ledge, false);
        }

        // First arena, floor top y = 0, z 66..98. Entered by dropping off the ledge platform.
        private static void BuildArenaA(Transform section, Palette p)
        {
            Block(section, "Floor_ArenaA", new Vector3(0f, -0.5f, 82f), new Vector3(24f, 1f, 32f), p.Floor);
            Block(section, "Wall_ArenaA_Left", new Vector3(-12.5f, 3f, 82f), new Vector3(1f, 6f, 32f), p.Wall);
            Block(section, "Wall_ArenaA_Right", new Vector3(12.5f, 3f, 82f), new Vector3(1f, 6f, 32f), p.Wall);
            Block(section, "Wall_ArenaA_Entry_Left", new Vector3(-7.5f, 3f, 66.5f), new Vector3(9f, 6f, 1f), p.Wall);
            Block(section, "Wall_ArenaA_Entry_Right", new Vector3(7.5f, 3f, 66.5f), new Vector3(9f, 6f, 1f), p.Wall);
            Block(section, "Wall_ArenaA_Exit_Left", new Vector3(-7.5f, 3f, 98.5f), new Vector3(9f, 6f, 1f), p.Wall);
            Block(section, "Wall_ArenaA_Exit_Right", new Vector3(7.5f, 3f, 98.5f), new Vector3(9f, 6f, 1f), p.Wall);
            Block(section, "Prop_ShoppingCart_Giant", new Vector3(6f, 1.5f, 78f), new Vector3(4f, 3f, 2.5f), p.Cart);
            Block(section, "Prop_OfficeDesk_01", new Vector3(-6f, 0.6f, 86f), new Vector3(3f, 1.2f, 1.6f), p.Wood);
            Block(section, "Prop_FloatingChair_01", new Vector3(-8f, 5f, 74f), new Vector3(1f, 1.2f, 1f), p.Wood);
            Cylinder(section, "Prop_GiantAlarmClock_01", new Vector3(9f, 2f, 94f), new Vector3(3f, 0.5f, 3f), p.Appliance, new Vector3(90f, 0f, 0f));
        }

        // Grapple twice over the void, land, then wall jump up the shaft and climb into arena B.
        private static void BuildParkourTwo(Transform section, Palette p)
        {
            Block(section, "Floor_Bridge", new Vector3(0f, -0.5f, 101f), new Vector3(6f, 1f, 6f), p.Floor);
            Block(section, "Platform_Landing", new Vector3(0f, 0.5f, 133f), new Vector3(8f, 1f, 14f), p.Floor);
            Block(section, "Shaft_Wall_Left", new Vector3(-2.5f, 4.5f, 138f), new Vector3(1f, 9f, 4f), p.WallRun);
            Block(section, "Shaft_Wall_Right", new Vector3(2.5f, 4.5f, 138f), new Vector3(1f, 9f, 4f), p.WallRun);
            Block(section, "Shaft_Wall_Back", new Vector3(0f, 2.6f, 140.5f), new Vector3(6f, 5.2f, 1f), p.Wall);
            Block(section, "Decal_LedgeEdge_02", new Vector3(0f, 5.215f, 140.15f), new Vector3(4f, 0.02f, 0.25f), p.Ledge, false);
        }

        // Second arena, floor top y = 5.2, z 141..181. Clowns stand on the crate stacks.
        private static void BuildArenaB(Transform section, Palette p)
        {
            Block(section, "Floor_ArenaB", new Vector3(0f, 4.7f, 161f), new Vector3(26f, 1f, 40f), p.Floor);
            Block(section, "Wall_ArenaB_Left", new Vector3(-13.5f, 8.2f, 161f), new Vector3(1f, 6f, 40f), p.Wall);
            Block(section, "Wall_ArenaB_Right", new Vector3(13.5f, 8.2f, 161f), new Vector3(1f, 6f, 40f), p.Wall);
            Block(section, "Wall_ArenaB_Entry_Left", new Vector3(-8.25f, 8.2f, 140.5f), new Vector3(10.5f, 6f, 1f), p.Wall);
            Block(section, "Wall_ArenaB_Entry_Right", new Vector3(8.25f, 8.2f, 140.5f), new Vector3(10.5f, 6f, 1f), p.Wall);
            Block(section, "Wall_ArenaB_Exit_Left", new Vector3(-8.25f, 8.2f, 181.5f), new Vector3(10.5f, 6f, 1f), p.Wall);
            Block(section, "Wall_ArenaB_Exit_Right", new Vector3(8.25f, 8.2f, 181.5f), new Vector3(10.5f, 6f, 1f), p.Wall);
            Block(section, "Prop_CrateStack_Left", new Vector3(-9f, 6.7f, 172f), new Vector3(4f, 3f, 4f), p.Crate);
            Block(section, "Prop_CrateStack_Right", new Vector3(9f, 6.7f, 172f), new Vector3(4f, 3f, 4f), p.Crate);
            Block(section, "Prop_WashingMachine_03", new Vector3(7f, 6.45f, 150f), new Vector3(2.5f, 2.5f, 2.5f), p.Appliance);
            Cylinder(section, "Prop_GiantToiletPaper_01", new Vector3(-6f, 6.7f, 150f), new Vector3(1.5f, 1.5f, 1.5f), p.Appliance, Vector3.zero);
        }

        // The dream toilet. Floor top y = 5.2, z 181..196.
        private static void BuildGoalRoom(Transform section, Palette p)
        {
            Block(section, "Floor_Goal", new Vector3(0f, 4.7f, 188.5f), new Vector3(8f, 1f, 15f), p.Floor);
            Block(section, "Wall_Goal_Left", new Vector3(-4.5f, 8.2f, 188.5f), new Vector3(1f, 6f, 15f), p.Wall);
            Block(section, "Wall_Goal_Right", new Vector3(4.5f, 8.2f, 188.5f), new Vector3(1f, 6f, 15f), p.Wall);
            Block(section, "Wall_Goal_Back", new Vector3(0f, 8.2f, 196.5f), new Vector3(10f, 6f, 1f), p.Wall);
        }

        private static Transform Group(string name, Transform parent)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static GameObject Place(GameObject prefab, Transform parent, string name, Vector3 position, float yaw = 0f)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = name;
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            return instance;
        }

        /// <summary>Level geometry. Solid blocks go on the Environment layer so the player can stand on, run along and climb them.</summary>
        private static GameObject Block(Transform parent, string name, Vector3 center, Vector3 size, Material material, bool solid = true)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ConfigureLevelPiece(block, parent, name, center, size, Vector3.zero, material, solid);
            return block;
        }

        private static GameObject Cylinder(Transform parent, string name, Vector3 center, Vector3 scale, Material material, Vector3 euler)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ConfigureLevelPiece(cylinder, parent, name, center, scale, euler, material, true);
            return cylinder;
        }

        private static void ConfigureLevelPiece(GameObject piece, Transform parent, string name, Vector3 center, Vector3 scale, Vector3 euler, Material material, bool solid)
        {
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = center;
            piece.transform.localRotation = Quaternion.Euler(euler);
            piece.transform.localScale = scale;
            piece.GetComponent<Renderer>().sharedMaterial = material;
            if (solid)
            {
                piece.layer = EnvironmentLayer;
            }
            else
            {
                Object.DestroyImmediate(piece.GetComponent<Collider>());
            }
            GameObjectUtility.SetStaticEditorFlags(piece, StaticEditorFlags.BatchingStatic);
        }
    }
}
