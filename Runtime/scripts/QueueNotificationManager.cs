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
        public GameObject notificationPanel;
        public TextMeshProUGUI notificationText;
        public Image notificationBackground;

        [Header("Audio")]
        public AudioSource notificationAudioSource;
        [SerializeField] private AudioClip notificationSound;

        [Header("Notification Settings")]
        [SerializeField] private float displayDuration = 5f; // 通知表示時間
        [SerializeField] private string notificationMessage = "あなたの番です！";
        [SerializeField] private Color notificationColor = new Color(1f, 0.8f, 0f, 0.9f);

        [Header("Animation Settings")]
        [SerializeField] private bool enablePulseAnimation = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseScale = 1.1f;

        private VRCPlayerApi localPlayer;
        private Vector3 originalScale;
        private bool isDisplaying = false; // 表示状態フラグ
        private float disableTime = 0f; // 非表示になる時刻

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
            if (isDisplaying && notificationPanel != null)
            {
                if (enablePulseAnimation)
                {
                    float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * (pulseScale - 1f);
                    notificationPanel.transform.localScale = originalScale * scale;
                }

                // displayDuration に達したら自動で非表示
                if (Time.time >= disableTime)
                {
                    HideNotification();
                }
            }
        }

        /// <summary>
        /// 通知をトリガーします。全員に表示し、本人と他人でメッセージを変えます。
        /// </summary>
        public void TriggerNotification(string targetPlayerIdStr)
        {
            // ターゲット（呼ばれた人）の情報を取得
            if (!int.TryParse(targetPlayerIdStr, out int targetId)) return;
            VRCPlayerApi targetPlayer = VRCPlayerApi.GetPlayerById(targetId);
            string targetName = (targetPlayer != null) ? targetPlayer.displayName : "退出したプレイヤー";

            // ローカルプレイヤー（自分自身）のチェック
            if (localPlayer == null) return;

            // ▼▼▼ 修正ポイント ▼▼▼
            
            // 1. 全員共通で「表示ON」にする
            if (notificationPanel != null) notificationPanel.SetActive(true);
            if (notificationAudioSource != null) notificationAudioSource.Play();
            
            isDisplaying = true;
            disableTime = Time.time + displayDuration;

            // 2. 「本人」と「周りの人」でメッセージを変える
            if (notificationText != null)
            {
                if (localPlayer.playerId == targetId)
                {
                    // ★ 本人の場合
                    notificationText.text = "あなたの番です！\n<size=80%>受付へお越しください</size>";
                    notificationText.color = new Color(1f, 0.9f, 0.2f); // 黄色っぽく目立たせる
                }
                else
                {
                    // ★ 他人の場合（周りの人への案内）
                    notificationText.text = $"次は\n{targetName} さん\nの番です";
                    notificationText.color = Color.white; // 普通の色
                }
            }
            // ▲▲▲ 修正ここまで ▲▲▲
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

            isDisplaying = true;
            disableTime = Time.time + displayDuration;

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

            isDisplaying = false;

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
