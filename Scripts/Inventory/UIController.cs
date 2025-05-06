using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace CustomNamespace
{
    [AddComponentMenu("Custom/Player UI Controller")]
    public class UIController : NetworkBehaviour
    {
        [Header("UI Prefabs (Assigned in Inspector)")]
        public List<GameObject> uiPrefabs; // Assign UI prefabs in Inspector

        [Header("Parent Canvas (Optional)")]
        public Transform uiParent; // Optional: leave null to use default Canvas

        private readonly List<GameObject> spawnedUI = new List<GameObject>();

        public override void OnStartLocalPlayer()
        {
            if (uiPrefabs == null || uiPrefabs.Count == 0)
            {
                Debug.LogWarning("⚠️ UIController: No UI prefabs assigned.");
                return;
            }

            if (uiParent == null)
            {
                Canvas canvas = FindObjectOfType<Canvas>();
                if (canvas != null) uiParent = canvas.transform;
                else Debug.LogError("❌ UIController: No Canvas found in scene!");
            }

            foreach (GameObject prefab in uiPrefabs)
            {
                if (prefab == null) continue;

                GameObject instance = Instantiate(prefab, uiParent);
                instance.SetActive(true);
                spawnedUI.Add(instance);
            }

            Debug.Log($"🖥️ UIController: Spawned {spawnedUI.Count} UI elements.");
        }

        public override void OnStopLocalPlayer()
        {
            foreach (GameObject go in spawnedUI)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            spawnedUI.Clear();
            Debug.Log("🧹 UIController: Cleaned up local player UI.");
        }
    }
}
