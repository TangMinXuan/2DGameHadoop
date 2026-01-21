using System;
using System.Reflection;
using HadoopCore.Scripts.Annotation;
using UnityEngine;

namespace HadoopCore.Scripts.Utils {
    public static class MySugarUtil {
        private static readonly string groundLayers = "Terrain";

        public static bool IsGround(GameObject go) {
            return go.layer == LayerMask.NameToLayer(groundLayers) ||
                   go.layer == LayerMask.NameToLayer("Ground");
        }

        public static GameObject TryToFindObject(GameObject begin, string name) {
            GameObject rez = FindInChildren(begin, name);
            if (rez != null) {
                return rez;
            }

            rez = FindInParents(begin, name);
            if (rez != null) {
                return rez;
            }

            return GameObject.Find(name);
        }

        public static T TryToFindComponent<T>(GameObject begin, string componentOwner) where T : Component {
            // 先找到owner
            GameObject owner = TryToFindObject(begin, componentOwner);

            return owner != null ? owner.GetComponent<T>() : null;
        }

        /// <summary>
        /// 自动扫描目标对象的所有 GameObject 类型字段,如果为 null,则尝试根据字段名查找并赋值
        /// 同时支持 [Serializable] 的引用类(如 XXXRefs)的自动查找
        /// </summary>
        public static void AutoFindObjects(object target, GameObject begin) {
            if (target == null || begin == null) return;

            System.Type type = target.GetType();
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            foreach (FieldInfo field in fields) {
                // 检查是否有 DontNeedAutoFind 注解
                if (field.GetCustomAttribute<DontNeedAutoFind>() != null) {
                    continue;
                }

                // 1. 处理普通 GameObject 字段
                if (field.FieldType == typeof(GameObject)) {
                    GameObject currentValue = field.GetValue(target) as GameObject;
                    if (currentValue == null) {
                        string objectName = GetCleanFieldName(field.Name);
                        GameObject found = TryToFindObject(begin, objectName);
                        if (found != null) {
                            field.SetValue(target, found);
                        }
                    }
                }
                // 2. 处理 [Serializable] 引用类(如 XXXRefs)
                else if (field.FieldType.IsClass &&
                         field.FieldType.GetCustomAttribute<SerializableAttribute>() != null &&
                         field.FieldType.Name.EndsWith("Refs")) {
                    object refsInstance = field.GetValue(target);
                    if (refsInstance != null) {
                        AutoFindRefsObject(refsInstance, begin, field.FieldType);
                    }
                }
            }
        }

        /// <summary>
        /// 处理 XXXRefs 类型的自动查找
        /// </summary>
        private static void AutoFindRefsObject(object refsInstance, GameObject begin, System.Type refsType) {
            // 1. 查找 obj 字段
            FieldInfo objField = refsType.GetField("obj",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (objField == null || objField.FieldType != typeof(GameObject)) {
                return;
            }

            GameObject obj = objField.GetValue(refsInstance) as GameObject;

            // 2. 如果 obj 为空,尝试根据类名查找
            if (obj == null) {
                string refsName = refsType.Name;
                if (refsName.EndsWith("Refs")) {
                    string objectName = refsName.Substring(0, refsName.Length - 4); // 去掉 "Refs"
                    obj = TryToFindObject(begin, objectName);

                    if (obj != null) {
                        objField.SetValue(refsInstance, obj);
                    }
                    else {
                        Debug.LogWarning($"[AutoFindObjects] 未找到 Refs 对象: {refsType.Name}.obj (查找名称: {objectName})");
                        return;
                    }
                }
            }

            // 3. 从 obj 上查找 [NonSerialized] 标记的组件
            if (obj != null) {
                FieldInfo[] componentFields = refsType.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (FieldInfo componentField in componentFields) {
                    // 跳过 obj 字段本身
                    if (componentField.Name == "obj") continue;

                    // 跳过被 DontNeedAutoFind 标注的字段
                    if (componentField.GetCustomAttribute<DontNeedAutoFind>() != null) {
                        continue;
                    }

                    // 只处理标记了 [NonSerialized] 的字段
                    if (componentField.GetCustomAttribute<NonSerializedAttribute>() == null) {
                        continue;
                    }

                    object currentValue = componentField.GetValue(refsInstance);
                    if (currentValue == null && typeof(Component).IsAssignableFrom(componentField.FieldType)) {
                        Component found = obj.GetComponent(componentField.FieldType);
                        if (found != null) {
                            componentField.SetValue(refsInstance, found);
                        }
                        else {
                            Debug.LogWarning(
                                $"[AutoFindObjects] 未找到组件: {refsType.Name}.{componentField.Name} (类型: {componentField.FieldType.Name})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 清理字段名(去掉前缀,首字母大写)
        /// </summary>
        private static string GetCleanFieldName(string fieldName) {
            if (fieldName.StartsWith("_")) {
                fieldName = fieldName.Substring(1);
            }
            else if (fieldName.StartsWith("m_")) {
                fieldName = fieldName.Substring(2);
            }

            if (fieldName.Length > 0) {
                fieldName = char.ToUpper(fieldName[0]) + fieldName.Substring(1);
            }

            return fieldName;
        }

        private static GameObject FindInChildren(GameObject begin, string childName) {
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

        private static GameObject FindInParents(GameObject begin, string parentName = null) {
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