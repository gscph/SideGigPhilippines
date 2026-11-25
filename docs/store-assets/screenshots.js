// Renders the Google Play phone screenshots (1080x1920): five of the app's real screens, rebuilt with clean sample
// data (the live database holds ranking-test workers), each under a caption on the brand purple.
// Copy comes from SideGigPh.Maui/Services/Localization/Copy.cs and the icons from wwwroot/images/icons/.
// Run: node docs/store-assets/screenshots.js  (needs Chrome or Edge installed)
const fs = require('fs'), path = require('path'), os = require('os'), { execFileSync } = require('child_process');
const here = __dirname, app = path.join(here, '../../SideGigPh.Maui/wwwroot');
const font = fs.readFileSync(path.join(app, 'fonts/dm-sans.woff2')).toString('base64');
const icon = (name) => 'data:image/svg+xml;base64,' + fs.readFileSync(path.join(app, 'images/icons', name + '.svg')).toString('base64');
const C = { purple: '#5B2A86', tint: '#E9DFF3', ink: '#222222', muted: '#6A6A6A', line: '#DDDDDD', soft: '#F7F7F7', green: '#118C4F', gold: '#FFB400' };
const logo = (fill) => `<svg width="34" height="34" viewBox="0 0 24 24">
  <path fill="${fill}" d="M4.08 3.6H13.2L11.76 7.92H2.64Z"/><path fill="${fill}" fill-opacity="0.78" d="M13.2 3.6H21.36L19.92 7.92H11.76Z"/>
  <path fill="${fill}" fill-opacity="0.62" d="M10.32 7.92H15.12L11.76 20.4H6.96Z"/><path fill="${fill}" d="M10.32 7.92H15.12L13.68 10.8Z"/></svg>`;
const star = `<svg width="15" height="15" viewBox="0 0 24 24"><path fill="${C.gold}" d="M12 17.27 18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/></svg>`;
const chevron = `<svg width="18" height="18" viewBox="0 0 24 24"><path fill="none" stroke="${C.ink}" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round" d="M15 5l-7 7 7 7"/></svg>`;

const css = `
@font-face{font-family:'DM Sans';src:url(data:font/woff2;base64,${font}) format('woff2');font-weight:100 1000}
*{box-sizing:border-box}
html,body{margin:0;width:1080px;height:1920px;overflow:hidden;background:${C.purple};font-family:'DM Sans',sans-serif}
.cap{position:absolute;top:96px;left:60px;right:60px;text-align:center;color:#fff}
.cap h1{margin:0;font-size:78px;line-height:1.08;font-weight:700;letter-spacing:-1.5px}
.cap p{margin:22px 0 0;font-size:40px;color:${C.tint};font-weight:500}
.phone{position:absolute;left:170px;top:370px;width:740px;height:1500px;border-radius:60px;background:#111;padding:14px}
.screen{width:712px;height:1472px;border-radius:48px;overflow:hidden;background:#fff;position:relative}
.dp{width:412px;height:852px;transform:scale(1.7282);transform-origin:0 0;position:relative;color:${C.ink};font-size:15px}
.status{height:26px}
.bar{height:56px;display:flex;align-items:center;justify-content:space-between;padding:0 16px;border-bottom:1px solid #EBEBEB}
.av{width:40px;height:40px;border-radius:50%;background:${C.ink};color:#fff;display:flex;align-items:center;justify-content:center;font-size:15px;font-weight:500}
.tabs{position:absolute;bottom:0;left:0;right:0;height:64px;border-top:1px solid #EBEBEB;display:flex;background:#fff}
.tab{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;font-size:12px;color:${C.muted};gap:3px}
.tab.on{color:${C.purple};font-weight:600}.tab i{width:22px;height:22px;border-radius:6px;border:2px solid currentColor;display:block}
.tab.on i{background:currentColor}
.prog{height:4px;background:#EEE;margin:10px 16px 0;border-radius:2px}.prog b{display:block;height:4px;background:${C.purple};border-radius:2px}
.ill{width:96px;height:96px;border-radius:50%;background:${C.tint};display:flex;align-items:center;justify-content:center;margin:0 auto}
.ill img{width:60px;height:60px}
.q{font-size:25px;font-weight:700;text-align:center;line-height:1.15;margin:14px 20px 4px}
.why{font-size:14px;color:${C.muted};text-align:center;margin:0 24px}
.tiles{display:grid;grid-template-columns:1fr 1fr;gap:10px;padding:18px 16px 0}
.tile{border:1.5px solid ${C.line};border-radius:12px;padding:12px 10px;display:flex;align-items:center;gap:10px;font-size:14px;font-weight:500;height:62px}
.tile img{width:36px;height:36px}.tile.on{border:2.5px solid ${C.purple};background:#FBF8FD}
.btn{background:${C.purple};color:#fff;border-radius:8px;height:50px;display:flex;align-items:center;justify-content:center;font-size:16px;font-weight:600}
.foot{position:absolute;left:0;right:0;bottom:0;padding:12px 16px 20px;border-top:1px solid #EBEBEB;background:#fff}
.field{background:${C.soft};border-radius:12px;padding:10px 14px;margin:18px 16px 0;border-bottom:2px solid ${C.purple}}
.field small{font-size:12px;color:${C.purple}}.field div{font-size:26px;font-weight:600;margin-top:2px}
.chips{display:flex;gap:8px;padding:0 16px;flex-wrap:wrap}
.chip{border:1.5px solid ${C.line};border-radius:999px;padding:8px 14px;font-size:14px}
.chip.on{background:${C.purple};border-color:${C.purple};color:#fff}
.note{font-size:13px;color:${C.muted};padding:8px 16px 0}
.card{border:1px solid ${C.line};border-radius:12px;margin:0 16px}
.row{display:flex;justify-content:space-between;align-items:flex-start;padding:14px 16px;border-top:1px solid #EBEBEB}
.row:first-child{border-top:0}.row h4{margin:0;font-size:15px;font-weight:600}.row p{margin:3px 0 0;font-size:14px;color:${C.muted}}
.row u{font-size:14px;font-weight:600;color:${C.ink}}
.h{font-size:24px;font-weight:700;margin:16px 16px 14px;line-height:1.15}
.money{color:${C.green};font-weight:700}
.pill{border:1px solid ${C.ink};border-radius:999px;margin:14px 16px 0;padding:9px;text-align:center;box-shadow:0 3px 10px rgba(0,0,0,.08)}
.pill b{font-size:15px;display:block}.pill span{font-size:13px;color:${C.muted}}
.res{display:flex;gap:12px;padding:14px 0;margin:0 16px;border-top:1px solid #EBEBEB}
.res:first-of-type{border-top:0}
.ph{width:80px;height:80px;border-radius:10px;flex:none;display:flex;align-items:center;justify-content:center;color:#fff;font-size:24px;font-weight:500}
.res .t{display:flex;justify-content:space-between;font-weight:600;font-size:15px}.res .t span{display:flex;align-items:center;gap:3px;font-weight:500;font-size:14px}
.res p{margin:2px 0 0;font-size:14px;color:${C.muted}}
.tag{display:inline-block;background:#EFEFEF;border-radius:999px;padding:4px 10px;font-size:12px;margin-top:6px;color:${C.ink}}
.radio{display:flex;align-items:center;gap:12px;padding:12px 16px;font-size:15px;font-weight:500}
.radio i{width:22px;height:22px;border-radius:50%;border:2px solid ${C.muted};display:block;flex:none}
.radio.on i{border:7px solid ${C.purple}}
.pics{display:flex;gap:8px;margin-top:10px}.pics div{width:70px;height:70px;border-radius:10px;background:${C.tint};display:flex;align-items:center;justify-content:center}
.pics img{width:40px;height:40px}
`;

const tabBar = (on) => `<div class="tabs">${['Home', 'Post', 'Inbox', 'Me'].map(t => `<div class="tab${t === on ? ' on' : ''}"><i></i>${t}</div>`).join('')}</div>`;
const topBar = `<div class="bar">${logo(C.purple)}<div class="av">AN</div></div>`;
const back = (pct) => `<div style="padding:12px 16px 0">${chevron}</div><div class="prog"><b style="width:${pct}%"></b></div>`;

const screens = [
  {
    name: '1-seeker-start', title: 'Find a skilled Kuya or Ate', sub: 'Pick what you need. Takes a minute.',
    body: `<div class="status"></div>${back(20)}
      <div style="margin-top:22px" class="ill"><img src="${icon('toolbox')}"></div>
      <div class="q">What do you need done?</div><div class="why">Pick one. You can change it later.</div>
      <div class="tiles">${[['aircon', 'Aircon Technician', 1], ['construction', 'Construction'], ['errand', 'Errands'], ['driver', 'Driver (On-Call)'],
        ['mechanic', 'Car Mechanic'], ['pest-control', 'Pest Control'], ['more', 'Others']]
        .map(([i, t, on]) => `<div class="tile${on ? ' on' : ''}"><img src="${icon(i)}">${t}</div>`).join('')}</div>
      <div class="foot"><div class="btn">Next</div></div>`,
  },
  {
    name: '2-search-results', title: 'See the price first', sub: 'Then message the one you like.',
    body: `<div class="status"></div>${topBar}
      <div class="pill"><b>Aircon Technician in Angono</b><span>Aircon cleaning · ₱800 – ₱1,500</span></div>
      <div class="h" style="margin-top:20px;font-size:21px">6 skilled workers in Angono</div>
      ${[['BS', '#3D5A80', 'Kuya Ben Santos', '4.9', 32, '10+ years · 41 services done', '1,000', 'Fits your budget'],
        ['LR', '#7A4E2D', 'Ate Liza Reyes', '4.8', 21, '3–10 years · 26 services done', '900', 'Fits your budget'],
        ['RC', '#2F6B4F', 'Manong Rudy Cruz', '4.7', 15, '10+ years · 18 services done', '1,500', ''],
        ['JD', '#6B3E75', 'Kuya Jun Dela Cruz', '4.6', 9, '1–3 years · 9 services done', '800', 'Fits your budget']]
        .map(([ini, bg, n, r, c, f, p, tag]) => `<div class="res"><div class="ph" style="background:${bg}">${ini}</div><div style="flex:1">
          <div class="t">Aircon Technician in Angono<span>${star}${r} (${c})</span></div><p>${n}</p><p>${f}</p>
          <div style="margin-top:3px"><span class="money">₱${p}</span> avg per service</div>${tag ? `<span class="tag">${tag}</span>` : ''}</div></div>`).join('')}
      ${tabBar('Home')}`,
  },
  {
    name: '3-post', title: 'Post what you need done', sub: 'Workers near you send their price.',
    body: `<div class="status"></div><div style="padding:12px 16px 0">${chevron}</div>
      <div class="h" style="font-size:26px">Post what you need done</div>
      <div class="card">
        <div class="row"><div><h4>What</h4><p>Aircon cleaning<br>Aircon Technician</p></div><u>Change</u></div>
        <div class="row"><div><h4>Where</h4><p>Angono · Brgy. San Isidro</p></div><u>Change</u></div>
        <div class="row"><div><h4>When</h4><p>Tomorrow</p></div><u>Change</u></div>
        <div class="row"><div><h4>Your budget</h4><p><span class="money">₱800 – ₱1,200</span></p></div><u>Change</u></div>
      </div>
      <div style="margin:18px 16px 0"><h4 style="margin:0;font-size:15px">Describe the problem</h4>
        <div style="background:${C.soft};border-radius:12px;padding:12px 14px;margin-top:8px;font-size:14px;line-height:1.4">Two split-type units, 1.5 HP. Not cold anymore and dripping water inside.</div>
        <h4 style="margin:16px 0 0;font-size:15px">Photos of the problem</h4>
        <div class="pics"><div><img src="${icon('aircon')}"></div><div><img src="${icon('drop')}"></div></div></div>
      <div class="foot"><div class="btn">Post</div></div>`,
  },
  {
    name: '4-worker-price', title: 'Set your own price', sub: 'Libre. Walang komisyon. Sa\'yo ang buong bayad.',
    body: `<div class="status"></div>${back(45)}
      <div style="margin-top:26px" class="ill"><img src="${icon('money')}"></div>
      <div class="q">What's your usual price per service?</div><div class="why">Type one amount. Customers see it first.</div>
      <div class="field"><small>Your average price (₱)</small><div>1,000</div></div>
      <div class="note" style="padding-top:10px">Only a guide. You and the customer agree on the final price.</div>
      <div style="font-size:15px;font-weight:600;padding:22px 16px 10px">What others charge for this:</div>
      <div class="chips"><div class="chip">Low ₱755</div><div class="chip on">Average ₱1,000</div><div class="chip">High ₱1,500</div></div>
      <div class="note">Tap one to use it, or type your own.</div>
      <div class="foot"><div class="btn">Next</div></div>`,
  },
  {
    name: '5-request-offer', title: 'Requests near you', sub: 'Send your price in one tap.',
    body: `<div class="status"></div><div style="padding:12px 16px 0">${chevron}</div>
      <div class="h" style="font-size:26px">Send your price</div>
      <div class="card">
        <div class="row" style="gap:12px;justify-content:flex-start"><div class="pics" style="margin:0"><div style="width:56px;height:56px"><img src="${icon('aircon')}"></div></div>
          <div><h4>Aircon not cooling / repair</h4><p>Angono · Brgy. San Isidro · 2 km away</p><p>Needed tomorrow</p></div></div>
        <div class="row"><div><h4>Your price</h4><p><span class="money">₱1,500</span> avg per service</p></div><u>Change</u></div>
        <div class="row"><div><h4>Their budget</h4><p><span class="money">₱1,000 – ₱1,500</span></p></div></div>
        <div class="row"><div><h4>What they need</h4><p>Split-type in the bedroom, blowing warm air since Monday.</p>
          <div class="pics"><div><img src="${icon('aircon')}"></div><div><img src="${icon('thermometer')}"></div></div></div></div>
      </div>
      <div style="margin-top:8px"><div class="radio on"><i></i>Take it for ₱1,500</div><div class="radio"><i></i>Offer another price</div></div>
      <div class="foot"><div class="btn">Send offer</div></div>`,
  },
  {
    name: '6-worker-profile', title: 'Reviews and work photos', sub: 'See what other households said.',
    body: `<div class="status"></div><div style="padding:12px 16px 0">${chevron}</div>
      <div style="text-align:center;margin-top:4px"><div class="ph" style="background:#3D5A80;width:96px;height:96px;border-radius:50%;margin:0 auto;font-size:32px">BS</div>
        <div style="font-size:24px;font-weight:700;margin-top:10px">Kuya Ben Santos</div>
        <div style="font-size:14px;color:${C.muted};margin-top:2px">Aircon Technician in Angono</div></div>
      <div style="display:flex;justify-content:space-around;margin:16px 16px 0;padding:12px 0;border-top:1px solid #EBEBEB;border-bottom:1px solid #EBEBEB;text-align:center">
        <div><b style="display:flex;align-items:center;gap:3px;justify-content:center;font-size:17px">${star}4.9</b><small style="color:${C.muted}">32 reviews</small></div>
        <div><b style="font-size:17px">41</b><br><small style="color:${C.muted}">services done</small></div>
        <div><b style="font-size:17px">10+</b><br><small style="color:${C.muted}">years</small></div></div>
      <div style="font-size:16px;font-weight:700;margin:16px 16px 8px">What we checked</div>
      ${[['profile-card', 'ID checked'], ['medal', 'Clearance checked'], ['phone', 'Number verified']].map(([i, t]) =>
        `<div style="display:flex;align-items:center;gap:12px;padding:6px 16px;font-size:14px"><img src="${icon(i)}" style="width:30px;height:30px">${t}</div>`).join('')}
      <div style="font-size:16px;font-weight:700;margin:14px 16px 0">Work photos</div>
      <div class="pics" style="padding:0 16px">${['aircon', 'thermometer', 'drop', 'wrench'].map(i => `<div><img src="${icon(i)}"></div>`).join('')}</div>
      <div style="font-size:16px;font-weight:700;margin:16px 16px 8px">Reviews</div>
      <div style="display:flex;gap:10px;padding:0 16px;overflow:hidden">
        ${[['Maricel', 'Mabilis at malinis. Sakto sa presyo na sinabi niya.'], ['Arnel', 'On time and explained the problem first.']].map(([n, t]) =>
          `<div style="flex:none;width:230px;border:1px solid ${C.line};border-radius:12px;padding:12px;font-size:13px;line-height:1.35">
            <div style="display:flex;gap:2px">${star.repeat(5)}</div><div style="margin-top:6px">${t}</div>
            <div style="margin-top:8px;font-weight:600">${n} · Angono</div></div>`).join('')}</div>
      <div class="foot" style="display:flex;justify-content:space-between;align-items:center">
        <div><span class="money" style="font-size:18px">₱1,000</span><div style="font-size:13px;color:${C.muted}">avg per service</div></div>
        <div class="btn" style="width:150px">Contact</div></div>`,
  },
  {
    name: '7-chat', title: 'Chat in the app', sub: 'Your number stays private.',
    body: `<div class="status"></div>
      <div class="bar" style="justify-content:flex-start;gap:12px">${chevron}<div class="ph" style="background:#3D5A80;width:40px;height:40px;border-radius:50%;font-size:15px">BS</div>
        <div><div style="font-weight:600">Kuya Ben Santos</div><div style="font-size:12px;color:${C.muted}">Aircon cleaning · Angono</div></div></div>
      <div style="padding:16px;display:flex;flex-direction:column;gap:10px;font-size:15px;line-height:1.35">
        <div style="align-self:center;font-size:12px;color:${C.muted}">Today</div>
        ${[[1, 'Hi Kuya! Pwede po ba bukas ng umaga? Two split-type units, 1.5 HP, Brgy. San Isidro.'],
          [0, 'Pwede po, mga 9 AM. ₱1,000 per unit, kasama na ang chemical wash.'],
          [1, 'Sige po, salamat! See you bukas.'],
          [0, 'Noted po. Text ko kayo pag paalis na ako.']].map(([me, t]) =>
          `<div style="max-width:78%;padding:10px 14px;border-radius:18px;${me ? `align-self:flex-end;background:${C.ink};color:#fff` : `align-self:flex-start;background:#F1F1F1`}">${t}</div>`).join('')}
      </div>
      <div class="foot" style="display:flex;gap:10px;align-items:center">
        <div style="flex:1;background:${C.soft};border-radius:24px;padding:13px 16px;color:${C.muted};font-size:15px">Write a message</div>
        <div class="btn" style="width:80px;border-radius:24px">Send</div></div>`,
  },
  {
    name: '8-offers', title: 'Get offers, pick one', sub: 'Compare prices from Kuya and Ate nearby.',
    body: `<div class="status"></div><div style="padding:12px 16px 0">${chevron}</div>
      <div class="h" style="font-size:26px">3 offers for Aircon cleaning</div>
      <div style="font-size:14px;color:${C.muted};margin:-6px 16px 8px">Your budget: <span class="money">₱800 – ₱1,200</span></div>
      ${[['LR', '#7A4E2D', 'Ate Liza Reyes', '4.8', 21, '900', '1 km away', 'Kaya ko po bukas ng hapon.'],
        ['BS', '#3D5A80', 'Kuya Ben Santos', '4.9', 32, '1,000', '2 km away', 'May dala akong pang-chemical wash.'],
        ['JD', '#6B3E75', 'Kuya Jun Dela Cruz', '4.6', 9, '1,200', '3 km away', 'Pwede po today.']]
        .map(([ini, bg, n, r, c, p, d, note]) => `<div class="card" style="margin-bottom:10px;padding:14px">
          <div style="display:flex;gap:12px"><div class="ph" style="background:${bg};width:56px;height:56px;font-size:18px">${ini}</div>
          <div style="flex:1"><div class="t" style="display:flex;justify-content:space-between;font-weight:600">${n}<span style="display:flex;align-items:center;gap:3px;font-weight:500;font-size:14px">${star}${r} (${c})</span></div>
          <div style="font-size:14px;color:${C.muted};margin-top:2px">${d}</div>
          <div style="margin-top:4px"><span class="money" style="font-size:17px">₱${p}</span></div></div></div>
          <div style="font-size:14px;margin-top:10px;color:${C.ink}">"${note}"</div>
          <div style="display:flex;gap:10px;margin-top:12px"><div style="flex:1;border:1.5px solid ${C.ink};border-radius:8px;height:42px;display:flex;align-items:center;justify-content:center;font-weight:600">Profile</div>
          <div class="btn" style="flex:1;height:42px">Pick</div></div></div>`).join('')}`,
  },
];

const exe = ['C:/Program Files/Google/Chrome/Application/chrome.exe', 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'].find(fs.existsSync);
const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'tarbaho-shots-'));
const out = path.join(here, 'phone');
fs.mkdirSync(out, { recursive: true });
for (const s of screens) {
  const html = `<!doctype html><html><head><meta charset="utf-8"><style>${css}</style></head><body>
    <div class="cap"><h1>${s.title}</h1><p>${s.sub}</p></div>
    <div class="phone"><div class="screen"><div class="dp">${s.body}</div></div></div></body></html>`;
  const file = path.join(tmp, s.name + '.html');
  fs.writeFileSync(file, html);
  execFileSync(exe, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
    `--user-data-dir=${path.join(tmp, 'profile')}`, '--window-size=1080,1920', '--virtual-time-budget=3000',
    `--screenshot=${path.join(out, s.name + '.png')}`, 'file:///' + file.split(path.sep).join('/')], { stdio: 'ignore' });
}

// Tablet screenshots (1920x1080, landscape 16:9; Play uses the same files for 7- and 10-inch): a caption on the left,
// two of the phone screens above side by side on the right.
const byName = Object.fromEntries(screens.map(s => [s.name, s]));
const tablet = [
  { name: '1-find-and-compare', title: 'Find a skilled Kuya or Ate', sub: 'See the price first, then message the one you like.', shots: ['1-seeker-start', '2-search-results'] },
  { name: '2-post-and-pick', title: 'Post what you need done', sub: 'Workers near you send their price. Pick one.', shots: ['3-post', '8-offers'] },
  { name: '3-check-and-chat', title: 'Reviews, then chat', sub: 'See what we checked. Your number stays private.', shots: ['6-worker-profile', '7-chat'] },
  { name: '4-for-workers', title: 'Got skills? Set your price', sub: 'Libre. Walang komisyon. Sa\'yo ang buong bayad.', shots: ['4-worker-price', '5-request-offer'] },
];
const tout = path.join(here, 'tablet');
fs.mkdirSync(tout, { recursive: true });
const tcss = css.replace('width:1080px;height:1920px', 'width:1920px;height:1080px') + `
.tcap{position:absolute;left:110px;top:0;bottom:0;width:640px;display:flex;flex-direction:column;justify-content:center;color:#fff}
.tcap h1{margin:0;font-size:84px;line-height:1.05;font-weight:700;letter-spacing:-1.5px}
.tcap p{margin:30px 0 0;font-size:40px;line-height:1.3;color:${C.tint};font-weight:500}
.tcap .brand{display:flex;align-items:center;gap:14px;font-size:44px;font-weight:700;margin-bottom:56px}
.tphones{position:absolute;right:90px;top:60px;display:flex;gap:56px}
.tphones .phone{position:relative;left:0;top:0;transform:scale(0.64);transform-origin:0 0;margin-right:-266px;margin-bottom:-540px}`;
for (const t of tablet) {
  const html = `<!doctype html><html><head><meta charset="utf-8"><style>${tcss}</style></head><body>
    <div class="tcap"><div class="brand">${logo('#fff').replace(/34/g, '56')}Tarbaho</div><h1>${t.title}</h1><p>${t.sub}</p></div>
    <div class="tphones">${t.shots.map(n => `<div class="phone"><div class="screen"><div class="dp">${byName[n].body}</div></div></div>`).join('')}</div></body></html>`;
  const file = path.join(tmp, 'tablet-' + t.name + '.html');
  fs.writeFileSync(file, html);
  execFileSync(exe, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
    `--user-data-dir=${path.join(tmp, 'profile')}`, '--window-size=1920,1080', '--virtual-time-budget=3000',
    `--screenshot=${path.join(tout, t.name + '.png')}`, 'file:///' + file.split(path.sep).join('/')], { stdio: 'ignore' });
}

// Foldable screenshots (Play asks for an unfolded 1:1, an unfolded 4:3 and the outer screen folded, portrait 10:16):
// the same screens laid out at the wider unfolded width, in a foldable frame with a faint crease.
const fout = path.join(here, 'foldable');
fs.mkdirSync(fout, { recursive: true });
const foldable = [
  { name: '1-unfolded-square', w: 2160, h: 2160, title: 'See the price first', sub: 'Then message the one you like.', shot: '2-search-results', dpW: 800, dpH: 800, frameW: 1600, frameH: 1600, top: 440, crease: 'v' },
  { name: '2-unfolded-landscape', w: 2400, h: 1800, title: 'Get offers, pick one', sub: 'Compare prices from Kuya and Ate nearby.', shot: '8-offers', dpW: 1000, dpH: 750, frameW: 1880, frameH: 1410, top: 340, crease: 'v' },
  { name: '3-folded-outer', w: 1200, h: 1920, title: 'Post what you need done', sub: 'Workers near you send their price.', shot: '3-post', dpW: 412, dpH: 852, frameW: 740, frameH: 1500, top: 370 },
];
for (const f of foldable) {
  const inner = f.frameW - 28, k = inner / f.dpW;
  const html = `<!doctype html><html><head><meta charset="utf-8"><style>${css.replace('width:1080px;height:1920px', `width:${f.w}px;height:${f.h}px`)}
.fcap{position:absolute;top:${Math.round(f.top * 0.27)}px;left:60px;right:60px;text-align:center;color:#fff}
.fcap h1{margin:0;font-size:${f.w > 1500 ? 96 : 78}px;line-height:1.08;font-weight:700;letter-spacing:-1.5px}
.fcap p{margin:22px 0 0;font-size:${f.w > 1500 ? 48 : 40}px;color:${C.tint};font-weight:500}
.fold{position:absolute;left:${(f.w - f.frameW) / 2}px;top:${f.top}px;width:${f.frameW}px;height:${f.frameH}px;border-radius:${f.crease ? 44 : 60}px;background:#111;padding:14px;box-sizing:border-box}
.fscreen{width:${inner}px;height:${f.frameH - 28}px;border-radius:${f.crease ? 32 : 48}px;overflow:hidden;background:#fff;position:relative}
.fscreen .dp{width:${f.dpW}px;height:${f.dpH}px;transform:scale(${k})}
.crease{position:absolute;top:0;bottom:0;left:50%;width:2px;background:linear-gradient(rgba(0,0,0,.05),rgba(0,0,0,.09),rgba(0,0,0,.05));pointer-events:none}
</style></head><body><div class="fcap"><h1>${f.title}</h1><p>${f.sub}</p></div>
<div class="fold"><div class="fscreen"><div class="dp">${byName[f.shot].body}</div>${f.crease ? '<div class="crease"></div>' : ''}</div></div></body></html>`;
  const file = path.join(tmp, 'fold-' + f.name + '.html');
  fs.writeFileSync(file, html);
  execFileSync(exe, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1',
    `--user-data-dir=${path.join(tmp, 'profile')}`, `--window-size=${f.w},${f.h}`, '--virtual-time-budget=3000',
    `--screenshot=${path.join(fout, f.name + '.png')}`, 'file:///' + file.split(path.sep).join('/')], { stdio: 'ignore' });
}
