using System.Collections.Generic;
using UnityEngine;

namespace FeverDream.Core
{
    /// <summary>Keeps objects spawned at runtime under "--- RUNTIME ---" so the hierarchy stays readable while playing.</summary>
    public static class RuntimeHierarchy
    {
        private const string RootName = "--- RUNTIME ---";

        private static readonly Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
        private static Transform root;

        public static Transform GetGroup(string groupName)
        {
            if (groups.TryGetValue(groupName, out var group) && group != null)
            {
                return group;
            }

            if (root == null)
            {
                root = new GameObject(RootName).transform;
            }

            group = new GameObject(groupName).transform;
            group.SetParent(root, false);
            groups[groupName] = group;
            return group;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            groups.Clear();
            root = null;
        }
    }
}
