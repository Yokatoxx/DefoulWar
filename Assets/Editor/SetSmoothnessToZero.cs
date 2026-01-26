using UnityEditor;
using UnityEngine;

public class SetSmoothnessToZero
{
    [MenuItem("Tools/Materials/Set Smoothness To 0")]
    static void SetAllMaterialsSmoothness()
    {
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        int modifiedCount = 0;

        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat == null)
                continue;

            // Standard / URP
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0f);
                modifiedCount++;
            }

            // HDRP (Lit)
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", 0f);
                modifiedCount++;
            }

            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Smoothness mise à 0 sur {modifiedCount} matériaux.");
    }
}
