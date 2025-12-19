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
        public TextMeshProUGUI worldQueueListText;
        public ScrollRect worldScrollRect;
        public Button worldToggleButton;
        public TextMeshProUGUI worldButtonText;

        [Header("Owner Control UI References")]
        public Button ownerAdvanceButton;
        public TextMeshProUGUI ownerAdvanceButtonText;
        public Button ownerRestoreButton;
        public TextMeshProUGUI ownerCurrentCalledText;
        public TextMeshProUGUI ownerQueueCountText;

        [Header("Display Settings")]
        [SerializeField] private int maxDisplayLines = 20;

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
            
            // ボタンの初期状態を設定
            UpdateButtonState(false);
        }

        /// <summary>
        /// キューの表示を更新します。
        /// </summary>
        public void UpdateQueueDisplay(string[] playerNames, int[] playerIds, int localPlayerId, bool playerIsInQueue, int lastCalledPlayerId)
        {
            isInQueue = playerIsInQueue;
            
            UpdateButtonState(isInQueue);
            
            UpdateQueueListDisplay(playerNames, playerIds, localPlayerId);
            
            Debug.Log($"[QueueUIManager] UpdateQueueDisplay: lastCalledPlayerId='{lastCalledPlayerId}', playerIds.Length={(playerIds != null ? playerIds.Length : 0)}");
            
            UpdateOwnerDisplay(playerNames, playerIds, lastCalledPlayerId);
        }

        /// <summary>
        /// Colorを16進数HTML色文字列に変換します。
        /// ColorUtility.ToHtmlStringRGB()がUdonに公開されていないため、カスタム実装です。
        /// </summary>
        private string ColorToHexString(Color color)
        {
            int r = Mathf.RoundToInt(color.r * 255f);
            int g = Mathf.RoundToInt(color.g * 255f);
            int b = Mathf.RoundToInt(color.b * 255f);
            return r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        }

        /// <summary>
        /// ボタンのテキストを更新します。
        /// </summary>
        private void UpdateButtonState(bool inQueue)
        {
            string buttonText = inQueue ? "待機列から出る" : "待機列に入る";
            Debug.Log($"[QueueUIManager] UpdateButtonState: inQueue={inQueue}, buttonText={buttonText}");
            
            if (worldButtonText != null)
            {
                worldButtonText.text = buttonText;
                Debug.Log($"[QueueUIManager] Updated worldButtonText to: {buttonText}");
            }
            else
            {
                Debug.LogWarning("[QueueUIManager] worldButtonText is null!");
            }
        }

        /// <summary>
        /// キューリストの表示を更新します。
        /// </summary>
        private void UpdateQueueListDisplay(string[] playerNames, int[] playerIds, int localPlayerId)
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

            for (int i = 0; i < playerIds.Length; i++)
            {
                if (playerIds[i] == localPlayerId)
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
                
                if (i == localPlayerIndex)
                {
                    string hexColor = ColorToHexString(new Color(1f, 1f, 0f));
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
        /// オーナー特用コントロールパネルの表示を更新します。
        /// 次に呼ばれる人と待機人数をリアルタイムで表示します。
        /// </summary>
        private void UpdateOwnerDisplay(string[] playerNames, int[] playerIds, int lastCalledPlayerId)
        {
            Debug.Log($"[QueueUIManager] UpdateOwnerDisplay: playerNames={playerNames}, playerNames.Length={(playerNames != null ? playerNames.Length : -1)}, ownerCurrentCalledText={ownerCurrentCalledText}");
            
            // 待機人数の表示
            if (ownerQueueCountText != null)
            {
                int count = (playerNames != null) ? playerNames.Length : 0;
                ownerQueueCountText.text = $"待機中: {count}人";
                Debug.Log($"[QueueUIManager] Updated queue count: {count}");
            }
            
            // 待機列の一番上の人を表示
            if (ownerCurrentCalledText != null)
            {
                Debug.Log($"[QueueUIManager] ownerCurrentCalledText is not null, checking playerNames...");
                
                if (playerNames != null && playerNames.Length > 0)
                {
                    ownerCurrentCalledText.text = playerNames[0];
                    ownerCurrentCalledText.color = new Color(1f, 0.9f, 0.3f); // 黄色
                    Debug.Log($"[QueueUIManager] Set CurrentCalledText to: {playerNames[0]}");
                }
                else
                {
                    // 誰も待機していない時
                    ownerCurrentCalledText.text = "-";
                    ownerCurrentCalledText.color = Color.gray;
                    Debug.Log($"[QueueUIManager] playerNames is null or empty, set to '-'");
                }
            }
            else
            {
                Debug.LogError("[QueueUIManager] ownerCurrentCalledText is NULL!");
            }
        }


    }
}
