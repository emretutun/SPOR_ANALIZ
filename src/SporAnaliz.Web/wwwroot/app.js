const $ = s => document.querySelector(s);
const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
const num = (v, d = 1) => Number(v).toLocaleString('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: d });
const fmtDay = d => new Date(d).toLocaleDateString('tr-TR', { weekday: 'long', day: 'numeric', month: 'long' });
const fmtTime = d => new Date(d).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
const fmtShort = d => new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: '2-digit' });
const SLOTS = []; for (let m = 1; m <= 45; m++) SLOTS.push(m + "'"); SLOTS.push("45+1'", "45+2'");
for (let m = 46; m <= 90; m++) SLOTS.push(m + "'"); for (let a = 1; a <= 5; a++) SLOTS.push("90+" + a + "'");

// Zemin rengine göre okunabilir yazı rengi
const ink = hex => { const n = parseInt(hex.slice(1, 7), 16); return (0.299 * (n >> 16) + 0.587 * (n >> 8 & 255) + 0.114 * (n & 255)) > 140 ? '#111' : '#fff'; };
const crest = (short, color, size) => `<span class="crest ${size}" style="background:${color};color:${ink(color)}">${esc(short)}</span>`;
const abbr = name => name.split(' ').map(w => w[0]).join('').toLocaleUpperCase('tr-TR');

let selected = null, play = null, fixtures = [], status = {};
const iso = d => { const x = new Date(d); return x.getFullYear() + '-' + String(x.getMonth() + 1).padStart(2, '0') + '-' + String(x.getDate()).padStart(2, '0'); };
const addDays = (d, n) => { const x = new Date(d); x.setDate(x.getDate() + n); return x; };
const F = { mode: 'upcoming', from: '', to: '', compId: null, stages: [], stageId: null, round: '', q: '', tr: false };

async function api(path, opt) {
  const r = await fetch(path, opt);
  const j = await r.json().catch(() => ({}));
  if (!r.ok) throw new Error(j.error || ('Hata ' + r.status));
  return j;
}
function showError(msg) { const e = $('#error'); e.style.display = msg ? 'block' : 'none'; e.textContent = msg || ''; }

async function init() {
  try {
    const st = await api('/api/status');
    if (!st.ok) { showError('Veritabanına bağlanılamadı: ' + st.error + ' — appsettings.json içindeki MySQL bağlantı bilgisini kontrol edip uygulamayı yeniden başlatın.'); return; }
    status = st;
    const until = st.dataUntil ? new Date(st.dataUntil).toLocaleDateString('tr-TR') : null;
    $('#badge').textContent = st.testData ? 'TEST VERİSİ' : until ? 'VERİ: ' + until + "'E KADAR" : st.source.toLocaleUpperCase('tr-TR');
    $('#sub').textContent = st.source + ' · ' + num(st.teams, 0) + ' takım';
    await setupFilters();
    await setMode('upcoming');
    const first = visibleFixtures()[0];
    if (first) select(first.id);
  } catch (e) { showError(e.message); }
}

// ---------------- maç listesi ve filtreler ----------------
// Turnuva seçilince sezon ve hafta seçilir; "Tüm turnuvalar"da hazır aralıklar (yaklaşan / sonucu yok / oynanmış) kullanılır.
async function setupFilters() {
  $('#modes').querySelectorAll('button').forEach(b => b.onclick = () => setMode(b.dataset.m));
  $('#fQ').oninput = () => { F.q = $('#fQ').value; drawFixtures(); };
  $('#fTr').onchange = () => { F.tr = $('#fTr').checked; drawFixtures(); };
  if (!status.dataUntil) $('#modes').querySelector('[data-m="pending"]').style.display = 'none';
  const comps = await api('/api/competitions');
  $('#fComp').innerHTML = '<option value="">Tüm turnuvalar</option>' + comps.map(c => '<option value="' + c.id + '">' + esc(c.name) + '</option>').join('');
  $('#fComp').onchange = () => setCompetition($('#fComp').value);
  $('#fStage').onchange = () => { F.stageId = Number($('#fStage').value); fillRounds(); loadFixtures(); };
  $('#fRound').onchange = () => { F.round = $('#fRound').value; loadFixtures(); };
  $('#wkPrev').onclick = () => stepRound(-1);
  $('#wkNext').onclick = () => stepRound(1);
}

async function setCompetition(id) {
  F.compId = id ? Number(id) : null;
  $('#roundCtl').style.display = F.compId ? '' : 'none';
  $('#modes').style.display = F.compId ? 'none' : '';
  if (!F.compId) { await setMode(F.mode); return; }
  F.stages = await api('/api/rounds?competition=' + F.compId);
  $('#fStage').innerHTML = F.stages.map(s => '<option value="' + s.id + '">' + esc(s.name) + '</option>').join('');
  const cur = F.stages.find(s => s.current) || F.stages[0];
  if (!cur) { fixtures = []; drawFixtures(); return; }
  F.stageId = cur.id; $('#fStage').value = cur.id;
  fillRounds();
  await loadFixtures();
}
function fillRounds() {
  const st = F.stages.find(s => s.id === F.stageId), rounds = st ? st.rounds : [];
  $('#fRound').innerHTML = rounds.map(r => '<option value="' + esc(r.name) + '">' + esc(r.name) + ' · ' + fmtShort(r.first) + (r.played === r.matches ? '' : r.played ? ' · ' + r.played + '/' + r.matches : ' · oynanmadı') + '</option>').join('');
  const cur = rounds.find(r => r.current) || rounds[0];
  F.round = cur ? cur.name : ''; $('#fRound').value = F.round;
}
function stepRound(d) {
  const sel = $('#fRound'), i = sel.selectedIndex + d;
  if (i < 0 || i >= sel.options.length) return;
  sel.selectedIndex = i; F.round = sel.value; loadFixtures();
}

// Hazır aralıklar: yaklaşan maçlar; tarihi geçmiş ama sonucu olmayanlar; sonucu bilinen son maçlar
async function setMode(mode) {
  F.mode = mode;
  const today = new Date(), until = status.dataUntil ? new Date(status.dataUntil) : today;
  if (mode === 'upcoming') { F.from = iso(today); F.to = iso(addDays(today, 7)); }
  else if (mode === 'pending') { F.from = iso(addDays(until, 1)); F.to = iso(addDays(today, -1)); }
  else { F.from = iso(addDays(until, -30)); F.to = iso(until); }
  $('#modes').querySelectorAll('button').forEach(b => b.classList.toggle('on', b.dataset.m === mode));
  await loadFixtures();
}

async function loadFixtures() {
  let url;
  if (F.compId) {
    if (!F.round) { fixtures = []; drawFixtures(); return; }
    url = '/api/fixtures?competition=' + F.compId + '&stage=' + F.stageId + '&round=' + encodeURIComponent(F.round);
  } else {
    if (!F.from || !F.to) return;
    url = '/api/fixtures?from=' + F.from + '&to=' + F.to;
  }
  try { fixtures = await api(url); showError(''); }
  catch (e) { showError(e.message); return; }
  drawFixtures();
}

function visibleFixtures() {
  const q = F.q.trim().toLocaleLowerCase('tr-TR'), now = new Date();
  return fixtures.filter(f => {
    const played = f.status === 'played', past = new Date(f.date) < now;
    if (!F.compId) {                                                   // hafta seçiliyken o haftanın bütün maçları görünür
      if (F.mode === 'played' ? !played : played) return false;        // "Oynanmış" yalnızca sonucu olanları gösterir
      if (F.mode === 'upcoming' && past && iso(f.date) !== iso(now)) return false;
    }
    if (F.tr && !f.turkish) return false;
    return !q || f.home.toLocaleLowerCase('tr-TR').includes(q) || f.away.toLocaleLowerCase('tr-TR').includes(q);
  });
}

function drawFixtures() {
  const box = $('#fixtures'); box.innerHTML = '';
  let list = visibleFixtures();
  if (F.mode === 'played' && !F.compId) list = list.slice().reverse();              // en yeni maç üstte
  $('#fCount').textContent = list.length + ' maç' + (list.length > 400 ? ' · ilk 400 gösteriliyor, filtreyi daraltın' : '');
  let day = '';
  for (const f of list.slice(0, 400)) {
    const d = fmtDay(f.date);
    if (d !== day) { day = d; box.insertAdjacentHTML('beforeend', '<div class="day">' + esc(d) + '</div>'); }
    const el = document.createElement('div');
    el.className = 'fx' + (f.id === selected ? ' sel' : '');
    el.dataset.id = f.id;
    const left = f.status === 'played' ? '<div class="fx-time sc">' + f.homeGoals + '-' + f.awayGoals + '</div>' : '<div class="fx-time">' + fmtTime(f.date) + '</div>';
    el.innerHTML = left +
      '<div class="fx-teams"><div><i class="dot" style="background:' + f.homeColor + '"></i>' + esc(f.home) + '</div><div><i class="dot" style="background:' + f.awayColor + '"></i>' + esc(f.away) + '</div></div>' +
      '<div class="fx-comp ' + (f.hasAnalysis ? 'done' : '') + '" title="' + esc(f.competition) + (f.hasAnalysis ? ' · analiz hazır' : '') + '">' + esc(abbr(f.competition)) + '</div>';
    el.onclick = () => select(f.id);
    box.appendChild(el);
  }
  if (!list.length) box.innerHTML = '<div class="day">Bu filtreyle maç yok</div>';
}

async function select(id, refresh) {
  selected = id; stopPlay();
  document.querySelectorAll('.fx').forEach(e => e.classList.toggle('sel', Number(e.dataset.id) === id));
  $('#main').innerHTML = '<div class="panel empty"><div><div class="spinner"></div>10.000 simülasyon çalışıyor…</div></div>';
  try {
    const a = await api('/api/analysis/' + id + (refresh ? '?refresh=true' : ''));
    if (selected !== id) return;
    render(a);
    const f = fixtures.find(x => x.id === id); if (f && !f.hasAnalysis) { f.hasAnalysis = true; drawFixtures(); }
  } catch (e) { $('#main').innerHTML = `<div class="panel empty"><div>${esc(e.message)}</div></div>`; }
}

// ---------------- analiz ekranı ----------------
function pairRows(items, max, hc, ac, digits) {
  return items.map(x => {
    const m = max || Math.max(x.home, x.away, 0.0001);
    return `<div class="pr"><div class="v r">${num(x.home, digits)}</div>
      <div class="bar l"><i style="width:${Math.min(100, x.home / m * 100)}%;background:${hc}"></i></div>
      <div class="lab">${esc(x.label)}</div>
      <div class="bar"><i style="width:${Math.min(100, x.away / m * 100)}%;background:${ac}"></i></div>
      <div class="v">${num(x.away, digits)}</div></div>`;
  }).join('');
}
function formBlock(items) {
  return `<div class="pills">${items.slice().reverse().map(f => `<span class="res ${f.result}">${f.result}</span>`).join('')}</div>
    <table>${items.map(f => `<tr><td><span class="res s ${f.result}">${f.result}</span></td><td>${esc(f.opponent)}</td>
      <td class="muted">${esc(f.venue)}</td><td class="n">${f.goalsFor}-${f.goalsAgainst}</td><td class="n muted">${fmtShort(f.date)}</td></tr>`).join('')}</table>`;
}

function render(a) {
  const H = a.match.home, A = a.match.away, p = a.probabilities;
  const hc = H.color, ac = A.color.toLowerCase() === H.color.toLowerCase() ? '#6cabdd' : A.color;
  const elo = t => `Güç ${num(t.elo, 0)}${t.eloApprox ? ' (yaklaşık)' : ''}`;
  const teamsH = `<div class="teams-h"><span>${esc(H.name)}</span><span>${esc(A.name)}</span></div>`;
  const maxScore = Math.max(...a.topScores.map(s => s.pct));
  const maxTiming = Math.max(...a.goalTiming.map(t => Math.max(t.home, t.away)), 0.0001);
  const maxScorer = Math.max(...a.scorers.map(s => s.pct), 1);
  const scorers = (side, t, col) => `<div class="sc-h">${crest(t.short, col, 'sm')}${esc(t.name)}</div>` +
    a.scorers.filter(s => s.side === side).map(s => `<div class="sc"><span>${esc(s.player)} <span class="muted">· ${s.recentGoals} gol</span></span>
      <span class="pos">${esc(s.pos)}</span><div class="bar"><i style="width:${s.pct / maxScorer * 100}%;background:${col}"></i></div><b>%${num(s.pct)}</b></div>`).join('');

  $('#main').innerHTML = `
  <section class="hero" style="--hc:${hc};--ac:${ac}">
    <div class="hero-meta"><span class="tag">${esc(a.match.competition)}</span><span>${fmtDay(a.match.date)} · ${fmtTime(a.match.date)}</span></div>
    <div class="hero-teams">
      <div class="hero-team">${crest(H.short, hc, 'lg')}<div><div class="hero-name">${esc(H.name)}</div><div class="hero-sub">Ev sahibi · ${elo(H)}</div></div></div>
      <div class="hero-vs">VS</div>
      <div class="hero-team r">${crest(A.short, ac, 'lg')}<div><div class="hero-name">${esc(A.name)}</div><div class="hero-sub">Deplasman · ${elo(A)}</div></div></div>
    </div>
    <div class="probs">
      <div class="prob"><div class="prob-n"><small>%</small>${num(p.home)}</div><div class="prob-l">${esc(H.name)} kazanır</div></div>
      <div class="prob"><div class="prob-n"><small>%</small>${num(p.draw)}</div><div class="prob-l">Beraberlik</div></div>
      <div class="prob"><div class="prob-n"><small>%</small>${num(p.away)}</div><div class="prob-l">${esc(A.name)} kazanır</div></div>
    </div>
    <div class="probbar"><i style="flex:${p.home};background:${hc}"></i><i style="flex:${p.draw};background:#77829a"></i><i style="flex:${p.away};background:${ac}"></i></div>
    <div class="hero-foot"><span>${num(a.simulations, 0)} simülasyon · ${a.testData ? 'takım gücü Elo tablosundan; maç geçmişi ve oyuncular test verisi' : 'güç, form ve oyuncular maç veritabanından'}</span>
      <button onclick="select(${a.match.id}, true)">Yeniden hesapla</button></div>
  </section>

  <section class="panel">
    <div class="panel-h"><h2 id="cmpTitle"></h2><span class="hint" id="cmpHint"></span></div>
    <div class="cmp">
      <div class="cmp-in"><span>${esc(H.short)}</span><input id="cmpH" type="number" min="0" max="15" inputmode="numeric"><span>-</span><input id="cmpA" type="number" min="0" max="15" inputmode="numeric"><span>${esc(A.short)}</span></div>
      <div class="cmp-out" id="cmpOut"></div>
    </div>
  </section>

  <section class="panel">
    <div class="panel-h"><h2>Simülasyon</h2><span class="hint">En olası sonucun en olası skoruyla biten temsili 90 dakika</span>
      <div class="controls"><button id="btnSpeed">Hız: 1x</button><button id="btnPlay" class="primary">▶ Oynat</button></div></div>
    <div class="sim-grid">
      <div>
        <div class="scoreboard">
          <div class="sb-team r">${crest(H.short, hc, 'sm')}<span>${esc(H.name)}</span></div>
          <div id="score">0 - 0</div>
          <div class="sb-team">${crest(A.short, ac, 'sm')}<span>${esc(A.name)}</span></div>
          <div class="clock" id="clock">0'</div>
        </div>
        <canvas id="pitch" width="1200" height="700"></canvas>
        <canvas id="mom" width="1200" height="76"></canvas>
        <div class="legend"><span>▲ ${esc(H.name)} baskısı</span><span>Devre arası</span><span>${esc(A.name)} baskısı ▼</span></div>
      </div>
      <div class="feed-wrap"><div class="feed-h">Maç akışı</div><div id="feed"></div></div>
    </div>
    <div id="simStats">${teamsH}${pairRows(a.simulation.stats, 0, hc, ac, 0)}</div>
  </section>

  ${lineupPanel(a, hc, ac)}

  <div class="cols">
    <section class="panel"><div class="panel-h"><h2>Maçın anahtarı</h2></div>
      <ol class="insights">${a.comments.map(c => `<li><span>${esc(c)}</span></li>`).join('')}</ol></section>
    <section class="panel"><div class="panel-h"><h2>Sonuca göre skorlar</h2><span class="hint">Her sonuç için en olası üç skor</span></div>
      ${outcomeScores(a, hc, ac)}
      <p class="note">İlk gol: ${esc(H.name)} %${num(a.firstGoal.home)} · ${esc(A.name)} %${num(a.firstGoal.away)} · golsüz %${num(a.firstGoal.none)}</p></section>
  </div>

  <div class="cols">
    <section class="panel"><div class="panel-h"><h2>Güç karşılaştırması</h2><span class="hint">0–100</span></div>${teamsH}${pairRows(a.comparison, 100, hc, ac, 0)}</section>
    <section class="panel"><div class="panel-h"><h2>Beklenen istatistikler</h2></div>${teamsH}${pairRows(a.expected, 0, hc, ac, 2)}</section>
  </div>

  <div class="cols">
    <section class="panel"><div class="panel-h"><h2>Gol dakikaları</h2><span class="hint">Dilim başına beklenen gol</span></div>
      <div class="timing">${a.goalTiming.map(t => `<div class="tcol"><div class="tbars">
        <i style="height:${t.home / maxTiming * 100}%;background:${hc}" title="${esc(H.name)}: ${num(t.home, 2)}"></i>
        <i style="height:${t.away / maxTiming * 100}%;background:${ac}" title="${esc(A.name)}: ${num(t.away, 2)}"></i></div><span>${esc(t.label)}</span></div>`).join('')}</div></section>
    <section class="panel"><div class="panel-h"><h2>Gol beklenen oyuncular</h2><span class="hint">Gol attığı simülasyon payı</span></div>
      ${scorers('home', H, hc)}${scorers('away', A, ac)}</section>
  </div>

  <div class="cols">
    <section class="panel"><div class="panel-h"><h2>${esc(H.name)}</h2><span class="hint">Son maçlar</span></div>${formBlock(a.homeForm)}</section>
    <section class="panel"><div class="panel-h"><h2>${esc(A.name)}</h2><span class="hint">Son maçlar</span></div>${formBlock(a.awayForm)}</section>
  </div>

  <section class="panel"><div class="panel-h"><h2>Aralarındaki maçlar</h2></div>${a.headToHead.length
    ? '<table>' + a.headToHead.map(h => `<tr><td class="muted">${fmtShort(h.date)}</td><td>${esc(h.home)}</td><td class="n">${h.homeGoals} - ${h.awayGoals}</td><td>${esc(h.away)}</td></tr>`).join('') + '</table>'
    : '<span class="muted">Kayıtlı maç yok.</span>'}</section>`;

  setupCompare(a);
  setupPlay(a, hc, ac);
}

// ---------------- sonuca göre skorlar ----------------
// Tek tek skorlar sıralanınca dengeli maçların çoğunda 1-1 birinci çıkar ve bir şey söylemez.
// Bunun yerine her sonuç (ev / beraberlik / deplasman) için o sonuç içindeki en olası skorlar gösterilir.
function outcomeScores(a, hc, ac) {
  const p = a.probabilities, H = a.match.home, A = a.match.away;
  const cols = [
    { label: H.name + ' kazanırsa', prob: p.home, col: hc, list: a.allScores.filter(s => s.home > s.away) },
    { label: 'Berabere biterse', prob: p.draw, col: '#77829a', list: a.allScores.filter(s => s.home === s.away) },
    { label: A.name + ' kazanırsa', prob: p.away, col: ac, list: a.allScores.filter(s => s.home < s.away) },
  ];
  const max = Math.max(...cols.map(c => c.list.length ? c.list[0].pct : 0), 0.1);
  return '<div class="oc">' + cols.map(c => `<div class="oc-col">
      <div class="oc-h"><i class="dot" style="background:${c.col}"></i><span>${esc(c.label)}</span><b>%${num(c.prob)}</b></div>
      ${c.list.slice(0, 3).map((s, i) => `<div class="oc-row ${i === 0 ? 'first' : ''}"><b>${s.home} - ${s.away}</b>
        <div class="bar"><i style="width:${s.pct / max * 100}%;background:${c.col}"></i></div><span>%${num(s.pct)}</span></div>`).join('') || '<div class="oc-row"><span>Simülasyonlarda çıkmadı</span></div>'}
    </div>`).join('') + '</div>';
}

// ---------------- kadrolar ----------------
function lineupPanel(a, hc, ac) {
  if (!a.lineups) return '';
  const side = (lu, t, col) => `<section class="panel">
    <div class="lu-h">${crest(t.short, col, 'sm')}<b>${esc(t.name)}</b><span class="lu-tag ${lu.confirmed ? '' : 'prob'}">${lu.confirmed ? 'Açıklanan 11' : 'Muhtemel 11'}</span><span class="frm">${esc(lu.formation)}</span></div>
    <p class="lu-sub">${lu.confirmed ? 'Maç kadrosundan' : 'Kadro açıklanmadı; son maçlarda en çok ilk 11 başlayanlar'}${lu.manager ? ' · T.D. ' + esc(lu.manager) : ''}</p>
    ${lu.starting.map(p => `<div class="lu-row"><i>${p.number || ''}</i><span>${esc(p.name)}</span><span>${esc(p.tactical || p.pos)}</span></div>`).join('')}
    ${lu.bench.length ? '<p class="lu-bench"><b>Yedekler:</b> ' + lu.bench.map(p => esc(p.shortName || p.name)).join(', ') + '</p>' : ''}
  </section>`;
  return '<div class="cols">' + side(a.lineups.home, a.match.home, hc) + side(a.lineups.away, a.match.away, ac) + '</div>';
}

// ---------------- tahmin – gerçek sonuç ----------------
function setupCompare(a) {
  const known = a.match.actualHome != null, past = new Date(a.match.date) < new Date();
  $('#cmpTitle').textContent = known ? 'Tahmin – gerçek sonuç' : past ? 'Gerçek skoru gir, modeli sına' : 'Skor tahminini dene';
  $('#cmpHint').textContent = known ? 'Model yalnızca bu maçtan önceki veriyi kullandı'
    : past ? 'Maç oynandı ama sonucu veritabanında yok' : 'Bir skor yaz; simülasyonlarda ne sıklıkla çıktığını gör';
  const h = $('#cmpH'), w = $('#cmpA');
  if (known) { h.value = a.match.actualHome; w.value = a.match.actualAway; h.disabled = w.disabled = true; }
  h.oninput = w.oninput = () => drawCompare(a);
  drawCompare(a);
}
function drawCompare(a) {
  const out = $('#cmpOut'), hv = $('#cmpH').value, av = $('#cmpA').value;
  if (hv === '' || av === '') { out.innerHTML = '<div class="cmp-box" style="grid-column:1/-1"><span>İki takımın skorunu yazın.</span></div>'; return; }
  const gh = Number(hv), ga = Number(av), p = a.probabilities, H = a.match.home, A = a.match.away;
  const res = gh > ga ? 'home' : gh === ga ? 'draw' : 'away';
  const names = { home: H.name + ' galibiyeti', draw: 'Beraberlik', away: A.name + ' galibiyeti' };
  const order = ['home', 'draw', 'away'].sort((x, y) => p[y] - p[x]), rank = order.indexOf(res) + 1;
  const si = a.allScores.findIndex(s => s.home === gh && s.away === ga), sp = si >= 0 ? a.allScores[si].pct : 0;
  const eg = a.expected[0];
  out.innerHTML =
    '<div class="cmp-box ' + (rank === 1 ? 'good' : rank === 3 ? 'bad' : '') + '"><small>Sonuç</small><b>%' + num(p[res]) + '</b><span>' + esc(names[res]) + ' · modelin ' + rank + '. olasılığı</span></div>' +
    '<div class="cmp-box ' + (si >= 0 && si < 3 ? 'good' : si < 0 || si > 9 ? 'bad' : '') + '"><small>Tam skor ' + gh + '-' + ga + '</small><b>%' + num(sp) + '</b><span>' + (si >= 0 ? 'en olası ' + (si + 1) + '. skor' : 'simülasyonlarda hiç çıkmadı') + '</span></div>' +
    '<div class="cmp-box"><small>Gol</small><b>' + (gh + ga) + ' <span style="font-size:14px">gerçek</span></b><span>beklenen toplam ' + num(eg.home + eg.away, 2) + ' (' + num(eg.home, 2) + ' - ' + num(eg.away, 2) + ')</span></div>';
}

// ---------------- Elo tablosu ----------------
async function loadElo() {
  const teams = await api('/api/teams');
  $('#eloNote').textContent = status.eloEditable
    ? 'Takımların genel gücü bu puanlardan gelir. Değerler yaklaşıktır; düzelttiğiniz puan "elle girildi" olarak işaretlenir ve saklanan analizler yeniden hesaplanır.'
    : 'Puanlar maç sonuçlarından hesaplanır (son maçlar daha ağırlıklı); farklı ligler Avrupa kupaları üzerinden aynı ölçeğe bağlanır. Son iki yılda en az 12 maçı olan takımlar listelenir.';
  if (!status.eloEditable) {
    $('#eloTable').innerHTML = '<tr><th>#</th><th>Takım</th><th>Ülke</th><th>Maç</th><th>Güç</th></tr>' + teams.map((t, i) =>
      `<tr><td class="muted" style="display:table-cell">${i + 1}</td><td><span style="display:inline-flex;align-items:center;gap:10px;font-weight:600"><i class="dot" style="background:${t.color}"></i>${esc(t.name)}</span></td>
        <td class="muted">${esc(t.country)}</td><td class="n muted">${t.matches}</td><td class="n">${num(t.elo, 0)}</td></tr>`).join('');
    return;
  }
  $('#eloTable').innerHTML = '<tr><th>Takım</th><th>Ülke</th><th>Elo</th><th>Durum</th><th></th></tr>' + teams.map(t =>
    `<tr><td><i class="dot" style="background:${t.color}"></i>${esc(t.name)}</td><td class="muted">${esc(t.country)}</td>
      <td><input class="elo" type="number" min="800" max="2400" value="${t.elo}" id="elo${t.id}"></td>
      <td class="muted">${t.eloApprox ? 'yaklaşık' : 'elle girildi'}</td>
      <td class="n"><button onclick="saveElo(${t.id})">Kaydet</button></td></tr>`).join('');
}
async function saveElo(id) {
  try {
    await api('/api/teams/' + id + '/elo', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ elo: Number($('#elo' + id).value) }) });
    showError(''); await loadElo();
  } catch (e) { showError(e.message); }
}

function showTab(elo) {
  $('#viewMatches').style.display = elo ? 'none' : '';
  $('#viewElo').style.display = elo ? '' : 'none';
  $('#tabElo').classList.toggle('on', elo); $('#tabMatches').classList.toggle('on', !elo);
  if (elo) loadElo();
}
$('#tabMatches').onclick = () => showTab(false);
$('#tabElo').onclick = () => showTab(true);
