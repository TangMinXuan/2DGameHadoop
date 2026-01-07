using UnityEngine;

namespace HadoopCore.Scripts.Utils {
    public static class MySugarUtil {
        private static readonly string groundLayers = "Terrain";

        public static bool IsGround(GameObject go) {
            return go.layer == LayerMask.NameToLayer(groundLayers) ||
                   go.layer == LayerMask.NameToLayer("Ground");
        }

        public static GameObject TryToFindObject(GameObject begin, string name, GameObject source) {
            if (source != null) return source;

            source = FindChild(begin, name);
            return source != null ? source : FindParent(begin, name);
        }

        public static T TryToFindComponent<T>(GameObject begin, string componentOwner, T source) where T : Component {
            if (source != null) return source;

            GameObject found = FindChild(begin, componentOwner);
            if (found == null)
                found = FindParent(begin, componentOwner);

            return found != null ? found.GetComponent<T>() : null;
        }


        public static GameObject FindChild(GameObject begin, string childName) {
            if (begin == null || string.IsNullOrEmpty(childName)) return null;

            // 以前的 Transform.Find(childName) 只保证能找到“符合路径/名字的某个直接子节点”。
            // 这里改为：循环遍历整个子层级（DFS），只按名字匹配。
            return FindChildRecursive(begin.transform, childName);
        }

        private static GameObject FindChildRecursive(Transform root, string childName) {
            if (root == null) return null;

            for (int i = 0; i < root.childCount; i++) {
                Transform child = root.GetChild(i);
                if (child == null) continue;

                if (child.name == childName)
                    return child.gameObject;

                GameObject deeper = FindChildRecursive(child, childName);
                if (deeper != null)
                    return deeper;
            }

            return null;
        }

        public static GameObject FindParent(GameObject begin, string parentName = null) {
            Transform current = begin.transform.parent;

            if (parentName == null)
                return current != null ? current.gameObject : null;

            while (current != null) {
                if (current.name == parentName)
                    return current.gameObject;
                current = current.parent;
            }

            return null;
        }
    }
}