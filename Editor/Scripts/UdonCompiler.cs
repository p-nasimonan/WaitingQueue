using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace Youkan.WaitingQueue.Editor
{
    /// <summary>
    /// UdonSharp C# スクリプト自動コンパイルツール
    /// C#ファイルを検出してUdonにコンパイルし、.assetファイルを自動生成します。
    /// </summary>
    public class UdonCompiler : EditorWindow
    {
        private Vector2 _scrollPosition = Vector2.zero;
        private string _targetFolder = "Assets/WaitingQueue/Runtime/scripts";
        private List<string> _compileLog = new List<string>();

        [MenuItem("Tools/Waiting Queue/Udon Compiler")]
        public static void ShowWindow()
        {
            GetWindow<UdonCompiler>("Udon Compiler");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("UdonSharp Compiler", EditorStyles.largeLabel);
            GUILayout.Space(15);

            EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);
            _targetFolder = EditorGUILayout.TextField("対象フォルダ:", _targetFolder);

            GUILayout.Space(15);

            if (GUILayout.Button("スクリプトを再インポート（.asset自動生成）", GUILayout.Height(40)))
            {
                ReimportScripts();
            }

            if (GUILayout.Button(".assetファイルを生成", GUILayout.Height(40)))
            {
                GenerateProgramAssets();
            }

            if (GUILayout.Button("アセットデータベースをリフレッシュ", GUILayout.Height(40)))
            {
                RefreshAssetDatabase();
            }

            GUILayout.Space(15);
            EditorGUILayout.LabelField("コンパイルログ", EditorStyles.boldLabel);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
            
            if (_compileLog.Count == 0)
            {
                EditorGUILayout.HelpBox("ログなし", MessageType.Info);
            }
            else
            {
                foreach (var log in _compileLog)
                {
                    EditorGUILayout.TextArea(log, GUILayout.MinHeight(20));
                }
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("ログをクリア", GUILayout.Height(30)))
            {
                _compileLog.Clear();
            }
        }

        private void ReimportScripts()
        {
            _compileLog.Clear();
            AddLog("=== スクリプト再インポート開始 ===");

            if (!Directory.Exists(_targetFolder))
            {
                AddLog($"エラー: フォルダが見つかりません: {_targetFolder}");
                EditorUtility.DisplayDialog("エラー", $"フォルダが見つかりません: {_targetFolder}", "OK");
                return;
            }

            // .cs ファイルを検索
            string[] csFiles = Directory.GetFiles(_targetFolder, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".meta"))
                .ToArray();

            if (csFiles.Length == 0)
            {
                AddLog("警告: C#ファイルが見つかりません");
                EditorUtility.DisplayDialog("警告", "C#ファイルが見つかりません", "OK");
                return;
            }

            AddLog($"検出されたファイル数: {csFiles.Length}");

            int count = 0;
            foreach (var csFile in csFiles)
            {
                string normalizedPath = csFile.Replace("\\", "/");
                int assetsIndex = normalizedPath.IndexOf("Assets");
                
                if (assetsIndex == -1) continue;

                string assetPath = normalizedPath.Substring(assetsIndex);
                
                try
                {
                    AddLog($"再インポート中: {Path.GetFileName(csFile)}");
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    count++;
                }
                catch (System.Exception ex)
                {
                    AddLog($"エラー: {Path.GetFileName(csFile)} - {ex.Message}");
                }
            }

            AddLog($"再インポート完了: {count}個");
            AddLog("アセットデータベースをリフレッシュ中...");
            
            // 重要: ForceUpdate オプションで強制的にアセットを再構築
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            
            AddLog("✓ 完了");
            AddLog("注: .assetファイルはUdonSharpEditorが自動生成します");
            AddLog("Unityエディタを少し待ってから、Projectウィンドウを確認してください");

            EditorUtility.DisplayDialog(
                "完了",
                $"{count}個のスクリプトを再インポートしました。\n\n" +
                ".assetファイルはUdonSharpが自動生成します。\n" +
                "Projectウィンドウをリフレッシュ(Ctrl+R)して確認してください。",
                "OK"
            );
        }

        private void RefreshAssetDatabase()
        {
            _compileLog.Clear();
            AddLog("=== アセットデータベースをリフレッシュ ===");

            EditorUtility.DisplayProgressBar("リフレッシュ", "アセットを再構築中...", 0.5f);

            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                AddLog("✓ リフレッシュ完了");
                
                // 生成されたアセットファイルを確認
                string[] assetFiles = Directory.GetFiles(_targetFolder, "*.asset", SearchOption.AllDirectories)
                    .Where(f => !f.EndsWith(".meta"))
                    .ToArray();

                AddLog($"検出された.assetファイル: {assetFiles.Length}個");
                foreach (var asset in assetFiles)
                {
                    AddLog($"  - {Path.GetFileName(asset)}");
                }
            }
            catch (System.Exception ex)
            {
                AddLog($"✗ エラー: {ex.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.DisplayDialog(
                "完了",
                "アセットデータベースをリフレッシュしました。",
                "OK"
            );
        }

        private void GenerateProgramAssets()
        {
            _compileLog.Clear();
            AddLog("=== プログラムアセット生成開始 ===");

            if (!Directory.Exists(_targetFolder))
            {
                AddLog($"エラー: フォルダが見つかりません: {_targetFolder}");
                EditorUtility.DisplayDialog("エラー", $"フォルダが見つかりません: {_targetFolder}", "OK");
                return;
            }

            // .cs ファイルを検索
            string[] csFiles = Directory.GetFiles(_targetFolder, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith(".meta"))
                .ToArray();

            if (csFiles.Length == 0)
            {
                AddLog("警告: C#ファイルが見つかりません");
                return;
            }

            AddLog($"検出されたファイル数: {csFiles.Length}");
            AddLog("\n=== UdonSharpEditor 利用可能なメソッドを検索中 ===");

            int successCount = 0;
            
            foreach (var csFile in csFiles)
            {
                string normalizedPath = csFile.Replace("\\", "/");
                string fileName = Path.GetFileNameWithoutExtension(csFile);
                
                int assetsIndex = normalizedPath.IndexOf("Assets");
                if (assetsIndex == -1) continue;

                string assetPath = normalizedPath.Substring(assetsIndex);

                try
                {
                    AddLog($"\n処理中: {fileName}");

                    // スクリプトをロード
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
                    if (script == null)
                    {
                        AddLog($"  ⚠ スクリプトがロードできませんでした");
                        continue;
                    }

                    System.Type scriptType = script.GetClass();
                    if (scriptType == null)
                    {
                        AddLog($"  ⚠ クラスが見つかりません");
                        continue;
                    }

                    // UdonSharpBehaviour かどうか確認
                    if (!IsUdonSharpBehaviour(scriptType))
                    {
                        AddLog($"  ℹ UdonSharpBehaviour ではありません");
                        continue;
                    }

                    // UdonSharpEditorUtility でメソッドを探す
                    bool created = TryCreateProgramAsset(script);
                    if (created)
                    {
                        AddLog($"✓ アセット生成成功: {fileName}");
                        successCount++;
                    }
                    else
                    {
                        AddLog($"  ⚠ アセット生成に失敗しました（別方法を試す...）");
                    }
                }
                catch (System.Exception ex)
                {
                    AddLog($"✗ エラー: {fileName} - {ex.Message}");
                }
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            AddLog("\n=== アセット生成完了 ===");
            AddLog($"成功: {successCount}");
            AddLog("\n注: UdonSharp の自動生成に委ねることをお勧めします");
            AddLog("コンパイル後、少し時間を置いてから Project ウィンドウをリフレッシュしてください");

            EditorUtility.DisplayDialog(
                "完了",
                $"{successCount}個のアセットを処理しました。\n\n" +
                "UdonSharp が自動的に .asset ファイルを生成します。\n" +
                "Project ウィンドウをリフレッシュ(Ctrl+R)して確認してください。",
                "OK"
            );
        }

        private bool TryCreateProgramAsset(MonoScript script)
        {
            try
            {
                // UdonSharpEditorUtility を取得
                var editorUtilityType = System.Type.GetType("UdonSharpEditor.UdonSharpEditorUtility, UdonSharp.Editor");
                
                if (editorUtilityType == null)
                {
                    AddLog("    UdonSharpEditor が見つかりません");
                    return false;
                }

                // 利用可能なメソッドを探す
                string[] methodNames = new[]
                {
                    "CreateUdonSharpProgramAsset",
                    "CreateUdonProgramAsset",
                    "CreateProgramAsset",
                    "CreateUdonProgram"
                };

                foreach (var methodName in methodNames)
                {
                    var method = editorUtilityType.GetMethod(methodName, 
                        BindingFlags.Public | BindingFlags.Static,
                        null,
                        new[] { typeof(MonoScript) },
                        null);

                    if (method != null)
                    {
                        AddLog($"    メソッド検出: {methodName}");
                        method.Invoke(null, new object[] { script });
                        return true;
                    }
                }

                // メソッドが見つからない場合は全メソッドを列挙（デバッグ用）
                var methods = editorUtilityType.GetMethods(BindingFlags.Public | BindingFlags.Static);
                AddLog($"    利用可能なメソッド数: {methods.Length}");
                foreach (var method in methods)
                {
                    if (method.Name.Contains("Create") || method.Name.Contains("Program"))
                    {
                        AddLog($"      - {method.Name}");
                    }
                }

                return false;
            }
            catch (System.Exception ex)
            {
                AddLog($"    リフレクション処理エラー: {ex.Message}");
                return false;
            }
        }

        private bool IsUdonSharpBehaviour(System.Type type)
        {
            try
            {
                var udonSharpBehaviourType = System.Type.GetType("UdonSharp.UdonSharpBehaviour, UdonSharp.Runtime");
                return udonSharpBehaviourType != null && type.IsSubclassOf(udonSharpBehaviourType);
            }
            catch
            {
                return false;
            }
        }

        private void AddLog(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            _compileLog.Add($"[{timestamp}] {message}");
        }
    }
}

