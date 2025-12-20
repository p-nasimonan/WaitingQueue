using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.Udon;
using VRC.Core;
using UdonSharp;
using UdonSharpEditor; // ★これ重要
using System.Linq;

namespace Youkan.WaitingQueue.Editor
{
    /// <summary>
    /// 待機列システムのUIを構築するヘルパークラスです。
    /// UIの基本構造を自動生成します。
    /// 
    /// 【統合されたスクリプトアーキテクチャ】
    /// - QueueSystem (親) ← QueueManager: コアロジック・UIボタンハンドラー統合
    /// - WorldFixedPanel ← QueueUIManager: UI表示管理
    /// - PlayerFollowNotification ← QueueNotificationManager: 通知・効果音管理
    /// 
    /// 【セットアップ】
    /// 1. QueueManager が親オブジェクトに自動アタッチ
    /// 2. UIManager と NotificationManager が各パネルに自動アタッチ
    /// 3. すべてのUI参照が自動設定
    /// 4. UIボタンの OnClick() を手動設定:
    ///    - WorldToggleButton のInspectorで Button > OnClick()
    ///    - 「Runtime Only」に設定
    ///    - QueueSystem を「Object」にドラッグ
    ///    - ドロップダウンで「QueueManager」→「OnToggleButtonClickEvent()」を選択
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
            
            // まず、アセット内のフォントを検索
            string packageFontPath = "Assets/WaitingQueue/Runtime/Resources/Fonts/JK-Maru-Gothic-M SDF.asset";
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
            canvasObject.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            canvasObject.transform.localRotation = Quaternion.identity;
            
            // Canvas設定
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            canvasObject.AddComponent<GraphicRaycaster>();
            
            // VRC UI Shape 設定（VRChat でのインタラクション有効化）
            canvasObject.AddComponent<VRC.SDK3.Components.VRCUiShape>();
            
            // レイヤーを Default に設定（UI レイヤーでは VRChat 外のインタラクションが無効）
            canvasObject.layer = LayerMask.NameToLayer("Default");
            
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
            scrollComponent.scrollSensitivity = 0f; // VRChat での移動中の誤発動を防止
            
            Image scrollImage = scrollViewObject.AddComponent<Image>();
            scrollImage.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
            scrollImage.raycastTarget = true;
            
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
            button.navigation = new Navigation { mode = Navigation.Mode.None }; // ナビゲーションを無効化
            
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);
            buttonImage.raycastTarget = true;
            
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
        
        /// <summary>
        /// プレイヤー追従型通知パネル（VR用）
        /// スマートウォッチ風のデザインとサイズに調整済み
        /// </summary>
        private GameObject CreatePlayerFollowNotificationPanel(Transform parent)
        {
            GameObject canvasObject = new GameObject("PlayerFollowNotification");
            canvasObject.transform.SetParent(parent);
            // 初期位置は仮置き（実行時にスクリプトで左手に飛びます）
            canvasObject.transform.localPosition = Vector3.zero;
            
            // Canvas設定 (World Space)
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 50; // 小さい文字もくっきりさせるため高めに
            

            // canvasObject.AddComponent<VRC.SDK3.Components.VRCUiShape>(); 
            // 念のため、Collider がついていたら削除する
            Collider col = canvasObject.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            // ▲▲▲ 修正ここまで ▲▲▲
            
            canvasObject.layer = LayerMask.NameToLayer("Default");
            
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            
            // ▼▼▼ スマートウォッチサイズに変更 ▼▼▼
            // キャンバス解像度
            canvasRect.sizeDelta = new Vector2(200, 75);
            // スケール: 0.0005 (実寸 幅15cm くらい)
            canvasRect.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);
            // ▲▲▲ 修正ここまで ▲▲▲

            
            // 1. 常時表示パネル（時計の文字盤部分）
            GameObject infoPanel = new GameObject("WristInfoPanel");
            infoPanel.transform.SetParent(canvasObject.transform, false);
            Image infoPanelImage = infoPanel.AddComponent<Image>();
            infoPanelImage.color = new Color(0.05f, 0.05f, 0.08f, 0.95f); // ほぼ黒
            
            RectTransform infoPanelRect = infoPanel.GetComponent<RectTransform>();
            infoPanelRect.anchorMin = Vector2.zero;
            infoPanelRect.anchorMax = Vector2.one;
            infoPanelRect.sizeDelta = Vector2.zero;

            // 自分の順番テキスト
            GameObject positionTextObj = new GameObject("WristPositionText");
            positionTextObj.transform.SetParent(infoPanel.transform, false);
            TextMeshProUGUI positionText = positionTextObj.AddComponent<TextMeshProUGUI>();
            if (_japaneseFontAsset != null) positionText.font = _japaneseFontAsset;
            
            positionText.text = "待機: -";
            positionText.fontSize = 13;
            positionText.alignment = TextAlignmentOptions.Center;
            positionText.color = new Color(0.7f, 0.7f, 0.7f);
            
            RectTransform positionTextRect = positionTextObj.GetComponent<RectTransform>();
            positionTextRect.anchorMin = new Vector2(0, 0.5f);
            positionTextRect.anchorMax = new Vector2(1, 0.9f);
            positionTextRect.sizeDelta = Vector2.zero;
            
            // 現在呼ばれている人のテキスト
            GameObject calledTextObj = new GameObject("WristCalledPlayerText");
            calledTextObj.transform.SetParent(infoPanel.transform, false);
            TextMeshProUGUI calledText = calledTextObj.AddComponent<TextMeshProUGUI>();
            if (_japaneseFontAsset != null) calledText.font = _japaneseFontAsset;
            
            calledText.text = "呼出: -";
            calledText.fontSize = 14;
            calledText.fontStyle = FontStyles.Bold;
            calledText.alignment = TextAlignmentOptions.Center;
            calledText.color = Color.white;
            
            RectTransform calledTextRect = calledTextObj.GetComponent<RectTransform>();
            calledTextRect.anchorMin = new Vector2(0, 0.1f);
            calledTextRect.anchorMax = new Vector2(1, 0.6f);
            calledTextRect.sizeDelta = Vector2.zero;
            

            // 2. 通知パネル（オーバーレイで全面に被せる）
            GameObject notificationPanel = new GameObject("NotificationPanel");
            notificationPanel.transform.SetParent(canvasObject.transform, false);
            Image panelImage = notificationPanel.AddComponent<Image>();
            panelImage.color = new Color(1f, 0.8f, 0f, 1f); // 黄色、不透明
            
            RectTransform panelRect = notificationPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            // 通知テキスト
            GameObject textObject = new GameObject("NotificationText");
            textObject.transform.SetParent(notificationPanel.transform, false);
            TextMeshProUGUI notificationText = textObject.AddComponent<TextMeshProUGUI>();
            if (_useJapanese && _japaneseFontAsset != null) notificationText.font = _japaneseFontAsset;
            
            notificationText.text = "あなたの番です！";
            notificationText.fontSize = 50;
            notificationText.fontStyle = FontStyles.Bold;
            notificationText.alignment = TextAlignmentOptions.Center;
            notificationText.color = Color.black;
            
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-10, -10);
            
            notificationPanel.SetActive(false);
            
            return canvasObject;
        }
        
        /// <summary>
        /// オーナー専用コントロールパネル（次の人を呼ぶボタンと現在の状態表示）
        /// </summary>
        private GameObject CreateOwnerControlPanel(Transform parent)
        {
            GameObject canvasObject = new GameObject("OwnerControlPanel");
            canvasObject.transform.SetParent(parent);
            canvasObject.transform.localPosition = new Vector3(0.7f, 0.5f, 0f);
            canvasObject.transform.localRotation = Quaternion.identity;
            
            // Canvas設定
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            
            canvasObject.AddComponent<GraphicRaycaster>();
            
            // VRC UI Shape 設定
            canvasObject.AddComponent<VRC.SDK3.Components.VRCUiShape>();
            
            // レイヤーを Default に設定
            canvasObject.layer = LayerMask.NameToLayer("Default");
            
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(500, 600);
            canvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            
            // 背景パネル
            GameObject backgroundPanel = new GameObject("Background");
            backgroundPanel.transform.SetParent(canvasObject.transform, false);
            Image bgImage = backgroundPanel.AddComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            RectTransform bgRect = backgroundPanel.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // タイトル
            CreateOwnerTitle(canvasObject.transform);
            
            // 現在呼ばれている人の表示
            CreateCurrentCalledDisplay(canvasObject.transform);
            
            // 待機人数表示
            CreateQueueCountDisplay(canvasObject.transform);
            
            // 次の人を呼ぶボタン
            CreateAdvanceButton(canvasObject.transform);
            
            // ★新規：戻すボタン
            CreateRestoreButton(canvasObject.transform);
            
            return canvasObject;
        }
        
        private void CreateOwnerTitle(Transform parent)
        {
            GameObject titleObject = new GameObject("OwnerTitle");
            titleObject.transform.SetParent(parent, false);
            TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
            
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                titleText.font = japaneseFont;
                titleText.text = "オーナーコントロール";
            }
            else
            {
                titleText.text = _useJapanese ? "オーナーコントロール" : "Owner Control";
            }
            
            titleText.fontSize = 32;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(0.3f, 0.8f, 1f);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.anchoredPosition = new Vector2(0, -30);
            titleRect.sizeDelta = new Vector2(-40, 50);
        }
        
        private void CreateCurrentCalledDisplay(Transform parent)
        {
            GameObject displayObject = new GameObject("CurrentCalledDisplay");
            displayObject.transform.SetParent(parent, false);
            
            // 背景
            Image bgImage = displayObject.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            RectTransform displayRect = displayObject.GetComponent<RectTransform>();
            displayRect.anchorMin = new Vector2(0.5f, 1);
            displayRect.anchorMax = new Vector2(0.5f, 1);
            displayRect.anchoredPosition = new Vector2(0, -120);
            displayRect.sizeDelta = new Vector2(440, 150);
            
            // ラベル
            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(displayObject.transform, false);
            TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
            
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                labelText.font = japaneseFont;
                labelText.text = "現在呼び出し中";
            }
            else
            {
                labelText.text = _useJapanese ? "現在呼び出し中" : "Currently Called";
            }
            
            labelText.fontSize = 20;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = new Color(0.7f, 0.7f, 0.7f);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.anchoredPosition = new Vector2(0, -15);
            labelRect.sizeDelta = new Vector2(-20, 30);
            
            // プレイヤー名表示
            GameObject nameObject = new GameObject("CurrentCalledText");
            nameObject.transform.SetParent(displayObject.transform, false);
            TextMeshProUGUI nameText = nameObject.AddComponent<TextMeshProUGUI>();
            
            if (_useJapanese && japaneseFont != null)
            {
                nameText.font = japaneseFont;
                nameText.text = "-";
            }
            else
            {
                nameText.text = "-";
            }
            
            nameText.fontSize = 36;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = new Color(1f, 0.9f, 0.3f);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.anchoredPosition = new Vector2(0, -15);
            nameRect.sizeDelta = new Vector2(-20, -50);
        }
        
        private void CreateQueueCountDisplay(Transform parent)
        {
            GameObject countObject = new GameObject("QueueCountDisplay");
            countObject.transform.SetParent(parent, false);
            
            // 背景
            Image bgImage = countObject.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
            RectTransform countRect = countObject.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.5f, 1);
            countRect.anchorMax = new Vector2(0.5f, 1);
            countRect.anchoredPosition = new Vector2(0, -300);
            countRect.sizeDelta = new Vector2(440, 80);
            
            // 待機人数テキスト
            GameObject textObject = new GameObject("QueueCountText");
            textObject.transform.SetParent(countObject.transform, false);
            TextMeshProUGUI countText = textObject.AddComponent<TextMeshProUGUI>();
            
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                countText.font = japaneseFont;
                countText.text = "待機中: 0人";
            }
            else
            {
                countText.text = _useJapanese ? "待機中: 0人" : "Waiting: 0";
            }
            
            countText.fontSize = 28;
            countText.alignment = TextAlignmentOptions.Center;
            countText.color = Color.white;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-20, -20);
        }
        
        private void CreateAdvanceButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("OwnerAdvanceButton");
            buttonObject.transform.SetParent(parent, false);
            
            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(-40, 80);
            buttonRect.sizeDelta = new Vector2(280, 100);
            
            Button button = buttonObject.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.15f, 0.4f, 0.15f, 1f);  // 暗めの緑
            buttonImage.raycastTarget = true;
            
            GameObject buttonTextObject = new GameObject("AdvanceButtonText");
            buttonTextObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI buttonText = buttonTextObject.AddComponent<TextMeshProUGUI>();
            
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                buttonText.font = japaneseFont;
                buttonText.text = "次の人を呼ぶ";
            }
            else
            {
                buttonText.text = _useJapanese ? "次の人を呼ぶ" : "Call Next Person";
            }
            
            buttonText.fontSize = 28;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
        }
        
        private void CreateRestoreButton(Transform parent)
        {
            GameObject buttonObject = new GameObject("OwnerRestoreButton");
            buttonObject.transform.SetParent(parent, false);
            
            RectTransform buttonRect = buttonObject.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0);
            buttonRect.anchorMax = new Vector2(0.5f, 0);
            buttonRect.anchoredPosition = new Vector2(160, 95);  // 右上に配置
            buttonRect.sizeDelta = new Vector2(100, 70);  // 小さいサイズ
            
            Button button = buttonObject.AddComponent<Button>();
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.4f, 0.3f, 0.15f, 1f);  // 茶色（戻す・アンドゥのイメージ）
            buttonImage.raycastTarget = true;
            
            GameObject buttonTextObject = new GameObject("RestoreButtonText");
            buttonTextObject.transform.SetParent(buttonObject.transform, false);
            TextMeshProUGUI buttonText = buttonTextObject.AddComponent<TextMeshProUGUI>();
            
            TMP_FontAsset japaneseFont = FindJapaneseFontAsset();
            if (_useJapanese && japaneseFont != null)
            {
                buttonText.font = japaneseFont;
                buttonText.text = "戻す";
            }
            else
            {
                buttonText.text = _useJapanese ? "戻す" : "Restore";
            }
            
            buttonText.fontSize = 20;
            buttonText.fontStyle = FontStyles.Bold;
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.color = Color.white;
            RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
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
        /// UdonSharp公式の拡張メソッドを使って安全にコンポーネントを追加する
        /// </summary>
        private static T AddUdonComponent<T>(GameObject target) where T : UdonSharpBehaviour
        {
            // ★超重要：AddUdonSharpComponent<T>() 拡張メソッドが一番確実です。
            // クラスの型から自動的に正しいAssetを見つけて割り当ててくれます。
            return target.AddUdonSharpComponent<T>();
        }

        /// <summary>
        /// 外部から呼び出すための静的メソッド
        /// </summary>
        public static string CreateQueueSystemUIStatic(bool useJapanese, TMP_FontAsset fontAsset)
        {
            _useJapanese = useJapanese;
            _japaneseFontAsset = fontAsset ?? FindJapaneseFontAsset();

            GameObject root = new GameObject("QueueSystem");
            QueueSystemBuilder b = CreateInstance<QueueSystemBuilder>();
            b.CreateWorldFixedPanel(root.transform);
            b.CreateOwnerControlPanel(root.transform);
            b.CreatePlayerFollowNotificationPanel(root.transform);
            b.CreateAudioSource(root.transform);

            // プレハブ保存
            string path = "Assets/WaitingQueue/Prefabs/QueueSystem.prefab";
            if (!System.IO.Directory.Exists("Assets/WaitingQueue/Prefabs"))
                System.IO.Directory.CreateDirectory("Assets/WaitingQueue/Prefabs");
                
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            // シーンに配置
            GameObject instance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)) as GameObject;
            
            if (instance != null)
            {
                instance.name = "QueueSystem";
                // セットアップ実行
                SetupQueueSystem(instance);
                
                Selection.activeGameObject = instance;
                Debug.Log("[QueueSystemBuilder] 完了: " + path);
            }
            return path;
        }
        
        private static void SetupQueueSystem(GameObject queueSystem)
        {
            Debug.Log("[QueueSystemBuilder] セットアップを開始します...");

            // 1. 各パネルの取得
            Transform worldPanelObj = queueSystem.transform.Find("WorldFixedPanel");
            Transform notifPanelObj = queueSystem.transform.Find("PlayerFollowNotification");
            Transform ownerPanelObj = queueSystem.transform.Find("OwnerControlPanel");

            // 2. コンポーネントの追加
            QueueManager qm = AddUdonComponent<QueueManager>(queueSystem);
            
            QueueUIManager ui = null;
            if (worldPanelObj) ui = AddUdonComponent<QueueUIManager>(worldPanelObj.gameObject);
            
            QueueNotificationManager notif = null;
            if (notifPanelObj) notif = AddUdonComponent<QueueNotificationManager>(notifPanelObj.gameObject);

            // エラーチェック
            if (qm == null) { Debug.LogError("[QueueSystemBuilder] QueueManagerの追加に失敗！"); return; }
            if (ui == null) { Debug.LogError("[QueueSystemBuilder] QueueUIManagerの追加に失敗！"); }
            if (notif == null) { Debug.LogError("[QueueSystemBuilder] QueueNotificationManagerの追加に失敗！"); }

            Debug.Log("[QueueSystemBuilder] コンポーネントの追加が完了しました");

            // 3. UI Managerの設定
            if (ui != null)
            {
                // --- ゲスト用パネル ---
                if (worldPanelObj != null)
                {
                    ui.worldQueueListText = worldPanelObj.Find("ScrollView/Viewport/Content/QueueListText")?.GetComponent<TextMeshProUGUI>();
                    ui.worldScrollRect = worldPanelObj.Find("ScrollView")?.GetComponent<ScrollRect>();
                    ui.worldToggleButton = worldPanelObj.Find("WorldToggleButton")?.GetComponent<Button>();
                    ui.worldButtonText = worldPanelObj.Find("WorldToggleButton/ButtonText")?.GetComponent<TextMeshProUGUI>();
                    
                    // ★自動化1: ゲスト用ボタンの配線
                    if (ui.worldToggleButton != null)
                    {
                        UdonBehaviour qmBacking = UdonSharpEditorUtility.GetBackingUdonBehaviour(qm);
                        if (qmBacking != null)
                        {
                            AddUdonEvent(ui.worldToggleButton, "m_OnClick", qmBacking, "OnToggleButtonClick");
                            Debug.Log("[QueueSystemBuilder] World Toggle Button を自動配線しました");
                        }
                    }
                }

                // --- オーナーパネル ---
                if (ownerPanelObj != null)
                {
                    ui.ownerCurrentCalledText = ownerPanelObj.Find("CurrentCalledDisplay/CurrentCalledText")?.GetComponent<TextMeshProUGUI>();
                    ui.ownerQueueCountText = ownerPanelObj.Find("QueueCountDisplay/QueueCountText")?.GetComponent<TextMeshProUGUI>();
                    ui.ownerAdvanceButton = ownerPanelObj.Find("OwnerAdvanceButton")?.GetComponent<Button>();
                    ui.ownerAdvanceButtonText = ownerPanelObj.Find("OwnerAdvanceButton/AdvanceButtonText")?.GetComponent<TextMeshProUGUI>();
                    ui.ownerRestoreButton = ownerPanelObj.Find("OwnerRestoreButton")?.GetComponent<Button>();  // ★新規
                }

                // --- プレイヤー腕表示 ---
                if (notifPanelObj != null)
                {
                    ui.wristPositionText = notifPanelObj.Find("WristInfoPanel/WristPositionText")?.GetComponent<TextMeshProUGUI>();
                    ui.wristCalledPlayerText = notifPanelObj.Find("WristInfoPanel/WristCalledPlayerText")?.GetComponent<TextMeshProUGUI>();
                }

                UdonSharpEditorUtility.CopyProxyToUdon(ui);
                Debug.Log("[QueueSystemBuilder] QueueUIManager の設定完了");
            }

            // 4. Notification Managerの設定
            if (notif != null && notifPanelObj != null)
            {
                notif.notificationPanel = notifPanelObj.Find("NotificationPanel")?.gameObject;
                notif.notificationText = notifPanelObj.Find("NotificationPanel/NotificationText")?.GetComponent<TextMeshProUGUI>();
                notif.notificationBackground = notifPanelObj.Find("NotificationPanel")?.GetComponent<Image>();
                notif.notificationAudioSource = queueSystem.transform.Find("NotificationAudio")?.GetComponent<AudioSource>();
                
                UdonSharpEditorUtility.CopyProxyToUdon(notif);
                Debug.Log("[QueueSystemBuilder] QueueNotificationManager の設定完了");
            }

            // 5. QueueManagerの設定
            if (qm != null)
            {
                qm.uiManager = ui;
                qm.notificationManager = notif;
                
                if (ownerPanelObj != null)
                {
                    qm.advanceButton = ownerPanelObj.Find("OwnerAdvanceButton")?.GetComponent<Button>();
                    qm.restoreButton = ownerPanelObj.Find("OwnerRestoreButton")?.GetComponent<Button>();

                    UdonBehaviour qmBacking = UdonSharpEditorUtility.GetBackingUdonBehaviour(qm);
                    if (qmBacking != null)
                    {
                        // ★自動化2: オーナー用「進むボタン」の配線
                        if (qm.advanceButton != null)
                        {
                            AddUdonEvent(qm.advanceButton, "m_OnClick", qmBacking, "AdvanceQueue");
                            Debug.Log("[QueueSystemBuilder] Advance Button を自動配線しました");
                        }

                        // ★自動化2.5: オーナー用「戻すボタン」の配線
                        if (qm.restoreButton != null)
                        {
                            AddUdonEvent(qm.restoreButton, "m_OnClick", qmBacking, "RestoreLastRemovedPlayer");
                            Debug.Log("[QueueSystemBuilder] Restore Button を自動配線しました");
                        }
                    }
                }

                UdonSharpEditorUtility.CopyProxyToUdon(qm);
                Debug.Log("[QueueSystemBuilder] QueueManager の設定完了");
            }
            
            Debug.Log("[QueueSystemBuilder] ✅ 全セットアップが正常に完了しました！");
            Debug.Log("[QueueSystemBuilder] 手動設定は不要です。すべて自動配線されています。");
        }

        /// <summary>
        /// 【修正版】SerializedObject を使ったイベント登録ヘルパー関数
        /// プロパティ名の揺らぎ（m_OnValueChanged / onValueChanged など）を自動吸収します。
        /// </summary>
        private static void AddUdonEvent(UnityEngine.Object uiComponent, string eventName, UdonBehaviour targetUdon, string udonMethodName)
        {
            if (uiComponent == null)
            {
                Debug.LogError($"[QueueSystemBuilder] エラー: イベント登録しようとしたコンポーネントが null です。({udonMethodName})");
                return;
            }

            SerializedObject so = new SerializedObject(uiComponent);
            
            // プロパティを探す（m_ あり/なし 両方試す）
            SerializedProperty eventProp = so.FindProperty(eventName);
            if (eventProp == null)
            {
                // "m_" がついていないパターン、あるいはついているパターンを試す
                string altName = eventName.StartsWith("m_") ? eventName.Substring(2) : "m_" + eventName;
                eventProp = so.FindProperty(altName);
            }

            // それでも見つからない場合
            if (eventProp == null)
            {
                // Toggleの可能性が高いので、Toggle専用のフォールバック
                if (uiComponent is Toggle)
                {
                    eventProp = so.FindProperty("onValueChanged"); // 一般的なプロパティ名
                    if (eventProp == null) eventProp = so.FindProperty("m_OnValueChanged"); // 内部名
                }
            }

            if (eventProp == null)
            {
                Debug.LogError($"[QueueSystemBuilder] エラー: {uiComponent.name} ({uiComponent.GetType().Name}) にイベントプロパティ '{eventName}' が見つかりません。");
                return;
            }

            SerializedProperty calls = eventProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
            
            if (calls == null)
            {
                Debug.LogError($"[QueueSystemBuilder] エラー: {uiComponent.name} の m_PersistentCalls.m_Calls にアクセスできません。");
                return;
            }

            // 既存のイベントをクリア
            calls.ClearArray();
            
            // 新しいイベントを追加
            calls.InsertArrayElementAtIndex(0);
            SerializedProperty newCall = calls.GetArrayElementAtIndex(0);
            
            // UdonのSendCustomEventを実行するように設定
            newCall.FindPropertyRelative("m_Target").objectReferenceValue = targetUdon;
            newCall.FindPropertyRelative("m_MethodName").stringValue = "SendCustomEvent";
            newCall.FindPropertyRelative("m_Mode").enumValueIndex = (int)UnityEngine.Events.PersistentListenerMode.String; // Stringモード
            newCall.FindPropertyRelative("m_Arguments.m_StringArgument").stringValue = udonMethodName; // 引数にメソッド名
            newCall.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEngine.Events.UnityEventCallState.RuntimeOnly; // Runtime Only

            so.ApplyModifiedProperties();
            Debug.Log($"[QueueSystemBuilder] イベント登録成功: {uiComponent.name} -> {udonMethodName}");
        }
    }
}
