using UnityEditor;
using UnityEngine;
using TMPro;
using TMPro.EditorUtilities;
using System.IO;

namespace Youkan.WaitingQueue.Editor
{
    /// <summary>
    /// 日本語フォントアセットを作成するエディタースクリプトです。
    /// </summary>
    public class FontAssetCreator : EditorWindow
    {
        private Font sourceFont;
        private string outputPath = "Packages/uk.youkan.waiting-queue/Assets/font/";
        private int fontSize = 42;
        private string characters = "";
        
        // [MenuItem("Tools/Waiting Queue/Create Japanese Font Asset")]  // QueueSystemSetupから呼び出し
        public static void ShowWindow()
        {
            GetWindow<FontAssetCreator>("日本語フォント作成");
        }
        
        private void OnEnable()
        {
            // デフォルトのフォントをロード (TTF)
            string fontPath = "Packages/uk.youkan.waiting-queue/Assets/font/JK-Maru-Gothic-M.ttf";
            sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            
            // 日本語文字セットをロード
            LoadDefaultCharacterSet();
        }
        
        private void LoadDefaultCharacterSet()
        {
            // japanese_full.txt から文字セットをロード
            string txtPath = "Packages/uk.youkan.waiting-queue/Assets/japanese_full.txt";
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(txtPath);
            
            if (textAsset != null)
            {
                characters = textAsset.text;
                Debug.Log($"[FontAssetCreator] Loaded {characters.Length} characters from japanese_full.txt");
            }
            else
            {
                Debug.LogWarning($"[FontAssetCreator] japanese_full.txt not found at {txtPath}, using fallback character set");
                // フォールバック: 基本的な文字セット
                characters = "";
                
                // ASCII文字 (英数字、記号)
                for (int i = 32; i <= 126; i++)
                {
                    characters += (char)i;
                }
                
                // ひらがな (U+3040 - U+309F)
                for (int i = 0x3040; i <= 0x309F; i++)
                {
                    characters += (char)i;
                }
                
                // カタカナ (U+30A0 - U+30FF)
                for (int i = 0x30A0; i <= 0x30FF; i++)
                {
                    characters += (char)i;
                }
                
                Debug.Log($"[FontAssetCreator] Loaded {characters.Length} fallback characters");
            }
        }
        
        private void CreateFontAsset()
        {
            if (sourceFont == null)
            {
                EditorUtility.DisplayDialog("エラー", "元フォントを選択してください。", "OK");
                return;
            }
            
            // 出力パスを確保
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            
            string assetPath = Path.Combine(outputPath, $"{sourceFont.name} SDF.asset");
            
            // フォントアセットを作成（デフォルト設定）
            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            
            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("エラー", "フォントアセットの作成に失敗しました。", "OK");
                return;
            }
            
            // 文字を追加
            fontAsset.TryAddCharacters(characters);
            
            // アセットとして保存
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            
            // テクスチャも保存
            string texturePath = Path.Combine(outputPath, $"{sourceFont.name} SDF Atlas.png");
            if (fontAsset.atlasTexture != null)
            {
                byte[] pngData = fontAsset.atlasTexture.EncodeToPNG();
                File.WriteAllBytes(texturePath, pngData);
                AssetDatabase.ImportAsset(texturePath);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // 作成したアセットを選択
            Selection.activeObject = fontAsset;
            EditorGUIUtility.PingObject(fontAsset);
            
            Debug.Log($"[FontAssetCreator] フォントアセットを作成しました: {assetPath}");
            EditorUtility.DisplayDialog(
                "完了",
                $"フォントアセットを作成しました！\n\nパス: {assetPath}\n\n文字数: {characters.Length}",
                "OK"
            );
        }
    }
}
