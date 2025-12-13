using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using TMPro;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// キューのUI表示を管理します。
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
        [SerializeField] private string joinButtonText = "キューに入る";
        [SerializeField] private string leaveButtonText = "キューから出る";

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
            
            UpdateButtonState(false);
        }

        /// <summary>
        /// キューの表示を更新します。
        /// </summary>
        public void UpdateQueueDisplay(string[] playerNames, string[] playerIds, int localPlayerId, bool playerIsInQueue)
        {
            isInQueue = playerIsInQueue;
            
            UpdateButtonState(isInQueue);
            
            UpdateQueueListDisplay(playerNames, playerIds, localPlayerId);
            
            UpdatePlayerStatus(playerNames, playerIds, localPlayerId);
        }

        /// <summary>
        /// ボタンのテキストと状態を更新します。
        /// </summary>
        private void UpdateButtonState(bool inQueue)
        {
            string buttonText = inQueue ? leaveButtonText : joinButtonText;
            
            if (worldButtonText != null)
            {
                worldButtonText.text = buttonText;
            }
            
            if (playerButtonText != null)
            {
                playerButtonText.text = buttonText;
            }
        }

        /// <summary>
        /// キューリストの表示を更新します。
        /// </summary>
        private void UpdateQueueListDisplay(string[] playerNames, string[] playerIds, int localPlayerId)
        {
            if (worldQueueListText == null) return;

            if (playerNames == null || playerNames.Length == 0)
            {
                worldQueueListText.text = "キューは空です";
                return;
            }

            if (playerIds == null || playerIds.Length == 0)
            {
                worldQueueListText.text = "キューは空です";
                return;
            }

            string displayText = "【キュー】\n";
            int localPlayerIndex = -1;
            string localPlayerIdStr = localPlayerId.ToString();

            for (int i = 0; i < playerIds.Length; i++)
            {
                if (playerIds[i] == localPlayerIdStr)
                {
                    localPlayerIndex = i;
                    break;
                }
            }

            int startIndex = 0;
            int endIndex = Mathf.Min(playerNames.Length, maxDisplayLines);

            if (localPlayerIndex >= 0 && playerNames.Length > maxDisplayLines)
            {
                int halfDisplay = maxDisplayLines / 2;
                startIndex = Mathf.Max(0, localPlayerIndex - halfDisplay);
                endIndex = Mathf.Min(playerNames.Length, startIndex + maxDisplayLines);
                
                if (endIndex == playerNames.Length)
                {
                    startIndex = Mathf.Max(0, endIndex - maxDisplayLines);
                }
            }

            if (startIndex > 0)
            {
                displayText += $"... ({startIndex}人省略)\n";
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                string prefix = $"{i + 1}. ";
                string playerName = playerNames[i];
                
                if (highlightCurrentPlayer && i == localPlayerIndex)
                {
                    string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
                    displayText += $"<color=#{hexColor}><b>→{prefix}{playerName} (あなた)</b></color>\n";
                }
                else
                {
                    displayText += $"{prefix}{playerName}\n";
                }
            }

            if (endIndex < playerNames.Length)
            {
                int remaining = playerNames.Length - endIndex;
                displayText += $"... (他{remaining}人)";
            }

            worldQueueListText.text = displayText;

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
                playerStatusText.text = "キューに登録されていません";
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
                playerStatusText.text = $"現在の番号: {position}番目 / 全{playerNames.Length}人";
            }
            else
            {
                playerStatusText.text = "キューに登録されていません";
            }
        }
    }
}
