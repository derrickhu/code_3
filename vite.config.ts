import path from 'node:path';
import fs from 'node:fs';
import { fileURLToPath } from 'node:url';
import { defineConfig, type Plugin } from 'vitest/config';

const rootDir = path.dirname(fileURLToPath(import.meta.url));
const pkg = JSON.parse(
  fs.readFileSync(path.resolve(rootDir, 'package.json'), 'utf8'),
) as { version: string };

export const BUNDLE_DIR = '.bundle';

function pixiUnsafeEvalPlugin(): Plugin {
  return {
    name: 'pixi-unsafe-eval-patch',
    writeBundle(options) {
      const outDir = options.dir || BUNDLE_DIR;
      const bundlePath = path.resolve(outDir, 'game-bundle.js');
      if (!fs.existsSync(bundlePath)) return;
      const code = fs.readFileSync(bundlePath, 'utf8');
      const re =
        /systemCheck\(\)\{if\(!\w+\(\)\)throw new Error\("Current environment does not allow unsafe-eval[^}]*\}/g;
      const patched = code.replace(re, 'systemCheck(){}');
      if (patched !== code) {
        fs.writeFileSync(bundlePath, patched);
        console.log('[pixi-unsafe-eval-patch] 已处理 systemCheck');
      }
    },
  };
}

function assemblePlatformsPlugin(): Plugin {
  return {
    name: 'assemble-platforms',
    async writeBundle() {
      // @ts-expect-error 组装脚本是 plain JS，与 code_2 同一份
      const { assembleAll } = await import('./scripts/build-platform.mjs');
      assembleAll(process.env.CODE3_PLATFORM ?? 'wechat');
    },
  };
}

export default defineConfig({
  define: {
    __APP_VERSION__: JSON.stringify(pkg.version),
  },
  resolve: {
    alias: {
      '@': path.resolve(rootDir, 'src'),
    },
    dedupe: ['@pixi/core', '@pixi/display', '@pixi/settings', '@pixi/constants', '@pixi/utils'],
  },
  publicDir: false,
  plugins: [pixiUnsafeEvalPlugin(), assemblePlatformsPlugin()],
  build: {
    outDir: BUNDLE_DIR,
    assetsInlineLimit: 0,
    lib: {
      entry: path.resolve(rootDir, 'src/main.ts'),
      formats: ['iife'],
      name: 'Code3',
      fileName: () => 'game-bundle.js',
    },
    rollupOptions: {
      output: { inlineDynamicImports: true },
    },
    minify: 'esbuild',
    emptyOutDir: true,
  },
  test: {
    globals: true,
    environment: 'node',
  },
});
