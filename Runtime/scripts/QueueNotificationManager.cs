using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;
using TMPro;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// キューの通知を管理します。
    /// プレイヤーの番号が来たときに通知表示と効果音を生成します。
    /// </summary>
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
        [SerializeField] private string notificationMessage = "あなたの番です！";
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

            if (notificationText != null)
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
            if (isNotificationActive && notificationPanel != null)
            {
                float elapsed = Time.time - notificationStartTime;

                if (enablePulseAnimation)
                {
                    float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * (pulseScale - 1f);
                    notificationPanel.transform.localScale = originalScale * scale;
                }

                if (elapsed >= notificationDuration)
                {
                    HideNotification();
                }
            }
        }

        /// <summary>
        /// 通知をトリガーします。呼び出されたプレイヤーIDと一致する場合のみ表示します。
        /// </summary>
        public void TriggerNotification(string calledPlayerId)
        {
            if (localPlayer == null) return;

            if (calledPlayerId == localPlayer.playerId.ToString())
            {
                ShowNotification();
            }

            PlayNotificationSound();
        }

        /// <summary>
        /// 通知パネルを表示します。
        /// </summary>
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
        /// 通知パネルを非表示にします。
        /// </summary>
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
        /// 通知効果音を生成します。
        /// </summary>
        private void PlayNotificationSound()
        {
            if (notificationAudioSource != null && notificationSound != null)
            {
                notificationAudioSource.PlayOneShot(notificationSound);
                Debug.Log("[QueueNotificationManager] Notification sound played");
            }
        }

        /// <summary>
        /// 手動で通知を閉じる場合に使用します。
        /// </summary>
        public void CloseNotification()
        {
            HideNotification();
        }
    }
}
