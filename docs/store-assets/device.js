// Frames real screenshots from a phone (device/*.png, 1080x2340 from `adb exec-out screencap -p`) as Google Play
// phone screenshots (1080x1920): the status and navigation bars cut off, the screen in a phone frame under a caption.
// Run: node docs/store-assets/device.js  (needs Chrome or Edge installed)
const fs = require('fs'), path = require('path'), os = require('os'), { execFileSync } = require('child_process');
const here = __dirname, src = path.join(here, 'device'), out = path.join(here, 'phone-real');
const font = fs.readFileSync(path.join(here, '../../SideGigPh.Maui/wwwroot/fonts/dm-sans.woff2')).toString('base64');
const purple = '#5B2A86', tint = '#E9DFF3';
const top = 82, bottom = 2206; // A15 (1080x2340): status bar above, navigation bar below
// The live database holds ranking-test workers ("S9 Quote Aircon"), so their names and initials are painted over
// with sample names in the app's own font, at the A15 screenshots' pixel positions.
const muted = '#6A6A6A', ink = '#222222';
const text = (x, capTop, w, size, value, o = {}) => ({ x, y: capTop - Math.round(0.141 * size), w, h: Math.round(size * 1.1), size, value, ...o });
const cover = (x, y, w, h, bg = '#fff') => ({ x, y, w, h, bg });
const initials = (x, y, w, h, value, size, round) => ({ x, y, w, h, value, size, bg: ink, color: '#fff', center: true, radius: round ? '50%' : '24px' });
const resultName = (capTop, value, avatarTop, avatarBottom, ini) => [
  cover(288, capTop - 12, 760, 50), text(294, capTop, 740, 39, value, { color: muted }),
  initials(45, avatarTop, 214, avatarBottom - avatarTop + 1, ini, 66)];
const shots = [
  ['1-home', 'c-home', 'Find a skilled Kuya or Ate', 'Aircon, construction, errands and more.', [cover(0, 1912, 1080, 39)]],
  ['2-search', 'f-search-sheet', 'Pick what you need', 'Takes a minute.', []],
  ['3-results', 'a-results', 'See the price first', 'Then message the one you like.', [
    ...resultName(832, 'Rodel Manalo', 756, 968, 'RM'),
    ...resultName(1265, 'Jun Villanueva', 1189, 1402, 'JV'),
    ...resultName(1821, 'Arnel Bautista', 1745, 1960, 'AB')]],
  ['4-profile', 'b-profile', 'See what we checked', 'Reviews, experience and price.', [
    initials(405, 510, 270, 270, 'RM', 100, true),
    cover(40, 828, 1000, 92), text(0, 841, 1080, 73.5, 'Rodel Manalo', { weight: 700, color: ink, align: 'center' })]],
  ['5-post', 'e-post-form', 'Post what you need done', 'Workers near you send their price.', []],
];
const patch = (p) => `<div style="position:absolute;left:${p.x}px;top:${p.y}px;width:${p.w}px;height:${p.h}px;background:${p.bg || 'transparent'};
  border-radius:${p.radius || 0};color:${p.color || ink};font-size:${p.size || 0}px;font-weight:${p.weight || 400};line-height:${p.center ? p.h + 'px' : 1};
  text-align:${p.center ? 'center' : p.align || 'left'};white-space:nowrap">${p.value || ''}</div>`;
const h = bottom - top, sw = 712, sh = Math.round(sw * h / 1080);
const exe = ['C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(fs.existsSync);
const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'tarbaho-real-'));
fs.mkdirSync(out, { recursive: true });
for (const [name, file, title, sub, patches] of shots) {
  const img = 'data:image/png;base64,' + fs.readFileSync(path.join(src, file + '.png')).toString('base64');
  const html = `<!doctype html><html><head><meta charset="utf-8"><style>
@font-face{font-family:'DM Sans';src:url(data:font/woff2;base64,${font}) format('woff2');font-weight:100 1000}
html,body{margin:0;width:1080px;height:1920px;overflow:hidden;background:${purple};font-family:'DM Sans',sans-serif}
.cap{position:absolute;top:96px;left:60px;right:60px;text-align:center;color:#fff}
.cap h1{margin:0;font-size:78px;line-height:1.08;font-weight:700;letter-spacing:-1.5px}
.cap p{margin:22px 0 0;font-size:40px;color:${tint};font-weight:500}
.phone{position:absolute;left:170px;top:370px;width:${sw + 28}px;height:${sh + 28}px;border-radius:60px;background:#111;padding:14px;box-sizing:border-box}
.screen{width:${sw}px;height:${sh}px;border-radius:48px;overflow:hidden;background:#fff;position:relative}
.layer{position:absolute;left:0;top:${-top * sw / 1080}px;width:1080px;height:2340px;transform:scale(${sw / 1080});transform-origin:0 0}
.layer img{position:absolute;left:0;top:0;width:1080px}
</style></head><body><div class="cap"><h1>${title}</h1><p>${sub}</p></div>
<div class="phone"><div class="screen"><div class="layer"><img src="${img}">${patches.map(patch).join('')}</div></div></div></body></html>`;
  const f = path.join(tmp, name + '.html');
  fs.writeFileSync(f, html);
  execFileSync(exe, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
    `--user-data-dir=${path.join(tmp, 'profile')}`, '--window-size=1080,1920', '--virtual-time-budget=3000',
    `--screenshot=${path.join(out, name + '.png')}`, 'file:///' + f.split(path.sep).join('/')], { stdio: 'ignore' });
}
