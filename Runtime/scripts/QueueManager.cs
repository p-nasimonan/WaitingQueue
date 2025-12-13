・ｿusing UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// 蠕・ｩ溷・邂｡逅・す繧ｹ繝・Β縺ｮ繧ｳ繧｢繝ｭ繧ｸ繝・け繧呈球蠖薙＠縺ｾ縺吶・    /// 繝励Ξ繧､繝､繝ｼ縺ｮ繧ｨ繝ｳ繝医Μ繝ｼ縲√Μ繧ｹ繝亥酔譛溘・夂衍繧､繝吶Φ繝育ｮ｡逅・ｒ陦後＞縺ｾ縺吶・    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class QueueManager : UdonSharpBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxQueueSize = 50;

        [Header("UI References")]
        [SerializeField] private QueueUIManager uiManager;
        [SerializeField] private QueueNotificationManager notificationManager;

        [Header("Synchronized Data")]
        [UdonSynced] private string[] queuedPlayerIds = new string[0];
        [UdonSynced] private string[] queuedPlayerNames = new string[0];
        [UdonSynced] private string lastCalledPlayerId = "";
        [UdonSynced] private int syncCounter = 0;

        [Header("Local State")]
        private VRCPlayerApi localPlayer;
        private int lastKnownSyncCounter = -1;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            
            if (localPlayer == null)
            {
                Debug.LogError("[QueueManager] Local player is null!");
                enabled = false;
                return;
            }

            // 繧ｪ繝ｼ繝翫・縺ｮ蝣ｴ蜷医・縺ｿ蛻晄悄蛹・            if (Networking.IsOwner(gameObject))
            {
                queuedPlayerIds = new string[0];
                queuedPlayerNames = new string[0];
                lastCalledPlayerId = "";
                syncCounter = 0;
            }

            UpdateUI();
        }

        /// <summary>
        /// 繝励Ξ繧､繝､繝ｼ縺悟ｾ・ｩ溷・縺ｫ蜈･繧・謚懊￠繧九ｒ繝医げ繝ｫ縺励∪縺吶・        /// </summary>
        public void ToggleQueue()
        {
            if (localPlayer == null) return;

            int existingIndex = GetPlayerQueueIndex(localPlayer.playerId);
            
            if (existingIndex >= 0)
            {
                // 譌｢縺ｫ蛻励↓縺・ｋ蝣ｴ蜷医・謚懊￠繧・                LeaveQueue();
            }
            else
            {
                // 蛻励↓縺・↑縺・ｴ蜷医・蜈･繧・                EnqueuePlayer();
            }
        }

        /// <summary>
        /// 繧ｲ繧ｹ繝医′縺薙・繝｡繧ｽ繝・ラ繧貞他縺ｳ蜃ｺ縺吶％縺ｨ縺ｧ蠕・ｩ溷・縺ｫ逋ｻ骭ｲ縺励∪縺吶・        /// </summary>
        public void EnqueuePlayer()
        {
            if (localPlayer == null)
            {
                Debug.LogWarning("[QueueManager] Local player is not set.");
                return;
            }

            // 譌｢縺ｫ逋ｻ骭ｲ貂医∩縺九メ繧ｧ繝・け
            int existingIndex = GetPlayerQueueIndex(localPlayer.playerId);
            if (existingIndex >= 0)
            {
                Debug.LogWarning($"[QueueManager] Player {localPlayer.displayName} is already in the queue.");
                return;
            }

            // 繧ｭ繝･繝ｼ繧ｵ繧､繧ｺ繝√ぉ繝・け
            if (queuedPlayerIds.Length >= maxQueueSize)
            {
                Debug.LogWarning("[QueueManager] Queue is full.");
                return;
            }

            // 繝励Ξ繧､繝､繝ｼID縺ｨ蜷榊燕繧偵Μ繧ｹ繝医↓霑ｽ蜉
            AddToQueue(localPlayer.playerId, localPlayer.displayName);

            // 蜈ｨ繧ｯ繝ｩ繧､繧｢繝ｳ繝医↓蜷梧悄
            RequestSerialization();
            
            // UI譖ｴ譁ｰ
            UpdateUI();
        }

        /// <summary>
        /// 蠕・ｩ溷・縺ｫ繝励Ξ繧､繝､繝ｼ繧定ｿｽ蜉縺励∪縺呻ｼ亥・驛ｨ繝｡繧ｽ繝・ラ・峨・        /// </summary>
        private void AddToQueue(int playerId, string playerName)
        {
            int newLength = queuedPlayerIds.Length + 1;
            
            // 驟榊・繧呈僑蠑ｵ
            string[] newIds = new string[newLength];
            string[] newNames = new string[newLength];

            // 譌｢蟄倥・繝・・繧ｿ繧偵さ繝斐・
            for (int i = 0; i < queuedPlayerIds.Length; i++)
            {
                newIds[i] = queuedPlayerIds[i];
                newNames[i] = queuedPlayerNames[i];
            }

            // 譁ｰ縺励＞繝励Ξ繧､繝､繝ｼ繧定ｿｽ蜉
            newIds[newLength - 1] = playerId.ToString();
            newNames[newLength - 1] = playerName;

            // 驟榊・繧呈峩譁ｰ
            queuedPlayerIds = newIds;
            queuedPlayerNames = newNames;

            Debug.Log($"[QueueManager] Player {playerName} (ID: {playerId}) has been added to the queue. Position: {newLength}");
        }

        /// <summary>
        /// 繝励Ξ繧､繝､繝ｼ縺悟ｾ・ｩ溷・蜀・・縺ｩ縺ｮ菴咲ｽｮ縺ｫ縺・ｋ縺九ｒ蜿門ｾ励＠縺ｾ縺吶・        /// 隕九▽縺九ｉ縺ｪ縺・ｴ蜷医・ -1 繧定ｿ斐＠縺ｾ縺吶・        /// </summary>
        public int GetPlayerQueueIndex(int playerId)
        {
            string playerIdStr = playerId.ToString();
            
            for (int i = 0; i < queuedPlayerIds.Length; i++)
            {
                if (queuedPlayerIds[i] == playerIdStr)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 繝励Ξ繧､繝､繝ｼ縺檎樟蝨ｨ菴慕分逶ｮ蠕・■縺九ｒ遒ｺ隱阪＠縺ｾ縺呻ｼ・繝吶・繧ｹ・峨・        /// 0 繧定ｿ斐＠縺溷ｴ蜷医・繝励Ξ繧､繝､繝ｼ縺後く繝･繝ｼ縺ｫ逋ｻ骭ｲ縺輔ｌ縺ｦ縺・∪縺帙ｓ縲・        /// </summary>
        public int GetPlayerQueuePosition()
        {
            int index = GetPlayerQueueIndex(localPlayer.playerId);
            return index >= 0 ? index + 1 : 0;
        }

        /// <summary>
        /// 迴ｾ蝨ｨ縺ｮ蠕・ｩ溷・縺ｫ逋ｻ骭ｲ縺輔ｌ縺ｦ縺・ｋ繝励Ξ繧､繝､繝ｼ謨ｰ繧貞叙蠕励＠縺ｾ縺吶・        /// </summary>
        public int GetQueueLength()
        {
            return queuedPlayerIds.Length;
        }

        /// <summary>
        /// 繧ｪ繝ｼ繝翫・縺梧ｬ｡縺ｮ繝励Ξ繧､繝､繝ｼ縺ｫ騾ｲ繧√ｋ蜃ｦ逅・ｒ螳溯｡後＠縺ｾ縺吶・        /// 蠕・ｩ溷・縺ｮ蜈磯ｭ繧貞炎髯､縺励∵ｬ｡縺ｮ繝励Ξ繧､繝､繝ｼ縺ｫ騾夂衍繧帝√ｊ縺ｾ縺吶・        /// </summary>
        public void AdvanceQueue()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Debug.LogWarning("[QueueManager] Only the owner can advance the queue.");
                return;
            }

            if (queuedPlayerIds.Length == 0)
            {
                Debug.LogWarning("[QueueManager] Queue is empty.");
                return;
            }

            // 蜻ｼ縺ｳ蜃ｺ縺輔ｌ縺溘・繝ｬ繧､繝､繝ｼ縺ｮID繧定ｨ倬鹸
            lastCalledPlayerId = queuedPlayerIds[0];
            
            // 蜷梧悄繧ｫ繧ｦ繝ｳ繧ｿ繝ｼ繧貞｢励ｄ縺励※騾夂衍繧偵ヨ繝ｪ繧ｬ繝ｼ
            syncCounter++;

            // 蜈磯ｭ繧貞炎髯､
            RemoveFromQueueAt(0);

            // 蜈ｨ繧ｯ繝ｩ繧､繧｢繝ｳ繝医↓蜷梧悄
            RequestSerialization();
            
            // UI譖ｴ譁ｰ
            UpdateUI();
        }

        /// <summary>
        /// 謖・ｮ壹＆繧後◆繧､繝ｳ繝・ャ繧ｯ繧ｹ縺ｮ繝励Ξ繧､繝､繝ｼ繧貞ｾ・ｩ溷・縺九ｉ蜑企勁縺励∪縺呻ｼ亥・驛ｨ繝｡繧ｽ繝・ラ・峨・        /// </summary>
        private void RemoveFromQueueAt(int index)
        {
            if (index < 0 || index >= queuedPlayerIds.Length)
            {
                Debug.LogWarning($"[QueueManager] Invalid index: {index}");
                return;
            }

            string removedPlayerName = queuedPlayerNames[index];
            string removedPlayerId = queuedPlayerIds[index];

            int newLength = queuedPlayerIds.Length - 1;

            if (newLength == 0)
            {
                queuedPlayerIds = new string[0];
                queuedPlayerNames = new string[0];
            }
            else
            {
                string[] newIds = new string[newLength];
                string[] newNames = new string[newLength];

                // 蜑企勁縺輔ｌ繧九う繝ｳ繝・ャ繧ｯ繧ｹ繧医ｊ蜑阪・繝・・繧ｿ繧偵さ繝斐・
                for (int i = 0; i < index; i++)
                {
                    newIds[i] = queuedPlayerIds[i];
                    newNames[i] = queuedPlayerNames[i];
                }

                // 蜑企勁縺輔ｌ繧九う繝ｳ繝・ャ繧ｯ繧ｹ繧医ｊ蠕後ｍ縺ｮ繝・・繧ｿ繧偵さ繝斐・
                for (int i = index + 1; i < queuedPlayerIds.Length; i++)
                {
                    newIds[i - 1] = queuedPlayerIds[i];
                    newNames[i - 1] = queuedPlayerNames[i];
                }

                queuedPlayerIds = newIds;
                queuedPlayerNames = newNames;
            }

            Debug.Log($"[QueueManager] Player {removedPlayerName} (ID: {removedPlayerId}) has been removed from the queue.");
        }

        /// <summary>
        /// 繝励Ξ繧､繝､繝ｼ縺悟ｾ・ｩ溷・縺九ｉ謇句虚縺ｧ閼ｱ蜃ｺ縺励∪縺吶・        /// </summary>
        public void LeaveQueue()
        {
            if (localPlayer == null) return;

            int index = GetPlayerQueueIndex(localPlayer.playerId);
            if (index >= 0)
            {
                RemoveFromQueueAt(index);
                RequestSerialization();
                UpdateUI();
                Debug.Log($"[QueueManager] Player {localPlayer.displayName} has left the queue.");
            }
            else
            {
                Debug.LogWarning("[QueueManager] Player is not in the queue.");
            }
        }

        /// <summary>
        /// 迴ｾ蝨ｨ縺ｮ蠕・ｩ溷・縺ｮ諠・ｱ繧偵ョ繝舌ャ繧ｰ繝ｭ繧ｰ縺ｫ蜃ｺ蜉帙＠縺ｾ縺吶・        /// </summary>
        public void PrintQueueInfo()
        {
            if (queuedPlayerIds.Length == 0)
            {
                Debug.Log("[QueueManager] Queue is empty.");
                return;
            }

            string queueInfo = "[QueueManager] Current Queue:\n";
            for (int i = 0; i < queuedPlayerIds.Length; i++)
            {
                queueInfo += $"  {i + 1}. {queuedPlayerNames[i]} (ID: {queuedPlayerIds[i]})\n";
            }
            Debug.Log(queueInfo);
        }

        /// <summary>
        /// UdonEvent繧帝壹§縺ｦ繝・・繧ｿ蜿嶺ｿ｡譎ゅ・蜃ｦ逅・        /// </summary>
        public override void OnDeserialization()
        {
            Debug.Log("[QueueManager] Queue data has been synchronized.");
            
            // 蜷梧悄繧ｫ繧ｦ繝ｳ繧ｿ繝ｼ縺悟､画峩縺輔ｌ縺溷ｴ蜷医・夂衍繧､繝吶Φ繝医ｒ繝√ぉ繝・け
            if (syncCounter != lastKnownSyncCounter && lastCalledPlayerId != "")
            {
                lastKnownSyncCounter = syncCounter;
                
                // 騾夂衍繝槭ロ繝ｼ繧ｸ繝｣繝ｼ縺ｫ騾夂衍繧帝∽ｿ｡
                if (notificationManager != null)
                {
                    notificationManager.TriggerNotification(lastCalledPlayerId);
                }
            }
            
            // UI譖ｴ譁ｰ
            UpdateUI();
        }

        /// <summary>
        /// UI繝槭ロ繝ｼ繧ｸ繝｣繝ｼ縺ｫ繝・・繧ｿ繧帝∽ｿ｡縺励※譖ｴ譁ｰ縺励∪縺吶・        /// </summary>
        private void UpdateUI()
        {
            if (localPlayer == null)
            {
                Debug.LogWarning("[QueueManager] Cannot update UI, local player is null.");
                return;
            }

            if (uiManager != null)
            {
                bool isInQueue = IsPlayerInQueue();
                uiManager.UpdateQueueDisplay(queuedPlayerNames, queuedPlayerIds, localPlayer.playerId, isInQueue);
            }
        }

        /// <summary>
        /// 繝ｭ繝ｼ繧ｫ繝ｫ繝励Ξ繧､繝､繝ｼ縺悟ｾ・ｩ溷・縺ｫ蜈･縺｣縺ｦ縺・ｋ縺九ｒ遒ｺ隱阪＠縺ｾ縺吶・        /// </summary>
        public bool IsPlayerInQueue()
        {
            if (localPlayer == null) return false;
            return GetPlayerQueueIndex(localPlayer.playerId) >= 0;
        }

        /// <summary>
        /// 蠕・ｩ溷・繝・・繧ｿ繧貞叙蠕励＠縺ｾ縺呻ｼ亥､夜Κ蜿ら・逕ｨ・峨・        /// </summary>
        public string[] GetQueuePlayerNames() => queuedPlayerNames;
        public string[] GetQueuePlayerIds() => queuedPlayerIds;
    }
}
