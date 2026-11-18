// Codex 사용량 표시: 가장 최근 Codex 세션 파일의 rate_limits를 읽어 한 줄로 출력
const fs = require("fs");
const path = require("path");
const os = require("os");

const root = process.argv[2] ||
  path.join(process.env.CODEX_HOME || path.join(os.homedir(), ".codex"), "sessions");

function newestJsonl(dir) {
  let best = null;
  const walk = (d) => {
    let entries;
    try { entries = fs.readdirSync(d, { withFileTypes: true }); } catch { return; }
    for (const e of entries) {
      const p = path.join(d, e.name);
      if (e.isDirectory()) walk(p);
      else if (e.name.endsWith(".jsonl")) {
        const m = fs.statSync(p).mtimeMs;
        if (!best || m > best.m) best = { p, m };
      }
    }
  };
  walk(dir);
  return best;
}

function lastRateLimits(file) {
  const lines = fs.readFileSync(file, "utf8").split("\n");
  for (let i = lines.length - 1; i >= 0; i--) {
    if (!lines[i].includes('"rate_limits"')) continue;
    try {
      const j = JSON.parse(lines[i]);
      const rl = j.payload && j.payload.rate_limits;
      if (rl && rl.primary) return { rl, ts: j.timestamp };
    } catch {}
  }
  return null;
}

const pad = (n) => String(n).padStart(2, "0");
const fmt = (sec, withDate) => {
  const d = new Date(sec * 1000);
  const t = `${pad(d.getHours())}:${pad(d.getMinutes())}`;
  return withDate ? `${d.getMonth() + 1}/${d.getDate()} ${t}` : t;
};
const now = Date.now() / 1000;
const show = (name, w, withDate) => {
  if (!w) return `${name} ?`;
  if (w.resets_at && w.resets_at < now) return `${name} 리셋됨(사용 0%)`;
  return `${name} 사용 ${Math.round(w.used_percent)}% (리셋 ${fmt(w.resets_at, withDate)})`;
};

const f = newestJsonl(root);
if (!f) { console.log(`Codex 사용량: 세션 파일 없음 (${root})`); process.exit(0); }
const r = lastRateLimits(f.p);
if (!r) { console.log("Codex 사용량: 최근 세션에 기록 없음 (Codex를 한 번 호출한 뒤 다시 확인)"); process.exit(0); }

const base = r.ts ? fmt(Date.parse(r.ts) / 1000, true) : "?";
console.log(`Codex 5h ${show("", r.rl.primary).trim()} · 주간 ${show("", r.rl.secondary, true).trim()} · 기준 ${base}`);
