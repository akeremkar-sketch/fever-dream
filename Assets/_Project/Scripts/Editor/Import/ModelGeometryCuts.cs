using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FeverDream.EditorTools.Import
{
    /// <summary>
    /// Cuts geometry out of a kit model while it is imported: parts the model ships but a prefab replaces. The source
    /// file stays as the artist exported it and no derived mesh is stored in the project. Once the artist has removed
    /// a part from the model its entry warns on import; delete the entry then.
    /// </summary>
    public sealed class ModelGeometryCuts : AssetPostprocessor
    {
        private readonly struct Cut
        {
            /// <summary>The model's GUID from its .meta file, not its path: the cut then survives a move or rename.</summary>
            public readonly string ModelGuid;
            public readonly string MeshName;
            /// <summary>In the mesh's own space. A triangle is cut when all three of its corners are inside.</summary>
            public readonly Bounds Box;

            public Cut(string modelGuid, string meshName, Bounds box)
            {
                ModelGuid = modelGuid;
                MeshName = meshName;
                Box = box;
            }
        }

        private static readonly Cut[] Cuts =
        {
            // The "WC" on the bathroom mirror is modelled as bars standing 5 cm proud of the glass, with a smudge of
            // ambient occlusion around them. PF_Prop_BathroomMirror writes it in lipstick instead (Decal_Writing).
            // Model: Art/Models/Props/SM_Prop_BathroomMirror.fbx
            new Cut("738c28e083bb7b94487f8a084c3ec6a2", "BathroomMirror_Clutter",
                new Bounds(new Vector3(-0.025f, 1.18f, -0.17f), new Vector3(2.4f, 0.75f, 0.08f))),
        };

        /// <summary>Raise this whenever <see cref="Cuts"/> changes, so Unity imports the models again.</summary>
        public override uint GetVersion() => 1;

        private void OnPostprocessModel(GameObject root)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            foreach (Cut cut in Cuts)
            {
                if (cut.ModelGuid != guid)
                {
                    continue;
                }

                int removed = 0;
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    Mesh mesh = filter.sharedMesh;
                    if (mesh != null && mesh.name == cut.MeshName)
                    {
                        removed += Remove(mesh, cut.Box);
                    }
                }
                if (removed == 0)
                {
                    Debug.LogWarning($"ModelGeometryCuts: nothing left to cut from '{cut.MeshName}' in {assetPath}. "
                        + "If the model no longer has the part, delete its entry in ModelGeometryCuts.", root);
                }
            }
        }

        /// <summary>
        /// Returns how many triangles were cut. Their vertices stay in the mesh, unused, so the mesh bounds are
        /// refitted to the triangles that are left.
        /// </summary>
        private static int Remove(Mesh mesh, Bounds box)
        {
            Vector3[] vertices = mesh.vertices;
            int removed = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                int[] triangles = mesh.GetTriangles(subMesh);
                var kept = new List<int>(triangles.Length);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (box.Contains(vertices[triangles[i]]) && box.Contains(vertices[triangles[i + 1]])
                        && box.Contains(vertices[triangles[i + 2]]))
                    {
                        removed++;
                        continue;
                    }
                    kept.Add(triangles[i]);
                    kept.Add(triangles[i + 1]);
                    kept.Add(triangles[i + 2]);
                }
                if (kept.Count != triangles.Length)
                {
                    mesh.SetTriangles(kept, subMesh);
                }
            }
            if (removed > 0)
            {
                FitBounds(mesh);
            }
            return removed;
        }

        private static void FitBounds(Mesh mesh)
        {
            bool any = false;
            Bounds bounds = default;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                UnityEngine.Rendering.SubMeshDescriptor part = mesh.GetSubMesh(subMesh);
                if (part.indexCount == 0)
                {
                    continue;
                }
                if (any)
                {
                    bounds.Encapsulate(part.bounds);
                }
                else
                {
                    bounds = part.bounds;
                    any = true;
                }
            }
            if (any)
            {
                mesh.bounds = bounds;
            }
        }
    }
}
