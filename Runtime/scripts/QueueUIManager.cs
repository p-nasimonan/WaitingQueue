using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// 待機列のUI表示を管理します。
    /// ワールド固定リスト、プレイヤー通知モニターのボタン状態を管理します。
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QueueUIManager : UdonSharpBehaviour
    {
        [Header("World Fixed UI References")]
        [SerializeField] private TextMeshProUGUI worldQueueListText;
        [SerializeField] private ScrollRect worldScrollRect;
        [SerializeField] private Button worldToggleButton;
        [SerializeField] private TextMeshProUGUI worldButtonText;

        [Header("Player Notification Monitor UI")]
        [SerializeField] private Button playerToggleButton;
        [SerializeField] private TextMeshProUGUI playerButtonText;
        [SerializeField] private TextMeshProUGUI playerStatusText;

        [Header("Button Text Configuration")]
        [SerializeField] private string joinButtonText = "待機列に入る";
        [SerializeField] private string leaveButtonText = "待機列から出る";

        [Header("Display Settings")]
        [SerializeField] private int maxDisplayLines = 20;
        [SerializeField] private bool highlightCurrentPlayer = true;
        [SerializeField] private Color highlightColor = Color.yellow;

        private VRCPlayerApi localPlayer;
        private bool isInQueue = false;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            
            if (localPlayer == null)
            {
                Debug.LogError("[QueueUIManager] Local player is null!");
                enabled = false;
                return;
            }
            
            // 初期状態のボタンテキストを設定
            UpdateButtonState(false);
        }

        /// <summary>
        /// 待機列の表示を更新します。
        /// </summary>
        public void UpdateQueueDisplay(string[] playerNames, string[] playerIds, int localPlayerId, bool playerIsInQueue)
        {
            isInQueue = playerIsInQueue;
            
            // ボタン状態を更新
            UpdateButtonState(isInQueue);
            
            // リスト表示を更新
            UpdateQueueListDisplay(playerNames, playerIds, localPlayerId);
            
            // プレイヤーステータステキストを更新
            UpdatePlayerStatus(playerNames, playerIds, localPlayerId);
        }

        /// <summary>
        /// ボタンのテキストと状態を更新します。
        /// </summary>
        private void UpdateButtonState(bool inQueue)
        {
            string buttonText = inQueue ? leaveButtonText : joinButtonText;
            
            // ワールド固定ボタン
            if (worldButtonText != null)
            {
                worldButtonText.text = buttonText;
            }
            
            // プレイヤー通知モニターボタン
            if (playerButtonText != null)
            {
                playerButtonText.text = buttonText;
            }
        }

        /// <summary>
        /// 待機列リストの表示を更新します。
        /// </summary>
        private void UpdateQueueListDisplay(string[] playerNames, string[] playerIds, int localPlayerId)
        {
            if (worldQueueListText == null) return;

            if (playerNames == null || playerNames.Length == 0)
            {
                worldQueueListText.text = "待機列は空です";
                return;
            }

            if (playerIds == null || playerIds.Length == 0)
            {
                worldQueueListText.text = "待機列は空です";
                return;
            }

            string displayText = "【待機列】\n";
            int localPlayerIndex = -1;
            string localPlayerIdStr = localPlayerId.ToString();

            // ローカルプレイヤーの位置を検索
            for (int i = 0; i < playerIds.Length; i++)
            {
                if (playerIds[i] == localPlayerIdStr)
                {
                    localPlayerIndex = i;
                    break;
                }
            }

            // 表示範囲を決定（自分の位置を中心に）
            int startIndex = 0;
            int endIndex = Mathf.Min(playerNames.Length, maxDisplayLines);

            if (localPlayerIndex >= 0 && playerNames.Length > maxDisplayLines)
            {
                // 自分の位置を中心に表示範囲を調整
                int halfDisplay = maxDisplayLines / 2;
                startIndex = Mathf.Max(0, localPlayerIndex - halfDisplay);
                endIndex = Mathf.Min(playerNames.Length, startIndex + maxDisplayLines);
                
                // 終端に達した場合は開始位置を調整
                if (endIndex == playerNames.Length)
                {
                    startIndex = Mathf.Max(0, endIndex - maxDisplayLines);
                }
            }

            // 上部に省略表示
            if (startIndex > 0)
            {
                displayText += $"... ({startIndex}人省略)\n";
            }

            // リストを生成
            for (int i = startIndex; i < endIndex; i++)
            {
                string prefix = $"{i + 1}. ";
                string playerName = playerNames[i];
                
                // 自分をハイライト
                if (highlightCurrentPlayer && i == localPlayerIndex)
                {
                    string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
                    displayText += $"<color=#{hexColor}><b>→ {prefix}{playerName} (あなた)</b></color>\n";
                }
                else
                {
                    displayText += $"{prefix}{playerName}\n";
                }
            }

            // 下部に省略表示
            if (endIndex < playerNames.Length)
            {
                int remaining = playerNames.Length - endIndex;
                displayText += $"... (他{remaining}人)";
            }

            worldQueueListText.text = displayText;

            // 自分の位置にスクロール（オプション）
            if (worldScrollRect != null && localPlayerIndex >= 0)
            {
                float scrollPosition = (float)localPlayerIndex / Mathf.Max(1, playerNames.Length - 1);
                worldScrollRect.verticalNormalizedPosition = 1f - scrollPosition;
            }
        }

        /// <summary>
        /// プレイヤー通知モニターのステータステキストを更新します。
        /// </summary>
        private void UpdatePlayerStatus(string[] playerNames, string[] playerIds, int localPlayerId)
        {
            if (playerStatusText == null) return;

            if (playerNames == null || playerIds == null || playerIds.Length == 0)
            {
                playerStatusText.text = "待機列に登録されていません";
                return;
            }

            string localPlayerIdStr = localPlayerId.ToString();
            int position = -1;

            for (int i = 0; i < playerIds.Length; i++)
            {
                if (playerIds[i] == localPlayerIdStr)
                {
                    position = i + 1;
                    break;
                }
            }

            if (position > 0)
            {
                playerStatusText.text = $"現在の順番: {position}番目 / 全{playerNames.Length}人";
            }
            else
            {
                playerStatusText.text = "待機列に登録されていません";
            }
        }
    }
}
