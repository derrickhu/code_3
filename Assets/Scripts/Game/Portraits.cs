using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>排行榜和主界面头像。没有微信头像时用默认立绘，下到的图按地址缓存。</summary>
    public sealed class Portraits : MonoBehaviour
    {
        static Portraits _it;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Dictionary<int, string> Want = new Dictionary<int, string>();

        static Portraits It
        {
            get
            {
                if (_it != null) return _it;
                var go = new GameObject("Portraits");
                DontDestroyOnLoad(go);
                _it = go.AddComponent<Portraits>();
                return _it;
            }
        }

        public static void Set(Image img, string url, bool fill = false)
        {
            if (!img) return;
            img.preserveAspect = !fill;
            int id = img.GetInstanceID();
            if (string.IsNullOrEmpty(url) || !url.StartsWith("https://", StringComparison.Ordinal))
            {
                var fallback = Art.Img("ui_avatar");
                if (fallback) img.sprite = fallback;
                Want.Remove(id);
                return;
            }
            if (Want.TryGetValue(id, out var pending) && pending == url)
            {
                if (Cache.TryGetValue(url, out var have) && have) img.sprite = have;
                return;
            }
            if (Cache.TryGetValue(url, out var ready) && ready)
            {
                img.sprite = ready;
                Want[id] = url;
                return;
            }
            Want[id] = url;
            It.StartCoroutine(It.Load(img, url));
        }

        static bool WxHost(string url)
        {
            int scheme = url.IndexOf("://", StringComparison.Ordinal);
            if (scheme < 0) return false;
            int start = scheme + 3;
            int slash = url.IndexOf('/', start);
            string host = slash < 0 ? url.Substring(start) : url.Substring(start, slash - start);
            int colon = host.IndexOf(':');
            if (colon >= 0) host = host.Substring(0, colon);
            host = host.ToLowerInvariant();
            return host == "thirdwx.qlogo.cn" || host == "wx.qlogo.cn"
                || host == "mmbiz.qpic.cn" || host == "thirdqq.qlogo.cn";
        }

        IEnumerator Load(Image img, string url)
        {
            Texture2D tex = null;
            // 微信头像域名不在这款小游戏的 request 名单里，改从已经能连上的接口拿图。
            if (WxHost(url))
                yield return Pull(url, t => tex = t);
            if (!tex)
                yield return Direct(url, t => tex = t);
            if (!img) yield break;
            if (!Want.TryGetValue(img.GetInstanceID(), out var want) || want != url) yield break;
            if (!tex) yield break;
            tex.wrapMode = TextureWrapMode.Clamp;
            if (!Cache.TryGetValue(url, out var sprite) || !sprite)
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[url] = sprite;
            img.sprite = sprite;
            img.enabled = true;
        }

        [Serializable]
        class AvatarAsk
        {
            public string url;
        }

        [Serializable]
        class AvatarPack
        {
            public bool ok;
            public AvatarBits data;
        }

        [Serializable]
        class AvatarBits
        {
            public string b64;
        }

        IEnumerator Pull(string url, Action<Texture2D> done)
        {
            long status = 0;
            string text = null;
            yield return Backend.CallText(CloudConfig.AvatarPath, JsonUtility.ToJson(new AvatarAsk { url = url }), (s, t, e) =>
            {
                status = s;
                text = t;
            });
            if (status != 200 || string.IsNullOrEmpty(text))
            {
                done(null);
                yield break;
            }
            AvatarPack pack = null;
            try { pack = JsonUtility.FromJson<AvatarPack>(text); }
            catch (Exception) { pack = null; }
            if (pack == null || pack.data == null || string.IsNullOrEmpty(pack.data.b64))
            {
                done(null);
                yield break;
            }
            byte[] bytes;
            try { bytes = Convert.FromBase64String(pack.data.b64); }
            catch (Exception)
            {
                done(null);
                yield break;
            }
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
            {
                done(null);
                yield break;
            }
            done(tex);
        }

        IEnumerator Direct(string url, Action<Texture2D> done)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                req.timeout = CloudConfig.RequestTimeoutSec;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[Avatar] " + req.responseCode + " " + req.error);
                    done(null);
                    yield break;
                }
                var bytes = req.downloadHandler != null ? req.downloadHandler.data : null;
                if (bytes == null || bytes.Length == 0)
                {
                    done(null);
                    yield break;
                }
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!tex.LoadImage(bytes))
                {
                    done(null);
                    yield break;
                }
                done(tex);
            }
        }
    }
}
