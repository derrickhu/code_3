using UnityEditor;

namespace Rescue.EditorTools
{
    /// <summary>Resources/Art 下的图一律按 Sprite 导入：不缩放到 2 的幂，1 个世界单位 = 图片长边。</summary>
    public sealed class ArtImporter : AssetPostprocessor
    {
        public override uint GetVersion() => 2;

        void OnPreprocessTexture()
        {
            if (!assetPath.Contains("/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = UnityEngine.SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.GetSourceTextureWidthAndHeight(out int w, out int h);
            if (w > 0) ti.spritePixelsPerUnit = System.Math.Max(w, h);
            var file = System.IO.Path.GetFileName(assetPath);
            if (file.StartsWith("pc_") || file.StartsWith("hr_") || file.StartsWith("en_")) ti.SetPlatformTextureSettings(Figure());
        }

        /// <summary>棋盘上的人物图：微信上 ASTC 4x4、原尺寸、最高质量，首包不被几张大图撑爆。</summary>
        static TextureImporterPlatformSettings Figure() => new TextureImporterPlatformSettings
        {
            name = "WeixinMiniGame",
            overridden = true,
            maxTextureSize = 2048,
            format = TextureImporterFormat.ASTC_4x4,
            textureCompression = TextureImporterCompression.CompressedHQ,
            compressionQuality = 100,
        };
    }
}
