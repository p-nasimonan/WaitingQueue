using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine;
using UnityEngine.UI;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// キューに登録するシステムのコアロジックを担当します。
    /// プレイヤーのエントリー、リスト同期、通知イベント管理を行います。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class QueueManager : UdonSharpBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxQueueSize = 50;

        [Header("Manager References")]
        public QueueUIManager uiManager;
        public QueueNotificationManager notificationManager;
        public Button advanceButton;
        public Image advanceButtonImage;
        public Button restoreButton;

        [Header("Synchronized Data")]
        [UdonSynced] private int[] queuedPlayerIds = new int[0];
        [UdonSynced] private string[] queuedPlayerNames = new string[0];
        [UdonSynced] private int lastCalledPlayerId = -1;
        [UdonSynced] private int syncCounter = 0;

        [Header("Local State")]
        private VRCPlayerApi localPlayer;
        private int lastKnownSyncCounter = -1;
        private int lastRemovedPlayerId = -1;
        private string lastRemovedPlayerName = "";

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            
            if (localPlayer == null)
            {
                Debug.LogError("[QueueManager] Local player is null!");
                enabled = false;
                return;
            }

            if (Networking.IsOwner(gameObject))
            {
                queuedPlayerIds = new int[0];
                queuedPlayerNames = new string[0];
                lastCalledPlayerId = -1;
                syncCounter = 0;
            }

            UpdateUI();
        }

        public void OnToggleButtonClick()
        {
            ToggleQueue();
        }

        public void OnAdvanceButtonClick()
        {
            AdvanceQueue();
        }



        /// <summary>
        /// プレイヤーがキューに入れ抜けるをトグルします。
        /// </summary>
        public void ToggleQueue()
        {
            Debug.Log("[QueueManager] ToggleQueue called");
            if (localPlayer == null) return;

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            int existingIndex = GetPlayerQueueIndex(localPlayer.playerId);
            
            if (existingIndex >= 0)
            {
                Debug.Log("[QueueManager] Player is in queue, leaving");
                RemoveFromQueueAt(existingIndex);
                RequestSerialization();
                UpdateUI();
            }
            else
            {
                Debug.Log("[QueueManager] Player not in queue, enqueuing");
                AddToQueue(localPlayer.playerId, localPlayer.displayName);
                RequestSerialization();
                UpdateUI();
            }
        }

        /// <summary>
        /// ゲストがこのメソッドを呼び出すことでキューに登録します。
        /// </summary>
        public void EnqueuePlayer()
        {
            if (localPlayer == null)
            {
                Debug.LogWarning("[QueueManager] Local player is not set.");
                return;
            }

            int existingIndex = GetPlayerQueueIndex(localPlayer.playerId);
            if (existingIndex >= 0)
            {
                Debug.LogWarning($"[QueueManager] Player {localPlayer.displayName} is already in the queue.");
                return;
            }

            if (queuedPlayerIds.Length >= maxQueueSize)
            {
                Debug.LogWarning("[QueueManager] Queue is full.");
                return;
            }

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            AddToQueue(localPlayer.playerId, localPlayer.displayName);
            RequestSerialization();
            UpdateUI();
        }

        /// <summary>
        /// キューにプレイヤーを追加します。
        /// </summary>
        private void AddToQueue(int playerId, string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("[QueueManager] Player name is null or empty.");
                return;
            }

            int newLength = queuedPlayerIds.Length + 1;
            
            int[] newIds = new int[newLength];
            string[] newNames = new string[newLength];

            for (int i = 0; i < queuedPlayerIds.Length; i++)
            {
                newIds[i] = queuedPlayerIds[i];
                newNames[i] = queuedPlayerNames[i];
            }

            newIds[newLength - 1] = playerId;
            newNames[newLength - 1] = playerName;

            queuedPlayerIds = newIds;
            queuedPlayerNames = newNames;

            Debug.Log($"[QueueManager] Player {playerName} (ID: {playerId}) has been added to the queue. Position: {newLength}");
        }

        /// <summary>
        /// プレイヤーがキュー内のどの位置にあるかを取得します。
        /// 見つからない場合は -1 を返します。
        /// </summary>
        public int GetPlayerQueueIndex(int playerId)
        {
            for (int i = 0; i < queuedPlayerIds.Length; i++)
            {
                if (queuedPlayerIds[i] == playerId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// プレイヤーが現在何番目かを確認します。ベース1です。
        /// 0 を返した場合はプレイヤーがキューに登録されていません。
        /// </summary>
        public int GetPlayerQueuePosition()
        {
            int index = GetPlayerQueueIndex(localPlayer.playerId);
            return index >= 0 ? index + 1 : 0;
        }

        /// <summary>
        /// 現在のキューに登録されているプレイヤー数を取得します。
        /// </summary>
        public int GetQueueLength()
        {
            return queuedPlayerIds.Length;
        }

        /// <summary>
        /// オーナーが次のプレイヤーに進める処理を実行します。
        /// </summary>
        public void AdvanceQueue()
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            if (lastCalledPlayerId != -1)
            {
                int lastCalledIndex = GetPlayerQueueIndex(lastCalledPlayerId);
                if (lastCalledIndex >= 0)
                {
                    RemoveFromQueueAt(lastCalledIndex);
                    Debug.Log($"[QueueManager] Removed previously called player (ID: {lastCalledPlayerId}) from queue");
                }
            }

            if (queuedPlayerIds.Length == 0)
            {
                lastCalledPlayerId = -1;
                RequestSerialization();
                UpdateUI();
                Debug.LogWarning("[QueueManager] Queue is now empty.");
                return;
            }

            lastCalledPlayerId = queuedPlayerIds[0];
            syncCounter++;

            RequestSerialization();
            UpdateUI();
        }

        /// <summary>
        /// 指定されたインデックスのプレイヤーをキューから削除します。
        /// </summary>
        private void RemoveFromQueueAt(int index)
        {
            if (index < 0 || index >= queuedPlayerIds.Length)
            {
                Debug.LogWarning($"[QueueManager] Invalid index: {index}");
                return;
            }

            lastRemovedPlayerId = queuedPlayerIds[index];
            lastRemovedPlayerName = queuedPlayerNames[index];

            int newLength = queuedPlayerIds.Length - 1;

            if (newLength == 0)
            {
                queuedPlayerIds = new int[0];
                queuedPlayerNames = new string[0];
            }
            else
            {
                int[] newIds = new int[newLength];
                string[] newNames = new string[newLength];

                for (int i = 0; i < index; i++)
                {
                    newIds[i] = queuedPlayerIds[i];
                    newNames[i] = queuedPlayerNames[i];
                }

                for (int i = index + 1; i < queuedPlayerIds.Length; i++)
                {
                    newIds[i - 1] = queuedPlayerIds[i];
                    newNames[i - 1] = queuedPlayerNames[i];
                }

                queuedPlayerIds = newIds;
                queuedPlayerNames = newNames;
            }

            Debug.Log($"[QueueManager] Player {lastRemovedPlayerName} (ID: {lastRemovedPlayerId}) has been removed from the queue.");
        }

        /// <summary>
        /// プレイヤーがキューから手動で脱出します。
        /// </summary>
        public void LeaveQueue()
        {
            if (localPlayer == null) return;

            int index = GetPlayerQueueIndex(localPlayer.playerId);
            if (index >= 0)
            {
                if (!Networking.IsOwner(gameObject))
                {
                    Networking.SetOwner(localPlayer, gameObject);
                }

                RemoveFromQueueAt(index);
                RequestSerialization();
                UpdateUI();
                Debug.Log($"[QueueManager] Player {localPlayer.displayName} has left the queue.");
            }
        }

        /// <summary>
        /// UdonEventを通じてデータ受信時の処理。
        /// </summary>
        public override void OnDeserialization()
        {
            Debug.Log("[QueueManager] Queue data has been synchronized.");
            
            if (syncCounter != lastKnownSyncCounter && lastCalledPlayerId != -1)
            {
                lastKnownSyncCounter = syncCounter;
                
                if (notificationManager != null)
                {
                    notificationManager.TriggerNotification(lastCalledPlayerId.ToString());
                }
            }
            
            UpdateUI();
        }

        /// <summary>
        /// UIマネージャーにデータを送信して更新します。
        /// </summary>
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
                uiManager.UpdateQueueDisplay(queuedPlayerNames, queuedPlayerIds, localPlayer.playerId, isInQueue, lastCalledPlayerId);
            }
        }

        /// <summary>
        /// ローカルプレイヤーがキューに入っているかを確認します。
        /// </summary>
        public bool IsPlayerInQueue()
        {
            if (localPlayer == null) return false;
            return GetPlayerQueueIndex(localPlayer.playerId) >= 0;
        }

        /// <summary>
        /// 最後に削除したプレイヤーを復元します。
        /// </summary>
        public void RestoreLastRemovedPlayer()
        {
            if (lastRemovedPlayerId == -1)
            {
                Debug.LogWarning("[QueueManager] No player to restore.");
                return;
            }

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            if (GetPlayerQueueIndex(lastRemovedPlayerId) >= 0)
            {
                Debug.LogWarning("[QueueManager] Removed player is already in queue.");
                return;
            }

            if (queuedPlayerIds.Length >= maxQueueSize)
            {
                Debug.LogWarning("[QueueManager] Queue is full. Cannot restore.");
                return;
            }

            AddToQueue(lastRemovedPlayerId, lastRemovedPlayerName);
            lastRemovedPlayerId = -1;
            lastRemovedPlayerName = "";
            
            RequestSerialization();
            UpdateUI();
            Debug.Log($"[QueueManager] Restored player: {lastRemovedPlayerName}");
        }

        /// <summary>
        /// キューデータを取得します（外部参照用）。
        /// </summary>
        public string[] GetQueuePlayerNames() => queuedPlayerNames;
        public int[] GetQueuePlayerIds() => queuedPlayerIds;
    }
}
