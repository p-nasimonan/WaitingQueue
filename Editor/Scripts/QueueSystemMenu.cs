using UnityEditor;
using UnityEngine;

namespace Youkan.WaitingQueue.Editor
{
    /// <summary>
    /// 待機列システムのメニュー項目を追加します。
    /// </summary>
    public static class QueueSystemMenu
    {
        private const string MenuPrefix = "GameObject/Waiting Queue/";
        
        // PrefabのGUID（Prefab作成後に設定する必要があります）
        private static string _queueSystemPrefabGuid = "PLACEHOLDER_GUID"; // TODO: 実際のGUIDに置き換える
        
        [MenuItem(MenuPrefix + "Queue System", priority = 1)]
        public static void CreateQueueSystem()
        {
            // GUIDが未設定の場合は警告
            if (_queueSystemPrefabGuid == "PLACEHOLDER_GUID")
            {
                EditorUtility.DisplayDialog(
                    "Prefab Not Found",
                    "QueueSystem Prefab has not been created yet.\n\nPlease follow the setup instructions in SETUP.md to create the prefab manually first.",
                    "OK"
                );
                return;
            }
            
            CreateGameObject(AssetDatabase.GUIDToAssetPath(_queueSystemPrefabGuid));
        }
        
        [MenuItem(MenuPrefix + "Create Prefab from Selection", priority = 100)]
        public static void CreatePrefabFromSelection()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a GameObject in the Hierarchy first.", "OK");
                return;
            }
            
            // Prefabの保存先
            string prefabPath = "Packages/uk.youkan.waiting-queue/Prefabs/QueueSystem.prefab";
            
            // Prefabとして保存
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(selected, prefabPath);
            
            if (prefab != null)
            {
                // GUIDを取得
                string guid = AssetDatabase.AssetPathToGUID(prefabPath);
                
                Debug.Log($"[QueueSystemMenu] Prefab created successfully!");
                Debug.Log($"[QueueSystemMenu] Path: {prefabPath}");
                Debug.Log($"[QueueSystemMenu] GUID: {guid}");
                Debug.Log($"[QueueSystemMenu] Please update the _queueSystemPrefabGuid in QueueSystemMenu.cs with this GUID.");
                
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Prefab created successfully!\n\nGUID: {guid}\n\nPlease copy this GUID and update _queueSystemPrefabGuid in QueueSystemMenu.cs",
                    "OK"
                );
                
                // Prefabを選択状態にする
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to create prefab.", "OK");
            }
        }
        
        private static void CreateGameObject(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[QueueSystemMenu] Invalid prefab path.");
                return;
            }
            
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogError($"[QueueSystemMenu] Could not load prefab at path: {path}");
                return;
            }

            Transform parent = Selection.activeTransform;
            GameObject obj = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (obj == null)
            {
                Debug.LogError("[QueueSystemMenu] Failed to instantiate prefab.");
                return;
            }

            obj.name = GameObjectUtility.GetUniqueNameForSibling(parent, prefab.name);
            Selection.activeGameObject = obj;
            
            Debug.Log($"[QueueSystemMenu] Created {obj.name} in the scene.");
        }
    }
}
