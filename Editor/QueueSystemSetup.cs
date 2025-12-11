using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;

namespace Youkan.WaitingQueue.Editor
{
    /// <summary>
    /// 待機列システム構築ウィンドウ
    /// フォント作成とUI構築を1つの画面で実行します。
    /// </summary>
    public class QueueSystemSetup : EditorWindow
    {
        private bool _useJapanese = true;
        private Font _sourceFont;
        private string _fontOutputPath = "Packages/uk.youkan.waiting-queue/Assets/font/";
        private string _fontCharacters = "";
        private TMP_FontAsset _japaneseFontAsset;
        private Vector2 _scrollPosition = Vector2.zero;

        [MenuItem("Tools/Waiting Queue/Build System")]
        public static void ShowWindow()
        {
            GetWindow<QueueSystemSetup>("待機列システム構築");
        }

        private void OnEnable()
        {
            string fontPath = "Packages/uk.youkan.waiting-queue/Assets/font/JK-Maru-Gothic-M.ttf";
            _sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            LoadFontCharacters();
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

            // フォント作成（日本語時）
            if (_useJapanese)
            {
                EditorGUILayout.LabelField("フォント作成", EditorStyles.boldLabel);
                _sourceFont = (Font)EditorGUILayout.ObjectField("フォント (TTF)", _sourceFont, typeof(Font), false);
                
                EditorGUILayout.LabelField($"読み込み文字数: {_fontCharacters.Length}", EditorStyles.miniLabel);
                GUILayout.Space(10);

                if (_sourceFont != null && !string.IsNullOrEmpty(_fontCharacters))
                {
                    EditorGUILayout.HelpBox(
                        $"フォント: {_sourceFont.name}\n文字セット: japanese_full.txt ({_fontCharacters.Length}文字)",
                        MessageType.Info
                    );

                    if (GUILayout.Button("フォントアセットを作成", GUILayout.Height(35)))
                    {
                        CreateFontAsset();
                    }

                    if (_japaneseFontAsset != null)
                    {
                        EditorGUILayout.HelpBox(
                            $"✓ 作成済み: {_japaneseFontAsset.name}",
                            MessageType.Info
                        );
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "フォントまたは文字セットが見つかりません。",
                        MessageType.Warning
                    );
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
                ? $"フォント: {(_japaneseFontAsset?.name ?? "未作成")}\n\nUIを作成します。"
                : "英語UIを作成します。";

            EditorGUILayout.HelpBox(uiInfo, MessageType.Info);

            GUILayout.Space(10);
            if (GUILayout.Button("UI構造を作成", GUILayout.Height(40)))
            {
                CreateQueueSystemUI();
            }

            GUILayout.EndScrollView();
        }

        private void LoadFontCharacters()
        {
            string txtPath = "Packages/uk.youkan.waiting-queue/Assets/japanese_full.txt";
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(txtPath);
            
            if (textAsset != null)
            {
                _fontCharacters = textAsset.text;
            }
            else
            {
                Debug.LogWarning("[QueueSystemSetup] japanese_full.txt not found");
            }
        }

        private void FindJapaneseFontAsset()
        {
            if (_japaneseFontAsset != null) return;

            string packageFontPath = "Packages/uk.youkan.waiting-queue/Assets/font/JK-Maru-Gothic-M SDF.asset";
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

        private void CreateFontAsset()
        {
            if (_sourceFont == null)
            {
                EditorUtility.DisplayDialog("エラー", "フォントを選択してください。", "OK");
                return;
            }

            if (!Directory.Exists(_fontOutputPath))
            {
                Directory.CreateDirectory(_fontOutputPath);
            }

            string assetPath = Path.Combine(_fontOutputPath, $"{_sourceFont.name} SDF.asset");
            
            // フォントアセットを作成
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(_sourceFont);

            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("エラー", "フォント作成に失敗しました。", "OK");
                return;
            }

            // 文字を追加
            if (!string.IsNullOrEmpty(_fontCharacters))
            {
                fontAsset.TryAddCharacters(_fontCharacters);
            }

            // 重要: CreateFontAssetが生成したマテリアル・テクスチャはフォントアセット内に含まれている
            // テクスチャをPNGファイルとして保存（参照用）
            string texturePath = Path.Combine(_fontOutputPath, $"{_sourceFont.name} SDF Atlas.png");
            if (fontAsset.atlasTexture != null)
            {
                byte[] pngData = fontAsset.atlasTexture.EncodeToPNG();
                if (pngData != null && pngData.Length > 0)
                {
                    File.WriteAllBytes(texturePath, pngData);
                    AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
                    Debug.Log($"[QueueSystemSetup] Saved atlas texture: {texturePath}");
                }
            }

            // フォントアセット自体を保存
            // 注意: CreateFontAssetが生成したマテリアルは自動的にフォントアセットに関連付けられる
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            
            // マテリアルをフォントアセットのサブアセットとして保存
            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{_sourceFont.name} SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, assetPath);
                Debug.Log($"[QueueSystemSetup] Added material as sub-asset: {fontAsset.material.name}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // アセットをリロード（ディスク上の完全に初期化されたアセットを取得）
            _japaneseFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            
            if (_japaneseFontAsset != null)
            {
                // マテリアルの確認
                Material mat = _japaneseFontAsset.material;
                Texture2D tex = _japaneseFontAsset.atlasTexture;
                
                Debug.Log($"[QueueSystemSetup] Font asset created successfully");
                Debug.Log($"  - Font: {_japaneseFontAsset.name}");
                Debug.Log($"  - Material: {mat?.name ?? "None"}");
                Debug.Log($"  - Texture: {tex?.name ?? "None"}");
                
                if (mat != null && tex != null)
                {
                    EditorUtility.DisplayDialog("完了", "フォントアセットを作成しました。", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("警告", "フォントアセットが作成されましたが、マテリアルまたはテクスチャが見つかりません。", "OK");
                }
            }
            else
            {
                Debug.LogError("[QueueSystemSetup] Failed to reload font asset");
                EditorUtility.DisplayDialog("エラー", "フォントアセットの保存に失敗しました。", "OK");
            }
        }

        private void CreateQueueSystemUI()
        {
            // QueueSystemBuilderを参照して実行
            QueueSystemBuilder.CreateQueueSystemUIStatic(_useJapanese, _japaneseFontAsset);

            EditorUtility.DisplayDialog(
                "完了",
                "UI構造を作成しました。\n\n次にUdonBehaviourコンポーネントを追加してください。",
                "OK"
            );
        }


    }
}
