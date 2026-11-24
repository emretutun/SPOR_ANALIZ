// ---------------- simülasyon oynatma (2D saha) ----------------
// Hareket mantığı: oyuncular dizilişlerinde YAVAŞ kayar; hızlı olan toptur ve oyuncudan oyuncuya PASLA gider.
// Kimse topun peşinden koşmaz. Tek istisna, şut atacak oyuncunun pozisyona koşusudur.
const EVENT_TEXT = { goal: 'GOL!', shot_on: 'İsabetli şut', shot_off: 'Şut dışarıda', corner: 'Korner', yellow: 'Sarı kart', red: 'Kırmızı kart', sub: 'Oyuncu değişikliği' };
const EVENT_ICON = { goal: '⚽', shot_on: '◎', shot_off: '○', corner: '⚑', yellow: '🟨', red: '🟥', sub: '⇅' };
const SLOT_MS = 560;        // bir maç dakikasının ekrandaki süresi (1x hızda, olaysız dakika)
const HALF_SLOT = 47;       // ikinci yarının ilk dilimi
const PLAYER_SPEED = 0.11;  // oyuncunun olağan hız sınırı: saniyede saha boyunun yüzde 11'i
const BLOCK_RATE = 1.0;     // takım bloğunun yeni bölgeye kayma hızı (saniyede; yüksek = daha çabuk)
const clamp = (v, a, b) => Math.min(b, Math.max(a, v));

// Aynı maç her oynatılışta aynı görünsün diye tohumlu rastgele sayı
function rngFor(seed) {
  let a = seed >>> 0;
  return () => { a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; };
}

// "4-2-3-1" gibi bir dizilişi 11 saha konumuna çevirir: [kendi kalesinden uzaklık, yan konum] (0..1)
function formationCoords(str) {
  let rows = String(str || '').split('-').map(Number).filter(n => n > 0 && n < 7);
  if (rows.reduce((s, n) => s + n, 0) !== 10) rows = [4, 3, 3];
  const out = [[0.045, 0.5]], R = rows.length;
  rows.forEach((c, r) => {
    const x = R === 1 ? 0.36 : 0.17 + r * (0.42 / (R - 1));
    for (let j = 0; j < c; j++) out.push([x, 0.5 + (j - (c - 1) / 2) * Math.min(0.24, 0.8 / c)]);
  });
  return out;
}

// İki takımın 11'er oyuncusu: kadro varsa gerçek isim ve forma numarasıyla, dizilişe göre
function buildDots(a) {
  const dots = [], rnd = rngFor(a.match.id * 131 + 7);
  for (const side of ['home', 'away']) {
    const lu = a.lineups ? a.lineups[side] : null, coords = formationCoords(lu && lu.formation);
    for (let i = 0; i < 11; i++) {
      const p = lu && lu.starting[i];
      // Hatlar cetvelle çizilmiş gibi durmasın: her oyuncunun kendine özgü küçük bir sapması var (maça göre sabit)
      const wide = i > 0 && Math.abs(coords[i][1] - 0.5) > 0.24;
      dots.push({ side, idx: i, base: coords[i], id: p ? p.id : null, num: p && p.number ? p.number : (i === 0 ? 1 : i + 1), name: p ? p.shortName : '',
        wide, ox: i === 0 ? 0 : (rnd() - 0.5) * 0.06 + (wide ? 0.012 : -0.008), oy: i === 0 ? 0 : (rnd() - 0.5) * 0.05 });
    }
  }
  return dots;
}

// Oyuncunun dizilişteki yeri. Hesap takımın kendi yönüne göre yapılır (0 = kendi kalesi), sonra sahaya çevrilir.
// - Savunma hattı, oyunun oynandığı bölgeye (zone) göre ileri geri gider; hücumda orta sahaya kadar çıkar.
// - Hatlar arası mesafe dardır: hücum eden takımın orta sahası ceza sahasına yakın, forveti içindedir.
// - Topun olduğu kanattaki oyuncular oyuna yaklaşır, hücumda kanat oyuncuları öne çıkar.
function targetOf(d, zone) {
  const home = d.side === 'home', zx = home ? zone.x : 1 - zone.x, zy = home ? zone.y : 1 - zone.y;
  let x, y;
  if (d.idx === 0) {
    x = 0.045 + clamp(zx - 0.5, -0.3, 0.5) * 0.07;
    y = 0.5 + (zy - 0.5) * 0.12;
  } else {
    const line = clamp(zx - 0.33, 0.13, 0.52);
    const near = Math.max(0, 1 - Math.abs(zy - d.base[1]) / 0.35);
    x = line + (d.base[0] - 0.17) * 0.72 + d.ox;
    x += 0.035 * near * (zx > x ? 1 : -0.5);
    if (d.wide) x += 0.04 * clamp((zx - 0.4) / 0.4, 0, 1);
    y = d.base[1] + (zy - d.base[1]) * 0.18 + d.oy;
    x = clamp(x, 0.07, 0.93); y = clamp(y, 0.04, 0.96);
  }
  return home ? { x, y } : { x: 1 - x, y: 1 - y };
}

// Olaylardan ve baskı eğrisinden topun yolunu çıkarır. Ev sahibi sağa hücum eder.
// Çıktı: moves (topun hareketleri), zones (oyunun oynandığı bölge), orders (oyuncu koşuları), fires (olayların ekrana düşme anı).
function buildTimeline(a, dots) {
  const rnd = rngFor(a.match.id * 97 + 5), m = a.simulation.momentum, n = m.length, max = Math.max(4, ...m.map(Math.abs));
  const moves = [], fires = [], zones = [], orders = [], slotStart = [], perSlot = {};
  for (const e of a.simulation.events) (perSlot[e.slot] ||= []).push(e);
  const isCard = e => e.type === 'yellow' || e.type === 'red' || e.type === 'sub';   // top hareketi gerektirmeyen olaylar
  // Her dairenin o an hangi oyuncu olduğu: değişiklikle değişir; kırmızı kart gören daire oyundan çıkar
  const curId = dots.map(d => d.id), gone = new Set();
  const outfield = side => dots.map((d, i) => i).filter(i => dots[i].side === side && dots[i].idx > 0 && !gone.has(i));
  const keeper = side => dots.findIndex(d => d.side === side && d.idx === 0);
  const CENTER = { x: 0.5, y: 0.5 };

  // Zaman ekseni esnektir: olaysız dakika 1 birim sürer, olaylı dakika olayların oynanması kadar uzar.
  let T = 0, y = 0.5, zone = { x: 0.5, y: 0.5 }, holder = -1, holderSide = 'home', lastArrive = 0, nextPass = 0.35;
  const block = { x: 0.5, y: 0.5 }; let blockT = 0;
  const settle = tNow => {
    const k = 1 - Math.exp(-Math.max(0, tNow - blockT) * SLOT_MS / 1000 * BLOCK_RATE);
    block.x += (zone.x - block.x) * k; block.y += (zone.y - block.y) * k; blockT = Math.max(blockT, tNow);
  };
  const setZone = (t, x, yy) => { settle(t); zone = { x, y: yy }; zones.push({ t, x, y: yy }); };
  setZone(-1, 0.5, 0.5);
  // Top hareketi: bir öncekinin bittiği yerden başlar, üst üste binmez
  // Topun beklenen yeri izlenir ki pas süresi mesafeye göre ayarlanabilsin: kısa pas çabuk, uzun pas daha uzun sürer
  let ballAt = { x: 0.5, y: 0.5 };
  const where = to => to.dot != null ? targetOf(dots[to.dot], block) : to;
  const passTime = to => { const p = where(to); return clamp(Math.hypot(p.x - ballAt.x, p.y - ballAt.y) / 0.5, 0.4, 1.2); };
  const move = (depart, dur, to, kind) => {
    settle(Math.max(depart, lastArrive));
    const mv = { depart: Math.max(depart, lastArrive), dur: dur == null ? passTime(to) : dur, to, kind };
    mv.arrive = mv.depart + mv.dur; moves.push(mv); lastArrive = mv.arrive; ballAt = where(to); return mv;
  };
  // Pasın gideceği oyuncu: istenen noktaya en yakın duran takım arkadaşı (biraz rastgelelikle)
  const pick = (side, point, exclude) => {
    let best = -1, bs = 9;
    for (const i of outfield(side)) {
      if (i === exclude) continue;
      const p = targetOf(dots[i], block), s = Math.hypot(p.x - point.x, (p.y - point.y) * 0.62) + rnd() * 0.10;
      if (s < bs) { bs = s; best = i; }
    }
    return best;
  };
  const passTo = (t, side, point) => { const r = pick(side, point, holder); const mv = move(t, null, { dot: r }); holder = r; holderSide = side; return mv; };

  for (let i = 0; i < n; i++) {
    slotStart.push(T);
    const evs = perSlot[i] || [], ballEvs = evs.filter(e => !isCard(e));
    evs.filter(isCard).forEach((e, j) => {
      fires.push({ t: T + 0.05 + j * 0.9, e });
      const k = curId.findIndex((id, q) => dots[q].side === e.side && id != null && id === (e.type === 'sub' ? e.outPlayerId : e.playerId));
      if (k < 0) return;
      if (e.type === 'sub') curId[k] = e.playerId;
      else if (e.type === 'red') gone.add(k);
    });
    y = clamp(y + (rnd() - 0.5) * 0.12, 0.2, 0.8);
    const ms = (m[Math.max(0, i - 1)] + 2 * m[i] + m[Math.min(n - 1, i + 1)]) / 4;
    let side = ms >= 0 ? 'home' : 'away', buildUp = null;
    for (let j = i + 1; j <= i + 5 && j < n && !buildUp; j++) {
      const ev = (perSlot[j] || []).find(e => e.type === 'goal' || e.type === 'shot_on' || e.type === 'shot_off');
      if (ev) buildUp = { side: ev.side, away: j - i };
    }
    if (buildUp) side = buildUp.side;

    if (i === HALF_SLOT) {
      // Devre arası: top santraya döner, ikinci yarıyı deplasman başlatır
      setZone(T, 0.5, 0.5);
      move(T, 0.7, CENTER, 'fade'); holder = -1; holderSide = 'away'; nextPass = lastArrive + 0.6; y = 0.5;
    }

    if (!ballEvs.length) {
      // Açık oyun: top, baskının olduğu bölgede oyuncudan oyuncuya dolaşır
      if (i !== 0 && i !== HALF_SLOT) {
        if (buildUp) setZone(T, 0.5 + (buildUp.side === 'home' ? 1 : -1) * (0.30 - 0.02 * buildUp.away), y);
        else setZone(T, 0.5 + clamp(ms / max, -1, 1) * 0.28, y);
      }
      while (nextPass < T + 1) {
        const t = Math.max(nextPass, lastArrive + 0.2), s = holder < 0 ? holderSide : side;
        passTo(t, s, { x: zone.x + (s === 'home' ? 0.03 : -0.03), y: y + (rnd() - 0.5) * 0.3 });
        nextPass = t + 1.3 + rnd() * 0.8;
      }
      T += 1;
      continue;
    }

    let c = Math.max(T, lastArrive + 0.15);
    for (const e of ballEvs) {
      const dir = e.side === 'home' ? 1 : -1, gx = v => 0.5 + dir * (v - 0.5), other = e.side === 'home' ? 'away' : 'home';
      if (e.type === 'corner') {
        const cy = rnd() < 0.5 ? 0.015 : 0.985;
        setZone(c, gx(0.86), 0.5);                                              // takım ceza sahasına yerleşir
        const out = move(c, 0.5, { x: gx(0.99), y: cy }, 'fade');               // top köşe bayrağında belirir
        fires.push({ t: out.arrive - 0.1, e });
        const targets = outfield(e.side).filter(k => dots[k].idx >= 5);
        const r = targets[Math.floor(rnd() * targets.length)];
        const cross = move(out.arrive + 0.35, null, { dot: r });                 // bekler, sonra orta
        holder = r; holderSide = e.side; c = cross.arrive + 0.2;
        continue;
      }

      // Şut: top şutu atacak takıma geçer, takım hücuma yerleşir, oyuncu şut noktasına gelir, pas ona ulaşır, vurur.
      // Şut hiçbir zaman olmadık yerden atılmaz: isabetsiz şut ceza sahası çevresinden, diğerleri daha yakından.
      let s = curId.findIndex((id, q) => dots[q].side === e.side && dots[q].idx > 0 && id != null && id === e.playerId && !gone.has(q));
      if (s < 0) { const f = outfield(e.side); s = f[f.length - 1 - Math.floor(rnd() * 3)]; }   // ileri uçtaki üç oyuncudan biri
      if (holder < 0 || dots[holder].side !== e.side) { const mv = passTo(c, e.side, { x: zone.x, y }); c = mv.arrive + 0.25; }
      setZone(c, gx(0.82), y);
      settle(c);
      const from = targetOf(dots[s], block), off = e.type === 'shot_off';
      const want = { x: gx((off ? 0.72 : 0.80) + rnd() * 0.06), y: 0.5 + (rnd() - 0.5) * (off ? 0.44 : 0.34) };
      const inRange = dir * (from.x - want.x) >= 0 && Math.abs(from.y - 0.5) < 0.3;        // zaten şut mesafesinde
      const spotTo = off && inRange ? from : want;
      const dist = Math.hypot(spotTo.x - from.x, spotTo.y - from.y);
      let run = 0;
      const origin = spotTo;
      if (dist > 0.30) {
        // Oyuncu çok geride (çoğunlukla savunmacı): duran top olarak oynanır.
        // Top serbest vuruş noktasında bekler, takım ceza sahasına yerleşir, orta gelir ve oyuncu vurur.
        const spot = { x: gx(0.76), y: want.y < 0.5 ? 0.86 : 0.14 };
        setZone(c, gx(0.86), 0.5);
        move(c, 0.5, spot, 'fade');
        run = clamp(dist / 0.12, 1.6, 3.6);
        orders.push({ dot: s, t0: c, t1: c + run, x: spotTo.x, y: spotTo.y });
        const d = clamp(Math.hypot(spotTo.x - spot.x, spotTo.y - spot.y) / 0.5, 0.4, 1.2);
        move(c + run - d, d, { dot: s }); ballAt = spotTo;
      } else if (dist > 0.015) {
        run = clamp(dist / 0.12, 0.6, 2.5);
        orders.push({ dot: s, t0: c, t1: c + run, x: spotTo.x, y: spotTo.y });
        if (holder !== s) { const d = clamp(Math.hypot(spotTo.x - ballAt.x, spotTo.y - ballAt.y) / 0.5, 0.4, 1.2); move(c + run - d, d, { dot: s }); }
        ballAt = spotTo;
      } else if (holder !== s) {
        const mv = move(c, null, { dot: s }); c = mv.arrive;
      }
      holder = s; holderSide = e.side;
      const gk = keeper(other);
      // Şutun süresi mesafeyle orantılıdır: uzaktan şutta top sahayı bir anda geçmez
      const shot = (end) => { const d = Math.hypot(end.x - origin.x, end.y - origin.y); const sh = move(Math.max(c + run, lastArrive) + 0.1, clamp(d / 0.7, 0.35, 1.0), end, 'shot'); fires.push({ t: sh.arrive, e }); return sh; };
      if (e.type === 'goal') {
        const sh = shot({ x: gx(1.012), y: 0.5 + (rnd() - 0.5) * 0.07 });
        setZone(sh.arrive + 0.8, 0.5, 0.5);                                     // top ağlarda bekler, takımlar yerine döner
        const back = move(sh.arrive + 0.9, 0.6, CENTER, 'fade');                // santra
        holder = -1; holderSide = other; c = back.arrive + 0.3; y = 0.5;
      } else if (e.type === 'shot_on') {
        const sh = shot({ x: gx(0.972), y: 0.5 + (rnd() - 0.5) * 0.08 });
        const mv = move(sh.arrive + 0.1, 0.22, { dot: gk });                    // kaleci topu aldı
        holder = gk; holderSide = other; c = mv.arrive + 0.3;
      } else {
        const sh = shot({ x: gx(1.03), y: 0.5 + (rnd() < 0.5 ? -1 : 1) * (0.1 + rnd() * 0.12) });
        const mv = move(sh.arrive + 0.25, 0.5, { dot: gk }, 'fade');            // kale vuruşu
        holder = gk; holderSide = other; c = mv.arrive + 0.3;
      }
    }
    T = Math.max(T + 1, c);
    nextPass = Math.max(nextPass, c + 0.3);
  }
  slotStart.push(T);
  fires.sort((p, q) => p.t - q.t);
  return { moves, zones, orders, fires, slotStart, total: T };
}

const freshState = () => ({ t: 0, fi: 0, mi: 0, zi: 0, si: 0, done: false, overlay: null, caption: null, trail: [], pl: null, sb: null, hg: 0, ag: 0, htShown: false });

function setupPlay(a, hc, ac) {
  const dots = buildDots(a);
  const roster = {};
  if (a.lineups) for (const side of ['home', 'away']) for (const p of a.lineups[side].starting.concat(a.lineups[side].bench)) roster[p.id] = p;
  play = Object.assign({ a, hc, ac, dots, roster, tl: buildTimeline(a, dots), running: false, speed: 1, last: 0 }, freshState());
  $('#btnPlay').onclick = () => play.running ? pausePlay() : startPlay();
  $('#btnSpeed').onclick = () => { play.speed = play.speed === 1 ? 2 : play.speed === 2 ? 4 : 1; $('#btnSpeed').textContent = 'Hız: ' + play.speed + 'x'; };
  drawAll(performance.now());
}
function startPlay() {
  if (play.done) {
    Object.assign(play, freshState());
    play.dots = buildDots(play.a);   // değişiklikler ve kırmızı kartlar geri alınır
    $('#feed').innerHTML = ''; $('#simStats').style.display = 'none'; $('#score').textContent = '0 - 0';
  }
  play.running = true; play.last = 0;
  $('#btnPlay').textContent = '❚❚ Duraklat';
  const mine = play;
  requestAnimationFrame(function loop(ts) {
    if (play !== mine || !mine.running) return;
    frame(ts); requestAnimationFrame(loop);
  });
}
function pausePlay() { play.running = false; $('#btnPlay').textContent = '▶ Oynat'; }
function stopPlay() { if (play) play.running = false; play = null; }

function frame(ts) {
  const dt = play.last ? Math.min(60, ts - play.last) : 0;   // sekme arka plandan dönünce sıçramasın
  play.last = ts;
  play.dt = dt * play.speed;
  play.t += dt / SLOT_MS * play.speed;
  const a = play.a, n = a.simulation.momentum.length, ss = play.tl.slotStart;
  while (play.fi < play.tl.fires.length && play.tl.fires[play.fi].t <= play.t) fire(play.tl.fires[play.fi++].e, ts);
  while (play.si < n - 1 && ss[play.si + 1] <= play.t) play.si++;
  $('#clock').textContent = SLOTS[play.si];
  if (!play.htShown && play.si >= HALF_SLOT) {
    play.htShown = true;
    play.overlay = { title: 'DEVRE ARASI', sub: a.match.home.short + '  ' + play.hg + ' - ' + play.ag + '  ' + a.match.away.short, col: '#8893a8', until: ts + 1800, big: false };
  }
  const endT = play.tl.total + 0.4;
  if (play.t >= endT) {
    while (play.fi < play.tl.fires.length) fire(play.tl.fires[play.fi++].e, ts);
    play.t = endT; play.running = false; play.done = true;
    play.overlay = { title: 'MAÇ SONU', score: play.hg + ' - ' + play.ag, sub: a.match.home.name + '  –  ' + a.match.away.name, col: '#e30613', until: Infinity, big: true };
    $('#btnPlay').textContent = '↻ Baştan oynat'; $('#clock').textContent = 'MS'; $('#simStats').style.display = 'block';
  }
  drawAll(ts);
}

function fire(e, ts) {
  const a = play.a, team = e.side === 'home' ? a.match.home : a.match.away;
  const col = e.side === 'home' ? play.hc : play.ac;
  const who = e.type === 'sub' ? '↑ ' + e.player + '  ↓ ' + e.outPlayer + ' (' + team.name + ')' : e.player ? e.player + ' (' + team.name + ')' : team.name;
  const label = SLOTS[e.slot];
  $('#feed').insertAdjacentHTML('afterbegin',
    `<div class="ev ${e.type === 'goal' ? 'goal' : ''}" style="border-left-color:${col}"><span class="ev-m">${label}</span><span class="ev-i">${EVENT_ICON[e.type] || '•'}</span><span>${EVENT_TEXT[e.type] || e.type} — ${esc(who)}${e.type === 'goal' ? ' · ' + e.homeGoals + '-' + e.awayGoals : ''}</span></div>`);
  if (e.type === 'sub') {
    // Çıkan oyuncunun dairesi giren oyuncuya geçer: numara ve isim değişir, kısa süre yeşil halkayla işaretlenir
    const d = play.dots.find(x => x.side === e.side && x.id === e.outPlayerId), info = play.roster[e.playerId];
    if (d) Object.assign(d, { id: e.playerId, name: info ? info.shortName : e.player, num: info && info.number ? info.number : '', flash: ts + 2600 });
    play.caption = { text: label + '  DEĞİŞİKLİK  ·  ↑ ' + (info ? info.shortName : e.player) + '   ↓ ' + (play.roster[e.outPlayerId] ? play.roster[e.outPlayerId].shortName : e.outPlayer), col, until: ts + 2200 };
    return;
  }
  if (e.type === 'red') { const d = play.dots.find(x => x.side === e.side && x.id === e.playerId); if (d) d.off = true; }   // atılan oyuncu sahadan çıkar
  if (e.type === 'goal') {
    play.hg = e.homeGoals; play.ag = e.awayGoals;
    $('#score').textContent = e.homeGoals + ' - ' + e.awayGoals;
    play.overlay = { title: 'GOL!', sub: (e.player || team.name) + ' · ' + label, score: e.homeGoals + ' - ' + e.awayGoals, col, until: ts + 2400, big: true };
  } else if (e.type === 'red' || e.type === 'yellow') {
    play.overlay = { title: EVENT_TEXT[e.type], sub: who + ' · ' + label, col: e.type === 'red' ? '#e03a3a' : '#f2c300', until: ts + 1500, big: false };
  } else {
    // Sahada o an ne olduğunu söyleyen etiket (korner, şut)
    play.caption = { text: label + '  ' + (EVENT_TEXT[e.type] || e.type).toLocaleUpperCase('tr-TR') + '  ·  ' + (e.player || team.name), col, until: ts + 1900 };
  }
}

// Bir top hedefinin o anki konumu: sabit nokta ya da bir oyuncunun ayağı (oyuncu hareket ettikçe birlikte gider)
function refPos(ref) {
  if (ref.dot == null) return ref;
  const p = play.pl[ref.dot];
  return { x: p.x + (play.dots[ref.dot].side === 'home' ? 0.013 : -0.013), y: p.y };
}

// Topun o anki durumu: bir oyuncudaysa onunla birlikte, pastaysa iki oyuncu arasında
function ballState(t) {
  const mv = play.tl.moves;
  while (play.mi < mv.length - 1 && mv[play.mi + 1].depart <= t) play.mi++;
  while (play.mi > 0 && mv[play.mi].depart > t) play.mi--;
  const k = play.mi, cur = mv[k];
  if (!cur || cur.depart > t) return { x: 0.5, y: 0.5, alpha: 1, holder: -1, flying: false };
  const from = k > 0 ? refPos(mv[k - 1].to) : { x: 0.5, y: 0.5 }, to = refPos(cur.to);
  if (t >= cur.arrive) return { x: to.x, y: to.y, alpha: 1, holder: cur.to.dot != null ? cur.to.dot : -1, flying: false };
  let u = (t - cur.depart) / cur.dur;
  if (cur.kind === 'fade') {
    // Top oyundan çıktı (aut, korner, santra): kayarak gitmez, kaybolup yeni yerinde belirir
    const at = u < 0.5 ? from : to;
    return { x: at.x, y: at.y, alpha: u < 0.3 ? 1 - u / 0.3 : u > 0.7 ? (u - 0.7) / 0.3 : 0, holder: -1, flying: false };
  }
  u = cur.kind === 'shot' ? u * (0.6 + 0.4 * u) : u * u * (3 - 2 * u);   // şut sert çıkar, pas yumuşak başlar ve biter
  return { x: from.x + (to.x - from.x) * u, y: from.y + (to.y - from.y) * u, alpha: 1, holder: -1, flying: true };
}

function drawAll(ts) { drawPitch(ts); drawMomentum(); }

function drawField(g, W, H, fx, fy, fw, fh) {
  const X = v => fx + v * fw, Y = v => fy + v * fh;
  g.fillStyle = '#0f2a1a'; g.fillRect(0, 0, W, H);
  for (let i = 0; i < 14; i++) { g.fillStyle = i % 2 ? '#1f7040' : '#1b6639'; g.fillRect(fx + i * fw / 14, fy, fw / 14 + 1, fh); }
  // Kenarlara doğru hafif kararma: saha ortası öne çıkar
  const vg = g.createRadialGradient(W / 2, H / 2, H * 0.25, W / 2, H / 2, W * 0.62);
  vg.addColorStop(0, 'rgba(0,0,0,0)'); vg.addColorStop(1, 'rgba(0,0,0,.32)');
  g.fillStyle = vg; g.fillRect(0, 0, W, H);

  g.strokeStyle = 'rgba(255,255,255,.82)'; g.lineWidth = 2.5; g.lineJoin = 'round';
  g.strokeRect(fx, fy, fw, fh);
  g.beginPath(); g.moveTo(X(0.5), fy); g.lineTo(X(0.5), fy + fh); g.stroke();
  const cr = fh * 0.135;
  g.beginPath(); g.arc(X(0.5), Y(0.5), cr, 0, Math.PI * 2); g.stroke();
  g.fillStyle = 'rgba(255,255,255,.82)'; g.beginPath(); g.arc(X(0.5), Y(0.5), 4, 0, Math.PI * 2); g.fill();
  for (const s of [0, 1]) {
    const d = s ? -1 : 1, x0 = X(s), spot = x0 + d * fw * 0.105;
    g.strokeRect(Math.min(x0, x0 + d * fw * 0.157), Y(0.5) - fh * 0.296, fw * 0.157, fh * 0.593);
    g.strokeRect(Math.min(x0, x0 + d * fw * 0.052), Y(0.5) - fh * 0.135, fw * 0.052, fh * 0.27);
    g.fillStyle = 'rgba(255,255,255,.82)'; g.beginPath(); g.arc(spot, Y(0.5), 3.5, 0, Math.PI * 2); g.fill();
    // Ceza yayı
    const th = Math.acos((fw * 0.052) / cr);
    g.beginPath(); if (s) g.arc(spot, Y(0.5), cr, Math.PI - th, Math.PI + th); else g.arc(spot, Y(0.5), cr, -th, th); g.stroke();
    // Kale ve ağlar
    const gw = 20, gy0 = Y(0.5) - fh * 0.054, gh = fh * 0.108, gx0 = s ? x0 : x0 - gw;
    g.fillStyle = 'rgba(255,255,255,.07)'; g.fillRect(gx0, gy0, gw, gh);
    g.save(); g.strokeStyle = 'rgba(255,255,255,.28)'; g.lineWidth = 1;
    g.beginPath();
    for (let k = 1; k < 4; k++) { g.moveTo(gx0 + k * gw / 4, gy0); g.lineTo(gx0 + k * gw / 4, gy0 + gh); }
    for (let k = 1; k < 7; k++) { g.moveTo(gx0, gy0 + k * gh / 7); g.lineTo(gx0 + gw, gy0 + k * gh / 7); }
    g.stroke(); g.restore();
    g.strokeRect(gx0, gy0, gw, gh);
    // Köşe yayları
    for (const top of [true, false]) {
      const cy = top ? fy : fy + fh, a0 = s ? (top ? Math.PI / 2 : Math.PI) : (top ? 0 : -Math.PI / 2);
      g.beginPath(); g.arc(x0, cy, 13, a0, a0 + Math.PI / 2); g.stroke();
    }
  }
}

// Oyuncuları bir kare ilerletir: herkes dizilişteki yerine doğru, hız sınırını aşmadan yürür.
function movePlayers(ts, dts) {
  const tl = play.tl, t = play.t;
  while (play.zi < tl.zones.length - 1 && tl.zones[play.zi + 1].t <= t) play.zi++;
  while (play.zi > 0 && tl.zones[play.zi].t > t) play.zi--;
  const zone = tl.zones[play.zi];
  // Bölge değişimi de yumuşatılır: takım bloğu yeni yerine bir anda değil, yavaşça kayar
  if (!play.sb) play.sb = { x: zone.x, y: zone.y };
  const k = 1 - Math.exp(-dts * BLOCK_RATE);
  play.sb.x += (zone.x - play.sb.x) * k; play.sb.y += (zone.y - play.sb.y) * k;

  const targets = play.dots.map(d => targetOf(d, play.sb));
  if (!play.pl) play.pl = targets.map(p => ({ x: p.x, y: p.y }));
  const running = {};
  for (const o of tl.orders) if (t >= o.t0 && t <= o.t1 + 0.6) running[o.dot] = o;

  play.pl.forEach((p, i) => {
    const o = running[i];
    let tx = targets[i].x, ty = targets[i].y, cap = PLAYER_SPEED * dts;
    if (o) {
      // Şut pozisyonuna koşu: tam zamanında varacak hızda, ama olağan hızın en fazla iki katı
      tx = o.x; ty = o.y;
      const left = Math.max(0.08, (o.t1 - t) * SLOT_MS / 1000), need = Math.hypot(tx - p.x, ty - p.y) / left;
      cap = clamp(need, PLAYER_SPEED, PLAYER_SPEED * 2.2) * dts;
    } else {
      // Yerinde çok hafif salınım: oyuncular donmuş görünmesin
      tx += Math.sin(ts / 3900 + i * 2.3) * 0.012;
      ty += Math.cos(ts / 4700 + i * 1.7) * 0.012;
    }
    const dx = tx - p.x, dy = ty - p.y, d = Math.hypot(dx, dy);
    // Hedefe yaklaştıkça yavaşlar (ani duruş olmaz), hiçbir zaman hız sınırını geçmez
    const step = Math.min(cap, d * (1 - Math.exp(-dts * 4)));
    if (d > 1e-5) { p.x += dx / d * step; p.y += dy / d * step; }
  });
}

function drawPitch(ts) {
  const c = $('#pitch'); if (!c || !play) return;
  const g = c.getContext('2d'), W = c.width, H = c.height, fx = 40, fy = 26, fw = W - 80, fh = H - 52;
  const X = v => fx + v * fw, Y = v => fy + v * fh;
  drawField(g, W, H, fx, fy, fw, fh);

  const dts = (play.dt || 0) / 1000; play.dt = 0;
  movePlayers(ts, dts);
  const b = ballState(play.t), pl = play.pl, dots = play.dots;

  // Oyuncular: gölge, forma renginde daire, forma numarası
  const R = 17;
  g.textAlign = 'center'; g.textBaseline = 'middle';
  pl.forEach((p, i) => {
    const d = dots[i], col = d.side === 'home' ? play.hc : play.ac, px = X(p.x), py = Y(p.y), gk = d.idx === 0;
    if (d.off) return;
    g.fillStyle = 'rgba(0,0,0,.28)'; g.beginPath(); g.ellipse(px + 2, py + R * 0.75, R * 0.95, R * 0.42, 0, 0, Math.PI * 2); g.fill();
    g.beginPath(); g.arc(px, py, R, 0, Math.PI * 2);
    g.fillStyle = gk ? '#1a1e25' : col; g.fill();
    g.lineWidth = 3; g.strokeStyle = gk ? col : ink(col) === '#fff' ? 'rgba(255,255,255,.9)' : 'rgba(10,12,16,.8)'; g.stroke();
    g.fillStyle = gk ? '#fff' : ink(col); g.font = '700 16px Bahnschrift, "Segoe UI", sans-serif';
    g.fillText(String(d.num), px, py + 1);
    if (d.flash && ts < d.flash) { g.lineWidth = 4; g.strokeStyle = 'rgba(62,207,122,' + Math.min(1, (d.flash - ts) / 600) + ')'; g.beginPath(); g.arc(px, py, R + 6, 0, Math.PI * 2); g.stroke(); }
  });
  // Top kimdeyse: halka ve isim
  if (b.holder >= 0 && b.alpha >= 1 && !dots[b.holder].off) {
    const p = pl[b.holder], px = X(p.x), py = Y(p.y), name = dots[b.holder].name;
    g.lineWidth = 2.5; g.strokeStyle = 'rgba(255,255,255,.95)'; g.beginPath(); g.arc(px, py, R + 5, 0, Math.PI * 2); g.stroke();
    if (name) {
      g.font = '600 17px "Segoe UI", sans-serif';
      const tw = g.measureText(name).width + 20, ty = py - R - 26;
      g.fillStyle = 'rgba(8,11,17,.86)'; g.beginPath(); g.roundRect(px - tw / 2, ty - 13, tw, 26, 13); g.fill();
      g.fillStyle = '#fff'; g.fillText(name, px, ty + 1);
    }
  }
  g.textBaseline = 'alphabetic';

  // Top: yalnızca havadayken iz bırakır
  if (!b.flying) play.trail = []; else { play.trail.push({ x: b.x, y: b.y }); if (play.trail.length > 10) play.trail.shift(); }
  g.globalAlpha = b.alpha;
  play.trail.forEach((p, i) => { g.fillStyle = `rgba(255,255,255,${i / play.trail.length * 0.25})`; g.beginPath(); g.arc(X(p.x), Y(p.y), 5, 0, Math.PI * 2); g.fill(); });
  g.fillStyle = 'rgba(0,0,0,.4)'; g.beginPath(); g.ellipse(X(b.x) + 2, Y(b.y) + 5, 8, 4, 0, 0, Math.PI * 2); g.fill();
  g.fillStyle = '#fff'; g.beginPath(); g.arc(X(b.x), Y(b.y), 8, 0, Math.PI * 2); g.fill();
  g.lineWidth = 1.5; g.strokeStyle = 'rgba(10,12,16,.7)'; g.stroke();
  g.globalAlpha = 1;

  const cp = play.caption;
  if (cp && ts < cp.until) {
    g.globalAlpha = Math.min(1, (cp.until - ts) / 300);
    g.font = '700 26px "Segoe UI", sans-serif'; g.textAlign = 'center';
    const tw = g.measureText(cp.text).width + 56, bx = W / 2 - tw / 2, by2 = H - 92;
    g.fillStyle = 'rgba(8,11,17,.88)'; g.beginPath(); g.roundRect(bx, by2, tw, 50, 25); g.fill();
    g.fillStyle = cp.col; g.beginPath(); g.arc(bx + 26, by2 + 25, 8, 0, Math.PI * 2); g.fill();
    g.fillStyle = '#fff'; g.fillText(cp.text, W / 2 + 12, by2 + 34);
    g.globalAlpha = 1;
  }

  const o = play.overlay;
  if (o && ts < o.until) {
    g.globalAlpha = Math.min(1, (o.until - ts) / 400);
    const bh = o.big ? 190 : 100, by = H / 2 - bh / 2;
    g.fillStyle = 'rgba(8,11,17,.86)'; g.fillRect(0, by, W, bh);
    g.fillStyle = o.col; g.fillRect(0, by, W, 6); g.fillRect(0, by + bh - 6, W, 6);
    g.textAlign = 'center'; g.fillStyle = '#fff';
    if (o.big) {
      g.font = '700 86px Bahnschrift, "Segoe UI", sans-serif'; g.fillText(o.title + '   ' + o.score, W / 2, by + 104);
      g.font = '600 32px "Segoe UI", sans-serif'; g.fillText(o.sub, W / 2, by + 154);
    } else {
      g.font = '700 36px Bahnschrift, "Segoe UI", sans-serif'; g.fillText(o.title, W / 2, by + 46);
      g.font = '500 25px "Segoe UI", sans-serif'; g.fillText(o.sub, W / 2, by + 82);
    }
    g.globalAlpha = 1;
  }
}

function drawMomentum() {
  const c = $('#mom'); if (!c || !play) return;
  const g = c.getContext('2d'), W = c.width, Hh = c.height, mid = Hh / 2, m = play.a.simulation.momentum;
  const max = Math.max(4, ...m.map(Math.abs)), bw = W / m.length, upto = play.done ? m.length : play.si;
  g.clearRect(0, 0, W, Hh);
  g.strokeStyle = '#3a4350'; g.lineWidth = 1; g.beginPath(); g.moveTo(0, mid); g.lineTo(W, mid); g.stroke();
  g.beginPath(); g.moveTo(HALF_SLOT * bw, 0); g.lineTo(HALF_SLOT * bw, Hh); g.stroke();
  for (let i = 0; i < upto && i < m.length; i++) {
    const h = Math.abs(m[i]) / max * (mid - 6);
    g.fillStyle = m[i] >= 0 ? play.hc : play.ac;
    g.fillRect(i * bw + 1, m[i] >= 0 ? mid - h : mid, bw - 2, h);
  }
  for (let k = 0; k < play.fi; k++) {
    const e = play.tl.fires[k].e; if (e.type !== 'goal') continue;
    g.fillStyle = '#fff'; g.beginPath(); g.arc(e.slot * bw + bw / 2, e.side === 'home' ? 8 : Hh - 8, 5, 0, Math.PI * 2); g.fill();
  }
}
