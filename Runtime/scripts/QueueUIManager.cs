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
        public TextMeshProUGUI ownerCurrentCalledText;
        public TextMeshProUGUI ownerQueueCountText;
        public Toggle ownerPermissionCheckbox;

        [Header("Display Settings")]
        [SerializeField] private int maxDisplayLines = 20;

        private VRCPlayerApi localPlayer;
        private bool isLocalPlayerOwner = false; // ボタン押下権を有しているか
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
            UpdateOwnerButtonState();
        }

        /// <summary>
        /// キューの表示を更新します。
        /// </summary>
        public void UpdateQueueDisplay(string[] playerNames, string[] playerIds, int localPlayerId, bool playerIsInQueue)
        {
            isInQueue = playerIsInQueue;
            
            UpdateButtonState(isInQueue);
            
            UpdateQueueListDisplay(playerNames, playerIds, localPlayerId);
            
            UpdateOwnerDisplay(playerNames, playerIds);
            
            UpdateOwnerButtonState();
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
        /// オーナー専用コントロールパネルの表示を更新します。
        /// 次に呼ばれる人と待機人数をリアルタイムで表示します。
        /// </summary>
        private void UpdateOwnerDisplay(string[] playerNames, string[] playerIds)
        {
            // 待機人数の表示
            if (ownerQueueCountText != null)
            {
                int count = (playerNames != null) ? playerNames.Length : 0;
                ownerQueueCountText.text = $"待機中: {count}人";
            }
            
            // 次呼ばれる人をリアルタイム表示
            if (ownerCurrentCalledText != null)
            {
                if (playerNames != null && playerNames.Length > 0)
                {
                    // 配列の0番目（＝先頭の人）を表示
                    ownerCurrentCalledText.text = playerNames[0];
                    ownerCurrentCalledText.color = new Color(1f, 0.9f, 0.3f); // 黄色で目立たせる
                }
                else
                {
                    // 誰もいない時
                    ownerCurrentCalledText.text = "-";
                    ownerCurrentCalledText.color = new Color(0.5f, 0.5f, 0.5f); // グレー
                }
            }
        }

        /// <summary>
        /// オーナーボタンの有効・無効を更新します。
        /// このプレイヤーが QueueSystem のオーナーの場合のみ有効。
        /// </summary>
        private void UpdateOwnerButtonState()
        {
            if (ownerAdvanceButton == null) return;

            // このプレイヤーがオーナーかチェック
            GameObject queueSystemObject = GameObject.Find("QueueSystem");
            if (queueSystemObject == null)
            {
                Transform parent = gameObject.transform.parent;
                if (parent != null)
                {
                    Transform grandParent = parent.parent;
                    if (grandParent != null)
                    {
                        queueSystemObject = grandParent.gameObject;
                    }
                }
            }
            
            isLocalPlayerOwner = queueSystemObject != null && Networking.IsOwner(Networking.LocalPlayer, queueSystemObject);

            // ボタンを有効・無効に設定
            ownerAdvanceButton.interactable = isLocalPlayerOwner;

            // ボタンの色を変更（無効時はグレーアウト）
            if (!isLocalPlayerOwner)
            {
                var colors = ownerAdvanceButton.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                ownerAdvanceButton.colors = colors;
            }
        }
    }
}
