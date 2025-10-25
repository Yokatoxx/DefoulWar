using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Génère automatiquement un modèle d'arme 3D simple.
    /// </summary>
    public class WeaponModelGenerator : MonoBehaviour
    {
        [Header("Weapon Visual Settings")]
        [SerializeField] private Color weaponColor = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color barrelColor = new Color(0.1f, 0.1f, 0.1f);
        
        /// <summary>
        /// Crée un modèle d'arme simple à partir de primitives (sans VFX).
        /// </summary>
        public static GameObject CreateSimpleWeaponModel(Transform parent)
        {
            GameObject weaponRoot = new GameObject("WeaponModel");
            weaponRoot.transform.SetParent(parent);
            weaponRoot.transform.localPosition = new Vector3(0.3f, -0.3f, 0.5f);
            weaponRoot.transform.localRotation = Quaternion.identity;
            weaponRoot.transform.localScale = Vector3.one;
            
            // Corps principal (receiver)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(weaponRoot.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.1f, 0.15f, 0.4f);
            
            Renderer bodyRenderer = body.GetComponent<Renderer>();
            Material bodyMat = new Material(Shader.Find("Standard"));
            bodyMat.color = new Color(0.2f, 0.2f, 0.2f);
            bodyRenderer.material = bodyMat;
            
            // Enlever le collider (pas nécessaire pour le modèle visuel)
            Object.Destroy(body.GetComponent<Collider>());
            
            // Canon (barrel)
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(weaponRoot.transform);
            barrel.transform.localPosition = new Vector3(0, 0, 0.3f);
            barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
            barrel.transform.localScale = new Vector3(0.03f, 0.15f, 0.03f);
            
            Renderer barrelRenderer = barrel.GetComponent<Renderer>();
            Material barrelMat = new Material(Shader.Find("Standard"));
            barrelMat.color = new Color(0.1f, 0.1f, 0.1f);
            barrelMat.SetFloat("_Metallic", 0.8f);
            barrelRenderer.material = barrelMat;
            
            Object.Destroy(barrel.GetComponent<Collider>());
            
            // Poignée (grip)
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(weaponRoot.transform);
            grip.transform.localPosition = new Vector3(0, -0.15f, -0.05f);
            grip.transform.localRotation = Quaternion.Euler(15, 0, 0);
            grip.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
            
            Renderer gripRenderer = grip.GetComponent<Renderer>();
            gripRenderer.material = bodyMat;
            
            Object.Destroy(grip.GetComponent<Collider>());
            
            // Point de tir
            GameObject muzzlePoint = new GameObject("MuzzlePoint");
            muzzlePoint.transform.SetParent(weaponRoot.transform);
            muzzlePoint.transform.localPosition = new Vector3(0, 0, 0.45f);
            
            return weaponRoot;
        }
    }
}
