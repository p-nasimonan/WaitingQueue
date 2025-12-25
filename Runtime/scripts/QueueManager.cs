using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine;
using UnityEngine.UI;

namespace Youkan.WaitingQueue
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class QueueManager : UdonSharpBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxQueueSize = 50;

        [Header("Manager References")]
        public QueueUIManager uiManager;
        public QueueNotificationManager notificationManager;
        public Button advanceButton;
        public Button restoreButton;

        [Header("Synchronized Data")]
        [UdonSynced] private int[] queuedPlayerIds = new int[0];
        [UdonSynced] private string[] queuedPlayerNames = new string[0];
        [UdonSynced] private int lastCalledPlayerId = -1;
        [UdonSynced] private int syncCounter = 0;
        
        [Header("Local State")]
        private VRCPlayerApi localPlayer;
        private int lastKnownSyncCounter = -1;
        
        // 最後に「削除」された人の履歴
        private int lastRemovedPlayerId = -1;
        private string lastRemovedPlayerName = "";

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (Networking.IsOwner(gameObject))
            {
                queuedPlayerIds = new int[0];
                queuedPlayerNames = new string[0];
                lastCalledPlayerId = -1;
            }
            UpdateUI();
        }

        public void OnToggleButtonClick() => ToggleQueue();
        public void OnAdvanceButtonClick() => AdvanceQueue();
        // UI側で RestoreLastRemovedPlayer を呼ぶように配線済み

        // --------------------------------------
        // コアロジック
        // --------------------------------------

        public void ToggleQueue()
        {
            if (localPlayer == null) return;
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);

            int existingIndex = GetPlayerQueueIndex(localPlayer.playerId);
            
            if (existingIndex >= 0)
            {
                RemoveFromQueueAt(existingIndex);
            }
            else
            {
                AddToQueue(localPlayer.playerId, localPlayer.displayName);
            }
            
            RequestSerialization();
            UpdateUI();
        }

        public void EnqueuePlayer() // 外部呼び出し用
        {
            if (localPlayer == null) return;
            int existingIndex = GetPlayerQueueIndex(localPlayer.playerId);
            if (existingIndex >= 0 || queuedPlayerIds.Length >= maxQueueSize) return;

            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);

            AddToQueue(localPlayer.playerId, localPlayer.displayName);
            RequestSerialization();
            UpdateUI();
        }

        // ★修正ポイント1：呼んだら即座にリストから消す
        public void AdvanceQueue()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);

            if (queuedPlayerIds.Length == 0)
            {
                // 列が空なら呼び出し状態もクリアする？（運用によるが今回はクリアしないでおく）
                // lastCalledPlayerId = -1; 
                // RequestSerialization();
                // UpdateUI();
                return;
            }

            // 1. 次に呼ぶ人を特定
            int nextPlayerId = queuedPlayerIds[0];
            
            // 2. 呼び出し状態を更新
            lastCalledPlayerId = nextPlayerId;
            syncCounter++;

            // 3. ★重要：リストから即座に削除する
            // これで「2回押さないと消えない」現象がなくなります
            // RemoveFromQueueAtメソッド内で lastRemovedPlayerId が更新されるので「戻す」も可能になります
            RemoveFromQueueAt(0);

            RequestSerialization();
            UpdateUI();
        }

        // ★修正ポイント2：最後尾ではなく「先頭」に戻すメソッドを追加
        public void RestoreLastRemovedPlayer()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(localPlayer, gameObject);

            if (lastRemovedPlayerId == -1) return;
            if (queuedPlayerIds.Length >= maxQueueSize) return;
            
            // 既に列にいるなら何もしない
            if (GetPlayerQueueIndex(lastRemovedPlayerId) >= 0) return;

            // ★先頭に割り込み挿入する
            InsertToQueueFront(lastRemovedPlayerId, lastRemovedPlayerName);
            
            // 戻したので履歴をクリア
            lastRemovedPlayerId = -1;
            lastRemovedPlayerName = "";
            
            RequestSerialization();
            UpdateUI();
        }

        // --------------------------------------
        // 配列操作ヘルパー
        // --------------------------------------

        private void AddToQueue(int playerId, string playerName)
        {
            // 配列を拡張して末尾に追加
            int newLength = queuedPlayerIds.Length + 1;
            int[] newIds = new int[newLength];
            string[] newNames = new string[newLength];

            // コピー
            for (int i = 0; i < queuedPlayerIds.Length; i++)
            {
                newIds[i] = queuedPlayerIds[i];
                newNames[i] = queuedPlayerNames[i];
            }

            // 末尾に追加
            newIds[newLength - 1] = playerId;
            newNames[newLength - 1] = playerName;

            queuedPlayerIds = newIds;
            queuedPlayerNames = newNames;
        }

        // ★新規：先頭に追加する処理
        private void InsertToQueueFront(int playerId, string playerName)
        {
            int newLength = queuedPlayerIds.Length + 1;
            int[] newIds = new int[newLength];
            string[] newNames = new string[newLength];

            // 先頭にセット
            newIds[0] = playerId;
            newNames[0] = playerName;

            // 既存の要素を1つ後ろにずらしてコピー
            for (int i = 0; i < queuedPlayerIds.Length; i++)
            {
                newIds[i + 1] = queuedPlayerIds[i];
                newNames[i + 1] = queuedPlayerNames[i];
            }

            queuedPlayerIds = newIds;
            queuedPlayerNames = newNames;
        }

        private void RemoveFromQueueAt(int index)
        {
            if (index < 0 || index >= queuedPlayerIds.Length) return;

            // ★削除する人を「履歴」に保存（Restore用）
            lastRemovedPlayerId = queuedPlayerIds[index];
            lastRemovedPlayerName = queuedPlayerNames[index];

            int newLength = queuedPlayerIds.Length - 1;
            int[] newIds = new int[newLength];
            string[] newNames = new string[newLength];

            // 削除箇所を飛ばしてコピー
            for (int i = 0, j = 0; i < queuedPlayerIds.Length; i++)
            {
                if (i == index) continue;
                newIds[j] = queuedPlayerIds[i];
                newNames[j] = queuedPlayerNames[i];
                j++;
            }

            queuedPlayerIds = newIds;
            queuedPlayerNames = newNames;
        }

        public int GetPlayerQueueIndex(int playerId)
        {
            for (int i = 0; i < queuedPlayerIds.Length; i++)
            {
                if (queuedPlayerIds[i] == playerId) return i;
            }
            return -1;
        }

        // --------------------------------------
        // UI & Event
        // --------------------------------------

        public override void OnDeserialization()
        {
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

        private void UpdateUI()
        {
            if (uiManager != null && localPlayer != null)
            {
                bool isInQueue = IsPlayerInQueue();
                uiManager.UpdateQueueDisplay(queuedPlayerNames, queuedPlayerIds, localPlayer.playerId, isInQueue, lastCalledPlayerId);
            }
        }

        public bool IsPlayerInQueue()
        {
            if (localPlayer == null) return false;
            return GetPlayerQueueIndex(localPlayer.playerId) >= 0;
        }
        
        // 外部参照用
        public string[] GetQueuePlayerNames() => queuedPlayerNames;
        public int[] GetQueuePlayerIds() => queuedPlayerIds;
    }
}
