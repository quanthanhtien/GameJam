#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TinyGiantStudio.BetterInspector
{
    /// <summary>
    /// This is responsible for finding the correct ScriptableObject
    /// </summary>
    public static class ScalesFinder
    {
        private static Scales myScales;
        readonly static string fileLocation = "Assets/Plugins/Tiny Giant Studio/Better Inspector/Common Scripts/Scales.asset";

        /// <summary>
        /// 
        /// </summary>
        /// <returns>The scales ScriptableObject file. This can be null</returns>
        public static Scales MyScales()
        {
            if (myScales != null)
                return myScales;

            //Scales mySettings = Resources.Load(fileLocation) as Scales;
            Scales mySettings = AssetDatabase.LoadAssetAtPath<Scales>(fileLocation);
            if (mySettings)
                return mySettings;

            var objects = Resources.FindObjectsOfTypeAll(typeof(Scales));

            if (objects.Length > 1)
            {
                Debug.LogWarning("Multiple scales files have been found. Only one is necessary");
                for (int i = 0; i < objects.Length; i++)
                {
                    Debug.Log("Scales file " + (i + 1) + " : " + AssetDatabase.GetAssetPath(objects[i]));
                }
                return (Scales)objects[0];
            }
            else if (objects.Length == 0)
            {
                Debug.Log("Creating Scales settings file for Better Inspector.");
                Scales asset = ScriptableObject.CreateInstance<Scales>();
                AssetDatabase.CreateAsset(asset, fileLocation);
                AssetDatabase.SaveAssets();
                Debug.Log("Scales setting has been successfully created: " + fileLocation);
                return asset;
            }
            else
            {
                return (Scales)objects[0];
            }
        }
    }
}
#endif