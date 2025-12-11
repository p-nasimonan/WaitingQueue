using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;

namespace Youkan.WaitingQueue
{
    /// <summary>
    /// 待機列の通知を管理します。
    /// プレイヤーの順番が来たときの通知表示と効果音を再生します。
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
            
            // 初期状態では通知パネルを非表示
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

            // 通知テキストとカラーを設定
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
            // 通知が有効で時間経過をチェック
            if (isNotificationActive && notificationPanel != null)
            {
                float elapsed = Time.time - notificationStartTime;

                // パルスアニメーション
                if (enablePulseAnimation)
                {
                    float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * (pulseScale - 1f);
                    notificationPanel.transform.localScale = originalScale * scale;
                }

                // 時間経過で通知を非表示
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

            // 自分のIDと一致する場合のみ通知を表示
            if (calledPlayerId == localPlayer.playerId.ToString())
            {
                ShowNotification();
            }

            // 全員に効果音を再生（オプション: 自分だけにしたい場合は上記のif内に移動）
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
        /// 通知効果音を再生します。
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
