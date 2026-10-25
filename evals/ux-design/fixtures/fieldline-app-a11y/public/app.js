/* Fieldline work orders: assign unplanned work orders (FL-31). Vanilla JS, no build. */
(function () {
  'use strict';

  // ---------- Constants and helpers ----------
  var TYPES = ['Installation', 'Maintenance', 'Repair'];
  var SKILLS = ['Heat pump', 'Air conditioning', 'Gas boiler'];
  var DUES = [['any', 'Any'], ['overdue', 'Overdue'], ['today', 'By today'], ['2days', 'Within 2 work days']];
  var VIEWS = [['open', 'All open'], ['unplanned', 'Unplanned'], ['waiting', 'Waiting for parts'], ['repairs', 'Repairs']];
  var STATUS_BADGE = { 'Unplanned': 'neutral', 'Planned': 'info', 'In progress': 'info', 'Waiting for parts': 'warning', 'Done': 'success', 'Cancelled': 'neutral' };
  var MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  var DAYS = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
  var TIMEOUT_MS = 10000;
  var UNDO_MS = 8000;
  var params = new URLSearchParams(location.search);
  var POLL_MS = Math.max(200, parseInt(params.get('poll') || '30000', 10) || 30000);

  function $(sel, root) { return (root || document).querySelector(sel); }
  function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]; }); }
  function slug(s) { return s.toLowerCase().replace(/[^a-z0-9]+/g, '-'); }
  function toDate(iso) { var p = iso.split('-'); return new Date(Date.UTC(+p[0], +p[1] - 1, +p[2])); }
  function toIso(d) { return d.toISOString().slice(0, 10); }
  function fmtDay(iso) { var d = toDate(iso); return d.getUTCDate() + ' ' + MONTHS[d.getUTCMonth()]; }
  function fmtDate(iso) { var d = toDate(iso); return DAYS[d.getUTCDay()] + ' ' + fmtDay(iso); }
  function fmtDateLong(iso) { return fmtDate(iso) + ' ' + toDate(iso).getUTCFullYear(); }
  function isWorkday(d) { var w = d.getUTCDay(); return w !== 0 && w !== 6; }
  function addWorkdays(iso, n) { var d = toDate(iso); while (n > 0) { d.setUTCDate(d.getUTCDate() + 1); if (isWorkday(d)) n--; } return toIso(d); }
  function joinList(items) { if (items.length <= 1) return items.join(''); return items.slice(0, -1).join(', ') + ' and ' + items[items.length - 1]; }
  function plural(n, one, many) { return n === 1 ? one : many; }
  function woCount(n) { return n + ' ' + plural(n, 'work order', 'work orders'); }
  function town(address) { var i = address.lastIndexOf(','); return i < 0 ? address : address.slice(i + 1).trim(); }
  function slotLabel(slot) { return slot === 'morning' ? 'morning' : 'afternoon'; }
  function article(type) { return /^[aeiou]/i.test(type) ? 'an ' + type.toLowerCase() : 'a ' + type.toLowerCase(); }

  function parseDate(text) {
    var t = String(text || '').trim();
    var m = t.match(/^(\d{4})-(\d{2})-(\d{2})$/);
    if (m) return validIso(+m[1], +m[2], +m[3]);
    m = t.match(/^(\d{1,2})[-/.](\d{1,2})[-/.](\d{4})$/);
    if (m) return validIso(+m[3], +m[2], +m[1]);
    m = t.match(/^(?:[A-Za-z]{2,}\.?,?\s+)?(\d{1,2})\s+([A-Za-z]{3,})\.?(?:\s+(\d{4}))?$/);
    if (m) {
      var mi = MONTHS.map(function (x) { return x.toLowerCase(); }).indexOf(m[2].slice(0, 3).toLowerCase());
      if (mi < 0) return null;
      return validIso(m[3] ? +m[3] : toDate(state.today).getUTCFullYear(), mi + 1, +m[1]);
    }
    return null;
  }
  function validIso(y, mo, d) {
    var dt = new Date(Date.UTC(y, mo - 1, d));
    if (dt.getUTCFullYear() !== y || dt.getUTCMonth() !== mo - 1 || dt.getUTCDate() !== d) return null;
    return toIso(dt);
  }

  // fetch with a timeout. Resolves {ok, status, data}. Rejects {network: true} on a network error or timeout.
  function api(method, url, body) {
    var ctrl = new AbortController();
    var timer = setTimeout(function () { ctrl.abort(); }, TIMEOUT_MS);
    var opts = { method: method, signal: ctrl.signal, headers: {} };
    if (body !== undefined) { opts.headers['Content-Type'] = 'application/json'; opts.body = JSON.stringify(body); }
    return fetch(url, opts).then(function (res) {
      clearTimeout(timer);
      return res.text().then(function (txt) {
        var data = null;
        try { data = txt ? JSON.parse(txt) : null; } catch (e) { data = null; }
        return { ok: res.ok, status: res.status, data: data };
      });
    }, function () { clearTimeout(timer); return Promise.reject({ network: true }); });
  }

  // ---------- State ----------
  var state = {
    view: 'unplanned',
    load: 'loading',          // loading | ok | error
    rows: [],
    region: 'Central',
    today: '2026-10-26',
    nextWorkDay: '2026-10-27',
    updatedAt: '',
    seq: 0,
    filters: { type: [], skill: [], due: 'any' },
    selected: [],             // ids
    lastClicked: null,
    panel: null,
    bulk: null,               // {html}
    undoError: null,          // {assignment}
    stale: 0,
    offline: !navigator.onLine,
    toast: null,
    loadToken: 0
  };

  // ---------- Data access ----------
  function sortRows(rows) {
    return rows.slice().sort(function (a, b) {
      if (a.due !== b.due) return a.due < b.due ? -1 : 1;
      var ar = a.type === 'Repair' ? 0 : 1, br = b.type === 'Repair' ? 0 : 1;
      if (ar !== br) return ar - br;
      return a.id < b.id ? -1 : 1;
    });
  }
  function filtersActive() { var f = state.filters; return f.type.length > 0 || f.skill.length > 0 || f.due !== 'any'; }
  function visibleRows() {
    var f = state.filters, today = state.today, limit2 = addWorkdays(today, 2);
    return sortRows(state.rows).filter(function (w) {
      if (f.type.length && f.type.indexOf(w.type) < 0) return false;
      if (f.skill.length && f.skill.indexOf(w.skill) < 0) return false;
      if (f.due === 'overdue' && !(w.due < today)) return false;
      if (f.due === 'today' && !(w.due <= today)) return false;
      if (f.due === '2days' && !(w.due <= limit2)) return false;
      return true;
    });
  }
  function rowById(id) { return state.rows.filter(function (w) { return w.id === id; })[0]; }
  function selectedRows() { return sortRows(state.selected.map(rowById).filter(Boolean)); }
  function isSelected(id) { return state.selected.indexOf(id) >= 0; }
  function isUnplannedView() { return state.view === 'unplanned'; }

  function loadList(opts) {
    opts = opts || {};
    var token = ++state.loadToken;
    state.load = 'loading';
    state.stale = 0;
    if (!opts.keepSelection) { state.selected = []; closePanel(false); }
    renderAll();
    api('GET', state.view === 'unplanned' ? '/api/work-orders' : '/api/work-orders?status=' + encodeURIComponent(state.view)).then(function (r) {
      if (token !== state.loadToken) return;
      if (!r.ok || !r.data || !Array.isArray(r.data.workOrders)) { state.load = 'error'; renderAll(); return; }
      var d = r.data;
      state.rows = d.workOrders;
      state.region = d.region || state.region;
      state.today = d.today || state.today;
      state.nextWorkDay = d.nextWorkDay || state.nextWorkDay;
      state.updatedAt = d.updatedAt || '';
      state.seq = d.seq || 0;
      state.load = 'ok';
      pruneSelection();
      renderAll();
    }, function () {
      if (token !== state.loadToken) return;
      state.load = 'error';
      renderAll();
    });
  }

  function pruneSelection() {
    var visible = visibleRows().map(function (w) { return w.id; });
    state.selected = state.selected.filter(function (id) { return visible.indexOf(id) >= 0; });
    if (!state.selected.length && state.panel) closePanel(false);
  }

  // ---------- Rendering: header, chips, filters ----------
  function renderHeader() {
    var meta = $('#meta');
    meta.textContent = 'Region ' + state.region + (state.updatedAt ? ' · Updated ' + state.updatedAt : '');
    var btnNew = $('#btn-new');
    btnNew.className = 'fl-btn' + (state.selected.length ? '' : ' fl-btn--primary');
  }

  function renderStatusChips() {
    var el = $('#status-chips');
    el.innerHTML = VIEWS.map(function (v) {
      return '<button type="button" class="fl-chip" data-view="' + v[0] + '" data-testid="status-' + v[0] + '" aria-pressed="' + (state.view === v[0]) + '">' + esc(v[1]) + '</button>';
    }).join('');
  }

  function renderFilters() {
    var f = state.filters, el = $('#filters');
    var focusId = document.activeElement && el.contains(document.activeElement) ? document.activeElement.id : null;
    function chip(group, value, label, pressed) {
      var id = 'filter-' + group + '-' + slug(value);
      return '<button type="button" class="fl-chip" id="' + id + '" data-testid="' + id + '" data-group="' + group + '" data-value="' + esc(value) + '" aria-pressed="' + pressed + '">' + esc(label) + '</button>';
    }
    var html = '';
    html += '<span class="fl-label app-group-label" id="grp-type">Type</span>' + TYPES.map(function (t) { return chip('type', t, t, f.type.indexOf(t) >= 0); }).join('');
    html += '<span class="app-sep"></span>';
    html += '<span class="fl-label app-group-label" id="grp-skill">Skill needed</span>' + SKILLS.map(function (s) { return chip('skill', s, s, f.skill.indexOf(s) >= 0); }).join('');
    html += '<span class="app-sep"></span>';
    html += '<span class="fl-label app-group-label" id="grp-due">Due</span>' + DUES.map(function (d) { return chip('due', d[0], d[1], f.due === d[0]); }).join('');
    html += '<span class="fl-result-count" role="status" id="result-count" data-testid="result-count">' + esc(resultCountText()) + '</span>';
    if (filtersActive()) html += '<button type="button" class="fl-btn fl-btn--ghost" id="clear-filters" data-testid="clear-filters">Clear filters</button>';
    el.innerHTML = html;
    if (focusId && document.getElementById(focusId)) document.getElementById(focusId).focus();
  }

  function resultCountText() {
    if (state.load === 'loading') return 'Loading work orders';
    if (state.load === 'error') return '';
    var n = visibleRows().length;
    if (isUnplannedView()) return n + ' unplanned ' + plural(n, 'work order', 'work orders');
    return woCount(n);
  }

  // ---------- Rendering: work area ----------
  function ensureWorkArea() {
    var wa = $('#work-area');
    if (wa.firstChild) return;
    wa.innerHTML =
      '<div id="split">' +
        '<div id="list-col">' +
          '<div id="alerts"></div>' +
          '<div class="fl-bulkbar" role="region" aria-label="Selection" id="bulkbar" data-testid="bulkbar" hidden>' +
            '<strong role="status" id="bulk-count" data-testid="bulk-count"></strong>' +
            '<button type="button" class="fl-btn fl-btn--primary" id="assign-open" data-testid="assign-open">Assign to technician</button>' +
            '<button type="button" class="fl-btn fl-btn--ghost" id="clear-selection" data-testid="clear-selection">Clear selection</button>' +
          '</div>' +
          '<div id="list"></div>' +
          '<div class="fl-result-count app-pager" id="pager"></div>' +
        '</div>' +
        '<aside class="fl-sidepanel" id="assign-panel" data-testid="assign-panel" aria-labelledby="ap-title" hidden></aside>' +
      '</div>';
  }

  function renderAll() {
    ensureWorkArea();
    renderHeader();
    renderStatusChips();
    renderFilters();
    renderAlerts();
    renderBulkbar();
    renderList();
    renderPanel();
  }

  function renderAlerts() {
    var el = $('#alerts'), html = '';
    if (state.offline) {
      html += '<div class="fl-alert fl-alert--warning app-gap" role="status" id="offline-alert" data-testid="offline-alert">No connection. You can look at the list. You cannot assign until the connection is back.</div>';
    }
    if (state.stale > 0 && state.load === 'ok') {
      html += '<div class="fl-alert fl-alert--info app-gap app-center" role="status" data-testid="stale-alert"><div class="app-alert-body">' +
        state.stale + ' new unplanned ' + plural(state.stale, 'work order', 'work orders') + ' since ' + esc(state.updatedAt) + '.</div>' +
        '<button type="button" class="fl-btn" id="refresh-list" data-testid="refresh-list">Refresh list</button></div>';
    }
    if (state.undoError) {
      var a = state.undoError;
      html += '<div class="fl-alert fl-alert--danger app-gap app-center" role="alert" data-testid="error-undo"><div class="app-alert-body">The assignment was not undone. The server did not answer. <strong>' +
        esc(joinList(a.assigned)) + '</strong> ' + plural(a.assigned.length, 'is', 'are') + ' still planned with ' + esc(a.techName) + ', ' + esc(fmtDate(a.date)) + ', ' + slotLabel(a.slot) + '.</div>' +
        '<button type="button" class="fl-btn" id="undo-again" data-testid="undo-again">Undo again</button>' +
        '<button type="button" class="fl-btn fl-btn--ghost" id="undo-error-close" data-testid="undo-error-close">Close</button></div>';
    }
    if (state.bulk) {
      html += '<div class="fl-alert fl-alert--warning app-gap" role="status" tabindex="-1" id="result-alert" data-testid="result-alert"><div class="app-alert-body">' + state.bulk.html + '</div>' +
        '<button type="button" class="fl-btn fl-btn--ghost" id="result-close" data-testid="result-close">Close</button></div>';
    }
    if (el.innerHTML !== html) el.innerHTML = html;
  }

  function renderBulkbar() {
    var bar = $('#bulkbar'), n = state.selected.length;
    var show = n > 0 && state.load === 'ok' && isUnplannedView();
    bar.hidden = !show;
    if (!show) return;
    $('#bulk-count').textContent = n + ' ' + plural(n, 'work order', 'work orders') + ' selected';
    var btn = $('#assign-open');
    btn.className = 'fl-btn' + (state.panel ? '' : ' fl-btn--primary');
    btn.disabled = state.offline;
    if (state.offline) btn.setAttribute('aria-describedby', 'offline-alert'); else btn.removeAttribute('aria-describedby');
  }

  function renderList() {
    var list = $('#list'), pager = $('#pager');
    var split = $('#split');
    split.className = state.panel ? 'app-split' : '';
    var unplanned = isUnplannedView();
    var tableLabel = unplanned ? 'Unplanned work orders, region ' + state.region + ', earliest due first' : 'Work orders, region ' + state.region + ', earliest due first';

    if (state.load === 'loading') {
      var cols = unplanned ? 7 : 7, rows = '';
      for (var i = 0; i < 8; i++) { rows += '<tr>' + new Array(cols + 1).join('<td><div class="fl-skeleton"></div></td>') + '</tr>'; }
      list.innerHTML = '<div class="fl-table-wrap" data-testid="list-loading"><table class="fl-table" aria-busy="true" aria-label="' + esc(unplanned ? 'Unplanned work orders' : 'Work orders') + '"><thead>' + headRow(unplanned, 0, true) + '</thead><tbody>' + rows + '</tbody></table></div>';
      pager.textContent = '';
      return;
    }
    if (state.load === 'error') {
      list.innerHTML = '<div class="fl-alert fl-alert--danger app-center" role="alert" data-testid="error-load"><div class="app-alert-body">The work orders did not load. The server did not answer.</div><button type="button" class="fl-btn" id="retry-load" data-testid="retry-load">Try again</button></div>';
      pager.textContent = '';
      return;
    }
    var vis = visibleRows();
    if (!vis.length) {
      if (filtersActive()) {
        list.innerHTML = '<div class="fl-table-wrap"><div class="fl-empty" data-testid="empty"><p class="fl-empty__title">No unplanned work orders match these filters.</p><p>Active filters: ' + esc(activeFiltersText()) + '.</p><button type="button" class="fl-btn" id="empty-clear" data-testid="empty-clear-filters">Clear filters</button></div></div>';
      } else if (unplanned) {
        list.innerHTML = '<div class="fl-table-wrap"><div class="fl-empty" data-testid="empty-all-done"><p class="fl-empty__title">All work orders in region ' + esc(state.region) + ' are planned.</p><button type="button" class="fl-btn" id="show-all-open" data-testid="show-all-open">Show all open work orders</button></div></div>';
      } else {
        list.innerHTML = '<div class="fl-table-wrap"><div class="fl-empty" data-testid="empty"><p class="fl-empty__title">No work orders.</p></div></div>';
      }
      pager.textContent = '';
      return;
    }
    var body = vis.map(function (w) { return unplanned ? unplannedRow(w) : openRow(w); }).join('');
    list.innerHTML = '<div class="fl-table-wrap"><table class="fl-table" id="wo-table" data-testid="wo-table" aria-label="' + esc(tableLabel) + '"><thead>' + headRow(unplanned, vis.length, false) + '</thead><tbody>' + body + '</tbody></table></div>';
    pager.textContent = '1 to ' + vis.length + ' of ' + vis.length + ' · 100 per page';
    syncHeaderCheckbox();
  }

  function headRow(unplanned, n, loading) {
    if (!unplanned) {
      return '<tr><th scope="col">Work order</th><th scope="col">Type</th><th scope="col">Customer</th><th scope="col">Address</th><th scope="col">Status</th><th scope="col">Technician</th><th scope="col" class="fl-num"' + (loading ? '' : ' aria-sort="ascending"') + '>Due</th></tr>';
    }
    var all = loading
      ? '<input type="checkbox" aria-label="Select all on this page" disabled>'
      : '<input type="checkbox" id="select-all" data-testid="select-all" aria-label="Select all ' + n + ' on this page">';
    var cust = (state.panel && !loading) ? '<th scope="col">Customer and address</th>' : '<th scope="col">Customer</th><th scope="col">Address</th>';
    return '<tr><th scope="col">' + all + '</th><th scope="col">Work order</th><th scope="col">Type</th>' + cust + '<th scope="col">Skill needed</th><th scope="col" class="fl-num"' + (loading ? '' : ' aria-sort="ascending"') + '>Due</th></tr>';
  }

  function dueCell(w) {
    var badge = w.due < state.today ? '<span class="fl-badge fl-badge--danger">Overdue</span> ' : '';
    return '<td class="fl-num">' + badge + '<span class="app-nowrap">' + esc(fmtDay(w.due)) + '</span></td>';
  }

  function unplannedRow(w) {
    var sel = isSelected(w.id);
    var cust = state.panel
      ? '<td>' + esc(w.customer) + '<br><span class="app-small app-muted">' + esc(w.address) + '</span></td>'
      : '<td>' + esc(w.customer) + '</td><td>' + esc(w.address) + '</td>';
    return '<tr data-testid="wo-row" data-wo="' + w.id + '"' + (sel ? ' aria-selected="true"' : '') + '>' +
      '<td><input type="checkbox" id="chk-' + w.id + '" data-testid="wo-check" data-wo="' + w.id + '" aria-label="Select ' + w.id + '"' + (sel ? ' checked' : '') + '></td>' +
      '<td><a href="#' + w.id + '" class="app-link"' + (w.due < state.today ? ' tabindex="1"' : '') + '>' + w.id + '</a></td>' +
      '<td>' + esc(w.type) + '</td>' + cust + '<td>' + esc(w.skill) + '</td>' + dueCell(w) + '</tr>';
  }

  function openRow(w) {
    return '<tr data-testid="wo-row" data-wo="' + w.id + '"><td><a href="#' + w.id + '" class="app-link">' + w.id + '</a></td><td>' + esc(w.type) + '</td><td>' + esc(w.customer) + '</td><td>' + esc(w.address) + '</td>' +
      '<td><span class="fl-badge fl-badge--' + (STATUS_BADGE[w.status] || 'neutral') + '">' + esc(w.status) + '</span></td><td>' + esc(w.technician || '-') + '</td>' + dueCell(w) + '</tr>';
  }

  function activeFiltersText() {
    var f = state.filters, parts = [];
    if (f.type.length) parts.push('Type ' + f.type.join(' or '));
    if (f.skill.length) parts.push('Skill needed ' + f.skill.join(' or '));
    if (f.due !== 'any') parts.push('Due ' + DUES.filter(function (d) { return d[0] === f.due; })[0][1]);
    return parts.join(', ');
  }

  function syncRowsSelection() {
    var rows = document.querySelectorAll('#wo-table tbody tr');
    for (var i = 0; i < rows.length; i++) {
      var id = rows[i].getAttribute('data-wo'), sel = isSelected(id);
      if (sel) rows[i].setAttribute('aria-selected', 'true'); else rows[i].removeAttribute('aria-selected');
      var cb = rows[i].querySelector('input[type=checkbox]');
      if (cb) cb.checked = sel;
    }
    syncHeaderCheckbox();
  }

  function syncHeaderCheckbox() {
    var all = $('#select-all');
    if (!all) return;
    var vis = visibleRows(), n = vis.filter(function (w) { return isSelected(w.id); }).length;
    all.checked = n > 0 && n === vis.length;
    all.indeterminate = n > 0 && n < vis.length;
  }

  // ---------- Selection ----------
  function setSelection(ids) {
    state.selected = ids;
    renderHeader();
    renderBulkbar();
    syncRowsSelection();
    if (state.panel) {
      if (!ids.length) closePanel(true); else panelSelectionChanged();
    }
  }

  function onRowCheck(cb, shift) {
    var id = cb.getAttribute('data-wo');
    var vis = visibleRows().map(function (w) { return w.id; });
    var sel = state.selected.slice();
    var want = cb.checked;
    if (shift && state.lastClicked && vis.indexOf(state.lastClicked) >= 0) {
      var a = vis.indexOf(state.lastClicked), b = vis.indexOf(id);
      var range = vis.slice(Math.min(a, b), Math.max(a, b) + 1);
      range.forEach(function (rid) {
        var i = sel.indexOf(rid);
        if (want && i < 0) sel.push(rid);
        if (!want && i >= 0) sel.splice(i, 1);
      });
    } else {
      var i = sel.indexOf(id);
      if (want && i < 0) sel.push(id);
      if (!want && i >= 0) sel.splice(i, 1);
    }
    state.lastClicked = id;
    setSelection(sel);
  }

  // ---------- Assign panel ----------
  function neededSkills() {
    var s = [];
    selectedRows().forEach(function (w) { if (s.indexOf(w.skill) < 0) s.push(w.skill); });
    return SKILLS.filter(function (x) { return s.indexOf(x) >= 0; });
  }

  function openPanel() {
    if (!state.selected.length || state.offline) return;
    state.bulk = null;
    state.panel = {
      dateIso: state.nextWorkDay, dateText: fmtDateLong(state.nextWorkDay), dateError: false,
      tech: 'loading', techs: [], now: '', choice: null, save: 'idle', conflict: null, expanded: false,
      skillsKey: ''
    };
    renderAlerts();
    renderBulkbar();
    renderList();
    renderPanel();
    loadTechs();
    var t = $('#ap-title');
    if (t) t.focus();
  }

  function closePanel(returnFocus) {
    if (!state.panel) return;
    state.panel = null;
    var aside = $('#assign-panel');
    if (aside) { aside.hidden = true; aside.innerHTML = ''; }
    if ($('#work-area').firstChild) { renderBulkbar(); renderList(); }
    if (returnFocus) {
      var b = $('#assign-open');
      if (b && !$('#bulkbar').hidden) b.focus();
      else focusFirstRow();
    }
  }

  function focusFirstRow() {
    var cb = document.querySelector('#wo-table tbody input[type=checkbox]') || document.querySelector('#wo-table tbody a');
    if (cb) cb.focus();
  }

  function panelSelectionChanged() {
    var p = state.panel;
    p.conflict = null;
    p.save = 'idle';
    if (neededSkills().join(',') !== p.skillsKey) { p.choice = null; renderPanel(); loadTechs(); }
    else renderPanel();
  }

  function loadTechs(silent) {
    var p = state.panel;
    if (!p) return;
    var skills = neededSkills();
    p.skillsKey = skills.join(',');
    var token = p.techToken = (p.techToken || 0) + 1;
    if (!silent) { p.tech = 'loading'; renderPanelBody(); renderPanelFoot(); }
    var url = '/api/technicians?skills=' + encodeURIComponent(skills.join(',')) + '&date=' + encodeURIComponent(p.dateIso);
    api('GET', url).then(function (r) {
      if (state.panel !== p || token !== p.techToken) return;
      if (!r.ok || !r.data || !Array.isArray(r.data.technicians)) { if (!silent) { p.tech = 'error'; renderPanelBody(); renderPanelFoot(); } return; }
      var oldFree = p.choice ? freeOf(p.choice.techId, p.choice.slot) : null;
      var same = JSON.stringify(p.techs) === JSON.stringify(r.data.technicians);
      p.techs = r.data.technicians;
      p.now = r.data.now || '';
      p.tech = 'ok';
      if (silent && same) return; // nothing changed
      if (p.choice) {
        var ch = p.choice, t = techOf(ch.techId), nowFree = freeOf(ch.techId, ch.slot);
        if (silent && nowFree !== oldFree && t) p.conflict = { time: p.now, techName: t.name, slot: ch.slot, free: nowFree };
        if (!nowFree || !isEligible(t)) p.choice = null;
      }
      renderPanelBody();
      renderPanelFoot();
    }, function () {
      if (state.panel !== p || token !== p.techToken || silent) return;
      p.tech = 'error';
      renderPanelBody();
      renderPanelFoot();
    });
  }

  function techOf(id) { var p = state.panel; return p ? p.techs.filter(function (t) { return t.id === id; })[0] : null; }
  function freeOf(id, slot) { var t = techOf(id); return t && t.free ? (t.free[slot] || 0) : 0; }
  function isEligible(t) { return t && t.hasSkills && ((t.free.morning || 0) > 0 || (t.free.afternoon || 0) > 0); }

  function skillPhrase(skills) {
    return skills.length === 1 ? 'the skill ' + skills[0] : 'the skills ' + joinList(skills);
  }
  function withoutPhrase(skills) {
    if (skills.length === 1) return 'without the skill ' + skills[0];
    if (skills.length === 2) return 'without both skills';
    return 'without all ' + skills.length + ' skills';
  }

  function renderPanel() {
    var aside = $('#assign-panel');
    if (!aside) return;
    var p = state.panel;
    if (!p) { aside.hidden = true; aside.innerHTML = ''; return; }
    aside.hidden = false;
    if (!$('#ap-head', aside)) {
      aside.innerHTML =
        '<div class="app-panel-head" id="ap-head"><h2 class="fl-section-title" id="ap-title" tabindex="-1"></h2>' +
        '<button type="button" class="fl-btn fl-btn--ghost" id="assign-close" data-testid="assign-close">Close</button></div>' +
        '<ul class="app-sel-list" id="ap-sel" data-testid="assign-selected"></ul>' +
        '<p class="app-small app-muted" id="ap-skills"></p>' +
        '<div class="fl-field" id="ap-date-field">' +
          '<label class="fl-label" for="ap-date">Date</label>' +
          '<div class="app-row"><input class="fl-input" id="ap-date" data-testid="assign-date" autocomplete="off" aria-describedby="ap-date-hint">' +
          '<button type="button" class="fl-btn" id="ap-pick" data-testid="pick-date">Pick date</button>' +
          '<input type="date" class="app-date-native" id="ap-date-native" tabindex="-1" aria-hidden="true"></div>' +
          '<span class="fl-hint" id="ap-date-hint">Morning 08:00 to 12:00, afternoon 12:30 to 17:00</span>' +
          '<span class="fl-error-text" id="ap-date-error" hidden>Type a date like Tue 27 Oct 2026.</span>' +
        '</div>' +
        '<div id="ap-body"></div>' +
        '<div class="app-panel-foot" id="ap-foot"></div>';
    }
    var rows = selectedRows(), n = rows.length;
    $('#ap-title').textContent = 'Assign ' + woCount(n);
    var shown = p.expanded || n <= 3 ? rows : rows.slice(0, 3);
    $('#ap-sel').innerHTML = shown.map(function (w) {
      return '<li>' + esc(w.id) + ' · ' + esc(w.type) + ' · ' + esc(town(w.address)) + ' · due ' + esc(fmtDay(w.due)) + '</li>';
    }).join('') + (shown.length < n ? '<li><button type="button" class="fl-btn fl-btn--ghost app-inline" id="ap-show-all" data-testid="show-all-selected">Show all, ' + (n - shown.length) + ' more</button></li>' : '');
    $('#ap-skills').textContent = 'Skills needed: ' + neededSkills().join(', ');
    var input = $('#ap-date');
    if (document.activeElement !== input) input.value = p.dateText;
    $('#ap-date-field').className = 'fl-field' + (p.dateError ? ' fl-field--error' : '');
    $('#ap-date-error').hidden = !p.dateError;
    input.setAttribute('aria-invalid', p.dateError ? 'true' : 'false');
    input.setAttribute('aria-describedby', p.dateError ? 'ap-date-hint ap-date-error' : 'ap-date-hint');
    renderPanelBody();
    renderPanelFoot();
  }

  function renderPanelBody() {
    var p = state.panel, body = $('#ap-body');
    if (!p || !body) return;
    var focused = document.activeElement && body.contains(document.activeElement) ? document.activeElement : null;
    var focusKey = focused ? (focused.getAttribute('data-testid') || '') + '|' + (focused.getAttribute('data-tech') || '') + '|' + (focused.getAttribute('data-slot') || '') : null;
    var skills = neededSkills(), dateTxt = fmtDate(p.dateIso), html = '';
    if (p.conflict) {
      html += '<div class="fl-alert fl-alert--warning app-gap" role="status" data-testid="conflict-alert">The free places changed at ' + esc(p.conflict.time) + '. ' +
        esc(p.conflict.techName) + ', ' + slotLabel(p.conflict.slot) + ' now has ' + p.conflict.free + ' free ' + plural(p.conflict.free, 'place', 'places') + '.</div>';
    }
    if (p.tech === 'loading') {
      var sk = '';
      for (var i = 0; i < 4; i++) sk += '<tr><td><div class="fl-skeleton"></div></td><td><div class="fl-skeleton"></div></td><td><div class="fl-skeleton"></div></td><td><div class="fl-skeleton"></div></td></tr>';
      html += '<div class="fl-table-wrap" data-testid="tech-loading"><table class="fl-table" aria-busy="true" aria-label="Technician and slot"><thead><tr><th scope="col">Technician</th><th scope="col">Skills</th><th scope="col">Morning</th><th scope="col">Afternoon</th></tr></thead><tbody>' + sk + '</tbody></table></div>' +
        '<p class="app-small app-muted" role="status">Loading the free places of the technicians.</p>';
    } else if (p.tech === 'error') {
      html += '<div class="fl-alert fl-alert--danger app-center" role="alert" data-testid="techs-error"><div class="app-alert-body">The free places of the technicians did not load.</div><button type="button" class="fl-btn" id="retry-techs" data-testid="retry-techs">Try again</button></div>';
    } else {
      var eligible = p.techs.filter(isEligible);
      var without = p.techs.filter(function (t) { return !t.hasSkills; }).length;
      var noFree = p.techs.filter(function (t) { return t.hasSkills && !isEligible(t); }).length;
      var notShownParts = [];
      if (without) notShownParts.push(without + ' ' + plural(without, 'technician', 'technicians') + ' ' + withoutPhrase(skills));
      if (noFree) notShownParts.push(noFree + ' ' + plural(noFree, 'technician', 'technicians') + ' with no free place');
      if (!eligible.length) {
        html += '<div class="fl-empty app-empty-sm" data-testid="no-technician"><p class="fl-empty__title">No technician with ' + esc(skillPhrase(skills)) + ' has a free place on ' + esc(dateTxt) + '.</p>' +
          (notShownParts.length ? '<p data-testid="not-shown">Not shown: ' + esc(notShownParts.join(', ')) + '.</p>' : '') +
          '<button type="button" class="fl-btn" id="next-work-day" data-testid="next-work-day">Next work day</button></div>';
      } else {
        html += '<div class="fl-table-wrap" role="radiogroup" aria-label="Technician and slot" data-testid="tech-list"><table class="fl-table"><thead><tr><th scope="col">Technician</th><th scope="col">Skills</th><th scope="col">Morning</th><th scope="col">Afternoon</th></tr></thead><tbody>' +
          eligible.map(function (t) {
            var rowSel = p.choice && p.choice.techId === t.id;
            return '<tr data-testid="tech-row" data-tech="' + t.id + '"' + (rowSel ? ' aria-selected="true"' : '') + '><td>' + esc(t.name) + '</td><td class="app-small">' + esc(t.skills.join(', ')) + '</td>' +
              ['morning', 'afternoon'].map(function (slot) {
                var free = t.free[slot] || 0;
                if (!free) return '<td class="app-muted" data-testid="slot-full" data-tech="' + t.id + '" data-slot="' + slot + '">Full</td>';
                var checked = p.choice && p.choice.techId === t.id && p.choice.slot === slot;
                return '<td><label class="app-radio"><input type="radio" name="tech-slot" data-testid="slot-radio" data-tech="' + t.id + '" data-slot="' + slot + '"' + (checked ? ' checked' : '') +
                  ' aria-label="' + esc(t.name + ', ' + slot + ', ' + free + ' free') + '"> ' + free + ' free</label></td>';
              }).join('') + '</tr>';
          }).join('') + '</tbody></table></div>' +
          (notShownParts.length ? '<p class="app-small app-muted" data-testid="not-shown">Not shown: ' + esc(notShownParts.join(', ')) + ' on ' + esc(dateTxt) + '.</p>' : '');
      }
    }
    body.innerHTML = html;
    if (focusKey) {
      var parts = focusKey.split('|');
      var sel = parts[0] ? '[data-testid="' + parts[0] + '"]' + (parts[1] ? '[data-tech="' + parts[1] + '"]' : '') + (parts[2] ? '[data-slot="' + parts[2] + '"]' : '') : null;
      var again = sel && body.querySelector(sel);
      if (again) again.focus(); else if ($('#ap-date')) $('#ap-date').focus();
    }
  }

  function plan() {
    var p = state.panel, rows = selectedRows();
    if (!p || !p.choice) return null;
    var free = freeOf(p.choice.techId, p.choice.slot);
    var take = Math.min(free, rows.length);
    return { rows: rows, assign: rows.slice(0, take), rest: rows.slice(take), tech: techOf(p.choice.techId), slot: p.choice.slot, free: free };
  }

  function renderPanelFoot() {
    var p = state.panel, foot = $('#ap-foot');
    if (!p || !foot) return;
    var focusedId = document.activeElement && foot.contains(document.activeElement) ? document.activeElement.id : null;
    var html = '', pl = plan(), skills = neededSkills();
    var offlineDesc = state.offline ? ' aria-describedby="offline-alert"' : '';
    if (p.save === 'error' && pl) {
      html += '<div class="fl-alert fl-alert--danger" role="alert" data-testid="error-save">The work orders were not assigned. The server did not answer. Nothing changed.</div>' +
        '<div class="app-actions"><button type="button" class="fl-btn fl-btn--primary" id="retry-save" data-testid="retry-save"' + (state.offline ? ' disabled' + offlineDesc : '') + '>Try again</button>' +
        '<button type="button" class="fl-btn" id="assign-cancel" data-testid="assign-cancel">Cancel</button></div>';
    } else if (pl && p.tech === 'ok' && pl.assign.length) {
      var dateTxt = fmtDate(p.dateIso), techName = pl.tech.name;
      var ids = pl.assign.map(function (w) { return w.id; });
      var restIds = pl.rest.map(function (w) { return w.id; });
      html += '<p class="app-summary" role="status" data-testid="assign-summary"><strong>' + esc(joinList(ids)) + '</strong> ' + plural(ids.length, 'gets', 'get') + ' a visit with ' + esc(techName) + ', ' + esc(dateTxt) + ', ' + slotLabel(pl.slot) + '.<br>' +
        (restIds.length ? '<strong>' + esc(joinList(restIds)) + ' ' + plural(restIds.length, 'stays', 'stay') + ' unplanned:</strong> no free place left. ' : '') +
        'You can undo for 8 seconds.</p>';
      pl.assign.forEach(function (w) {
        if (w.due < p.dateIso) {
          html += '<div class="fl-alert fl-alert--warning" data-testid="due-warning">' + esc(w.id) + ' is ' + esc(article(w.type)) + ' due ' + esc(fmtDate(w.due)) + '. This date is after the due date.</div>';
        }
      });
      var busy = p.save === 'saving';
      html += '<div class="app-actions"><button type="button" class="fl-btn fl-btn--primary" id="assign-confirm" data-testid="assign-confirm"' + (state.offline || busy ? ' disabled' : '') + offlineDesc + (busy ? ' aria-busy="true"' : '') + '>Assign ' + esc(woCount(ids.length)) + '</button>' +
        '<button type="button" class="fl-btn" id="assign-cancel" data-testid="assign-cancel">Cancel</button></div>';
    } else {
      var hint = 'Select a technician and a slot.';
      if (p.tech === 'error') hint = 'You can assign when the free places load.';
      else if (p.tech === 'ok' && !p.techs.filter(isEligible).length) hint = skills.length > 1 ? 'Select another date, or select work orders that need one skill.' : 'Select another date.';
      html += '<p class="app-small app-muted" id="ap-hint" data-testid="assign-hint">' + esc(hint) + '</p>' +
        '<div class="app-actions"><button type="button" class="fl-btn fl-btn--primary" id="assign-confirm" data-testid="assign-confirm" disabled aria-describedby="ap-hint">Assign</button>' +
        '<button type="button" class="fl-btn" id="assign-cancel" data-testid="assign-cancel">Cancel</button></div>';
    }
    foot.innerHTML = html;
    if (focusedId && document.getElementById(focusedId) && !document.getElementById(focusedId).disabled) document.getElementById(focusedId).focus();
  }

  function setDate(iso) {
    var p = state.panel;
    p.dateError = false;
    p.conflict = null;
    p.save = 'idle';
    if (iso === p.dateIso) { p.dateText = fmtDateLong(iso); renderPanel(); return; }
    p.dateIso = iso;
    p.dateText = fmtDateLong(iso);
    p.choice = null;
    renderPanel();
    loadTechs();
  }

  function commitDateText() {
    var p = state.panel, input = $('#ap-date');
    if (!p || !input) return;
    var iso = parseDate(input.value);
    if (!iso) { p.dateError = true; p.dateText = input.value; renderPanel(); return; }
    input.value = fmtDateLong(iso);
    setDate(iso);
  }

  // ---------- Assign, result, undo ----------
  function doAssign() {
    var p = state.panel, pl = plan();
    if (!p || !pl || state.offline || p.save === 'saving') return;
    p.save = 'saving';
    p.conflict = null;
    renderPanelBody();
    renderPanelFoot();
    var payload = { workOrderIds: pl.rows.map(function (w) { return w.id; }), technicianId: pl.tech.id, date: p.dateIso, slot: pl.slot, expectedFree: pl.free };
    api('POST', '/api/assignments', payload).then(function (r) {
      if (state.panel !== p) return;
      if (r.status === 409 && r.data) {
        p.save = 'idle';
        var t = techOf(pl.tech.id);
        if (t && typeof r.data.free === 'number') t.free[pl.slot] = r.data.free;
        p.conflict = { time: r.data.changedAt || p.now, techName: pl.tech.name, slot: pl.slot, free: r.data.free };
        if (!freeOf(pl.tech.id, pl.slot)) p.choice = null;
        renderPanelBody();
        renderPanelFoot();
        var ca = $('[data-testid=assign-confirm]');
        if (ca && !ca.disabled) ca.focus();
        return;
      }
      if (!r.ok || !r.data || !Array.isArray(r.data.assigned)) { saveFailed(p); return; }
      handleResult(pl, payload, r.data);
    }, function () {
      if (state.panel !== p) return;
      saveFailed(p);
      checkHealth();
    });
  }

  function saveFailed(p) {
    p.save = 'error';
    renderPanelBody();
    renderPanelFoot();
    var b = $('#retry-save');
    if (b) b.focus();
  }

  function handleResult(pl, payload, data) {
    var assigned = data.assigned, failed = data.failed || [];
    var removed = state.rows.filter(function (w) { return assigned.indexOf(w.id) >= 0; });
    state.rows = state.rows.filter(function (w) { return assigned.indexOf(w.id) < 0; });
    var assignment = { id: data.assignmentId, assigned: assigned, rows: removed, techName: pl.tech.name, date: payload.date, slot: payload.slot };
    var failedIds = failed.map(function (f) { return f.id; });
    state.selected = state.selected.filter(function (id) { return failedIds.indexOf(id) >= 0; });
    state.undoError = null;
    var where = pl.tech.name + ', ' + fmtDate(payload.date) + ', ' + slotLabel(payload.slot);
    if (!failed.length) {
      state.bulk = null;
      state.panel = null;
      renderAll();
      if (assigned.length) showToast(assignment, woCount(assigned.length) + ' assigned to ' + where + '.');
      focusFirstRow();
      return;
    }
    var total = payload.workOrderIds.length;
    var lines = failed.map(function (f) {
      var w = rowById(f.id);
      return esc(f.id) + (w ? ' (' + esc(w.customer) + ', ' + esc(town(w.address)) + ')' : '') + ': ' + esc(f.reason);
    });
    state.bulk = { html: '<strong>' + assigned.length + ' of ' + total + ' ' + plural(total, 'work order', 'work orders') + ' assigned to ' + esc(where) + '. ' + failed.length + ' not assigned:</strong><br>' + lines.join('<br>') };
    var p = state.panel;
    if (p && state.selected.length) { p.save = 'idle'; p.choice = null; p.conflict = null; p.expanded = false; }
    else state.panel = null;
    renderAll();
    if (state.panel) loadTechs();
    if (assigned.length) showToast(assignment, woCount(assigned.length) + ' assigned to ' + where + '.');
    var al = $('#result-alert');
    if (al) al.focus();
  }

  function showToast(assignment, text) {
    hideToast();
    var region = $('#toast-region');
    var t = document.createElement('div');
    t.className = 'fl-toast';
    t.setAttribute('data-testid', 'toast');
    t.innerHTML = '<span>' + esc(text) + '</span><button type="button" class="fl-toast__action" id="toast-undo" data-testid="toast-undo">Undo</button>';
    region.appendChild(t);
    var toast = { el: t, assignment: assignment, remaining: UNDO_MS, started: Date.now(), timer: null, paused: 0 };
    state.toast = toast;
    function start() { toast.started = Date.now(); toast.timer = setTimeout(function () { if (state.toast === toast) hideToast(); }, toast.remaining); }
    function pause() { toast.paused++; if (toast.timer) { clearTimeout(toast.timer); toast.timer = null; toast.remaining -= Date.now() - toast.started; } }
    function resume() { toast.paused = Math.max(0, toast.paused - 1); if (!toast.paused && !toast.timer) start(); }
    t.addEventListener('mouseenter', pause);
    t.addEventListener('mouseleave', resume);
    t.addEventListener('focusin', pause);
    t.addEventListener('focusout', resume);
    t.querySelector('button').addEventListener('click', function () { doUndo(assignment, true); });
    start();
  }

  function hideToast() {
    var t = state.toast;
    if (!t) return;
    if (t.timer) clearTimeout(t.timer);
    var hadFocus = t.el.contains(document.activeElement);
    t.el.remove();
    state.toast = null;
    if (hadFocus) focusFirstRow();
  }

  function doUndo(assignment, fromToast) {
    if (!assignment || !assignment.id) return;
    var btn = fromToast ? $('#toast-undo') : $('#undo-again');
    if (btn) btn.disabled = true;
    api('POST', '/api/assignments/undo', { assignmentId: assignment.id }).then(function (r) {
      if (!r.ok) return undoFailed(assignment);
      if (state.toast && state.toast.assignment === assignment) { var t = state.toast; if (t.timer) clearTimeout(t.timer); t.el.remove(); state.toast = null; }
      state.undoError = null;
      state.bulk = null;
      var back = assignment.rows.filter(function (w) { return !rowById(w.id); });
      back.forEach(function (w) { state.rows.push(w); });
      if (isUnplannedView()) {
        var ids = assignment.assigned.slice();
        state.selected.forEach(function (id) { if (ids.indexOf(id) < 0) ids.push(id); });
        state.selected = ids;
      }
      var p = state.panel;
      if (p) { p.choice = null; p.conflict = null; p.save = 'idle'; }
      renderAll();
      if (p) loadTechs();
      var first = assignment.assigned[0] && document.getElementById('chk-' + assignment.assigned[0]);
      if (first) first.focus(); else focusFirstRow();
    }, function () { undoFailed(assignment); });
  }

  function undoFailed(assignment) {
    if (state.toast && state.toast.assignment === assignment) { var t = state.toast; if (t.timer) clearTimeout(t.timer); t.el.remove(); state.toast = null; }
    state.undoError = assignment;
    renderAlerts();
    var b = $('#undo-again');
    if (b) { b.disabled = false; b.focus(); }
  }

  // ---------- Connection and changes ----------
  function setOffline(off) {
    if (state.offline === off) return;
    state.offline = off;
    renderAlerts();
    renderBulkbar();
    renderPanelFoot();
  }
  function checkHealth() {
    return api('GET', '/api/health').then(function () { setOffline(!navigator.onLine); }, function () { setOffline(true); });
  }
  function checkChanges() {
    if (state.load !== 'ok' || !isUnplannedView() || state.offline) return;
    api('GET', '/api/work-orders/changes?since=' + state.seq).then(function (r) {
      if (!r.ok || !r.data || typeof r.data.newCount !== 'number') return;
      if (r.data.newCount !== state.stale) { state.stale = r.data.newCount; renderAlerts(); }
    }, function () { });
  }
  function poll() {
    checkHealth().then(function () {
      checkChanges();
      if (state.panel && state.panel.tech === 'ok' && state.panel.save !== 'saving' && !state.offline) loadTechs(true);
    });
  }

  // ---------- Events ----------
  document.addEventListener('click', function (e) {
    var t = e.target;
    var chip = t.closest('.fl-chip');
    if (chip && chip.hasAttribute('data-view')) {
      var v = chip.getAttribute('data-view');
      if (v !== state.view) { state.view = v; state.bulk = null; loadList(); }
      return;
    }
    if (chip && chip.hasAttribute('data-group')) {
      var g = chip.getAttribute('data-group'), val = chip.getAttribute('data-value'), f = state.filters;
      if (g === 'due') f.due = val;
      else { var arr = f[g], i = arr.indexOf(val); if (i >= 0) arr.splice(i, 1); else arr.push(val); }
      afterFilterChange();
      return;
    }
    var id = t.closest('button') ? t.closest('button').id : '';
    switch (id) {
      case 'clear-filters': case 'empty-clear':
        state.filters = { type: [], skill: [], due: 'any' };
        afterFilterChange();
        var anyChip = $('#filter-due-any'); if (anyChip) anyChip.focus();
        return;
      case 'retry-load': loadList(); return;
      case 'refresh-list': loadList({ keepSelection: true }); return;
      case 'show-all-open': state.view = 'open'; loadList(); return;
      case 'assign-open': openPanel(); return;
      case 'clear-selection': setSelection([]); focusFirstRow(); return;
      case 'assign-close': case 'assign-cancel': closePanel(true); return;
      case 'assign-confirm': case 'retry-save': doAssign(); return;
      case 'retry-techs': loadTechs(); return;
      case 'next-work-day': setDate(addWorkdays(state.panel.dateIso, 1)); return;
      case 'ap-show-all': state.panel.expanded = true; renderPanel(); var li = $('#ap-sel li:last-child'); if (li) li.setAttribute('tabindex', '-1'), li.focus(); return;
      case 'ap-pick': openPicker(); return;
      case 'undo-again': doUndo(state.undoError, false); return;
      case 'undo-error-close': state.undoError = null; renderAlerts(); focusFirstRow(); return;
      case 'result-close': state.bulk = null; renderAlerts(); focusFirstRow(); return;
    }
    if (t.matches && t.matches('#wo-table tbody input[type=checkbox]')) { onRowCheck(t, e.shiftKey); return; }
    if (t.id === 'select-all') {
      var vis = visibleRows().map(function (w) { return w.id; });
      setSelection(t.checked ? vis : []);
      return;
    }
    if (t.matches && t.matches('#wo-table a')) { e.preventDefault(); }
  });

  document.addEventListener('change', function (e) {
    var t = e.target;
    if (t.name === 'tech-slot' && state.panel) {
      state.panel.choice = { techId: t.getAttribute('data-tech'), slot: t.getAttribute('data-slot') };
      state.panel.save = 'idle';
      var rows = document.querySelectorAll('#ap-body tr[data-tech]');
      for (var i = 0; i < rows.length; i++) {
        if (rows[i].getAttribute('data-tech') === state.panel.choice.techId) rows[i].setAttribute('aria-selected', 'true'); else rows[i].removeAttribute('aria-selected');
      }
      renderPanelFoot();
      return;
    }
    if (t.id === 'ap-date') { commitDateText(); return; }
    if (t.id === 'ap-date-native' && t.value) { setDate(t.value); var inp = $('#ap-date'); if (inp) inp.focus(); }
  });

  document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && state.panel) { e.preventDefault(); closePanel(true); return; }
    if (e.key === 'Tab' && state.panel) {
      var f = $('#assign-panel').querySelectorAll('button:not([disabled]), input:not([disabled]), select, a[href], [tabindex="0"]');
      if (f.length) {
        var first = f[0], last = f[f.length - 1];
        if (!e.shiftKey && document.activeElement === last) { e.preventDefault(); first.focus(); }
        else if (e.shiftKey && document.activeElement === first) { e.preventDefault(); last.focus(); }
        else if (!$('#assign-panel').contains(document.activeElement)) { e.preventDefault(); first.focus(); }
      }
    }
    if (e.key === 'Enter' && e.target.id === 'ap-date') { e.preventDefault(); commitDateText(); }
  });

  function openPicker() {
    var nat = $('#ap-date-native');
    if (!nat || !state.panel) return;
    nat.value = state.panel.dateIso;
    try { nat.showPicker(); } catch (err) { var inp = $('#ap-date'); if (inp) inp.focus(); }
  }

  function afterFilterChange() {
    pruneSelection();
    renderHeader();
    renderFilters();
    renderBulkbar();
    renderList();
    if (state.panel) panelSelectionChanged();
  }

  window.addEventListener('online', function () { checkHealth(); });
  window.addEventListener('offline', function () { setOffline(true); });

  // ---------- Start ----------
  loadList();
  if (state.offline) renderAll();
  setInterval(poll, POLL_MS);
})();
