namespace Rescue
{
    /// <summary>
    /// 云存档。gameKey 用经分已经登记的 songjiang，不另开一个。
    /// 云函数目录、网关路径、GAME_KEY 环境变量三处都是 songjiang。
    /// </summary>
    public static class CloudConfig
    {
        public const string GameKey = "songjiang";
        // 新建路由挂在默认 HTTP 域名上。service 域名要等旧缓存过期，刚建时会间歇 INVALID_PATH。
        public const string BaseUrl = "https://rosa-env-d7grf78r5dbd37323-1414200063.ap-shanghai.app.tcloudbase.com";
        const string Prefix = "/" + GameKey + "-api";

        public const string LoginPath = Prefix + "/login";
        public const string PullPath = Prefix + "/save/pull";
        public const string PushPath = Prefix + "/save/push";

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
