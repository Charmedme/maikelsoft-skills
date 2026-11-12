/* proto-shell/1: fixed shell runtime. Do not edit per prototype. */
(() => {
  'use strict';

  const SIZES = {
    mobile: { w: 390, h: 844, label: 'Mobile' },
    tablet: { w: 820, h: 1180, label: 'Tablet' },
    desktop: { w: 1440, h: 900, label: 'Desktop' },
    fit: { w: 0, h: 0, label: 'Fit' },
  };
  const MODES = [
    ['explore', 'Explore'], ['overview', 'Overview'], ['compare', 'Compare'], ['test', 'Test'], ['results', 'Results'],
  ];
  const DESIGNS = [['styled', 'Styled'], ['wireframe', 'Wireframe']];
  const INTERACTIVE = 'a,button,input,select,textarea,label,summary,[role=button],[role=tab],[role=link],[role=checkbox],[role=menuitem],[tabindex],[data-goto],[data-back],[data-set-state],[data-task-success],[contenteditable]';

  const $ = (s, r = document) => r.querySelector(s);

  function h(tag, props, ...kids) {
    const n = document.createElement(tag);
    for (const [k, v] of Object.entries(props || {})) {
      if (v === undefined || v === null || v === false) continue;
      if (k === 'class') n.className = v;
      else if (k === 'text') n.textContent = v;
      else if (k.startsWith('on')) n.addEventListener(k.slice(2), v);
      else n.setAttribute(k, v === true ? '' : String(v));
    }
    for (const kid of kids.flat()) if (kid !== null && kid !== undefined && kid !== false) n.append(kid.nodeType ? kid : String(kid));
    return n;
  }

  function fatal(msg) {
    document.body.innerHTML = '';
    document.body.append(h('div', { class: 'sh-card', role: 'alert' }, h('h2', { text: 'The prototype cannot start' }), h('p', { text: msg })));
  }

  function toast(msg) {
    const t = h('div', { class: 'sh-toast', role: 'status', text: msg });
    document.body.append(t);
    setTimeout(() => t.remove(), 2200);
  }

  // ---------- Content ----------
  let M;
  try { M = JSON.parse($('#proto-manifest').textContent); } catch (e) { fatal('The manifest is not valid JSON. ' + e.message); return; }
  const CSS = ($('#proto-css') || {}).textContent || '';
  let HS = {};
  try { HS = JSON.parse(($('#proto-strings') || {}).textContent || '{}'); } catch (e) { HS = {}; }
  const P = Object.assign({}, HS.en || {}, HS[(M.lang || 'en').slice(0, 2)] || {});
  const tr = (k, vars) => String(P[k] || k).replace(/\{(\w+)\}/g, (_, x) => (vars && x in vars ? vars[x] : ''));
  const SCRIPT = ($('#proto-script') || {}).textContent || '';
  const screens = M.screens || [];
  const variants = (M.variants && M.variants.length) ? M.variants : [{ id: 'A', name: 'A' }];
  const themes = (M.themes && M.themes.length) ? M.themes : ['light'];
  const tasks = M.tasks || [];
  const notes = M.notes || [];
  const hasWireTokens = /data-design\s*=\s*["']?wireframe/.test(CSS);
  const screenById = (id) => screens.find((s) => s.id === id) || screens[0];
  const statesOf = (id) => (screenById(id) || { states: ['default'] }).states || ['default'];
  const variantName = (id) => (variants.find((v) => v.id === id) || { name: id }).name;

  if (!screens.length) { fatal('The manifest has no screens.'); return; }

  function template(screenId, v) {
    const q = (sel) => document.querySelector(sel);
    return q(`template[data-screen="${screenId}"][data-variant="${v}"]`)
      || q(`template[data-screen="${screenId}"][data-variant="*"]`)
      || q(`template[data-screen="${screenId}"]:not([data-variant])`);
  }

  // ---------- View state ----------
  const start = M.start || { screen: screens[0].id, state: statesOf(screens[0].id)[0] };
  const view = {
    mode: 'explore', screen: start.screen, state: start.state || statesOf(start.screen)[0],
    v: variants[0].id, theme: themes[0], design: 'styled', size: 'desktop', notes: false, link: true,
  };
  const memory = {}; // proto.get / proto.set

  function readHash() {
    const p = new URLSearchParams(location.hash.slice(1));
    const pick = (k, ok) => { const x = p.get(k); if (x !== null && ok(x)) return x; return undefined; };
    view.mode = pick('mode', (x) => MODES.some((m) => m[0] === x)) || view.mode;
    view.screen = pick('screen', (x) => screens.some((s) => s.id === x)) || view.screen;
    view.state = pick('state', (x) => statesOf(view.screen).includes(x)) || (statesOf(view.screen).includes(view.state) ? view.state : statesOf(view.screen)[0]);
    view.v = pick('v', (x) => variants.some((v) => v.id === x)) || view.v;
    view.theme = pick('theme', (x) => themes.includes(x)) || view.theme;
    view.design = pick('design', (x) => DESIGNS.some((d) => d[0] === x)) || view.design;
    view.size = pick('size', (x) => x in SIZES) || view.size;
    view.notes = p.get('notes') === '1' ? true : (p.get('notes') === '0' ? false : view.notes);
  }
  function writeHash() {
    if (view.mode === 'test') return;
    const p = new URLSearchParams({ mode: view.mode, screen: view.screen, state: view.state, v: view.v, theme: view.theme, design: view.design, size: view.size, notes: view.notes ? '1' : '0' });
    try { history.replaceState(null, '', '#' + p.toString()); } catch (e) { /* sandboxed */ }
  }

  // ---------- Frame documents ----------
  const frames = {};
  let frameSeq = 0;

  function stateCss(state) {
    return `[data-show-state]:not([data-show-state~="${state}"]){display:none!important}[data-hide-state~="${state}"]{display:none!important}`;
  }
  const NOTE_CSS = 'html[data-notes="on"] [data-note]{outline:2px dashed #d6409f;outline-offset:3px;position:relative}'
    + 'html[data-notes="on"] [data-note]::after{content:attr(data-note);position:absolute;top:-11px;left:-11px;min-width:20px;height:20px;padding:0 5px;border-radius:10px;background:#d6409f;color:#fff;font:700 11px/20px system-ui,sans-serif;text-align:center;z-index:2147483647}';

  function buildDoc(key, f) {
    const tpl = template(f.screen, f.v);
    const body = tpl ? tpl.innerHTML : `<p style="font:16px system-ui;padding:24px">No template for screen "${f.screen}".</p>`;
    const wire = hasWireTokens ? '' : 'html[data-design="wireframe"] body{filter:grayscale(1)}';
    const lang = M.lang || 'en';
    return '<!doctype html><html lang="' + lang + '" data-theme="' + f.theme + '" data-design="' + f.design + '" data-state="' + f.state
      + '" data-variant="' + f.v + '" data-screen="' + f.screen + '" data-notes="' + (f.notes ? 'on' : 'off') + '"><head><meta charset="utf-8">'
      + '<meta name="viewport" content="width=device-width,initial-scale=1">'
      + '<script>window.proto=parent.__protoApi("' + key + '");<\/script>'
      + '<style>html,body{margin:0}' + stateCss(f.state) + NOTE_CSS + wire + '</style>'
      + '<style>' + CSS + '</style></head><body>' + body
      + (SCRIPT ? '<script>' + SCRIPT + '<\/script>' : '') + '</body></html>';
  }

  window.__protoApi = (key) => {
    const f = () => frames[key] || {};
    return {
      get screen() { return f().screen; }, get state() { return f().state; }, get variant() { return f().v; },
      get theme() { return f().theme; }, get design() { return f().design; },
      goto: (screen, state, delayMs) => { if (delayMs > 0) setTimeout(() => navigate(key, screen, state), delayMs); else navigate(key, screen, state); },
      setState: (state) => navigate(key, f().screen, state),
      back: () => back(key),
      success: (taskId) => { if (f().onSuccess) f().onSuccess(taskId); },
      get: (k) => memory[k],
      set: (k, val) => { memory[k] = val; },
    };
  };

  function makeFrame(opts) {
    const key = 'f' + (++frameSeq);
    const f = Object.assign({ key, hist: [], interactive: true }, opts);
    f.iframe = h('iframe', { title: opts.title || 'Prototype frame', tabindex: opts.interactive === false ? '-1' : null });
    f.iframe.addEventListener('load', () => wire(f));
    frames[key] = f;
    f.render = () => { f.iframe.srcdoc = buildDoc(key, f); };
    return f;
  }

  function wire(f) {
    let doc;
    try { doc = f.iframe.contentDocument; } catch (e) { return; }
    if (!doc) return;
    doc.addEventListener('click', (ev) => {
      if (f.onClick) f.onClick(ev.target.closest ? !!ev.target.closest(INTERACTIVE) : false);
      const t = ev.target.closest && ev.target.closest('[data-goto],[data-back],[data-set-state],[data-task-success]');
      const a = ev.target.closest && ev.target.closest('a[href]');
      if (a && (!t || !a.getAttribute('href').startsWith('http'))) ev.preventDefault();
      if (!t) return;
      ev.preventDefault();
      if (t.hasAttribute('data-task-success') && f.onSuccess) f.onSuccess(t.getAttribute('data-task-success'));
      if (t.hasAttribute('data-back')) back(f.key);
      else if (t.hasAttribute('data-goto')) { const [s, st] = t.getAttribute('data-goto').split(':'); navigate(f.key, s, st); }
      else if (t.hasAttribute('data-set-state')) navigate(f.key, f.screen, t.getAttribute('data-set-state'));
    }, true);
    doc.addEventListener('submit', (ev) => {
      ev.preventDefault();
      const form = ev.target;
      if (form.hasAttribute('data-task-success') && f.onSuccess) f.onSuccess(form.getAttribute('data-task-success'));
      if (form.hasAttribute('data-goto')) { const [s, st] = form.getAttribute('data-goto').split(':'); navigate(f.key, s, st); }
    }, true);
  }

  function navigate(key, screen, state) {
    const f = frames[key];
    if (!f || f.interactive === false) return;
    if (!screens.some((s) => s.id === screen)) { toast('Unknown screen: ' + screen); return; }
    const st = state && statesOf(screen).includes(state) ? state : (screen === f.screen ? f.state : statesOf(screen)[0]);
    f.hist.push([f.screen, f.state]);
    f.screen = screen; f.state = st;
    if (f.onNavigate) f.onNavigate(f);
    f.render();
  }
  function back(key) {
    const f = frames[key];
    if (!f || !f.hist.length) return;
    const [s, st] = f.hist.pop();
    f.screen = s; f.state = st;
    if (f.onNavigate) f.onNavigate(f, true);
    f.render();
  }

  // ---------- Device box and scaling ----------
  function device(f, size) {
    const sz = SIZES[size];
    const wrap = h('div', { class: 'sh-device ' + size });
    wrap.append(f.iframe);
    const fit = (box) => {
      let w = sz.w, hgt = sz.h, s = 1;
      if (size === 'fit') { w = Math.max(320, Math.floor(box.w)); hgt = Math.max(320, Math.floor(box.h)); }
      else { const pad = size === 'desktop' ? 0 : 16; s = Math.max(0.1, Math.min(1, (box.w - pad) / w, (box.h - pad) / hgt)); }
      f.iframe.style.width = w + 'px'; f.iframe.style.height = hgt + 'px'; f.iframe.style.transform = 'scale(' + s + ')';
      wrap.style.width = Math.round(w * s) + 'px'; wrap.style.height = Math.round(hgt * s) + 'px';
      f.scale = s; f.w = w; f.h = hgt;
    };
    return { wrap, fit };
  }

  function stageBox(stage, cols, extraH) {
    const r = stage.getBoundingClientRect();
    const pad = r.width < 700 ? 24 : 48; return { w: (r.width - pad - (cols - 1) * 24) / cols, h: r.height - pad - (extraH || 0) };
  }

  // ---------- Storage ----------
  const STORE_KEY = 'proto-shell:' + (M.title || 'prototype');
  let memSessions = [];
  let storageOk = true;
  function loadSessions() {
    try { const raw = localStorage.getItem(STORE_KEY); return raw ? JSON.parse(raw) : memSessions; } catch (e) { storageOk = false; return memSessions; }
  }
  function saveSessions(list) {
    memSessions = list;
    try { localStorage.setItem(STORE_KEY, JSON.stringify(list)); } catch (e) { storageOk = false; }
  }

  // ---------- Chrome ----------
  const root = h('div', { id: 'sh' });
  document.body.append(root);
  let stage, side, ro;

  function seg(label, options, current, onPick) {
    return h('div', { class: 'sh-field' }, h('span', { text: label }),
      h('div', { class: 'sh-seg', role: 'group', 'aria-label': label },
        options.map(([val, text]) => h('button', { type: 'button', 'aria-pressed': String(val === current), onclick: () => onPick(val) }, text))));
  }
  function select(label, options, current, onPick) {
    const s = h('select', { class: 'sh-select', 'aria-label': label, onchange: (e) => onPick(e.target.value) },
      options.map(([val, text]) => h('option', { value: val, selected: val === current }, text)));
    return h('label', { class: 'sh-field' }, h('span', { text: label }), s);
  }

  function set(patch) {
    Object.assign(view, patch);
    if (!statesOf(view.screen).includes(view.state)) view.state = statesOf(view.screen)[0];
    writeHash();
    render();
  }

  function topBar() {
    const testing = view.mode === 'test' && T.phase !== 'setup';
    const L = P.badge ? P : { badge: 'Prototype, not the product' };
    const kind = M.kind || 'throwaway';
    return h('header', { class: 'sh-top', id: 'proto-header', 'data-title': M.title || '', 'data-kind': kind, 'data-scope': M.scope || '' },
      h('span', { class: 'sh-badge', text: L.badge }),
      h('h1', { class: 'sh-title', text: M.title || 'Prototype', title: M.title || '' }),
      testing ? null : h('span', { class: 'sh-kind', text: (L[kind] || kind) + (M.scope ? ' · ' + M.scope : '') }),
      testing ? null : h('nav', { class: 'sh-tabs', role: 'tablist', 'aria-label': 'Mode' },
        MODES.map(([id, label]) => h('button', { class: 'sh-tab', role: 'tab', type: 'button', 'aria-selected': String(view.mode === id), onclick: () => set({ mode: id }) }, label))));
  }

  function toolBar() {
    if (view.mode === 'test' || view.mode === 'results') return null;
    const sizeSeg = seg('Size', Object.entries(SIZES).map(([k, v]) => [k, v.label]), view.size, (x) => set({ size: x }));
    const themeSeg = themes.length > 1 ? seg('Theme', themes.map((t) => [t, t[0].toUpperCase() + t.slice(1)]), view.theme, (x) => set({ theme: x })) : null;
    const designSeg = seg('Design', DESIGNS, view.design, (x) => set({ design: x }));
    const parts = [];
    if (view.mode !== 'overview') {
      parts.push(select('Screen', screens.map((s) => [s.id, s.name || s.id]), view.screen, (x) => set({ screen: x, state: statesOf(x)[0] })));
      parts.push(select('State', statesOf(view.screen).map((s) => [s, s]), view.state, (x) => set({ state: x })));
    }
    if (view.mode !== 'compare' && variants.length > 1) parts.push(seg('Variant', variants.map((v) => [v.id, v.id + ' · ' + (v.name || '')]), view.v, (x) => set({ v: x })));
    parts.push(designSeg);
    if (themeSeg) parts.push(themeSeg);
    parts.push(sizeSeg);
    if (view.mode !== 'overview') parts.push(seg('Notes', [['1', 'On'], ['0', 'Off']], view.notes ? '1' : '0', (x) => set({ notes: x === '1' })));
    if (view.mode === 'compare') parts.push(seg('Navigation', [['1', 'Linked'], ['0', 'Free']], view.link ? '1' : '0', (x) => set({ link: x === '1' })));
    parts.push(h('div', { class: 'sh-spacer' }));
    if (view.mode === 'explore') {
      parts.push(h('button', { class: 'sh-btn', type: 'button', onclick: () => current && back(current.key) }, 'Back'));
      parts.push(h('button', { class: 'sh-btn', type: 'button', onclick: () => set({ screen: start.screen, state: start.state || statesOf(start.screen)[0] }) }, 'Reset'));
    }
    parts.push(h('button', { class: 'sh-btn', type: 'button', onclick: copyLink }, 'Copy link'));
    return h('div', { class: 'sh-bar', role: 'toolbar', 'aria-label': 'View' }, parts);
  }

  async function copyLink() {
    try { await navigator.clipboard.writeText(location.href); toast('Link copied'); } catch (e) { toast('Copy the address bar: ' + location.hash); }
  }

  function questionLine() {
    if (view.mode === 'test') return null;
    return h('div', { class: 'sh-question' }, h('b', { text: 'Question: ' }), M.question || 'No question set.');
  }

  function sidePanel() {
    const v = variants.find((x) => x.id === view.v);
    const sn = notes.filter((n) => !n.screen || n.screen === view.screen);
    return h('aside', { class: 'sh-side', 'aria-label': 'Details' },
      h('h3', { text: 'Variants' }),
      h('ol', {}, variants.map((x) => h('li', {}, h('b', { text: x.id + ' · ' + (x.name || '') }), x.hypothesis ? h('div', { text: x.hypothesis }) : null))),
      h('h3', { text: 'Screen' }),
      h('p', { text: (screenById(view.screen).name || view.screen) + ' · state: ' + view.state }),
      v && view.mode === 'explore' ? h('p', { text: 'Variant ' + v.id + ': ' + (v.name || '') }) : null,
      h('h3', { text: 'Decision notes' }),
      sn.length ? h('ol', {}, sn.map((n) => h('li', {}, h('span', { class: 'sh-note-n', text: String(n.n) }), n.text))) : h('p', { text: 'No notes for this screen.' }),
      h('h3', { text: 'Keys' }),
      h('p', { text: '1 2 3 0: size · v: variant · t: theme · d: design · n: notes · [ ]: state' }));
  }

  // ---------- Modes ----------
  let current = null;

  function renderExplore() {
    const f = makeFrame({ screen: view.screen, state: view.state, v: view.v, theme: view.theme, design: view.design, notes: view.notes, title: 'Prototype frame' });
    f.onNavigate = (fr) => { view.screen = fr.screen; view.state = fr.state; writeHash(); refreshBars(); };
    current = f;
    const d = device(f, view.size);
    const label = h('div', { class: 'sh-label' });
    stage.append(h('div', { class: 'sh-frames' }, h('div', { class: 'sh-col' }, d.wrap, label)));
    f.render();
    return () => {
      d.fit(stageBox(stage, 1, 24));
      label.replaceChildren(h('b', { text: SIZES[view.size].label }), ' ' + f.w + ' x ' + f.h + ' · ' + Math.round(f.scale * 100) + '%');
    };
  }

  function renderCompare() {
    const shown = variants.slice(0, 3);
    const fs = shown.map((v) => makeFrame({ screen: view.screen, state: view.state, v: v.id, theme: view.theme, design: view.design, notes: view.notes, title: 'Variant ' + v.id }));
    fs.forEach((f) => {
      f.onNavigate = (fr) => {
        if (!view.link) return;
        view.screen = fr.screen; view.state = fr.state; writeHash();
        fs.forEach((o) => { if (o !== fr) { o.hist.push([o.screen, o.state]); o.screen = fr.screen; o.state = fr.state; o.render(); } });
        refreshBars();
      };
    });
    const wrap = h('div', { class: 'sh-frames' });
    const ds = fs.map((f, i) => {
      const d = device(f, view.size);
      const v = shown[i];
      wrap.append(h('div', { class: 'sh-col' }, d.wrap, h('div', { class: 'sh-label' }, h('b', { text: 'Variant ' + v.id }), ' ' + (v.name || ''))));
      return d;
    });
    stage.append(wrap);
    fs.forEach((f) => f.render());
    current = fs[0];
    return () => ds.forEach((d) => d.fit(stageBox(stage, fs.length, 24)));
  }

  function renderOverview() {
    const size = view.size === 'fit' ? 'desktop' : view.size;
    const sz = SIZES[size];
    const thumbW = size === 'desktop' ? 300 : (size === 'tablet' ? 220 : 170);
    stage.style.setProperty('--thumb', thumbW + 'px');
    const grid = h('div', { class: 'sh-grid' });
    for (const s of screens) {
      for (const st of s.states || ['default']) {
        const f = makeFrame({ screen: s.id, state: st, v: view.v, theme: view.theme, design: view.design, notes: false, interactive: false, title: s.id + ' ' + st });
        const scale = thumbW / sz.w;
        f.iframe.style.width = sz.w + 'px'; f.iframe.style.height = sz.h + 'px'; f.iframe.style.transform = 'scale(' + scale + ')';
        f.iframe.setAttribute('loading', 'lazy');
        const dev = h('div', { class: 'sh-device', style: 'width:' + thumbW + 'px;height:' + Math.round(sz.h * scale) + 'px' }, f.iframe);
        grid.append(h('button', { class: 'sh-thumb', type: 'button', 'aria-label': 'Open ' + (s.name || s.id) + ', ' + st, onclick: () => set({ mode: 'explore', screen: s.id, state: st }) },
          dev, h('div', { class: 'sh-label' }, h('b', { text: s.name || s.id }), ' · ' + st)));
        f.render();
      }
    }
    stage.append(grid);
    return null;
  }

  // ---------- Test mode ----------
  const T = { phase: 'setup', session: null, queue: [], i: 0 };

  function order(n, design) {
    const ids = variants.map((v) => v.id);
    if (ids.length < 2) return ids;
    if (design === 'between') return [ids[(n - 1) % ids.length]];
    const k = (n - 1) % ids.length;
    return ids.slice(k).concat(ids.slice(0, k));
  }

  function renderTest() {
    const sessions = loadSessions();
    if (T.phase === 'setup') {
      if (!tasks.length) { stage.append(h('div', { class: 'sh-card' }, h('h2', { text: 'No tasks' }), h('p', { text: 'The manifest has no tasks. Add tasks to run a test.' }))); return null; }
      const n = sessions.length + 1;
      const pid = h('input', { type: 'text', value: 'P' + String(n).padStart(2, '0') });
      let design = (M.test && M.test.design) || 'within';
      let size = view.size === 'fit' ? 'desktop' : view.size;
      const card = h('div', { class: 'sh-card' },
        h('h2', { text: 'Set up a test session' }),
        h('p', { text: tasks.length + ' tasks, ' + variants.length + ' version(s). The participant does not see the variant names or this question: ' + (M.question || '') }),
        h('label', {}, 'Participant', pid),
        h('div', { class: 'row' }, seg('Design', [['within', 'Within (all versions)'], ['between', 'Between (one version)']], design, (x) => { design = x; card.querySelectorAll('[aria-label="Design"] button').forEach((b, i) => b.setAttribute('aria-pressed', String(['within', 'between'][i] === x))); })),
        h('div', { class: 'row' }, seg('Device', [['mobile', 'Mobile'], ['tablet', 'Tablet'], ['desktop', 'Desktop']], size, (x) => { size = x; card.querySelectorAll('[aria-label="Device"] button').forEach((b, i) => b.setAttribute('aria-pressed', String(['mobile', 'tablet', 'desktop'][i] === x))); })),
        h('div', { class: 'row' },
          h('button', { class: 'sh-btn primary', type: 'button', onclick: () => {
            const num = parseInt((pid.value.match(/\d+/) || [n])[0], 10) || n;
            const ord = order(num, design);
            T.session = { id: Date.now().toString(36), participant: pid.value.trim() || ('P' + n), design, order: ord, size, startedAt: new Date().toISOString(), attempts: [], preference: null };
            T.queue = [];
            ord.forEach((v, vi) => tasks.forEach((t) => T.queue.push({ v, label: tr('version') + ' ' + (vi + 1), task: t })));
            T.i = 0; T.phase = 'ready'; render();
          } }, 'Start session'),
          h('button', { class: 'sh-btn', type: 'button', onclick: () => set({ mode: 'explore' }) }, 'Cancel')));
      stage.append(card);
      return null;
    }
    if (T.phase === 'done') {
      stage.append(h('div', { class: 'sh-card' }, h('h2', { text: tr('thanks') }), h('p', { text: tr('saved') + (storageOk ? '' : ' Browser storage is not available: export the results now.') }),
        h('div', { class: 'row' },
          h('button', { class: 'sh-btn primary', type: 'button', onclick: () => { T.phase = 'setup'; render(); } }, 'Next participant'),
          h('button', { class: 'sh-btn', type: 'button', onclick: () => { T.phase = 'setup'; set({ mode: 'results' }); } }, 'Results'))));
      return null;
    }
    if (T.phase === 'pref') {
      const labels = T.session.order.map((v, i) => [tr('version') + ' ' + (i + 1), v]);
      let pick = null;
      const why = h('textarea', {});
      const card = h('div', { class: 'sh-card' },
        h('h2', { text: (M.test && M.test.preference) || tr('preference') }),
        h('div', { class: 'row', role: 'group', 'aria-label': 'Preference' }, labels.concat([[tr('no_difference'), 'none']]).map(([t, v]) =>
          h('button', { class: 'sh-btn', type: 'button', 'aria-pressed': 'false', onclick: (e) => { pick = v; card.querySelectorAll('[aria-label="Preference"] button').forEach((b) => { b.classList.remove('primary'); b.setAttribute('aria-pressed', 'false'); }); e.currentTarget.classList.add('primary'); e.currentTarget.setAttribute('aria-pressed', 'true'); } }, t))),
        h('label', {}, tr('why'), why),
        h('div', { class: 'row' }, h('button', { class: 'sh-btn primary', type: 'button', onclick: () => { T.session.preference = { variant: pick, comment: why.value.trim() }; finish(); } }, tr('finish'))));
      stage.append(card);
      return null;
    }
    const item = T.queue[T.i];
    const meta = tr('task_of', { i: T.i + 1, n: T.queue.length }) + ' · ' + item.label;
    if (T.phase === 'ready') {
      stage.append(h('div', { class: 'sh-card' }, h('p', { class: 'meta', text: meta }), h('h2', { text: item.task.text }),
        h('div', { class: 'row' }, h('button', { class: 'sh-btn primary', type: 'button', onclick: () => { T.phase = 'run'; render(); } }, tr('start_task')))));
      return null;
    }
    // run or seq: show the frame
    const st0 = item.task.start || start;
    const st = (T.phase === 'seq' && T.run && T.run.last) ? T.run.last : { screen: st0.screen, state: st0.state || statesOf(st0.screen)[0] };
    const f = makeFrame({ screen: st.screen, state: st.state, v: item.v, theme: themes[0], design: 'styled', notes: false, title: 'Prototype' });
    const rec = T.run && T.run.i === T.i ? T.run : (T.run = { i: T.i, t0: performance.now(), path: [st.screen], clicks: 0, misclicks: 0, done: false });
    const ok = item.task.success || {};
    const hasRule = !!(ok.screen || ok.state || ok.element);
    const end = (success) => {
      if (rec.done) return; rec.done = true;
      rec.timeMs = Math.round(performance.now() - rec.t0);
      rec.success = success;
      rec.last = { screen: f.screen, state: f.state };
      T.phase = 'seq'; render();
    };
    f.onClick = (interactive) => { rec.clicks++; if (!interactive) rec.misclicks++; };
    f.onSuccess = (id) => { if (id === item.task.id) end(true); };
    f.onNavigate = (fr) => {
      rec.last = { screen: fr.screen, state: fr.state };
      rec.path.push(fr.screen + (fr.state !== 'default' ? ':' + fr.state : ''));
      if ((ok.screen || ok.state) && !ok.element && (!ok.screen || ok.screen === fr.screen) && (!ok.state || ok.state === fr.state)) setTimeout(() => end(true), 400);
    };
    const d = device(f, T.session.size);
    stage.append(h('div', { class: 'sh-frames' }, h('div', { class: 'sh-col' }, d.wrap)));
    f.render();
    if (T.phase === 'run') {
      stage.append(h('div', { class: 'sh-taskbar', role: 'region', 'aria-label': tr('task') },
        h('div', {}, h('div', { class: 'meta', text: meta }), h('div', { class: 't', text: item.task.text })),
        h('button', { class: 'sh-btn primary', type: 'button', onclick: () => end(hasRule ? false : 'self-reported') }, tr('done')),
        h('button', { class: 'sh-btn', type: 'button', onclick: () => end(false) }, tr('give_up'))));
    } else {
      stage.append(h('div', { class: 'sh-overlay' }, h('div', { class: 'sh-card', role: 'dialog', 'aria-label': tr('rate') },
        h('p', { class: 'meta', text: meta }),
        h('h2', { text: tr('seq') }),
        h('div', { class: 'sh-seq' }, [1, 2, 3, 4, 5, 6, 7].map((n) => h('button', { type: 'button', 'aria-label': String(n), onclick: () => rate(n) }, String(n)))),
        h('div', { class: 'sh-seq-ends' }, h('span', { text: tr('very_difficult') }), h('span', { text: tr('very_easy') })))));
    }
    function rate(n) {
      T.session.attempts.push({ variant: item.v, task: item.task.id, success: rec.success, timeMs: rec.timeMs, seq: n, clicks: rec.clicks, misclicks: rec.misclicks, path: rec.path.join(' > ') });
      T.run = null; T.i++;
      if (T.i < T.queue.length) T.phase = 'ready';
      else T.phase = (T.session.design === 'within' && T.session.order.length > 1) ? 'pref' : finish(true);
      render();
    }
    return () => d.fit(stageBox(stage, 1, 90));
  }

  function finish(silent) {
    T.session.endedAt = new Date().toISOString();
    const all = loadSessions(); all.push(T.session); saveSessions(all);
    T.phase = 'done';
    if (silent !== true) render();
    return 'done';
  }

  // ---------- Results ----------
  const median = (xs) => { if (!xs.length) return null; const s = xs.slice().sort((a, b) => a - b); const m = s.length >> 1; return s.length % 2 ? s[m] : (s[m - 1] + s[m]) / 2; };
  const mean = (xs) => xs.length ? xs.reduce((a, b) => a + b, 0) / xs.length : null;
  const pct = (x) => x === null ? 'n/a' : Math.round(x * 100) + '%';
  const sec = (ms) => ms === null ? 'n/a' : (ms / 1000).toFixed(1) + ' s';
  const num = (x) => x === null ? 'n/a' : x.toFixed(1);

  function renderResults() {
    const sessions = loadSessions();
    const box = h('div', { class: 'sh-results' });
    const attempts = sessions.flatMap((s) => s.attempts.map((a) => Object.assign({ session: s.id, participant: s.participant, design: s.design }, a)));
    const perVar = {};
    variants.forEach((v) => { perVar[v.id] = new Set(sessions.filter((s) => s.order.includes(v.id)).map((s) => s.id)).size; });
    box.append(h('h2', { text: 'Results: ' + sessions.length + ' session(s)' }));
    if (!sessions.length) box.append(h('p', { text: 'No sessions yet. Run a test in Test mode, or import a JSON file.' }));
    if (sessions.length && Object.values(perVar).some((n) => n < 20)) box.append(h('div', { class: 'sh-warnline', text: 'Indicative. Fewer than 20 participants per variant. Read the sessions for the reasons, not only the numbers.' }));
    if (!storageOk) box.append(h('div', { class: 'sh-warnline', text: 'Browser storage is not available. The results stay only until you close this page. Export them.' }));
    const head = h('tr', {}, ['Variant', 'Task', 'Attempts', 'Success', 'Median time', 'Mean SEQ', 'Mean misclicks'].map((t) => h('th', { scope: 'col', text: t })));
    const rows = [];
    for (const v of variants) {
      for (const t of tasks) {
        const a = attempts.filter((x) => x.variant === v.id && x.task === t.id);
        const ruled = a.filter((x) => x.success !== 'self-reported');
        rows.push(h('tr', {}, h('td', { text: v.id + ' · ' + (v.name || '') }), h('td', { text: t.id + ': ' + t.text }), h('td', { text: String(a.length) }),
          h('td', { text: ruled.length ? pct(ruled.filter((x) => x.success === true).length / ruled.length) : 'n/a' }),
          h('td', { text: sec(median(a.map((x) => x.timeMs))) }), h('td', { text: num(mean(a.map((x) => x.seq))) }), h('td', { text: num(mean(a.map((x) => x.misclicks))) })));
      }
    }
    box.append(h('table', { class: 'sh-table' }, h('thead', {}, head), h('tbody', {}, rows)));
    const prefs = sessions.filter((s) => s.preference && s.preference.variant);
    if (prefs.length) {
      const counts = {};
      prefs.forEach((s) => { const k = s.preference.variant; counts[k] = (counts[k] || 0) + 1; });
      box.append(h('table', { class: 'sh-table' }, h('thead', {}, h('tr', {}, h('th', { text: 'Preference' }), h('th', { text: 'Count' }), h('th', { text: 'Comments' }))),
        h('tbody', {}, Object.entries(counts).map(([k, c]) => h('tr', {}, h('td', { text: k === 'none' ? 'No difference' : k + ' · ' + variantName(k) }), h('td', { text: String(c) }),
          h('td', { text: prefs.filter((s) => s.preference.variant === k && s.preference.comment).map((s) => s.participant + ': ' + s.preference.comment).join(' | ') }))))));
    }
    const file = h('input', { type: 'file', accept: 'application/json', class: 'sh-hidden', onchange: (e) => importJson(e.target.files[0]) });
    const data = h('div', { class: 'sh-data sh-hidden' });
    const showData = (name, text, type) => {
      const ta = h('textarea', { readonly: true, 'aria-label': name });
      ta.value = text;
      data.replaceChildren(ta, h('div', { class: 'sh-actions' },
        h('button', { class: 'sh-btn primary', type: 'button', onclick: async () => { try { await navigator.clipboard.writeText(text); toast('Copied'); } catch (e) { ta.focus(); ta.select(); toast('Press Ctrl+C or Cmd+C to copy'); } } }, 'Copy ' + name),
        h('button', { class: 'sh-btn', type: 'button', onclick: () => download(name, text, type) }, 'Download (local file only)')));
      data.classList.remove('sh-hidden');
    };
    const del = h('button', { class: 'sh-btn danger', type: 'button' }, 'Delete all sessions');
    del.addEventListener('click', () => {
      if (del.dataset.armed === '1') { saveSessions([]); render(); return; }
      del.dataset.armed = '1'; del.textContent = 'Click again to delete ' + sessions.length + ' session(s)';
      setTimeout(() => { if (del.isConnected) { del.dataset.armed = ''; del.textContent = 'Delete all sessions'; } }, 4000);
    });
    box.append(h('div', { class: 'sh-actions' },
      h('button', { class: 'sh-btn primary', type: 'button', onclick: () => showData('results.json', JSON.stringify({ standard: 'proto-shell/1', title: M.title, sessions }, null, 2), 'application/json') }, 'Export JSON'),
      h('button', { class: 'sh-btn', type: 'button', onclick: () => showData('results.csv', toCsv(sessions), 'text/csv') }, 'Export CSV'),
      h('button', { class: 'sh-btn', type: 'button', onclick: () => file.click() }, 'Import JSON'), file, del), data);
    stage.append(box);
    return null;
  }

  function toCsv(sessions) {
    const cols = ['session', 'participant', 'design', 'order', 'variant', 'variant_name', 'task', 'success', 'time_ms', 'seq', 'clicks', 'misclicks', 'path', 'preference', 'comment', 'started_at'];
    const esc = (x) => { const s = x === null || x === undefined ? '' : String(x); return /[",\n]/.test(s) ? '"' + s.replace(/"/g, '""') + '"' : s; };
    const lines = [cols.join(',')];
    sessions.forEach((s) => s.attempts.forEach((a) => lines.push([s.id, s.participant, s.design, s.order.join('|'), a.variant, variantName(a.variant), a.task, a.success, a.timeMs, a.seq, a.clicks, a.misclicks, a.path,
      s.preference ? s.preference.variant : '', s.preference ? s.preference.comment : '', s.startedAt].map(esc).join(','))));
    return lines.join('\n');
  }
  function download(name, text, type) {
    try {
      const url = URL.createObjectURL(new Blob([text], { type }));
      const a = h('a', { href: url, download: name }); document.body.append(a); a.click(); a.remove();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (e) { toast('Download is blocked here.'); }
  }
  function importJson(fileObj) {
    if (!fileObj) return;
    fileObj.text().then((t) => {
      const data = JSON.parse(t);
      const incoming = Array.isArray(data) ? data : (data.sessions || []);
      const all = loadSessions(); const ids = new Set(all.map((s) => s.id));
      const add = incoming.filter((s) => s && s.id && !ids.has(s.id));
      saveSessions(all.concat(add)); toast('Imported ' + add.length + ' session(s)'); render();
    }).catch(() => toast('This file is not a results file.'));
  }

  // ---------- Render ----------
  let refit = null;
  function refreshBars() {
    const bar = root.querySelector('.sh-bar'); const sideOld = root.querySelector('.sh-side');
    const nb = toolBar(); if (bar && nb) bar.replaceWith(nb);
    if (sideOld) sideOld.replaceWith(sidePanel());
  }
  function render() {
    Object.keys(frames).forEach((k) => delete frames[k]);
    current = null;
    root.innerHTML = '';
    stage = h('main', { class: 'sh-stage', 'aria-label': 'Prototype stage' });
    const showSide = view.mode === 'explore' || view.mode === 'compare';
    root.append(topBar(), toolBar() || h('div'), questionLine() || h('div'), h('div', { class: 'sh-main', style: showSide ? '' : 'grid-template-columns:1fr' }, stage, showSide ? sidePanel() : null));
    refit = ({ explore: renderExplore, compare: renderCompare, overview: renderOverview, test: renderTest, results: renderResults }[view.mode])();
    if (refit) requestAnimationFrame(refit);
  }

  let rt;
  window.addEventListener('resize', () => { clearTimeout(rt); rt = setTimeout(() => refit && refit(), 120); });
  window.addEventListener('hashchange', () => { readHash(); render(); });
  window.addEventListener('keydown', (e) => {
    if (e.target.closest('input,textarea,select') || e.metaKey || e.ctrlKey || e.altKey) return;
    if (view.mode === 'test') return;
    const cyc = (arr, cur) => arr[(arr.indexOf(cur) + 1) % arr.length];
    const sts = statesOf(view.screen);
    const map = {
      '1': () => set({ size: 'mobile' }), '2': () => set({ size: 'tablet' }), '3': () => set({ size: 'desktop' }), '0': () => set({ size: 'fit' }),
      v: () => set({ v: cyc(variants.map((x) => x.id), view.v) }), t: () => set({ theme: cyc(themes, view.theme) }),
      d: () => set({ design: view.design === 'styled' ? 'wireframe' : 'styled' }), n: () => set({ notes: !view.notes }),
      '[': () => set({ state: sts[(sts.indexOf(view.state) - 1 + sts.length) % sts.length] }), ']': () => set({ state: cyc(sts, view.state) }),
      Escape: () => set({ mode: 'explore' }),
    };
    if (map[e.key]) { e.preventDefault(); map[e.key](); }
  });

  readHash();
  render();
})();
