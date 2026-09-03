#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HairSalonGame;

namespace TwoCutGame.EditorTools
{
    /// <summary>
    /// Professional Editor Tool for TwoCut.
    /// Auto-configures, spawns and sets up all Salon Tables (masa.fbx), Haircut Chairs (koltuk.fbx),
    /// Wash Sinks (bb1.fbx), Massage Chairs (mc.fbx), and Scissors with accurate physics colliders.
    /// </summary>
    [InitializeOnLoad]
    public static class ScissorsConfigurator
    {
        static ScissorsConfigurator()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                {
                    AutoConfigureEntireSalon();
                }
            };
        }

        [MenuItem("TwoCut/1. Masaları, Koltukları ve İstasyonları Sahneye Kur & Kalibre Et")]
        public static void SetupAndSpawnSalonObjectsMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) return;
            SetupAndSpawnSalonObjects();
            AutoConfigureEntireSalon();
        }

        [MenuItem("TwoCut/2. Sadece Fizik & İstasyonları Kalibre Et")]
        public static void ConfigureMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) return;
            AutoConfigureEntireSalon();
        }

        public static void SetupAndSpawnSalonObjects()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) return;

            var activeScene = EditorSceneManager.GetActiveScene();

            // Find or Create Salon Hierarchy
            GameObject salonRoot = GameObject.Find("=== TWO CUT SALON ===");
            if (salonRoot == null)
            {
                salonRoot = new GameObject("=== TWO CUT SALON ===");
            }

            // 1. Setup Solid Boundary Perimeter Walls & Floor (Prevents falling into void/walls)
            CreateOrUpdateBoundary(salonRoot, "Boundary - Back Wall", new Vector3(-3.5f, 2f, 5.8f), new Vector3(32f, 4f, 1f));
            CreateOrUpdateBoundary(salonRoot, "Boundary - Front Wall", new Vector3(-3.5f, 2f, -9.5f), new Vector3(32f, 4f, 1f));
            CreateOrUpdateBoundary(salonRoot, "Boundary - Left Wall", new Vector3(-19.2f, 2f, -1.8f), new Vector3(1f, 4f, 16f));
            CreateOrUpdateBoundary(salonRoot, "Boundary - Right Wall", new Vector3(12.2f, 2f, -1.8f), new Vector3(1f, 4f, 16f));
            CreateOrUpdateBoundary(salonRoot, "Boundary - Main Floor", new Vector3(-3.5f, -0.5f, -1.8f), new Vector3(32f, 1f, 16f));

            // 2. Clean up old placeholder cubes that were trapping the player
            string[] obsoleteNames = { "Wall Left", "Wall Right", "LeftWall", "BackWall", "Floor" };
            foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (obj == null) continue;
                foreach (string obName in obsoleteNames)
                {
                    if (obj.name == obName && obj.GetComponent<MeshFilter>() != null && obj.GetComponentInParent<Transform>() != null)
                    {
                        Collider c = obj.GetComponent<Collider>();
                        if (c != null) c.enabled = false;
                        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
                        if (mr != null) mr.enabled = false;
                    }
                }
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
            }
        }

        private static void CreateOrUpdateBoundary(GameObject parent, string name, Vector3 pos, Vector3 size)
        {
            Transform existing = parent.transform.Find(name);
            GameObject boundObj;
            if (existing == null)
            {
                boundObj = new GameObject(name);
                boundObj.transform.SetParent(parent.transform);
            }
            else
            {
                boundObj = existing.gameObject;
            }

            boundObj.transform.localPosition = pos;
            boundObj.transform.localRotation = Quaternion.identity;
            boundObj.transform.localScale = Vector3.one;

            BoxCollider col = boundObj.GetComponent<BoxCollider>();
            col ??= boundObj.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.size = size;
            col.center = Vector3.zero;
        }

        private static void AutoConfigureEntireSalon()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) return;

            var activeScene = EditorSceneManager.GetActiveScene();
            bool dirty = false;

            PhysicsMaterial stableMaterial = GetOrCreatePhysicsMaterial();

            if (ConfigureSalonBuildingColliders()) dirty = true;
            if (ConfigureScissorsInScene(stableMaterial)) dirty = true;
            if (ConfigureStationsInScene()) dirty = true;
            if (ConfigureGameManagerInScene()) dirty = true;

            if (dirty && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                Debug.Log("✨ [TwoCut Setup] Kuaför zemin basamakları, gerçek 3D MeshCollider ve mobilyalar başarıyla kalibre edildi!");
            }
        }

        private static bool ConfigureSalonBuildingColliders()
        {
            bool dirty = false;

            // 1. Clean up old blocking placeholder cubes and artificial boundary boxes that were trapping the player
            string[] obsoleteNames = { "Wall Left", "Wall Right", "LeftWall", "BackWall", "Floor", "Boundary - Back Wall", "Boundary - Front Wall", "Boundary - Left Wall", "Boundary - Right Wall", "Boundary - Main Floor" };
            foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (obj == null) continue;
                foreach (string obName in obsoleteNames)
                {
                    if (obj.name == obName)
                    {
                        Collider c = obj.GetComponent<Collider>();
                        if (c != null)
                        {
                            Object.DestroyImmediate(c);
                            dirty = true;
                        }
                        MeshRenderer mr = obj.GetComponent<MeshRenderer>();
                        if (mr != null && !obName.StartsWith("Boundary"))
                        {
                            mr.enabled = false;
                            dirty = true;
                        }
                    }
                }

                // Masa / Table objelerindeki tüm collider'ları kaldır (Karakterin masaya rahatça yanaşabilmesi için)
                string nameLower = obj.name.ToLower();
                if (nameLower.Contains("masa") || nameLower == "table")
                {
                    foreach (Collider c in obj.GetComponents<Collider>())
                    {
                        if (c != null)
                        {
                            Object.DestroyImmediate(c);
                            dirty = true;
                        }
                    }
                }
            }

            // 2. Setup real 3D MeshCollider on salontemel (exact floor, platform, stairs, and outer walls)
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                if (obj.name.ToLower().Contains("salontemel"))
                {
                    // Remove any flat BoxCollider
                    BoxCollider box = obj.GetComponent<BoxCollider>();
                    if (box != null)
                    {
                        Object.DestroyImmediate(box);
                        dirty = true;
                    }

                    // Add MeshCollider to all mesh filters
                    foreach (MeshFilter mf in obj.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (mf.sharedMesh != null)
                        {
                            MeshCollider mc = mf.GetComponent<MeshCollider>();
                            mc ??= mf.gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                            mc.convex = false;
                            dirty = true;
                        }
                    }
                }
            }

            return dirty;
        }

        private static PhysicsMaterial GetOrCreatePhysicsMaterial()
        {
            string matPath = "Assets/Materials/TableToolFriction.physicsMaterial";
            PhysicsMaterial mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(matPath);
            if (mat == null)
            {
                mat = new PhysicsMaterial("TableToolFriction")
                {
                    dynamicFriction = 0.8f,
                    staticFriction = 0.9f,
                    bounciness = 0.0f,
                    frictionCombine = PhysicsMaterialCombine.Maximum,
                    bounceCombine = PhysicsMaterialCombine.Minimum
                };

                if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                {
                    AssetDatabase.CreateFolder("Assets", "Materials");
                }
                AssetDatabase.CreateAsset(mat, matPath);
                AssetDatabase.SaveAssets();
            }
            return mat;
        }

        private static bool ConfigureScissorsInScene(PhysicsMaterial mat)
        {
            bool any = false;
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;
                string nameLower = obj.name.ToLower();

                if (nameLower.Contains("scissors") || nameLower.Contains("makas") || nameLower.Contains("scissor"))
                {
                    if (obj.GetComponent<Camera>() != null || obj.GetComponent<Light>() != null) continue;
                    if (IsChildPartOfScissorsModel(obj)) continue;

                    CleanChildPhysicsComponents(obj);

                    SalonItem item = obj.GetComponent<SalonItem>();
                    item ??= obj.AddComponent<SalonItem>();
                    item.itemName = "Mavi Makas";
                    item.itemType = ItemType.Scissors;

                    BoxCollider boxCol = obj.GetComponent<BoxCollider>();
                    boxCol ??= obj.AddComponent<BoxCollider>();
                    boxCol.material = mat;
                    AutoSizeColliderToChildren(obj, boxCol);

                    Rigidbody rb = obj.GetComponent<Rigidbody>();
                    rb ??= obj.AddComponent<Rigidbody>();
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    rb.useGravity = true;
                    rb.isKinematic = false;
                    rb.mass = 0.4f;

                    any = true;
                }
            }
            return any;
        }

        private static bool ConfigureStationsInScene()
        {
            bool any = false;
            SalonStation[] stations = Object.FindObjectsByType<SalonStation>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var station in stations)
            {
                if (station == null) continue;
                string nameLower = station.name.ToLower();

                if (nameLower.Contains("wash") || nameLower.Contains("sink") || nameLower.Contains("lavabo"))
                {
                    station.stationType = StationType.HairWashSink;
                }
                else if (nameLower.Contains("massage") || nameLower.Contains("masaj"))
                {
                    station.stationType = StationType.MassageChair;
                }
                else if (nameLower.Contains("chair") || nameLower.Contains("kesim") || nameLower.Contains("haircut"))
                {
                    station.stationType = StationType.HaircutChair;
                }

                if (station.itemOrCustomerPoint == null)
                {
                    station.itemOrCustomerPoint = station.transform;
                }
                any = true;
            }
            return any;
        }

        private static bool ConfigureGameManagerInScene()
        {
            bool dirty = false;
            SalonGameManager gm = Object.FindFirstObjectByType<SalonGameManager>();
            if (gm != null)
            {
                if (gm.customerPrefab == null)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Müşteri.prefab");
                    if (prefab != null)
                    {
                        gm.customerPrefab = prefab;
                        dirty = true;
                        Debug.Log("[TwoCut Setup] SalonGameManager'a Müşteri.prefab bağlandı.");
                    }
                }

                gm.FindAllStationsInScene();
            }

            TwoCutGameUI ui = Object.FindFirstObjectByType<TwoCutGameUI>();
            if (ui == null && gm != null)
            {
                gm.gameObject.AddComponent<TwoCutGameUI>();
                dirty = true;
            }

            return dirty;
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
            foreach (Transform child in rootObj.GetComponentsInChildren<Transform>())
            {
                if (child == rootObj.transform) continue;

                Rigidbody childRb = child.GetComponent<Rigidbody>();
                if (childRb != null) Object.DestroyImmediate(childRb);

                Collider childCol = child.GetComponent<Collider>();
                if (childCol != null) Object.DestroyImmediate(childCol);

                SalonItem childItem = child.GetComponent<SalonItem>();
                if (childItem != null) Object.DestroyImmediate(childItem);
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

                Vector3 localCenter = rootObj.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = rootObj.transform.InverseTransformVector(bounds.size);

                localSize.x = Mathf.Abs(localSize.x);
                localSize.y = Mathf.Abs(localSize.y);
                localSize.z = Mathf.Abs(localSize.z);

                if (localSize.x < 0.15f) localSize.x = 0.2f;
                if (localSize.y < 0.15f) localSize.y = 0.2f;
                if (localSize.z < 0.15f) localSize.z = 0.2f;

                boxCol.center = localCenter;
                boxCol.size = localSize;
            }
        }
    }
}
#endif
