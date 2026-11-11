using UnityEditor;
using UnityEngine;

namespace FeverDream.EditorTools.Import
{
    /// <summary>
    /// Import settings for every model under Art/Models, so a newly added FBX needs no manual setup.
    /// Materials named FD_X in the file use the shared palette material Art/Materials/Palette/M_FD_X.
    /// Applied on the first import only, so later changes made in the Inspector are kept.
    /// </summary>
    public sealed class ModelImportRules : AssetPostprocessor
    {
        private const string ModelsRoot = "Assets/_Project/Art/Models/";
        private const string PaletteFolder = "Assets/_Project/Art/Materials/Palette";
        private const string PalettePrefix = "M_";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ModelsRoot) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (ModelImporter)assetImporter;
            // Blender exports carry their preview camera and lights; the game never wants them.
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.useFileScale = true;
            importer.globalScale = 1f;
            // Without this the root keeps Blender's -90° X rotation and every prefab would have to undo it.
            importer.bakeAxisConversion = true;
            importer.addCollider = false;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { PaletteFolder }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (material != null && material.name.StartsWith(PalettePrefix))
                {
                    string sourceName = material.name.Substring(PalettePrefix.Length);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), sourceName), material);
                }
            }
        }
    }
}
