# Waiting Queue System - セットアップ

## 使用方法

### 1. セットアップウィンドウを開く
`Tools > Waiting Queue > Setup` メニューからセットアップウィンドウを開きます。

### 2. セットアップウィンドウで実行
- **ステップ1**: 言語を選択（日本語/英語）
- **ステップ2**: フォントアセットを作成（日本語選択時）
- **ステップ3**: UI構造を自動生成
- **ステップ4**: セットアップ完了

### 3. 手動設定

セットアップウィンドウで生成されたQueueSystemに以下を設定します：

#### マネージャーに UdonBehaviour を追加

1. **QueueManager** に UdonBehaviour を追加
   - Program Source → QueueManager.cs を割り当て

2. **QueueUIManager** に UdonBehaviour を追加
   - Program Source → QueueUIManager.cs を割り当て

3. **QueueNotificationManager** に UdonBehaviour を追加
   - Program Source → QueueNotificationManager.cs を割り当て

4. **QueueButtonHandler** に UdonBehaviour を追加
   - Program Source → QueueButtonHandler.cs を割り当て

#### 参照設定

**QueueManager の設定:**
- UI Manager → QueueUIManager の UdonBehaviour
- Notification Manager → QueueNotificationManager の UdonBehaviour

**QueueUIManager の設定:**
- Queue Manager → QueueManager の UdonBehaviour
- World Fixed Panel → WorldFixedPanel Canvas
- Player Notification Monitor → PlayerNotificationMonitor Canvas
- Queue List Text → QueueListText TextMeshProUGUI
- World Toggle Button → WorldToggleButton Button

**QueueNotificationManager の設定:**
- Queue Manager → QueueManager の UdonBehaviour
- Notification Panel → PlayerFollowNotification Canvas
- Notification Text → NotificationText TextMeshProUGUI
- Notification Audio → NotificationAudio AudioSource

**QueueButtonHandler の設定:**
- Queue Manager → QueueManager の UdonBehaviour

### 4. Prefab化

完成したQueueSystemをPrefabとして保存します。
- World Queue List Text → WorldFixedPanel の QueueListText
- World Scroll Rect → WorldFixedPanel の ScrollView
- World Toggle Button → WorldFixedPanel の ToggleButton
- World Button Text → WorldFixedPanel の ToggleButton/ButtonText
- Player Toggle Button → PlayerNotificationMonitor の ToggleButton
- Player Button Text → PlayerNotificationMonitor の ToggleButton/ButtonText
- Player Status Text → PlayerNotificationMonitor の StatusText

**QueueNotificationManager の設定:**
- Notification Panel → PlayerFollowNotification
- Notification Text → PlayerFollowNotification/NotificationPanel/NotificationText
- Notification Background → PlayerFollowNotification/NotificationPanel (Image)
- Notification Audio Source → NotificationAudio (AudioSource)
- Notification Sound → お好みの効果音 (AudioClip)

**QueueButtonHandler の設定:**
- Queue Manager → `QueueSystem` のQueueManager (UdonBehaviour)

**ボタンのOnClickイベント設定:**
- WorldFixedPanel/ToggleButton → ButtonHandler の `OnToggleButtonClick`
- PlayerNotificationMonitor/ToggleButton → ButtonHandler の `OnToggleButtonClick`

#### ステップ 5: Prefab化
1. `QueueSystem` をProject Viewの `Packages/uk.youkan.waiting-queue/Prefabs/` にドラッグ
2. Prefab名を `QueueSystem.prefab` に変更
3. シーンから削除

### 3. 使用方法

1. `QueueSystem.prefab` をシーンに配置
2. 必要に応じて位置を調整
3. テストプレイで動作確認

## トラブルシューティング

### スクリプトがコンパイルされない
- Unity エディタを再起動
- Packages フォルダを確認
- UdonSharp のバージョンを確認

### 参照が null になる
- Inspector で参照が正しく設定されているか確認
- UdonBehaviour コンポーネントが正しくアタッチされているか確認

### ボタンが反応しない
- ボタンの OnClick イベントが正しく設定されているか確認
- ButtonHandler の QueueManager 参照が設定されているか確認
