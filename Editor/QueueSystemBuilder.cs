using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.Udon;
using System.Linq;

namespace Youkan.WaitingQueue.Editor
{
    /// <summary>
    /// 待機列システムのUIを構築するヘルパークラスです。
    /// UIの基本構造を自動生成します。
    /// </summary>
    public class QueueSystemBuilder : EditorWindow
    {
        private static TMP_FontAsset _japaneseFontAsset;
        private static bool _useJapanese = true;
        
        /// <summary>
        /// 日本語フォントアセットを検索します。
        /// </summary>
        private static TMP_FontAsset FindJapaneseFontAsset()
        {
            if (_japaneseFontAsset != null) return _japaneseFontAsset;
            
            // まず、パッケージ内のフォントを検索
            string packageFontPath = "Packages/uk.youkan.waiting-queue/Assets/font/JK-Maru-Gothic-M SDF.asset";
            _japaneseFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(packageFontPath);
            
            if (_japaneseFontAsset != null)
            {
                Debug.Log($"[QueueSystemBuilder] Found package Japanese font: {_japaneseFontAsset.name}");
                return _japaneseFontAsset;
            }
            
            // よく使われる日本語フォント名のリスト
            string[] commonJapaneseFontNames = new[]
            {
                "JK-Maru-Gothic",
                "JK Maru Gothic",
                "NotoSansJP",
                "Noto Sans JP",
                "NotoSansCJKjp",
                "GenShinGothic",
                "M+ 1p",
                "Mplus1p",
                "Japanese"
            };
            
            // すべてのTMP_FontAssetを検索
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                
                if (font == null) continue;
                
                // 日本語フォントかどうかを名前で判定
                foreach (string fontName in commonJapaneseFontNames)
                {
                    if (font.name.Contains(fontName))
                    {
                        _japaneseFontAsset = font;
                        Debug.Log($"[QueueSystemBuilder] Found Japanese font: {font.name}");
                        return _japaneseFontAsset;
                    }
                }
            }
            
            Debug.LogWarning("[QueueSystemBuilder] Japanese font not found.");
            return null;
        }
        
        private GameObject CreateWorldFixedPanel(Transform parent)
        {
            GameObject canvasObject = new GameObject("WorldFixedPanel");
            canvasObject.transform.SetParent(parent);
            canvasObject.transform.localPosition = new Vector3(0, 2, 3);
            canvasObject.transform.localRotation = Quaternion.identity;
            
            // Canvas設定
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            canvasObject.AddComponent<GraphicRaycaster>();
            
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(600, 800);
            canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            
            // 背景パネル
            GameObject backgroundPanel = new GameObject("Background");
            backgroundPanel.transform.SetParent(canvasObject.transform, false);
            Image bgImage = backgroundPanel.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            RectTransform bgRect = backgroundPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // タイトル
            CreateTitle(canvasObject.transform);
            
            // スクロールビュー
            CreateScrollView(canvasObject.transform);
            
            // ボタン
            CreateToggleButton(canvasObject.transform, "WorldToggleButton");
            
            return canvasObject;
        }
        
        private void CreateTitle(Transform parent)
        {
            GameObject titleObject = new GameObject("Title");
            titleObject.transform.SetParent(parent, false);
            TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
            
            // 日本語フォントを検索
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                titleText.font = japaneseFont;
                titleText.text = "待機列システム";
            }
            else
            {
                titleText.text = _useJapanese ? "待機列システム" : "Waiting Queue System";
            }
            
            titleText.fontSize = 36;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.anchoredPosition = new Vector2(0, -40);
            titleRect.sizeDelta = new Vector2(-40, 60);
        }
        
        private void CreateScrollView(Transform parent)
        {
            GameObject scrollViewObject = new GameObject("ScrollView");
            scrollViewObject.transform.SetParent(parent, false);
            RectTransform scrollRect = scrollViewObject.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0, 0);
            scrollRect.anchorMax = new Vector2(1, 1);
            scrollRect.offsetMin = new Vector2(20, 120);
            scrollRect.offsetMax = new Vector2(-20, -100);
            
            ScrollRect scrollComponent = scrollViewObject.AddComponent<ScrollRect>();
            scrollComponent.horizontal = false;
            scrollComponent.vertical = true;
            
            Image scrollImage = scrollViewObject.AddComponent<Image>();
            scrollImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            
            // Viewport
            GameObject viewportObject = new GameObject("Viewport");
            viewportObject.transform.SetParent(scrollViewObject.transform, false);
            RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            Mask viewportMask = viewportObject.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            viewportObject.AddComponent<Image>();
            
            // Content
            GameObject contentObject = new GameObject("Content");
            contentObject.transform.SetParent(viewportObject.transform, false);
            RectTransform contentRect = contentObject.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 500);
            
            // QueueListText
            GameObject listTextObject = new GameObject("QueueListText");
            listTextObject.transform.SetParent(contentObject.transform, false);
            TextMeshProUGUI listText = listTextObject.AddComponent<TextMeshProUGUI>();
            
            // 日本語フォントを検索
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                listText.font = japaneseFont;
                listText.text = "待機列は空です";
            }
            else
            {
                listText.text = _useJapanese ? "待機列は空です" : "Queue is empty";
            }
            
            listText.fontSize = 24;
            listText.alignment = TextAlignmentOptions.TopLeft;
            listText.color = Color.white;
            RectTransform listTextRect = listTextObject.GetComponent<RectTransform>();
            listTextRect.anchorMin = new Vector2(0, 1);
            listTextRect.anchorMax = new Vector2(1, 1);
            listTextRect.pivot = new Vector2(0.5f, 1);
            listTextRect.anchoredPosition = Vector2.zero;
            listTextRect.sizeDelta = new Vector2(-20, 500);
            
            scrollComponent.content = contentRect;
            scrollComponent.viewport = viewportRect;
        }
        
        private void CreateToggleButton(Transform parent, string buttonName)
        {
            GameObject buttonObject = new GameObject(buttonName);
            buttonObject.transform.SetParent(parent, false);
            
            // RectTransformを追加（UIElementはデフォルトで持つが明示的に確保）
            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(0, 60);
            buttonRect.sizeDelta = new Vector2(300, 80);
            
            Button button = buttonObject.AddComponent<Button>();
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);
            
            GameObject buttonTextObject = new GameObject("ButtonText");
            buttonTextObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI buttonText = buttonTextObject.AddComponent<TextMeshProUGUI>();
            
            // 日本語フォントを検索
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                buttonText.font = japaneseFont;
                buttonText.text = "待機列に入る";
            }
            else
            {
                buttonText.text = _useJapanese ? "待機列に入る" : "Join Queue";
            }
            
            buttonText.fontSize = 28;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
        }
        
        private GameObject CreatePlayerNotificationMonitor(Transform parent)
        {
            GameObject canvasObject = new GameObject("PlayerNotificationMonitor");
            canvasObject.transform.SetParent(parent);
            canvasObject.transform.localPosition = Vector3.zero;
            
            // Canvas設定 (Screen Space - Overlay)
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObject.AddComponent<GraphicRaycaster>();
            
            // パネル
            GameObject panelObject = new GameObject("Panel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0.5f);
            panelRect.anchorMax = new Vector2(0, 0.5f);
            panelRect.pivot = new Vector2(0, 0.5f);
            panelRect.anchoredPosition = new Vector2(50, 0);
            panelRect.sizeDelta = new Vector2(400, 200);
            
            // ステータステキスト
            GameObject statusTextObject = new GameObject("StatusText");
            statusTextObject.transform.SetParent(panelObject.transform, false);
            TextMeshProUGUI statusText = statusTextObject.AddComponent<TextMeshProUGUI>();
            
            // 日本語フォントを検索
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                statusText.font = japaneseFont;
                statusText.text = "待機列に登録されていません";
            }
            else
            {
                statusText.text = _useJapanese ? "待機列に登録されていません" : "Not in queue";
            }
            
            statusText.fontSize = 20;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.color = Color.white;
            RectTransform statusTextRect = statusTextObject.GetComponent<RectTransform>();
            statusTextRect.anchorMin = new Vector2(0, 0.5f);
            statusTextRect.anchorMax = new Vector2(1, 1);
            statusTextRect.offsetMin = new Vector2(10, 10);
            statusTextRect.offsetMax = new Vector2(-10, -10);
            
            // ボタン
            CreateToggleButton(panelObject.transform, "PlayerToggleButton");
            
            // 初期状態で非表示
            canvasObject.SetActive(false);
            
            return canvasObject;
        }
        
        private GameObject CreatePlayerFollowNotificationPanel(Transform parent)
        {
            GameObject canvasObject = new GameObject("PlayerFollowNotification");
            canvasObject.transform.SetParent(parent);
            canvasObject.transform.localPosition = new Vector3(0, 0.5f, 1.5f);
            
            // Canvas設定 (World Space)
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(500, 200);
            canvasRect.localScale = new Vector3(0.003f, 0.003f, 0.003f);
            
            // 背景パネル
            GameObject panelObject = new GameObject("NotificationPanel");
            panelObject.transform.SetParent(canvasObject.transform, false);
            Image panelImage = panelObject.AddComponent<Image>();
            panelImage.color = new Color(1f, 0.8f, 0f, 0.9f);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            // 通知テキスト
            GameObject textObject = new GameObject("NotificationText");
            textObject.transform.SetParent(panelObject.transform, false);
            TextMeshProUGUI notificationText = textObject.AddComponent<TextMeshProUGUI>();
            
            // 日本語フォントを検索
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                notificationText.font = japaneseFont;
                notificationText.text = "あなたの番です！";
            }
            else
            {
                notificationText.text = _useJapanese ? "あなたの番です！" : "It's your turn!";
            }
            
            notificationText.fontSize = 48;
            notificationText.fontStyle = FontStyles.Bold;
            notificationText.alignment = TextAlignmentOptions.Center;
            notificationText.color = Color.black;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-20, -20);
            
            // 初期状態で非表示
            canvasObject.SetActive(false);
            
            return canvasObject;
        }
        
        private void CreateAudioSource(Transform parent)
        {
            GameObject audioObject = new GameObject("NotificationAudio");
            audioObject.transform.SetParent(parent);
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }

        /// <summary>
        /// 外部から呼び出すための静的メソッド（QueueSystemSetupから使用）
        /// </summary>
        public static void CreateQueueSystemUIStatic(bool useJapanese, TMP_FontAsset fontAsset)
        {
            _useJapanese = useJapanese;
            _japaneseFontAsset = fontAsset;

            // 日本語の場合、フォントアセットの確認
            if (_useJapanese && _japaneseFontAsset == null)
            {
                Debug.LogError("[QueueSystemBuilder] Japanese font asset is required but not provided");
                EditorUtility.DisplayDialog("エラー", "日本語フォントアセットが見つかりません。先にフォントを作成してください。", "OK");
                return;
            }

            GameObject rootObject = new GameObject("QueueSystem");

            // マネージャーオブジェクト
            new GameObject("QueueManager").transform.SetParent(rootObject.transform);
            new GameObject("QueueUIManager").transform.SetParent(rootObject.transform);
            new GameObject("QueueNotificationManager").transform.SetParent(rootObject.transform);
            new GameObject("QueueButtonHandler").transform.SetParent(rootObject.transform);

            // UIパネル作成
            QueueSystemBuilder builder = CreateInstance<QueueSystemBuilder>();
            builder.CreateWorldFixedPanel(rootObject.transform);
            builder.CreatePlayerNotificationMonitor(rootObject.transform);
            builder.CreatePlayerFollowNotificationPanel(rootObject.transform);
            builder.CreateAudioSource(rootObject.transform);

            Selection.activeGameObject = rootObject;

            Debug.Log("[QueueSystemBuilder] UI structure created successfully.");
        }
    }
}
