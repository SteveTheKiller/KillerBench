(function () {
  'use strict';
  var input = document.getElementById('docsSearch');
  var box = document.getElementById('docsSearchResults');
  // KillerBenchSearch holds one index per language; follow the page's current language.
  var all = window.KillerBenchSearch || {};
  function currentPages() { return all[document.documentElement.getAttribute('data-lang') || 'en'] || all.en || []; }
  if (!input || !box || !(all.en || []).length) return;

  function escapeHtml(value) {
    return String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  function snippet(text, term) {
    var at = text.toLowerCase().indexOf(term);
    if (at < 0) return '';
    var start = Math.max(0, at - 50);
    return (start ? '...' : '') + text.slice(start, at + term.length + 80) + '...';
  }

  function search(query) {
    var terms = query.toLowerCase().split(/\s+/).filter(function (t) { return t.length > 1; });
    if (!terms.length) return [];
    var results = [];
    currentPages().forEach(function (page) {
      var title = page.t.toLowerCase(), body = page.x.toLowerCase(), score = 0, anchor = '', heading = '';
      for (var i = 0; i < terms.length; i++) {
        var term = terms[i], hit = false;
        if (title.indexOf(term) >= 0) { score += 10; hit = true; }
        page.h.forEach(function (h) {
          if (h[1].toLowerCase().indexOf(term) >= 0) { score += 5; hit = true; if (!anchor) { anchor = h[0]; heading = h[1]; } }
        });
        if (body.indexOf(term) >= 0) { score += 1; hit = true; }
        if (!hit) return;
      }
      results.push({ page: page, score: score, anchor: anchor, heading: heading, text: snippet(page.x, terms[0]) });
    });
    return results.sort(function (a, b) { return b.score - a.score; }).slice(0, 8);
  }

  function show() {
    var results = search(input.value.trim());
    if (!input.value.trim()) { box.hidden = true; box.innerHTML = ''; return; }
    box.hidden = false;
    box.innerHTML = results.length ? results.map(function (r) {
      var anchor = r.anchor || r.page.a || '';
      var href = '../' + r.page.u + (anchor ? '#' + anchor : '');
      return '<a href="' + escapeHtml(href) + '"><small>' + escapeHtml(r.page.g) + '</small><strong>' + escapeHtml(r.page.t) +
        (r.heading && r.heading !== r.page.t ? ' / ' + escapeHtml(r.heading) : '') + '</strong><span>' + escapeHtml(r.text) + '</span></a>';
    }).join('') : '<p class="docs-search-empty">' + escapeHtml(document.body.getAttribute('data-ui-no-results') || 'No matching pages.') + '</p>';
  }

  input.addEventListener('input', show);
  input.addEventListener('keydown', function (event) {
    if (event.key === 'Escape') { input.value = ''; show(); }
    if (event.key === 'Enter') { var first = box.querySelector('a'); if (first) window.location.href = first.getAttribute('href'); }
  });
})();
