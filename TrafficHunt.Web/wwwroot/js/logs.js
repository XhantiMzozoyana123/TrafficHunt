(function () {
    'use strict';

    const consoleEl = document.getElementById('logConsole');
    const countEl   = document.getElementById('logCount');
    let sinceId = window.logSinceId || 0;
    let total   = window.logTotal  || 0;
    let refreshing = false;

    function fmtTime(iso) {
        try { return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', second: '2-digit' }); }
        catch { return iso; }
    }

    function esc(s) {
        if (s == null) return '';
        return String(s).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
    }

    function categoryShort(cat) {
        if (!cat) return '—';
        const parts = cat.split('.');
        return parts[parts.length - 1];
    }

    function renderRow(e) {
        const cls = e.levelClass || '';
        let detail = esc(e.message || '');
        if (e.category === 'HTTP Request') {
            detail = '<span class="muted">' + esc(e.method) + '</span> '
                + '<span class="link-dim">' + esc(e.path) + '</span>'
                + ' <span class="' + cls + '">' + (e.status != null ? ('HTTP ' + e.status) : 'OK') + '</span>'
                + (e.durationMs != null ? ' <span class="muted">· ' + e.durationMs + ' ms</span>' : '');
        }
        let meta = '';
        if (e.campaignId != null) meta += '<span class="muted">campaign ' + e.campaignId + '</span>';
        if (e.jobId) meta += '<span class="muted">job ' + esc(e.jobId) + '</span>';
        return '<div class="log-row ' + cls + '">'
            + '<span class="log-time muted">' + fmtTime(e.time) + '</span>'
            + '<span class="log-lvl ' + cls + '">' + esc(e.level) + '</span>'
            + '<span class="log-cat muted">' + esc(categoryShort(e.category)) + '</span>'
            + '<span class="log-msg" title="' + esc(e.message || '') + '">' + detail + '</span>'
            + meta + '</div>';
    }

    function appendEntries(entries) {
        if (!entries || !entries.length) return;
        if (consoleEl) consoleEl.insertAdjacentHTML('beforeend', entries.map(renderRow).join(''));
        sinceId = entries[entries.length - 1].id;
        total += entries.length;
        if (countEl) countEl.textContent = total + ' entries · live';
        scrollIfBottom();
    }

    function resetFeed() {
        sinceId = 0;
        total = 0;
        if (consoleEl) consoleEl.innerHTML = '<p class="small muted">Loading…</p>';
    }

    function scrollIfBottom() {
        var wrap = consoleEl && consoleEl.parentElement;
        if (!wrap) return;
        if (wrap.scrollTop + wrap.clientHeight >= wrap.scrollHeight - 40) {
            wrap.scrollTop = wrap.scrollHeight;
        }
    }

    function getFilters() {
        var p = new URLSearchParams();
        if (sinceId > 0) p.set('sinceId', sinceId);
        p.set('limit', '200');
        var cat = document.querySelector('input[name="category"]:checked');
        if (cat && cat.value && cat.value !== 'all') p.set('category', cat.value);
        var camp = document.getElementById('filterCampaign');
        if (camp && camp.value) p.set('campaignId', camp.value);
        return p;
    }

    async function poll() {
        if (refreshing) return;
        refreshing = true;
        try {
            var r = await fetch('/logs/recent?' + getFilters().toString());
            if (!r.ok) { refreshing = false; return; }
            var d = await r.json();
            appendEntries(d.entries || []);
        } catch (_) { /* transient - keep polling */ }
        refreshing = false;
    }

    // Reset the feed whenever a filter changes.
    document.querySelectorAll('input[name="category"]').forEach(function (r) {
        r.addEventListener('change', function () { resetFeed(); poll(); });
    });
    var campInput = document.getElementById('filterCampaign');
    if (campInput) campInput.addEventListener('change', function () { resetFeed(); poll(); });
    var loadRecent = document.getElementById('loadRecent');
    if (loadRecent) loadRecent.addEventListener('click', function () { resetFeed(); poll(); });

    // If there were no server-rendered entries, fetch the initial batch; otherwise
    // we already rendered them server-side and just continue from `sinceId`.
    if (total === 0) { resetFeed(); poll(); } else { poll(); }

    setInterval(poll, 2000);
})();
