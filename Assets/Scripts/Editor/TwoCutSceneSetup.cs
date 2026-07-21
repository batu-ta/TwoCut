#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using HairSalonGame;
using TwoCutGame;

namespace TwoCutGame.EditorTools
{
    public static class TwoCutSceneSetup
    {
        [MenuItem("TwoCut/Clean & Build Single Player Scene")]
        public static void BuildPlayableSceneMenu()
        {
            BuildCompletePlayableScene();
        }

        public static void BuildCompletePlayableScene()
        {
            ClearAllSceneObjects();

            var activeScene = EditorSceneManager.GetActiveScene();

            GameObject salonRoot = new GameObject("=== TWO CUT SALON ===");

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Main Salon Floor";
            floor.transform.SetParent(salonRoot.transform);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2.5f, 1f, 2f);
            SetColor(floor, new Color(0.18f, 0.2f, 0.24f));

            GameObject wallLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallLeft.name = "Wall Left";
            wallLeft.transform.SetParent(salonRoot.transform);
            wallLeft.transform.position = new Vector3(0f, 1.5f, 5f);
            wallLeft.transform.localScale = new Vector3(0.4f, 3f, 6f);
            SetColor(wallLeft, new Color(0.85f, 0.8f, 0.75f));

            GameObject wallRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallRight.name = "Wall Right";
            wallRight.transform.SetParent(salonRoot.transform);
            wallRight.transform.position = new Vector3(0f, 1.5f, -5f);
            wallRight.transform.localScale = new Vector3(0.4f, 3f, 6f);
            SetColor(wallRight, new Color(0.85f, 0.8f, 0.75f));

            GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Hairdresser Player";
            playerObj.tag = "Player";
            playerObj.transform.SetParent(salonRoot.transform);
            playerObj.transform.position = new Vector3(-2f, 1f, -1f);
            SetColor(playerObj, new Color(0.95f, 0.45f, 0.15f));

            Rigidbody rb = playerObj.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var playerCtrl = playerObj.AddComponent<HairSalonGame.PlayerController>();
            playerCtrl.isLocalPlayer = true;
            playerCtrl.playerIndex = 1;

            var playerInteract = playerObj.AddComponent<HairSalonGame.PlayerInteraction>();
            playerInteract.isLocalPlayer = true;

            GameObject holdPoint = new GameObject("HoldPoint");
            holdPoint.transform.SetParent(playerObj.transform);
            holdPoint.transform.localPosition = new Vector3(0f, 0.5f, 0.8f);
            playerInteract.holdPoint = holdPoint.transform;

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            mainCam.transform.position = new Vector3(0f, 15f, -13f);
            mainCam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

            var camCtrl = mainCam.gameObject.GetComponent<HairSalonGame.CameraController>();
            if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<HairSalonGame.CameraController>();
            camCtrl.target = playerObj.transform;
            playerCtrl.mainCamera = mainCam;

            GameObject haircutChairObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            haircutChairObj.name = "Room 1 - Haircut Chair";
            haircutChairObj.transform.SetParent(salonRoot.transform);
            haircutChairObj.transform.position = new Vector3(-6f, 0.5f, 4f);
            haircutChairObj.transform.localScale = new Vector3(1.6f, 1f, 1.6f);
            SetColor(haircutChairObj, new Color(0.2f, 0.6f, 0.9f));

            var haircutStation = haircutChairObj.AddComponent<HairSalonGame.SalonStation>();
            haircutStation.roomName = "Oda 1 - Saç Kesim Odası";
            haircutStation.stationType = HairSalonGame.StationType.HaircutChair;

            GameObject washSinkObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            washSinkObj.name = "Room 2 - Wash Sink";
            washSinkObj.transform.SetParent(salonRoot.transform);
            washSinkObj.transform.position = new Vector3(6f, 0.5f, 4f);
            washSinkObj.transform.localScale = new Vector3(1.6f, 1f, 1.6f);
            SetColor(washSinkObj, new Color(0.3f, 0.8f, 0.4f));

            var washStation = washSinkObj.AddComponent<HairSalonGame.SalonStation>();
            washStation.roomName = "Oda 2 - Saç Yıkama Odası";
            washStation.stationType = HairSalonGame.StationType.HairWashSink;

            GameObject massageChairObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            massageChairObj.name = "Room 3 - Massage Chair";
            massageChairObj.transform.SetParent(salonRoot.transform);
            massageChairObj.transform.position = new Vector3(6f, 0.5f, -3f);
            massageChairObj.transform.localScale = new Vector3(1.6f, 1f, 1.6f);
            SetColor(massageChairObj, new Color(0.8f, 0.3f, 0.7f));

            var massageStation = massageChairObj.AddComponent<HairSalonGame.SalonStation>();
            massageStation.roomName = "Oda 3 - Masaj Odası";
            massageStation.stationType = HairSalonGame.StationType.MassageChair;

            GameObject scissorsObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scissorsObj.name = "Scissors (Makas)";
            scissorsObj.transform.SetParent(salonRoot.transform);
            scissorsObj.transform.position = new Vector3(-3f, 0.2f, 1f);
            scissorsObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.6f);
            SetColor(scissorsObj, Color.gold);

            var scissorsItem = scissorsObj.AddComponent<HairSalonGame.SalonItem>();
            scissorsItem.itemType = HairSalonGame.ItemType.Scissors;

            GameObject broomObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            broomObj.name = "Broom (Süpürge)";
            broomObj.transform.SetParent(salonRoot.transform);
            broomObj.transform.position = new Vector3(3f, 0.3f, 1f);
            broomObj.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);
            SetColor(broomObj, Color.yellow);

            var broomItem = broomObj.AddComponent<HairSalonGame.SalonItem>();
            broomItem.itemType = HairSalonGame.ItemType.Broom;

            EnsureManagerObject<TwoCutEconomyManager>("TwoCutEconomyManager");
            EnsureManagerObject<DirtCleanerSystem>("DirtCleanerSystem");
            EnsureManagerObject<TwoCutShopUpgradeManager>("TwoCutShopUpgradeManager");
            EnsureManagerObject<TwoCutGameUI>("TwoCutGameUI");

            var manager = EnsureManagerObject<SalonGameManager>("SalonGameManager");
            manager.availableChairs = new HairSalonGame.SalonStation[] { haircutStation, washStation, massageStation };

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("✨ [TwoCut] Sahne Tek Karakter (WASD) Olacak Şekilde Temizlendi!");
        }

        private static void ClearAllSceneObjects()
        {
            foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (obj.CompareTag("MainCamera") || obj.GetComponent<Light>() != null) continue;
                if (obj.transform.parent == null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }

        private static T EnsureManagerObject<T>(string name) where T : MonoBehaviour
        {
            T instance = Object.FindFirstObjectByType<T>();
            if (instance == null)
            {
                GameObject obj = new GameObject(name);
                instance = obj.AddComponent<T>();
            }
            return instance;
        }

        private static void SetColor(GameObject obj, Color color)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                rend.material = mat;
            }
        }
    }
}
#endif
