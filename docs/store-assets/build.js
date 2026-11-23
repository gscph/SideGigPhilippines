// Renders the Google Play developer page images (header 4096x2304 under 1 MB, icon 512x512) from the Tarbaho brand.
// Run: node docs/store-assets/build.js  (needs Chrome or Edge installed)
const fs = require('fs'), path = require('path'), { execFileSync } = require('child_process');
const here = __dirname;
const font = fs.readFileSync(path.join(here, '../../SideGigPh.Maui/wwwroot/fonts/dm-sans.woff2')).toString('base64');
const purple = '#5B2A86', tint = '#E9DFF3';
const mark = (fill) => `
  <path fill="${fill}" d="M4.08 3.6H13.2L11.76 7.92H2.64Z"/>
  <path fill="${fill}" fill-opacity="0.78" d="M13.2 3.6H21.36L19.92 7.92H11.76Z"/>
  <path fill="${fill}" fill-opacity="0.62" d="M10.32 7.92H15.12L11.76 20.4H6.96Z"/>
  <path fill="${fill}" d="M10.32 7.92H15.12L13.68 10.8Z"/>`;
const page = (w, h, body) => `<!doctype html><html><head><meta charset="utf-8"><style>
@font-face{font-family:'DM Sans';src:url(data:font/woff2;base64,${font}) format('woff2');font-weight:100 1000}
html,body{margin:0;width:${w}px;height:${h}px;overflow:hidden;background:${purple};font-family:'DM Sans',sans-serif;color:#fff}
</style></head><body>${body}</body></html>`;

const header = page(4096, 2304, `
<svg width="4096" height="2304" viewBox="0 0 4096 2304" style="position:absolute;inset:0">
  <rect width="4096" height="2304" fill="${purple}"/>
  <g transform="translate(2900,-300) scale(70)" opacity="0.07">${mark('#fff')}</g>
  <g transform="translate(-400,1250) scale(55)" opacity="0.05">${mark('#fff')}</g>
</svg>
<div style="position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center">
  <div style="display:flex;align-items:center;gap:56px">
    <svg width="380" height="380" viewBox="0 0 24 24">${mark('#fff')}</svg>
    <div style="font-size:340px;font-weight:700;letter-spacing:-8px;line-height:1">Tarbaho</div>
  </div>
  <div style="margin-top:90px;font-size:128px;font-weight:600;line-height:1.15">Skilled Kuya and Ate near you</div>
  <div style="margin-top:44px;font-size:84px;font-weight:400;color:${tint};opacity:.9">Aircon · Construction · Errands · Driver · Mekaniko · Pest Control</div>
  <div style="margin-top:110px;padding:30px 70px;border:5px solid rgba(255,255,255,.55);border-radius:999px;font-size:72px;font-weight:500">Angono · Antipolo · Taytay, Rizal</div>
</div>`);

const icon = page(512, 512, `
<svg width="512" height="512" viewBox="0 0 456 456"><rect width="456" height="456" fill="${purple}"/>
<g transform="translate(78,78) scale(12.5)">${mark('#fff')}</g></svg>`);

const feature = page(1024, 500, `
<svg width="1024" height="500" style="position:absolute;inset:0"><rect width="1024" height="500" fill="${purple}"/>
  <g transform="translate(700,-80) scale(26)" opacity="0.07">${mark('#fff')}</g></svg>
<div style="position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;text-align:center">
  <div style="display:flex;align-items:center;gap:18px">
    <svg width="110" height="110" viewBox="0 0 24 24">${mark('#fff')}</svg>
    <div style="font-size:100px;font-weight:700;letter-spacing:-2px;line-height:1">Tarbaho</div>
  </div>
  <div style="margin-top:26px;font-size:40px;font-weight:600">Skilled Kuya and Ate near you</div>
  <div style="margin-top:14px;font-size:26px;color:${tint}">Prices shown first · No commissions</div>
</div>`);

const browsers = ['C:/Program Files/Google/Chrome/Application/chrome.exe','C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'];
const exe = browsers.find(fs.existsSync);
const tmp = fs.mkdtempSync(path.join(require('os').tmpdir(), 'tarbaho-'));
for (const [name, html, w, h] of [['developer-page-header', header, 4096, 2304], ['developer-page-icon', icon, 512, 512], ['feature-graphic', feature, 1024, 500]]) {
  const file = path.join(tmp, name + '.html'); fs.writeFileSync(file, html);
  execFileSync(exe, ['--headless=new','--disable-gpu','--hide-scrollbars','--force-device-scale-factor=1',
    `--user-data-dir=${path.join(tmp,'profile')}`, `--window-size=${w},${h}`, '--virtual-time-budget=3000',
    `--screenshot=${path.join(here, name + '.png')}`, 'file:///' + file.split(path.sep).join('/')], { stdio: 'inherit' });
}
