#if (UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN) && !UNITY_EDITOR
#define RESCUE_WX
#endif
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if RESCUE_WX
using WeChatWASM;
#endif

namespace Rescue
{
    /// <summary>
    /// 微信启动能力：隐私弹框、头像昵称按钮、右上角转发、屏幕常亮、系统字体。
    /// 编辑器里没有这些接口，隐私弹框仍会出，同意后按已同意记。
    /// </summary>
    public static class WxSession
    {
        const string PrivacyKey = "rescue_privacy_v1";
        const string ProfileKey = "songjiang_wx_profile";

        [Serializable]
        class WxProfile
        {
            public string nick = "";
            public string avatar = "";
        }

        static WxProfile _profile;

        static readonly string[] ShareTitles =
        {
            "摆字聚义，救宋江哥哥",
            "这一关，宋江有点危险",
            "我在梁山排到座次了",
        };

        public static bool CanAskProfile
        {
            get
            {
#if RESCUE_WX
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>真机小游戏。编辑器和 Mac 包没有微信启动封面。</summary>
        public static bool InWeChat
        {
            get
            {
#if RESCUE_WX
                return true;
#else
                return false;
#endif
            }
        }

        public static bool PrivacyAgreed => PlayerPrefs.GetString(PrivacyKey, "") == "1";

        public static bool PrivacyChosen => PlayerPrefs.GetString(PrivacyKey, "").Length > 0;

        public static bool PrivacyShowing => _privacyOpen;

        /// <summary>微信昵称。没授权、或只拿到「微信用户」时为空。</summary>
        public static bool HasProfile
        {
            get { LoadProfile(); return _profile.nick.Length > 0; }
        }

        public static string Nick
        {
            get { LoadProfile(); return _profile.nick; }
        }

        public static string Avatar
        {
            get { LoadProfile(); return _profile.avatar ?? ""; }
        }

        /// <summary>已经有昵称、照片还是空的时候，再向微信要一次头像。</summary>
        public static void EnsureAvatar()
        {
            LoadProfile();
            if (_profile.nick.Length == 0 || _profile.avatar.Length > 0 || _avatarAsked) return;
            _avatarAsked = true;
#if RESCUE_WX
            string nick = _profile.nick;
            AskAvatar(a =>
            {
                if (string.IsNullOrEmpty(a)) return;
                SetProfile(nick, a);
            });
#endif
        }

        public static bool ProfileButtonVisible
        {
            get
            {
#if RESCUE_WX
                return _infoBtn != null;
#else
                return false;
#endif
            }
        }

        static bool _booted;
        static bool _timeline;
        static bool _privacyOpen;
        static bool _privacyBusy;
        static Action<bool> _privacyDone;
        static GameObject _privacyLayer;
        static bool _avatarAsked;
        static int _coverGen;

        static Font _sysFont;
        static bool _sysFontAsked;
        static bool _sysFontDone;
        static Action<Font> _sysFontWait;

        /// <summary>启动封面盖住加载。主界面画出来之后再揭，避免封面关掉后又闪一张启动图。</summary>
        public static void HideLoadingCover()
        {
#if RESCUE_WX
            try { WX.HideLoadingPage(); }
            catch (Exception e) { Debug.LogWarning("[Wx] hide loading " + e.Message); }
#endif
        }

        /// <summary>开机：常亮、转发菜单、系统字体。朋友圈回调等启动页淡出后再挂。</summary>
        public static void Boot()
        {
            if (_booted) return;
            _booted = true;
            if (Save.D != null && MergeProfileIntoSave()) Save.Remember();
            KeepAwake();
            SystemFont(null);
#if RESCUE_WX
            try
            {
                WX.OnShow(_ => KeepAwake());
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Wx] " + e.Message);
            }
#endif
        }

        public static void KeepAwake()
        {
#if RESCUE_WX
            try
            {
                WX.SetKeepScreenOn(new SetKeepScreenOnOption { keepScreenOn = true });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Wx] keepScreen " + e.Message);
            }
#endif
        }

        public static void EnableTimeline()
        {
            if (_timeline) return;
            _timeline = true;
            InstallShare();
#if RESCUE_WX
            try
            {
                WX.OnShareTimeline(done => done(new OnShareTimelineListenerResult { title = PickTitle() }));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Share] " + e.Message);
            }
#endif
        }

        static void InstallShare()
        {
#if RESCUE_WX
            try
            {
                WX.ShowShareMenu(new ShowShareMenuOption
                {
                    menus = new[] { "shareAppMessage", "shareTimeline" },
                });
                WX.OnShareAppMessage(new WXShareAppMessageParam { title = PickTitle() }, done =>
                {
                    done(new WXShareAppMessageParam { title = PickTitle() });
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Share] " + e.Message);
            }
#endif
        }

        static string PickTitle() => ShareTitles[UnityEngine.Random.Range(0, ShareTitles.Length)];

        /// <summary>还没选过就弹出。force 时「暂不」之后还能再问。已经同意过的不再弹。</summary>
        public static void ShowPrivacy(Action<bool> done, bool force = false)
        {
            if (Application.isBatchMode || PrivacyAgreed || (!force && PrivacyChosen))
            {
                done?.Invoke(PrivacyAgreed || Application.isBatchMode);
                return;
            }
            if (Ui.Root == null)
            {
                done?.Invoke(false);
                return;
            }
            _privacyDone += done;
            if (_privacyOpen) return;
            _privacyOpen = true;
            _privacyBusy = false;
            BuildPrivacy();
        }

        static void BuildPrivacy()
        {
            var layer = Ui.Fill(Ui.Root, new Color(0f, 0f, 0f, 0.72f), true, "privacy");
            layer.SetAsLastSibling();
            _privacyLayer = layer.gameObject;
            var card = Ui.WoodPanel(Ui.Fit(layer, 980), new Vector2(860, 900), new Vector2(0, -10), Pal.Hex("C9402E"), Pal.Hex("F3E6C6"));
            Ui.Title(card, "隐私保护提示", new Vector2(0, 340), 72);
            var body = Ui.Label(card,
                "游戏会使用微信登录，用来保存进度和排行榜座次。\n\n同意之后，再点一次头像，才会用你的微信昵称和头像上榜。\n\n可以先阅读《隐私保护指引》。",
                34, Pal.Ink, new Vector2(0, 70), new Vector2(740, 340), TextAnchor.UpperLeft);
            body.lineSpacing = 1.12f;

            var link = Ui.Label(card, "查看隐私保护指引", 36, Pal.Hex("1F5FD6"), new Vector2(0, -150), new Vector2(520, 64));
            link.raycastTarget = true;
            Ui.Tap(link, OpenContract);

            Ui.CartoonBtn(card, "同意并继续", new Vector2(460, 110), new Vector2(0, -270), Ui.BtnYellow, AgreePrivacy, 46, null, Ui.Edge);
            var later = Ui.Label(card, "暂不", 34, Pal.InkSoft, new Vector2(0, -390), new Vector2(240, 56));
            later.raycastTarget = true;
            Ui.Tap(later, () =>
            {
                if (_privacyBusy) return;
                Ui.Toast("排行榜先用默认头像和名字");
                ClosePrivacy(false);
            });
            Tween.Pop(card, 0.28f);
        }

        static void AgreePrivacy()
        {
            if (_privacyBusy) return;
            // 昵称头像只走微信自己的透明按钮。这里再调 RequirePrivacyAuthorize，
            // 指引没勾用户信息时会直接失败，玩家就被提示去改小游戏后台。
            ClosePrivacy(true);
        }

        static void OpenContract()
        {
#if RESCUE_WX
            try
            {
                WX.OpenPrivacyContract(new OpenPrivacyContractOption
                {
                    fail = e => Ui.Toast("这会儿打不开"),
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Privacy] " + e.Message);
                Ui.Toast("这会儿打不开");
            }
#else
            Ui.Toast("真机上会打开隐私保护指引");
#endif
        }

        /// <summary>保留给头像点击。昵称授权不再先过微信的隐私接口。</summary>
        public static void AllowPrivacyRetry() { }

        public static void EnsurePrivacy(Action<bool> done) => done?.Invoke(true);

        static void ClosePrivacy(bool ok)
        {
            if (!_privacyOpen) return;
            _privacyOpen = false;
            _privacyBusy = false;
            PlayerPrefs.SetString(PrivacyKey, ok ? "1" : "0");
            PlayerPrefs.Save();
            if (_privacyLayer) UnityEngine.Object.Destroy(_privacyLayer);
            _privacyLayer = null;
            Action<bool> wait = _privacyDone;
            _privacyDone = null;
            wait?.Invoke(ok);
        }

        public static Rect ScreenRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
            return Rect.MinMaxRect(Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y), Mathf.Max(bl.x, tr.x), Mathf.Max(bl.y, tr.y));
        }

        /// <summary>等这块停稳再盖微信的透明按钮。弹出动画没停时盖会偏，而且盖上去就不能改位置。</summary>
        public static void Cover(MonoBehaviour host, RectTransform hit, Action<string, string, string> got)
        {
            HideProfileButton();
            if (!host || !hit || !CanAskProfile) return;
            int gen = _coverGen;
            EnsurePrivacy(ok =>
            {
                if (!ok || gen != _coverGen || !host || !hit) return;
                host.StartCoroutine(CoverSoon(gen, hit, got));
            });
        }

        static IEnumerator CoverSoon(int gen, RectTransform hit, Action<string, string, string> got)
        {
            float still = 0f;
            float start = Time.realtimeSinceStartup;
            Rect last = default;
            while (gen == _coverGen && hit && still < 0.12f && Time.realtimeSinceStartup - start < 1.5f)
            {
                yield return null;
                if (gen != _coverGen || !hit) yield break;
                Canvas.ForceUpdateCanvases();
                Rect now = ScreenRect(hit);
                bool moved = Mathf.Abs(now.x - last.x) > 0.5f || Mathf.Abs(now.y - last.y) > 0.5f
                    || Mathf.Abs(now.width - last.width) > 0.5f || Mathf.Abs(now.height - last.height) > 0.5f
                    || now.width < 4f || now.height < 4f;
                if (moved)
                {
                    last = now;
                    still = 0f;
                }
                else still += Time.unscaledDeltaTime;
            }
            if (gen != _coverGen || !hit || last.width < 4f || last.height < 4f) yield break;
            ShowProfileButton(last, got);
        }

        public static void HideProfileButton()
        {
            _coverGen++;
#if RESCUE_WX
            if (_infoBtn == null) return;
            _infoBtn.Destroy();
            _infoBtn = null;
#endif
        }

#if RESCUE_WX
        static WXUserInfoButton _infoBtn;

        static void ShowProfileButton(Rect screenRect, Action<string, string, string> got)
        {
            RectInt r = ToPhysical(screenRect);
            if (r.width < 4 || r.height < 4) return;
            _infoBtn = WX.CreateUserInfoButton(r.x, r.y, r.width, r.height, "zh_CN", false);
            if (_infoBtn == null) return;
            _infoBtn.OnTap(res =>
            {
                string nick = "";
                string avatar = "";
                string err = res == null ? "no response" : (res.errCode + " " + res.errMsg);
                if (res != null)
                {
                    try
                    {
                        nick = res.userInfo.nickName;
                        avatar = res.userInfo.avatarUrl ?? "";
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning("[Profile] " + e.Message);
                    }
                    if (string.IsNullOrEmpty(nick) || string.IsNullOrEmpty(avatar))
                    {
                        string fromRaw = NickFromRaw(res.userInfoRaw, ref avatar);
                        if (string.IsNullOrEmpty(nick)) nick = fromRaw;
                    }
                }
                avatar = Https(avatar);
                if (!string.IsNullOrEmpty(nick))
                {
                    if (string.IsNullOrEmpty(avatar))
                        AskAvatar(a => got?.Invoke(nick, a ?? "", null));
                    else
                        got?.Invoke(nick, avatar, null);
                    return;
                }
                Debug.LogWarning("[Profile] " + err);
                if (Denied(err))
                {
                    got?.Invoke(null, null, err);
                    return;
                }
                // 按钮这次没带昵称。已经授权过时，直接问一次还能拿到。
                WX.GetUserInfo(new GetUserInfoOption
                {
                    lang = "zh_CN",
                    withCredentials = false,
                    success = info =>
                    {
                        string n = "";
                        string a = "";
                        if (info != null)
                        {
                            try
                            {
                                n = info.userInfo.nickName;
                                a = info.userInfo.avatarUrl ?? "";
                            }
                            catch (Exception e)
                            {
                                Debug.LogWarning("[Profile] " + e.Message);
                            }
                            if (string.IsNullOrEmpty(n))
                                n = NickFromRaw(info.rawData, ref a);
                        }
                        got?.Invoke(string.IsNullOrEmpty(n) ? null : n, a ?? "", string.IsNullOrEmpty(n) ? err : null);
                    },
                    fail = _ => got?.Invoke(null, null, err),
                });
            });
            _infoBtn.Show();
        }

        static RectInt ToPhysical(Rect screenRect)
        {
            var info = WX.GetSystemInfoSync();
            float winW = Convert.ToSingle(info.windowWidth);
            float dpr = Convert.ToSingle(info.pixelRatio);
            if (dpr < 1f) dpr = 1f;
            float scale = winW > 0f && Screen.width < winW * 1.5f ? dpr : 1f;
            return new RectInt(
                Mathf.RoundToInt(screenRect.xMin * scale),
                Mathf.RoundToInt((Screen.height - screenRect.yMax) * scale),
                Mathf.Max(1, Mathf.RoundToInt(screenRect.width * scale)),
                Mathf.Max(1, Mathf.RoundToInt(screenRect.height * scale)));
        }

        static void AskAvatar(Action<string> done)
        {
            try
            {
                WX.GetUserInfo(new GetUserInfoOption
                {
                    lang = "zh_CN",
                    withCredentials = false,
                    success = info =>
                    {
                        string a = "";
                        if (info != null)
                        {
                            try { a = info.userInfo.avatarUrl; }
                            catch (Exception e) { Debug.LogWarning("[Profile] " + e.Message); }
                            if (string.IsNullOrEmpty(a)) NickFromRaw(info.rawData, ref a);
                        }
                        done?.Invoke(Https(a));
                    },
                    fail = _ => done?.Invoke(""),
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Profile] " + e.Message);
                done?.Invoke("");
            }
        }
#else
        static void ShowProfileButton(Rect screenRect, Action<string, string, string> got) { }
#endif

        /// <summary>拒绝过一次之后，微信不会再弹授权框，只能去设置里打开用户信息。</summary>
        public static void AskOpenSetting(string content, Action<bool> done)
        {
#if RESCUE_WX
            try
            {
                WX.ShowModal(new ShowModalOption
                {
                    title = "需要你的授权",
                    content = content,
                    confirmText = "去设置",
                    cancelText = "取消",
                    showCancel = true,
                    success = m =>
                    {
                        if (!m.confirm) { done?.Invoke(false); return; }
                        WX.OpenSetting(new OpenSettingOption
                        {
                            success = s =>
                            {
                                bool ok = s.authSetting != null && s.authSetting.ContainsKey("scope.userInfo") && s.authSetting["scope.userInfo"];
                                done?.Invoke(ok);
                            },
                            fail = _ => done?.Invoke(false),
                        });
                    },
                    fail = _ => done?.Invoke(false),
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Profile] " + e.Message);
                done?.Invoke(false);
            }
#else
            done?.Invoke(false);
#endif
        }

        /// <summary>
        /// 本机授权优先。云端旧档没有昵称时写回来；云端有、本机没有时记到本机。
        /// 返回存档字段是否被本机授权改过。
        /// </summary>
        public static bool MergeProfileIntoSave()
        {
            if (Save.D == null) return false;
            LoadProfile();
            string cloud = CleanNick(Save.D.nick);
            bool cloudOk = cloud.Length > 0 && cloud != "微信用户";
            if (_profile.nick.Length > 0)
            {
                string avatar = _profile.avatar ?? "";
                bool changed = Save.D.nick != _profile.nick || (Save.D.avatar ?? "") != avatar;
                Save.D.nick = _profile.nick;
                Save.D.avatar = avatar;
                return changed;
            }
            if (!cloudOk) return false;
            _profile.nick = cloud;
            _profile.avatar = Https(Save.D.avatar);
            SaveProfile();
            return false;
        }

        /// <summary>昵称可用就记到本机并上报。拒绝「微信用户」这种空授权。</summary>
        public static bool SetProfile(string nick, string avatar)
        {
            nick = CleanNick(nick);
            if (nick.Length == 0 || nick == "微信用户") return false;
            LoadProfile();
            _profile.nick = nick;
            _profile.avatar = Https(avatar);
            SaveProfile();
            if (Save.D != null)
            {
                Save.D.nick = _profile.nick;
                Save.D.avatar = _profile.avatar;
                Save.Write();
            }
            LiveConfig.SubmitRank(true);
            return true;
        }

        static void LoadProfile()
        {
            if (_profile != null) return;
            string raw = PlayerPrefs.GetString(ProfileKey, "");
            try { _profile = string.IsNullOrEmpty(raw) ? null : JsonUtility.FromJson<WxProfile>(raw); }
            catch (Exception) { _profile = null; }
            if (_profile == null) _profile = new WxProfile();
            _profile.nick = CleanNick(_profile.nick);
            if (_profile.nick == "微信用户") _profile.nick = "";
            _profile.avatar = Https(_profile.avatar);
        }

        static void SaveProfile()
        {
            PlayerPrefs.SetString(ProfileKey, JsonUtility.ToJson(_profile ?? new WxProfile()));
            PlayerPrefs.Save();
        }

        static string Https(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";
            url = url.Trim();
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                url = "https://" + url.Substring("http://".Length);
            if (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return "";
            return url.Length <= 2000 ? url : "";
        }

        /// <summary>昵称可用就写入存档并上报。返回是否写上了。</summary>
        public static bool TakeProfile(string nick, string avatar, string err, Action retry)
        {
            if (SetProfile(nick, avatar))
            {
                HideProfileButton();
                Ui.Toast("已用微信头像上榜");
                return true;
            }
            HideProfileButton();
            if (Denied(err))
            {
                AskOpenSetting("排行榜需要你的微信昵称和头像。可以在设置里打开「用户信息」，回来后再点一次。", ok =>
                {
                    if (ok) retry?.Invoke();
                });
                return false;
            }
            if (Gm.Allowed)
                Ui.Toast("开发者工具拿不到微信昵称，用手机预览");
            else if (nick == "微信用户")
                Ui.Toast("请先在微信里设置昵称和头像");
            else
                Ui.Toast("没拿到微信昵称，再点一次");
            return false;
        }

#if RESCUE_WX
        [Serializable]
        class WxUserBits
        {
            public string nickName;
            public string avatarUrl;
        }

        static string NickFromRaw(string raw, ref string avatar)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            WxUserBits bits = null;
            try { bits = JsonUtility.FromJson<WxUserBits>(raw); }
            catch (Exception) { bits = null; }
            if (bits == null) return "";
            if (string.IsNullOrEmpty(avatar)) avatar = bits.avatarUrl ?? "";
            return bits.nickName ?? "";
        }
#endif

        static bool Denied(string err)
        {
            if (string.IsNullOrEmpty(err)) return false;
            return err.IndexOf("auth deny", StringComparison.OrdinalIgnoreCase) >= 0
                   || err.IndexOf("auth denied", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string CleanNick(string n)
        {
            if (string.IsNullOrEmpty(n)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char ch in n.Trim())
            {
                if (ch < 32 || ch == '<' || ch == '>') continue;
                sb.Append(ch);
                if (sb.Length >= 12) break;
            }
            return sb.ToString();
        }

        public static void ApplyNickFont(Text t)
        {
            if (!t) return;
            if (_sysFont) { t.font = _sysFont; return; }
            SystemFont(f => { if (t && f) t.font = f; });
        }

        public static void SystemFont(Action<Font> done)
        {
            if (_sysFontDone)
            {
                done?.Invoke(_sysFont);
                return;
            }
            if (done != null) _sysFontWait += done;
            if (_sysFontAsked) return;
            _sysFontAsked = true;
#if RESCUE_WX
            try
            {
                WX.GetWXFont(null, FinishFont);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Font] " + e.Message);
                FinishFont(null);
            }
#else
            try
            {
                FinishFont(Font.CreateDynamicFontFromOSFont(
                    new[] { "PingFang SC", "Heiti SC", "Noto Sans CJK SC", "Arial Unicode MS" }, 32));
            }
            catch (Exception)
            {
                FinishFont(null);
            }
#endif
        }

        static void FinishFont(Font f)
        {
            _sysFont = f;
            _sysFontDone = true;
            Action<Font> wait = _sysFontWait;
            _sysFontWait = null;
            wait?.Invoke(f);
        }
    }
}
