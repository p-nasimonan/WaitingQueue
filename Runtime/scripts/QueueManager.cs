using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// キュー管理システムのコアロジックを担当します。
    /// プレイヤーのエントリー、リスト同期、通知イベント管理を行います。
    /// UIボタンの処理も統合しています。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class QueueManager : UdonSharpBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxQueueSize = 50;

        [Header("Manager References")]
        public QueueUIManager uiManager;
        public QueueNotificationManager notificationManager;
        public Toggle staffModeToggle;
        public Button advanceButton;
        public Image advanceButtonImage;

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

            // ボタンリスナーはエディタ時に設定されます

            // オーナーの場合のみ初期化
            if (Networking.IsOwner(gameObject))
            {
                queuedPlayerIds = new string[0];
                queuedPlayerNames = new string[0];
                lastCalledPlayerId = "";
                syncCounter = 0;
            }

            UpdateUI();
        }

        /// <summary>
        /// プレイヤーがキューに入れ抜けるをトグルします。
        /// Udon Custom Event から呼び出されます。
        /// </summary>
        public void OnToggleButtonClick()
        {
            Debug.Log("[QueueManager] OnToggleButtonClick called");
            ToggleQueue();
        }

        /// <summary>
        /// オーナーが次のプレイヤーを呼び出します。
        /// Unity Button の OnClick() から呼び出されます。
        /// </summary>
        public void OnAdvanceButtonClick()
        {
            AdvanceQueue();
        }

        /// <summary>
        /// スタッフトグルの状態が変わったときに呼び出されます。
        /// ボタンの色と有効/無効を切り替えます。
        /// </summary>
        public void OnStaffToggleChanged()
        {
            if (advanceButton != null && staffModeToggle != null)
            {
                advanceButton.interactable = staffModeToggle.isOn;
                
                // ボタンの色を変更
                if (advanceButtonImage != null)
                {
                    if (staffModeToggle.isOn)
                    {
                        // チェックがON：明るい緑
                        advanceButtonImage.color = new Color(0.2f, 0.8f, 0.3f, 1f);
                    }
                    else
                    {
                        // チェックがOFF：暗い灰色
                        advanceButtonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                }
            }
        }

        /// <summary>
        /// プレイヤーがキューに入れ抜けるをトグルします。
        /// </summary>
        public void ToggleQueue()
        {Debug.Log("[QueueManager] ToggleQueue called");
            if (localPlayer == null) return;

            int existingIndex = GetPlayerQueueIndex(localPlayer.playerId);
            
            if (existingIndex >= 0)
            {
                // 既に列にある場合は抜ける
                Debug.Log("[QueueManager] Player is in queue, leaving");
                LeaveQueue();
            }
            else
            {
                // 列にない場合は入る
                Debug.Log("[QueueManager] Player not in queue, enqueuing");
                // 列にない場合は入る
                EnqueuePlayer();
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

            // 既に登録済みかチェック
            int existingIndex = GetPlayerQueueIndex(localPlayer.playerId);
            if (existingIndex >= 0)
            {
                Debug.LogWarning($"[QueueManager] Player {localPlayer.displayName} is already in the queue.");
                return;
            }

            // キューサイズチェック
            if (queuedPlayerIds.Length >= maxQueueSize)
            {
                Debug.LogWarning("[QueueManager] Queue is full.");
                return;
            }

            // ▼▼▼ 追加：書き込む前にオーナー権限を取得する！ ▼▼▼
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }
            // ▲▲▲ 追加ここまで ▲▲▲

            // プレイヤーIDと名前をリストに追加
            AddToQueue(localPlayer.playerId, localPlayer.displayName);

            // 全クライアントに同期
            RequestSerialization();
            
            // UI更新
            UpdateUI();
        }

        /// <summary>
        /// キューにプレイヤーを追加します（内部メソッド）。
        /// </summary>
        private void AddToQueue(int playerId, string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogWarning("[QueueManager] Player name is null or empty.");
                return;
            }

            int newLength = queuedPlayerIds.Length + 1;
            
            // 配列を拡張
            string[] newIds = new string[newLength];
            string[] newNames = new string[newLength];

            // 既存データをコピー
            for (int i = 0; i < queuedPlayerIds.Length; i++)
            {
                newIds[i] = queuedPlayerIds[i];
                newNames[i] = queuedPlayerNames[i];
            }

            // 新しいプレイヤーを追加
            newIds[newLength - 1] = playerId.ToString();
            newNames[newLength - 1] = playerName;

            // 配列を更新
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
        /// キューの先頭を削除し、次のプレイヤーに通知を送ります。
        /// </summary>
        public void AdvanceQueue()
        {
            // スタッフ権限チェック
            if (staffModeToggle != null && !staffModeToggle.isOn)
            {
                Debug.LogWarning("[QueueManager] スタッフモードが無効なため操作できません。");
            }

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(localPlayer, gameObject);
            }

            // 前回呼ばれた人がいる場合、その人を列から削除
            if (!string.IsNullOrEmpty(lastCalledPlayerId))
            {
                if (int.TryParse(lastCalledPlayerId, out int lastCalledId))
                {
                    int lastCalledIndex = GetPlayerQueueIndex(lastCalledId);
                    if (lastCalledIndex >= 0)
                    {
                        RemoveFromQueueAt(lastCalledIndex);
                        Debug.Log($"[QueueManager] Removed previously called player (ID: {lastCalledPlayerId}) from queue");
                    }
                }
            }

            // 列が空になったかチェック
            if (queuedPlayerIds.Length == 0)
            {
                lastCalledPlayerId = "";
                RequestSerialization();
                UpdateUI();
                Debug.LogWarning("[QueueManager] Queue is now empty.");
                return;
            }

            // 新しい先頭の人を呼び出す
            lastCalledPlayerId = queuedPlayerIds[0];
            
            // 同期カウンターを増やして通知をトリガー
            syncCounter++;

            // 全クライアントに同期
            RequestSerialization();
            
            // UI更新（リアルタイムで次の人が表示される）
            UpdateUI();
        }

        /// <summary>
        /// 指定されたインデックスのプレイヤーをキューから削除します（内部メソッド）。
        /// </summary>
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

                // 削除されるインデックスより前のデータをコピー
                for (int i = 0; i < index; i++)
                {
                    newIds[i] = queuedPlayerIds[i];
                    newNames[i] = queuedPlayerNames[i];
                }

                // 削除されるインデックスより後ろのデータをコピー
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
        /// プレイヤーがキューから手動で脱出します。
        /// </summary>
        public void LeaveQueue()
        {
            if (localPlayer == null) return;

            int index = GetPlayerQueueIndex(localPlayer.playerId);
            if (index >= 0)
            {
                // ▼▼▼ 追加：書き込む前にオーナー権限を取得する！ ▼▼▼
                if (!Networking.IsOwner(gameObject))
                {
                    Networking.SetOwner(localPlayer, gameObject);
                }
                // ▲▲▲ 追加ここまで ▲▲▲

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
        /// 現在のキューの状態をデバッグログに出力します。
        /// </summary>
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
        /// UdonEventを通じてデータ受信時の処理。
        /// </summary>
        public override void OnDeserialization()
        {
            Debug.Log("[QueueManager] Queue data has been synchronized.");
            
            // 同期カウンターが変更された場合、通知イベントをチェック
            if (syncCounter != lastKnownSyncCounter && lastCalledPlayerId != "")
            {
                lastKnownSyncCounter = syncCounter;
                
                // 通知マネージャーに通知を送信
                if (notificationManager != null)
                {
                    notificationManager.TriggerNotification(lastCalledPlayerId);
                }
            }
            
            // UI更新
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
                uiManager.UpdateQueueDisplay(queuedPlayerNames, queuedPlayerIds, localPlayer.playerId, isInQueue);
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
        /// キューデータを取得します（外部参照用）。
        /// </summary>
        public string[] GetQueuePlayerNames() => queuedPlayerNames;
        public string[] GetQueuePlayerIds() => queuedPlayerIds;
    }
}
