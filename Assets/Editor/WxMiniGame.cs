using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Rescue.EditorTools
{
    /// <summary>
    /// 命令行：-executeMethod Rescue.EditorTools.WxMiniGame.Export
    /// 产物在 wechat-minigame/minigame/，用微信开发者工具打开这一层。
    /// </summary>
    public static class WxMiniGame
    {
        const string AppId = "wxe1acde0de051578b";
        const string ConfigPath = "Assets/WX-WASM-SDK-V2/Editor/MiniGameConfig.asset";

        public static void Export()
        {
            int code = 1;
            try
            {
                EnsureConfig();
                ActivateWeChat();
                Debug.Log("[微信] 开始转换，第一次编 WASM 要比较久。");
                object result = Invoke("WeChatWASM.WXConvertCore", "DoExport", new object[] { true });
                bool ok = result != null && result.ToString() == "SUCCEED";
                Debug.Log("[微信] 转换结果：" + (result ?? "没找到转换接口"));
                code = ok && Directory.Exists(Path.Combine(ProjectRoot, "wechat-minigame", "minigame")) && CheckGameJs() ? 0 : 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[微信] 转换失败：" + e);
            }
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }

        static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        static void EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ConfigPath);
            if (config == null)
                throw new Exception("缺少 " + ConfigPath);
            var so = new SerializedObject(config);
            Set(so, "ProjectConf.projectName", "rescue-songjiang");
            Set(so, "ProjectConf.Appid", AppId);
            Set(so, "ProjectConf.relativeDST", "wechat-minigame");
            Set(so, "ProjectConf.DST", Path.Combine(ProjectRoot, "wechat-minigame"));
            Set(so, "ProjectConf.assetLoadType", 1);
            // 压缩的首包资源启动时会被插件解压后整份写进用户文件缓存，开发者工具 10MB 的上限装不下。
            Set(so, "ProjectConf.compressDataPackage", false);
            Set(so, "ProjectConf.Orientation", 0);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        /// <summary>包体超 30MB 时转换器会悄悄改回 CDN 加载，而我们没有 CDN，必须拦下来。</summary>
        static bool CheckGameJs()
        {
            string js = File.ReadAllText(Path.Combine(ProjectRoot, "wechat-minigame", "minigame", "game.js"));
            bool sub = js.Contains("loadDataPackageFromSubpackage: true");
            bool raw = js.Contains("compressDataPackage: false");
            Debug.Log($"[微信] 分包加载首包={sub}，不压缩首包={raw}");
            if (!sub || !raw) Debug.LogError("[微信] 首包资源没走未压缩的小游戏分包，检查包体是否超限。");
            return sub && raw;
        }

        static void ActivateWeChat()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settings != null && settings.Length > 0)
            {
                var so = new SerializedObject(settings[0]);
                var sub = so.FindProperty("activeSubplatform");
                if (sub != null)
                {
                    sub.intValue = 1;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            PlayerSettings.productName = "营救宋江";
            PlayerSettings.companyName = "Rescue";
        }

        static object Invoke(string typeName, string method, object[] args)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(typeName);
                if (t == null) continue;
                var m = t.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
                if (m == null) continue;
                return m.Invoke(null, args);
            }
            return null;
        }

        static void Set(SerializedObject so, string path, object value)
        {
            var parts = path.Split('.');
            var p = so.FindProperty(parts[0]);
            for (int i = 1; i < parts.Length && p != null; i++)
                p = p.FindPropertyRelative(parts[i]);
            if (p == null) return;
            if (value is string s) p.stringValue = s;
            else if (value is int n) p.intValue = n;
            else if (value is bool b) p.boolValue = b;
        }
    }
}
