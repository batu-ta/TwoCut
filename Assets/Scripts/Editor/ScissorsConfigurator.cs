#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HairSalonGame;

namespace TwoCutGame.EditorTools
{
    /// <summary>
    /// Automatically searches the scene for any imported Scissors model (e.g. "Scissors", "Mavi Makas", "Blue Scissors")
    /// and configures only the ROOT object of the model. 
    /// Cleans up child components to prevent physics duplication ("exploding/splitting" bug).
    /// </summary>
    [InitializeOnLoad]
    public static class ScissorsConfigurator
    {
        static ScissorsConfigurator()
        {
            EditorApplication.delayCall += AutoConfigureScissors;
        }

        [MenuItem("TwoCut/Configure Blue Scissors in Scene")]
        public static void ConfigureScissorsMenu()
        {
            AutoConfigureScissors();
        }

        private static void AutoConfigureScissors()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            bool configuredAny = false;

            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                string nameLower = obj.name.ToLower();
                
                // Match names containing "scissors", "makas", or "scissor"
                if (nameLower.Contains("scissors") || nameLower.Contains("makas") || nameLower.Contains("scissor"))
                {
                    // Skip camera and light objects
                    if (obj.GetComponent<Camera>() != null || obj.GetComponent<Light>() != null) continue;

                    // Hata Önleme: Eğer bu objenin üst ebeveynlerinin adında da "scissors/makas" geçiyorsa,
                    // bu nesne aslında makasın içindeki bir bıçak, sap veya alt parçadır. Bunu atla!
                    if (IsChildPartOfScissorsModel(obj)) continue;

                    // --- BU NESNE ANA (ROOT) MAKAS NESNESİDİR ---

                    // 1. Alt parçalardaki (çocuklardaki) çakışan fizik bileşenlerini temizle (Patlama hatasını çözer!)
                    CleanChildPhysicsComponents(obj);

                    // 2. Add/Configure SalonItem
                    SalonItem item = obj.GetComponent<SalonItem>();
                    if (item == null)
                    {
                        item = obj.AddComponent<SalonItem>();
                        item.itemName = obj.name;
                    }
                    item.itemType = ItemType.Scissors;

                    // 3. Add & Auto-Size BoxCollider to fit all child meshes
                    BoxCollider boxCol = obj.GetComponent<BoxCollider>();
                    if (boxCol == null)
                    {
                        boxCol = obj.AddComponent<BoxCollider>();
                    }
                    AutoSizeColliderToChildren(obj, boxCol);

                    // 4. Add & Configure Rigidbody
                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = obj.AddComponent<Rigidbody>();
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        Debug.Log($"[TwoCut Config] '{obj.name}' ana makas objesine Rigidbody eklendi.");
                    }
                    rb.useGravity = true;
                    rb.isKinematic = false;

                    configuredAny = true;
                    Debug.Log($"✨ [TwoCut Config] Hiyerarşideki ana model '{obj.name}' başarıyla tek parça ve oynanabilir hale getirildi!");
                }
            }

            if (configuredAny)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }

        private static bool IsChildPartOfScissorsModel(GameObject obj)
        {
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                string parentNameLower = parent.name.ToLower();
                if (parentNameLower.Contains("scissors") || parentNameLower.Contains("makas") || parentNameLower.Contains("scissor"))
                {
                    return true;
                }
                parent = parent.parent;
            }
            return false;
        }

        private static void CleanChildPhysicsComponents(GameObject rootObj)
        {
            // Ana obje dışındaki tüm alt çocukları tara ve üzerlerindeki çarpışan bileşenleri yok et
            foreach (Transform child in rootObj.GetComponentsInChildren<Transform>())
            {
                if (child == rootObj.transform) continue;

                Rigidbody childRb = child.GetComponent<Rigidbody>();
                if (childRb != null)
                {
                    Object.DestroyImmediate(childRb);
                    Debug.Log($"[TwoCut Config] Alt parça '{child.name}' üzerindeki çakışan Rigidbody temizlendi.");
                }

                Collider childCol = child.GetComponent<Collider>();
                if (childCol != null)
                {
                    Object.DestroyImmediate(childCol);
                    Debug.Log($"[TwoCut Config] Alt parça '{child.name}' üzerindeki çakışan Collider temizlendi.");
                }

                SalonItem childItem = child.GetComponent<SalonItem>();
                if (childItem != null)
                {
                    Object.DestroyImmediate(childItem);
                }
            }
        }

        private static void AutoSizeColliderToChildren(GameObject rootObj, BoxCollider boxCol)
        {
            Renderer[] renderers = rootObj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }

                // Dünya koordinatlarındaki sınırları yerel koordinatlara çevir
                Vector3 localCenter = rootObj.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = rootObj.transform.InverseTransformVector(bounds.size);

                localSize.x = Mathf.Abs(localSize.x);
                localSize.y = Mathf.Abs(localSize.y);
                localSize.z = Mathf.Abs(localSize.z);

                // Eğer model çok ince ise colliderın algılanabilmesi için minimum kalınlık veriyoruz
                if (localSize.x < 0.1f) localSize.x = 0.2f;
                if (localSize.y < 0.1f) localSize.y = 0.2f;
                if (localSize.z < 0.1f) localSize.z = 0.2f;

                boxCol.center = localCenter;
                boxCol.size = localSize;
                Debug.Log($"[TwoCut Config] '{rootObj.name}' BoxCollider boyutu alt modellere göre otomatik ayarlandı.");
            }
        }
    }
}
#endif
