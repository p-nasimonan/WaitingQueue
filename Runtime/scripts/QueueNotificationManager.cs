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
    /// 蠕・ｩ溷・縺ｮ騾夂衍繧堤ｮ｡逅・＠縺ｾ縺吶・    /// 繝励Ξ繧､繝､繝ｼ縺ｮ鬆・分縺梧擂縺溘→縺阪・騾夂衍陦ｨ遉ｺ縺ｨ蜉ｹ譫憺浹繧貞・逕溘＠縺ｾ縺吶・    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QueueNotificationManager : UdonSharpBehaviour
    {
        [Header("Notification UI (Player Follow)")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private Image notificationBackground;

        [Header("Audio")]
        [SerializeField] private AudioSource notificationAudioSource;
        [SerializeField] private AudioClip notificationSound;

        [Header("Notification Settings")]
        [SerializeField] private float notificationDuration = 5f;
        [SerializeField] private string notificationMessage = "縺ゅ↑縺溘・逡ｪ縺ｧ縺呻ｼ・;
        [SerializeField] private Color notificationColor = new Color(1f, 0.8f, 0f, 0.9f);

        [Header("Animation Settings")]
        [SerializeField] private bool enablePulseAnimation = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseScale = 1.1f;

        private VRCPlayerApi localPlayer;
        private bool isNotificationActive = false;
        private float notificationStartTime = 0f;
        private Vector3 originalScale;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            
            if (localPlayer == null)
            {
                Debug.LogError("[QueueNotificationManager] Local player is null!");
                enabled = false;
                return;
            }
            
            // 蛻晄悄迥ｶ諷九〒縺ｯ騾夂衍繝代ロ繝ｫ繧帝撼陦ｨ遉ｺ
            if (notificationPanel != null)
            {
                originalScale = notificationPanel.transform.localScale;
                notificationPanel.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[QueueNotificationManager] Notification panel is not assigned!");
                originalScale = Vector3.one;
            }

            // 騾夂衍繝・く繧ｹ繝医→繧ｫ繝ｩ繝ｼ繧定ｨｭ螳・            if (notificationText != null)
            {
                notificationText.text = notificationMessage;
            }

            if (notificationBackground != null)
            {
                notificationBackground.color = notificationColor;
            }
        }

        private void Update()
        {
            // 騾夂衍縺梧怏蜉ｹ縺ｧ譎る俣邨碁℃繧偵メ繧ｧ繝・け
            if (isNotificationActive && notificationPanel != null)
            {
                float elapsed = Time.time - notificationStartTime;

                // 繝代Ν繧ｹ繧｢繝九Γ繝ｼ繧ｷ繝ｧ繝ｳ
                if (enablePulseAnimation)
                {
                    float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * (pulseScale - 1f);
                    notificationPanel.transform.localScale = originalScale * scale;
                }

                // 譎る俣邨碁℃縺ｧ騾夂衍繧帝撼陦ｨ遉ｺ
                if (elapsed >= notificationDuration)
                {
                    HideNotification();
                }
            }
        }

        /// <summary>
        /// 騾夂衍繧偵ヨ繝ｪ繧ｬ繝ｼ縺励∪縺吶ょ他縺ｳ蜃ｺ縺輔ｌ縺溘・繝ｬ繧､繝､繝ｼID縺ｨ荳閾ｴ縺吶ｋ蝣ｴ蜷医・縺ｿ陦ｨ遉ｺ縺励∪縺吶・        /// </summary>
        public void TriggerNotification(string calledPlayerId)
        {
            if (localPlayer == null) return;

            // 閾ｪ蛻・・ID縺ｨ荳閾ｴ縺吶ｋ蝣ｴ蜷医・縺ｿ騾夂衍繧定｡ｨ遉ｺ
            if (calledPlayerId == localPlayer.playerId.ToString())
            {
                ShowNotification();
            }

            // 蜈ｨ蜩｡縺ｫ蜉ｹ譫憺浹繧貞・逕滂ｼ医が繝励す繝ｧ繝ｳ: 閾ｪ蛻・□縺代↓縺励◆縺・ｴ蜷医・荳願ｨ倥・if蜀・↓遘ｻ蜍包ｼ・            PlayNotificationSound();
        }

        /// <summary>
        /// 騾夂衍繝代ロ繝ｫ繧定｡ｨ遉ｺ縺励∪縺吶・        /// </summary>
        private void ShowNotification()
        {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(true);
                notificationPanel.transform.localScale = originalScale;
            }

            isNotificationActive = true;
            notificationStartTime = Time.time;

            if (localPlayer != null)
            {
                Debug.Log($"[QueueNotificationManager] Notification shown for {localPlayer.displayName}");
            }
        }

        /// <summary>
        /// 騾夂衍繝代ロ繝ｫ繧帝撼陦ｨ遉ｺ縺ｫ縺励∪縺吶・        /// </summary>
        private void HideNotification()
        {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
                notificationPanel.transform.localScale = originalScale;
            }

            isNotificationActive = false;

            Debug.Log("[QueueNotificationManager] Notification hidden");
        }

        /// <summary>
        /// 騾夂衍蜉ｹ譫憺浹繧貞・逕溘＠縺ｾ縺吶・        /// </summary>
        private void PlayNotificationSound()
        {
            if (notificationAudioSource != null && notificationSound != null)
            {
                notificationAudioSource.PlayOneShot(notificationSound);
                Debug.Log("[QueueNotificationManager] Notification sound played");
            }
        }

        /// <summary>
        /// 謇句虚縺ｧ騾夂衍繧帝哩縺倥ｋ蝣ｴ蜷医↓菴ｿ逕ｨ縺励∪縺吶・        /// </summary>
        public void CloseNotification()
        {
            HideNotification();
        }
    }
}
