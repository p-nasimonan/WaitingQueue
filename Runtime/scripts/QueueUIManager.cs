・ｿusing UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// 蠕・ｩ溷・縺ｮUI陦ｨ遉ｺ繧堤ｮ｡逅・＠縺ｾ縺吶・    /// 繝ｯ繝ｼ繝ｫ繝牙崋螳壹Μ繧ｹ繝医√・繝ｬ繧､繝､繝ｼ騾夂衍繝｢繝九ち繝ｼ縺ｮ繝懊ち繝ｳ迥ｶ諷九ｒ邂｡逅・＠縺ｾ縺吶・    /// </summary>
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
        [SerializeField] private string joinButtonText = "蠕・ｩ溷・縺ｫ蜈･繧・;
        [SerializeField] private string leaveButtonText = "蠕・ｩ溷・縺九ｉ蜃ｺ繧・;

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
            
            // 蛻晄悄迥ｶ諷九・繝懊ち繝ｳ繝・く繧ｹ繝医ｒ險ｭ螳・            UpdateButtonState(false);
        }

        /// <summary>
        /// 蠕・ｩ溷・縺ｮ陦ｨ遉ｺ繧呈峩譁ｰ縺励∪縺吶・        /// </summary>
        public void UpdateQueueDisplay(string[] playerNames, string[] playerIds, int localPlayerId, bool playerIsInQueue)
        {
            isInQueue = playerIsInQueue;
            
            // 繝懊ち繝ｳ迥ｶ諷九ｒ譖ｴ譁ｰ
            UpdateButtonState(isInQueue);
            
            // 繝ｪ繧ｹ繝郁｡ｨ遉ｺ繧呈峩譁ｰ
            UpdateQueueListDisplay(playerNames, playerIds, localPlayerId);
            
            // 繝励Ξ繧､繝､繝ｼ繧ｹ繝・・繧ｿ繧ｹ繝・く繧ｹ繝医ｒ譖ｴ譁ｰ
            UpdatePlayerStatus(playerNames, playerIds, localPlayerId);
        }

        /// <summary>
        /// 繝懊ち繝ｳ縺ｮ繝・く繧ｹ繝医→迥ｶ諷九ｒ譖ｴ譁ｰ縺励∪縺吶・        /// </summary>
        private void UpdateButtonState(bool inQueue)
        {
            string buttonText = inQueue ? leaveButtonText : joinButtonText;
            
            // 繝ｯ繝ｼ繝ｫ繝牙崋螳壹・繧ｿ繝ｳ
            if (worldButtonText != null)
            {
                worldButtonText.text = buttonText;
            }
            
            // 繝励Ξ繧､繝､繝ｼ騾夂衍繝｢繝九ち繝ｼ繝懊ち繝ｳ
            if (playerButtonText != null)
            {
                playerButtonText.text = buttonText;
            }
        }

        /// <summary>
        /// 蠕・ｩ溷・繝ｪ繧ｹ繝医・陦ｨ遉ｺ繧呈峩譁ｰ縺励∪縺吶・        /// </summary>
        private void UpdateQueueListDisplay(string[] playerNames, string[] playerIds, int localPlayerId)
        {
            if (worldQueueListText == null) return;

            if (playerNames == null || playerNames.Length == 0)
            {
                worldQueueListText.text = "蠕・ｩ溷・縺ｯ遨ｺ縺ｧ縺・;
                return;
            }

            if (playerIds == null || playerIds.Length == 0)
            {
                worldQueueListText.text = "蠕・ｩ溷・縺ｯ遨ｺ縺ｧ縺・;
                return;
            }

            string displayText = "縲仙ｾ・ｩ溷・縲曾n";
            int localPlayerIndex = -1;
            string localPlayerIdStr = localPlayerId.ToString();

            // 繝ｭ繝ｼ繧ｫ繝ｫ繝励Ξ繧､繝､繝ｼ縺ｮ菴咲ｽｮ繧呈､懃ｴ｢
            for (int i = 0; i < playerIds.Length; i++)
            {
                if (playerIds[i] == localPlayerIdStr)
                {
                    localPlayerIndex = i;
                    break;
                }
            }

            // 陦ｨ遉ｺ遽・峇繧呈ｱｺ螳夲ｼ郁・蛻・・菴咲ｽｮ繧剃ｸｭ蠢・↓・・            int startIndex = 0;
            int endIndex = Mathf.Min(playerNames.Length, maxDisplayLines);

            if (localPlayerIndex >= 0 && playerNames.Length > maxDisplayLines)
            {
                // 閾ｪ蛻・・菴咲ｽｮ繧剃ｸｭ蠢・↓陦ｨ遉ｺ遽・峇繧定ｪｿ謨ｴ
                int halfDisplay = maxDisplayLines / 2;
                startIndex = Mathf.Max(0, localPlayerIndex - halfDisplay);
                endIndex = Mathf.Min(playerNames.Length, startIndex + maxDisplayLines);
                
                // 邨らｫｯ縺ｫ驕斐＠縺溷ｴ蜷医・髢句ｧ倶ｽ咲ｽｮ繧定ｪｿ謨ｴ
                if (endIndex == playerNames.Length)
                {
                    startIndex = Mathf.Max(0, endIndex - maxDisplayLines);
                }
            }

            // 荳企Κ縺ｫ逵∫払陦ｨ遉ｺ
            if (startIndex > 0)
            {
                displayText += $"... ({startIndex}莠ｺ逵∫払)\n";
            }

            // 繝ｪ繧ｹ繝医ｒ逕滓・
            for (int i = startIndex; i < endIndex; i++)
            {
                string prefix = $"{i + 1}. ";
                string playerName = playerNames[i];
                
                // 閾ｪ蛻・ｒ繝上う繝ｩ繧､繝・                if (highlightCurrentPlayer && i == localPlayerIndex)
                {
                    string hexColor = ColorUtility.ToHtmlStringRGB(highlightColor);
                    displayText += $"<color=#{hexColor}><b>竊・{prefix}{playerName} (縺ゅ↑縺・</b></color>\n";
                }
                else
                {
                    displayText += $"{prefix}{playerName}\n";
                }
            }

            // 荳矩Κ縺ｫ逵∫払陦ｨ遉ｺ
            if (endIndex < playerNames.Length)
            {
                int remaining = playerNames.Length - endIndex;
                displayText += $"... (莉本remaining}莠ｺ)";
            }

            worldQueueListText.text = displayText;

            // 閾ｪ蛻・・菴咲ｽｮ縺ｫ繧ｹ繧ｯ繝ｭ繝ｼ繝ｫ・医が繝励す繝ｧ繝ｳ・・            if (worldScrollRect != null && localPlayerIndex >= 0)
            {
                float scrollPosition = (float)localPlayerIndex / Mathf.Max(1, playerNames.Length - 1);
                worldScrollRect.verticalNormalizedPosition = 1f - scrollPosition;
            }
        }

        /// <summary>
        /// 繝励Ξ繧､繝､繝ｼ騾夂衍繝｢繝九ち繝ｼ縺ｮ繧ｹ繝・・繧ｿ繧ｹ繝・く繧ｹ繝医ｒ譖ｴ譁ｰ縺励∪縺吶・        /// </summary>
        private void UpdatePlayerStatus(string[] playerNames, string[] playerIds, int localPlayerId)
        {
            if (playerStatusText == null) return;

            if (playerNames == null || playerIds == null || playerIds.Length == 0)
            {
                playerStatusText.text = "蠕・ｩ溷・縺ｫ逋ｻ骭ｲ縺輔ｌ縺ｦ縺・∪縺帙ｓ";
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
                playerStatusText.text = $"迴ｾ蝨ｨ縺ｮ鬆・分: {position}逡ｪ逶ｮ / 蜈ｨ{playerNames.Length}莠ｺ";
            }
            else
            {
                playerStatusText.text = "蠕・ｩ溷・縺ｫ逋ｻ骭ｲ縺輔ｌ縺ｦ縺・∪縺帙ｓ";
            }
        }
    }
}
