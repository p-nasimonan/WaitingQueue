using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;

namespace Youkan.WaitingQueue.Editor
{
    /// <summary>
    /// 待機列システム構築ウィンドウ
    /// 手動作成したフォントアセットを使用してUI構築を実行します。
    /// </summary>
    public class QueueSystemSetup : EditorWindow
    {
        private bool _useJapanese = true;
        private TMP_FontAsset _japaneseFontAsset;
        private Vector2 _scrollPosition = Vector2.zero;

        [MenuItem("Tools/Waiting Queue/Build System")]
        public static void ShowWindow()
        {
            GetWindow<QueueSystemSetup>("待機列システム構築");
        }

        private void OnEnable()
        {
            FindJapaneseFontAsset();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("待機列システム構築", EditorStyles.largeLabel);
            GUILayout.Space(20);
            
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            
            // 言語設定
            EditorGUILayout.LabelField("言語設定", EditorStyles.boldLabel);
            _useJapanese = EditorGUILayout.Toggle("日本語を使用する", _useJapanese);
            GUILayout.Space(15);

            // フォント確認（日本語時）
            if (_useJapanese)
            {
                EditorGUILayout.LabelField("フォント確認", EditorStyles.boldLabel);
                
                if (_japaneseFontAsset != null)
                {
                    EditorGUILayout.HelpBox(
                        $"✓ 日本語フォント: {_japaneseFontAsset.name}",
                        MessageType.Info
                    );
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "日本語フォントアセット (JK-Maru-Gothic-M SDF) が見つかりません。\n\nTextMeshProの標準Font Asset Creatorで作成してください。",
                        MessageType.Warning
                    );
                    GUILayout.EndScrollView();
                    return;
                }
                
                GUILayout.Space(15);
            }

            // UI構築
            EditorGUILayout.LabelField("UI構築", EditorStyles.boldLabel);

            if (_useJapanese && _japaneseFontAsset == null)
            {
                EditorGUILayout.HelpBox(
                    "日本語フォントを先に作成してください。",
                    MessageType.Warning
                );
                GUILayout.EndScrollView();
                return;
            }

            string uiInfo = _useJapanese 
                ? $"フォント: {(_japaneseFontAsset?.name ?? "未設定")}\n\nUIを作成します。"
                : "英語UIを作成します。";

            EditorGUILayout.HelpBox(uiInfo, MessageType.Info);

            GUILayout.Space(10);
            if (GUILayout.Button("UI構造を作成", GUILayout.Height(40)))
            {
                CreateQueueSystemUI();
            }

            GUILayout.EndScrollView();
        }

        private void FindJapaneseFontAsset()
        {
            if (_japaneseFontAsset != null) return;

            string packageFontPath = "Assets/WaitingQueue/Runtime/Resources/Fonts/JK-Maru-Gothic-M SDF.asset";
            _japaneseFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(packageFontPath);

            if (_japaneseFontAsset == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset JK-Maru");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _japaneseFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }
        }

        private void CreateQueueSystemUI()
        {
            // QueueSystemBuilderを参照して実行
            string prefabPath = QueueSystemBuilder.CreateQueueSystemUIStatic(_useJapanese, _japaneseFontAsset);

            if (!string.IsNullOrEmpty(prefabPath))
            {
                EditorUtility.DisplayDialog(
                    "完了",
                    $"UIプレハブを作成し、シーンに配置しました。\n\nプレハブパス: {prefabPath}\n\n次にUdonBehaviourコンポーネントを追加してください。",
                    "OK"
                );
            }
            else
            {
                EditorUtility.DisplayDialog("エラー", "UIプレハブの作成に失敗しました。", "OK");
            }
        }


    }
}
