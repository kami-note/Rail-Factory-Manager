import react from '@vitejs/plugin-react';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { defineConfig } from 'vite';

export default defineConfig(({ command }) => {
  const localEnv = loadLocalEnv(process.cwd());
  const bffTarget = firstNonBlank(process.env.VITE_DEV_BFF_TARGET, localEnv.VITE_DEV_BFF_TARGET);
  const allowedHosts = normalizeAllowedHosts(firstNonBlank(process.env.VITE_ALLOWED_HOST, localEnv.VITE_ALLOWED_HOST));

  if (command === 'serve' && !bffTarget) {
    throw new Error('VITE_DEV_BFF_TARGET must be configured when running the Vite dev server.');
  }

  return {
    plugins: [react()],
    server: {
      host: '0.0.0.0',
      port: 5082,
      allowedHosts,
      proxy: {
        '/api': {
          target: bffTarget!,
          changeOrigin: false,
          xfwd: true
        },
        '/auth/google': {
          target: bffTarget!,
          changeOrigin: false,
          xfwd: true
        }
      }
    }
  };
});

function firstNonBlank(...values: Array<string | undefined>) {
  return values.find(value => value?.trim());
}

function loadLocalEnv(workDir: string) {
  const envPath = resolve(workDir, '.env.local');
  try {
    const content = readFileSync(envPath, 'utf8');
    const entries: Record<string, string> = {};

    for (const line of content.split(/\r?\n/)) {
      const trimmed = line.trim();
      if (!trimmed || trimmed.startsWith('#')) {
        continue;
      }

      const separatorIndex = trimmed.indexOf('=');
      if (separatorIndex <= 0) {
        continue;
      }

      const key = trimmed.slice(0, separatorIndex).trim();
      const rawValue = trimmed.slice(separatorIndex + 1).trim();
      entries[key] = normalizeLocalEnvValue(rawValue);
    }

    return entries;
  } catch {
    return {};
  }
}

function normalizeLocalEnvValue(value: string) {
  if (value.length >= 2 && value.startsWith('"') && value.endsWith('"')) {
    return value.slice(1, -1);
  }

  if (value.length >= 2 && value.startsWith("'") && value.endsWith("'")) {
    return value.slice(1, -1);
  }

  return value;
}

function normalizeAllowedHosts(value: string | undefined) {
  if (!value?.trim()) {
    return [];
  }

  return value
    .split(',')
    .map(item => item.trim())
    .filter(Boolean)
    .map(item => {
      try {
        return new URL(item).hostname;
      } catch {
        return item;
      }
    });
}
