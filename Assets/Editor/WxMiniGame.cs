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
                long packs = ArtPacks.Build(EditorUserBuildSettings.activeBuildTarget);
                Debug.Log($"[微信] 图包 {packs / 1048576f:0.0} MB，发布前跑 node tools/upload_cdn.mjs 把 wechat-minigame/webgl/StreamingAssets 传到 CDN。");
                Debug.Log("[微信] 开始转换，第一次编 WASM 要比较久。");
                object result = Invoke("WeChatWASM.WXConvertCore", "DoExport", new object[] { true });
                bool ok = result != null && result.ToString() == "SUCCEED";
                Debug.Log("[微信] 转换结果：" + (result ?? "没找到转换接口"));
                bool exported = ok && Directory.Exists(Path.Combine(ProjectRoot, "wechat-minigame", "minigame"));
                if (exported) ApplyLoadingCover();
                code = exported && CheckGameJs() ? 0 : 1;
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
            Set(so, "ProjectConf.CDN", CloudConfig.AssetCdn);
            Set(so, "ProjectConf.relativeDST", "wechat-minigame");
            Set(so, "ProjectConf.DST", Path.Combine(ProjectRoot, "wechat-minigame"));
            Set(so, "ProjectConf.assetLoadType", 1);
            // 压缩的首包资源启动时会被插件解压后整份写进用户文件缓存，开发者工具 10MB 的上限装不下。
            Set(so, "ProjectConf.compressDataPackage", false);
            Set(so, "ProjectConf.Orientation", 0);
            var hideCover = so.FindProperty("ProjectConf")?.FindPropertyRelative("HideAfterCallMain");
            if (hideCover != null)
            {
                if (hideCover.propertyType == SerializedPropertyType.Boolean) hideCover.boolValue = false;
                else if (hideCover.propertyType == SerializedPropertyType.Integer) hideCover.intValue = 0;
            }
            var update = so.FindProperty("ProjectConf")?.FindPropertyRelative("needCheckUpdate");
            if (update != null)
            {
                if (update.propertyType == SerializedPropertyType.Boolean) update.boolValue = true;
                else if (update.propertyType == SerializedPropertyType.Integer) update.intValue = 1;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        /// <summary>微信默认封面是橙色进度页，进游戏后又单独放健康忠告。导出后改成和游戏内同一张水墨页。</summary>
        static void ApplyLoadingCover()
        {
            string script = Path.Combine(ProjectRoot, "tools", "wechat_loading_cover.py");
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python3",
                Arguments = "\"" + script + "\"",
                WorkingDirectory = ProjectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using (var p = System.Diagnostics.Process.Start(psi))
            {
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit();
                if (!string.IsNullOrEmpty(stdout)) Debug.Log("[微信] " + stdout.Trim());
                if (p.ExitCode != 0)
                    Debug.LogError("[微信] 启动封面没换成健康忠告同页：" + stderr);
            }
        }

        /// <summary>
        /// 首包（画面、字体、音效）不到 30MB 时留在小游戏分包里；超了插件会改成从 CDN 整包下载。
        /// 美术质量优先，超 30MB 只提醒，发布前把 webgl.data 传到 CDN 即可。曲子和角色图包始终在 CDN。
        /// </summary>
        static bool CheckGameJs()
        {
            string js = File.ReadAllText(Path.Combine(ProjectRoot, "wechat-minigame", "minigame", "game.js"));
            bool sub = js.Contains("loadDataPackageFromSubpackage: true");
            bool raw = js.Contains("compressDataPackage: false");
            bool cdn = js.Contains(CloudConfig.AssetCdn);
            Debug.Log($"[微信] 分包加载首包={sub}，不压缩首包={raw}，资源 CDN={cdn}");
            if (!sub) Debug.LogWarning("[微信] 首包超 30MB，已改成从 CDN 下载。发布前要把 wechat-minigame/webgl/Build/webgl.data 传到 CDN。");
            if (!cdn) Debug.LogError("[微信] game.js 里没有资源 CDN 地址，曲子和角色图包会拉不到。");
            return cdn;
        }

        static void ActivateWeChat()
        {
            // Mac 演示包会把当前平台切走；图包按当前平台打，导出前切回微信。
            try
            {
                var group = (BuildTargetGroup)Enum.Parse(typeof(BuildTargetGroup), "MiniGame");
                var target = (BuildTarget)Enum.Parse(typeof(BuildTarget), "WeixinMiniGame");
                if (EditorUserBuildSettings.activeBuildTarget != target)
                    EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[微信] 切换 MiniGame 平台失败，请在 Build Settings 里把微信小游戏点成 Active：" + e.Message);
            }
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
