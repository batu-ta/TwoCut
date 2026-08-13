using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HairSalonGame
{
    public class SwapPlayerModel : EditorWindow
    {
        [MenuItem("TwoCut/Swap Player Model")]
        public static void SwapModel()
        {
            // Find the active scene GameObject
            GameObject playerObj = GameObject.Find("Hairdresser Player");
            if (playerObj == null)
            {
                Debug.LogError("GameObject 'Hairdresser Player' not found in the active scene! Make sure TwoCutMainScene is open.");
                return;
            }

            // Find the FBX model
            string modelPath = "Assets/Prefabs/Hairdresser player.fbx";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelPrefab == null)
            {
                Debug.LogError($"Model asset not found at {modelPath}!");
                return;
            }

            // Check if it already has a child model
            Transform existingChild = playerObj.transform.Find("Hairdresser player");
            if (existingChild != null)
            {
                Debug.LogWarning("Hairdresser player model is already a child of the player. Removing existing one first.");
                Undo.DestroyObjectImmediate(existingChild.gameObject);
            }

            // Disable MeshRenderer on the parent (Capsule) so it is no longer visible
            MeshRenderer parentRenderer = playerObj.GetComponent<MeshRenderer>();
            if (parentRenderer != null)
            {
                Undo.RecordObject(parentRenderer, "Disable parent MeshRenderer");
                parentRenderer.enabled = false;
                Debug.Log("Disabled parent (Capsule) MeshRenderer.");
            }

            // Instantiate model as a child
            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            Undo.RegisterCreatedObjectUndo(modelInstance, "Instantiate Player Model");
            modelInstance.name = "Hairdresser player";
            modelInstance.transform.SetParent(playerObj.transform);
            
            // Reset local transform
            modelInstance.transform.localPosition = new Vector3(0, -1f, 0); // Offset Y by -1 to align foot with collider base
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            // Make sure the collider is still configured properly on the parent
            CapsuleCollider capsuleCol = playerObj.GetComponent<CapsuleCollider>();
            if (capsuleCol != null)
            {
                Debug.Log("CapsuleCollider found on parent. Keeping it for physics movement.");
            }

            // Mark scene as dirty so it can be saved
            EditorSceneManager.MarkSceneDirty(playerObj.scene);
            Debug.Log("Player model swapped successfully! Please save your scene (Ctrl+S).");
        }
    }
}
