using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FeverDream.EditorTools.Setup
{
    /// <summary>
    /// Renders the sky of the open scene into a cubemap and makes it the scene's environment reflection, so shiny
    /// surfaces (mirror, metal) reflect the dream sky instead of Unity's default daytime sky. Run it again after the
    /// sky material or the moon light of a scene changes. Nothing else in the scene is baked.
    /// </summary>
    public static class SkyReflectionBaker
    {
        private const string TextureFolder = "Assets/_Project/Art/Textures";
        private const int FaceSize = 128;

        [MenuItem("Fever Dream/Setup/Bake Sky Reflection", priority = 20)]
        private static void BakeFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Sky reflection: leave Play mode first.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogWarning("Sky reflection: save the scene first; the texture is named after it.");
                return;
            }
            if (RenderSettings.skybox == null)
            {
                Debug.LogWarning($"Sky reflection: {scene.name} has no skybox material (Lighting > Environment).");
                return;
            }

            string path = $"{TextureFolder}/T_Env_{scene.name}.exr";
            if (!Bake(path))
            {
                Debug.LogError($"Sky reflection: baking {path} failed.");
                return;
            }

            var cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = cubemap;
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Sky reflection: {path} baked and set as the environment reflection of {scene.name}. Save the scene.", cubemap);
        }

        private static bool Bake(string path)
        {
            var probeObject = new GameObject("SkyReflectionBake") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                // High above the level with a one metre far plane: no geometry is drawn, only the sky. The culling
                // mask stays open so the moon light is still visible to the probe and the moon lands in the right place.
                probeObject.transform.position = new Vector3(0f, 10000f, 0f);
                var probe = probeObject.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Custom;
                probe.resolution = FaceSize;
                probe.hdr = true;
                probe.clearFlags = ReflectionProbeClearFlags.Skybox;
                probe.cullingMask = ~0;
                probe.nearClipPlane = 0.3f;
                probe.farClipPlane = 1f;
                probe.shadowDistance = 0f;
                return Lightmapping.BakeReflectionProbe(probe, path);
            }
            finally
            {
                Object.DestroyImmediate(probeObject);
            }
        }
    }
}
