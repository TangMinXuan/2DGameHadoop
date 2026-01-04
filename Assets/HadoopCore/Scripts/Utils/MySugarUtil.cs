using UnityEngine;

namespace HadoopCore.Scripts.Utils
{
    public static class MySugarUtil
    {
        private static readonly string groundLayers = "Terrain";
        
        public static bool IsGround(GameObject go)
        {
            return go.layer == LayerMask.NameToLayer(groundLayers) || 
                   go.layer == LayerMask.NameToLayer("Ground");
        }
        
        public static GameObject TryToFindObject(GameObject begin, string name, GameObject source) 
        {
            if (source != null) return source;
            
            source = FindChild(begin, name);
            return source != null ? source : FindParent(begin, name);
        }
        
        public static T TryToFindComponent<T>(GameObject begin, string componentOwner, T source) where T : Component
        {
            if (source != null) return source;
            
            GameObject found = FindChild(begin, componentOwner);
            if (found == null)
                found = FindParent(begin, componentOwner);
            
            return found != null ? found.GetComponent<T>() : null;
        }
        
        
        public static GameObject FindChild(GameObject begin, string childName)
        {
            Transform child = begin.transform.Find(childName);
            return child != null ? child.gameObject : null;
        }
        
        public static GameObject FindParent(GameObject begin, string parentName = null)
        {
            Transform current = begin.transform.parent;
    
            if (parentName == null)
                return current != null ? current.gameObject : null;
    
            while (current != null)
            {
                if (current.name == parentName)
                    return current.gameObject;
                current = current.parent;
            }
            return null;
        }
    }
}