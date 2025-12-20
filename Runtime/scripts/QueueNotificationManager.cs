using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using UnityEngine.UI;
using TMPro;

namespace Youkan.WaitingQueue
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class QueueNotificationManager : UdonSharpBehaviour
    {
        [Header("UI References")]
        public GameObject notificationPanel;
        public TextMeshProUGUI notificationText;
        public Image notificationBackground;
        public AudioSource notificationAudioSource;

        [Header("Settings")]
        [SerializeField] private float notificationDuration = 5.0f;

        // ▼▼▼ 調整ポイント ▼▼▼
        
        // 基準位置からのずれ（メートル単位）
        // X: 指先方向 (+), 肘方向 (-)
        // Y: 親指方向 (+), 小指方向 (-)
        // Z: 手の甲側 (+), 手のひら側 (-)
        // ※「後ろに行く」場合は Z を大きく(プラスに) してみてください
        [SerializeField] private Vector3 wristOffset = new Vector3(0.1f, 0f, 0.08f);
        
        // 回転調整
        // 手の甲に乗せて、文字盤を顔に向ける角度
        [SerializeField] private Vector3 wristRotationOffset = new Vector3(70f, 0f, 0f);

        // 基準となるアバターの身長（メートル）
        [SerializeField] private float referenceHeight = 1.6f;

        // ▲▲▲ 調整ここまで ▲▲▲

        private VRCPlayerApi localPlayer;
        private bool isNotificationActive = false;
        private float notificationTimer = 0f;
        private Vector3 initialScale; // 元のサイズを記憶しておく変数

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;
            if (notificationPanel != null) notificationPanel.SetActive(false);
            
            // 最初に設定されたスケール（0.0005など）を記憶
            initialScale = transform.localScale;
        }

        public override void PostLateUpdate()
        {
            if (localPlayer == null) return;
            if (!localPlayer.IsValid()) return;

            // 左手の位置と回転を取得
            VRCPlayerApi.TrackingData handData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
            
            if (handData.position == Vector3.zero) return;

            // --- 身長スケーリング計算 ---
            // 現在の身長を取得
            float currentHeight = localPlayer.GetAvatarEyeHeightAsMeters();
            // 基準(1.6m)に対して何倍か？（例: 身長0.8mなら 0.5倍）
            float scaleFactor = currentHeight / referenceHeight;
            
            // 安全策：極端に小さい/大きい値を防ぐ
            if (scaleFactor < 0.1f) scaleFactor = 0.1f;
            if (scaleFactor > 10f) scaleFactor = 10f;

            // 1. サイズを身長に合わせて変更
            transform.localScale = initialScale * scaleFactor;

            // 2. 位置（オフセット）も身長に合わせて遠く/近くする
            // これで巨大アバターでも手が埋まらなくなります
            Vector3 scaledOffset = wristOffset * scaleFactor;

            // 位置と回転を適用
            transform.position = handData.position + (handData.rotation * scaledOffset);
            transform.rotation = handData.rotation * Quaternion.Euler(wristRotationOffset);
        }

        public void TriggerNotification(string calledPlayerIdStr)
        {
            if (localPlayer == null) return;
            if (calledPlayerIdStr == localPlayer.playerId.ToString())
            {
                ShowNotification("あなたの番です！", Color.yellow);
                PlaySound();
            }
        }

        public void ShowNotification(string message, Color bgColor)
        {
            if (notificationPanel == null) return;
            if (notificationText != null) notificationText.text = message;
            if (notificationBackground != null) notificationBackground.color = bgColor;

            notificationPanel.SetActive(true);
            isNotificationActive = true;
            notificationTimer = notificationDuration;
        }

        private void Update()
        {
            if (isNotificationActive)
            {
                notificationTimer -= Time.deltaTime;
                if (notificationTimer <= 0)
                {
                    if (notificationPanel != null) notificationPanel.SetActive(false);
                    isNotificationActive = false;
                }
            }
        }

        private void PlaySound()
        {
            if (notificationAudioSource != null)
            {
                notificationAudioSource.PlayOneShot(notificationAudioSource.clip);
            }
        }
    }
}
