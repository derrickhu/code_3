namespace Rescue
{
    /// <summary>
    /// 云存档。gameKey 用经分已经登记的 songjiang，不另开一个。
    /// 云函数目录、网关路径、GAME_KEY 环境变量三处都是 songjiang。
    /// </summary>
    public static class CloudConfig
    {
        public const string GameKey = "songjiang";
        // 真机只放行公众平台里配过的 request 域名。这条 service 域名和别的游戏是同一个，后台多半已经加过。
        public const string BaseUrl = "https://rosa-env-d7grf78r5dbd37323.service.tcloudbase.com";
        // 默认 HTTP 域名同样挂着 songjiang-api。service 被域名校验拦住时再试这一条。
        public const string AltUrl = "https://rosa-env-d7grf78r5dbd37323-1414200063.ap-shanghai.app.tcloudbase.com";
        /// <summary>云存储上的按需资源，域名和别的游戏同一个桶。曲子在 songjiang/StreamingAssets/Music，用到哪首下哪首。</summary>
        public const string AssetCdn = "https://726f-rosa-env-d7grf78r5dbd37323-1414200063.tcb.qcloud.la/songjiang";
        const string Prefix = "/" + GameKey + "-api";

        public const string LoginPath = Prefix + "/login";
        public const string PullPath = Prefix + "/save/pull";
        public const string PushPath = Prefix + "/save/push";
        public const string LivePath = Prefix + "/config";
        public const string RankSubmitPath = Prefix + "/rank/submit";
        public const string RankListPath = Prefix + "/rank/list";
        public const string AvatarPath = Prefix + "/avatar";
        public const string LiveCacheKey = GameKey + "_live";

        public const int RequestTimeoutSec = 10;
        public const int SchemaVersion = 1;

        public const string TokenKey = GameKey + "_token";
        public const string AnonKey = GameKey + "_anon_id";
        public const string SyncMetaKey = GameKey + "_cloud_meta";

        public const float StartupTimeout = 6f;
        public const float Debounce = 1.5f;
        public const float BaseDelay = 1.5f;
        public const float MaxBackoff = 30f;
        public const int MaxFail = 5;
        public const float RetryInterval = 60f;
    }
}
