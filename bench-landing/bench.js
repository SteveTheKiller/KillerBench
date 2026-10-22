/* KillerBench site - shared chrome behavior (theme, accent, language, easter egg).
   Page-specific behavior (screenshot strip, outline scroll-spy) stays inline per page. */
(function () {
  var root = document.documentElement;
  var languageToggle = document.getElementById('languageToggle');
  var languageMenu = document.getElementById('languageMenu');
  if (languageToggle && languageMenu) {
    function closeLanguages() { languageMenu.hidden = true; languageToggle.setAttribute('aria-expanded', 'false'); }
    languageToggle.addEventListener('click', function (e) { e.stopPropagation(); languageMenu.hidden = !languageMenu.hidden; languageToggle.setAttribute('aria-expanded', String(!languageMenu.hidden)); });
    document.addEventListener('click', function (e) { if (!languageMenu.contains(e.target)) closeLanguages(); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && !languageMenu.hidden) { closeLanguages(); languageToggle.focus(); } });
  }
  var THEMES = ['dark','light','hc','blood','greed','cyanotic','ectoplasm','decay','malaise','sepulchre','delirium','mourning'];
  var NEUTRAL = ['dark','light','hc'];
  var THEMED = ['blood','greed','cyanotic','ectoplasm','decay','malaise','sepulchre','delirium','mourning'];  // fixed-color wordmark art
  // Per-family palette copied from the app: [ Accent (bright: text/links/logo/outlines), SelectionBg (darker fill: solid buttons, selected tab edges) ].
  var ACCENTS = {
    dark:  { red:['#FF7C77','#5E1C1C'], orange:['#E8962C','#F29A28'], yellow:['#EAD900','#F2E500'], green:['#22BB56','#1C5E38'], teal:['#1FB8A8','#1C5E5C'], blue:['#50AEE8','#1C3B5E'], purple:['#C08FE6','#411C5E'], magenta:['#FF6CD1','#FF2BBD'] },
    light: { red:['#901A1A','#901A1A'], orange:['#E8962C','#E8962C'], yellow:['#F2C500','#F2C500'], green:['#18541D','#18541D'], teal:['#085250','#085250'], blue:['#134D73','#134D73'], purple:['#5A1690','#5A1690'], magenta:['#8F0061','#8F0061'] },
    hc:    { red:['#FF3131','#FF3131'], orange:['#FF910A','#FF910A'], yellow:['#FFEB00','#FFEB00'], green:['#00FF66','#00FF66'], teal:['#0AFFE7','#0AFFE7'], blue:['#298DFF','#298DFF'], purple:['#C34AFF','#C34AFF'], magenta:['#FF2BBD','#FF2BBD'] }
  };
  // SelectionBg (muted accent) for inactive tab/card edges; brightens to accent on hover. Mirrors KillerTools themes.ts.
  var SEL = {
    dark:  { red:'#5E1C1C', orange:'#5E3B16', yellow:'#2A2908', green:'#1C5E38', teal:'#1C5E5C', blue:'#1C3B5E', purple:'#411C5E', magenta:'#2A0D24' },
    light: { red:'#901A1A', orange:'#E8962C', yellow:'#F2C500', green:'#18541D', teal:'#085250', blue:'#134D73', purple:'#5A1690', magenta:'#8F0061' },
    hc:    { red:'#380000', orange:'#4E2900', yellow:'#2E2B00', green:'#003314', teal:'#003832', blue:'#0A2C50', purple:'#250038', magenta:'#2E0022' }
  };
  function famFor(t) { return t === 'light' ? 'light' : t === 'hc' ? 'hc' : 'dark'; }
  // Text on an accent fill uses white or near-black, whichever contrasts more with that fill.
  // Swatches and filled buttons use a deeper red than the dark red accent text, which stays light for contrast.
  var FILL = { '#FF7C77': '#EA524C' };
  function fillOf(hex) { return FILL[String(hex).toUpperCase()] || hex; }
  function readableOn(hex) {
    var lum = function (h) { var c = [1, 3, 5].map(function (i) { var v = parseInt(h.substr(i, 2), 16) / 255; return v <= 0.03928 ? v / 12.92 : Math.pow((v + 0.055) / 1.055, 2.4); }); return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]; };
    var l = lum(hex), dark = lum('#1e1f22');
    return (1.05 / (l + 0.05)) >= ((l + 0.05) / (dark + 0.05)) ? '#ffffff' : '#1e1f22';
  }

  var swatches = [].slice.call(document.querySelectorAll('.swatch'));
  var accDots  = [].slice.call(document.querySelectorAll('.acc'));
  var accentSwitch = document.getElementById('accentSwitch');
  var accToggle = document.getElementById('accentToggle');
  var accPop = document.getElementById('accentPop');
  var curAccent = 'yellow';
  var syncThemePreview = function () {};

  function buildThemeFlyout() {
    var group = document.querySelector('.topbar .tgrp');
    if (!group || !group.parentNode) return;
    var toggle = document.createElement('button');
    toggle.type = 'button';
    toggle.className = 'theme-toggle';
    toggle.title = document.body.getAttribute('data-ui-theme') || 'Theme';
    toggle.setAttribute('aria-label', document.body.getAttribute('data-ui-choose-theme') || 'Choose theme');
    toggle.setAttribute('aria-haspopup', 'true');
    toggle.setAttribute('aria-expanded', 'false');
    var preview = document.createElement('span');
    preview.setAttribute('aria-hidden', 'true');
    toggle.appendChild(preview);
    group.parentNode.insertBefore(toggle, group);
    function closeFlyout(focusToggle) {
      group.classList.remove('open');
      toggle.setAttribute('aria-expanded', 'false');
      if (focusToggle) toggle.focus();
    }
    function syncPreview(name) {
      var active = group.querySelector('.swatch[data-theme="' + name + '"]') || group.querySelector('.swatch');
      if (active) preview.className = active.className;
      preview.removeAttribute('aria-pressed');
    }
    syncThemePreview = syncPreview;
    toggle.addEventListener('click', function (e) {
      e.stopPropagation();
      var opening = !group.classList.contains('open');
      group.classList.toggle('open', opening);
      toggle.setAttribute('aria-expanded', opening ? 'true' : 'false');
    });
    group.addEventListener('click', function (e) {
      var swatch = e.target.closest('.swatch[data-theme]');
      if (!swatch) return;
      syncPreview(swatch.getAttribute('data-theme'));
      closeFlyout(false);
    });
    document.addEventListener('click', function (e) {
      if (!group.contains(e.target) && !toggle.contains(e.target)) closeFlyout(false);
    });
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape' && group.classList.contains('open')) closeFlyout(true);
    });
    syncPreview(root.getAttribute('data-theme') || 'dark');
  }
  buildThemeFlyout();

  function applyAccent(name) {
    var theme = root.getAttribute('data-theme');
    var fam = famFor(theme);
    if (!ACCENTS[fam][name]) name = 'green';
    ['dark', 'light', 'hc'].forEach(function (neutralTheme) {
      var preview = ACCENTS[neutralTheme][name];
      if (preview) document.querySelectorAll('.sw-' + neutralTheme).forEach(function (dot) {
        dot.style.setProperty('--sw-accent', neutralTheme === 'light' && name === 'yellow' ? 'linear-gradient(#FFF5A3, #FFD43B)' : fillOf(preview[0]));
      });
    });
    curAccent = name;
    var pair = ACCENTS[fam][name];
    var neutral = NEUTRAL.indexOf(theme) >= 0;
    root.toggleAttribute('data-yellow-outline', neutral && fam === 'light' && (name === 'yellow' || name === 'orange'));
    if (neutral) {
      root.style.setProperty('--accent', pair[0]);
      root.style.setProperty('--btn', pair[1]);
      root.style.setProperty('--sel', (SEL[fam] && SEL[fam][name]) || pair[1]);
      root.style.setProperty('--btn-text', readableOn(pair[1]));
      root.style.setProperty('--accent-text', readableOn(pair[0]));
      try { localStorage.setItem('kbench-av', pair[0] + '|' + pair[1]); } catch (e) {}
    } else {
      root.style.removeProperty('--accent');
      root.style.removeProperty('--btn');
      root.style.removeProperty('--sel');
      root.style.removeProperty('--btn-text');
      root.style.removeProperty('--accent-text');
    }
    accDots.forEach(function (d) {
      var p = ACCENTS[fam][d.dataset.accent];
      if (p) { d.style.background = fillOf(p[0]); d.style.color = fillOf(p[0]); }
      d.setAttribute('aria-pressed', d.dataset.accent === name ? 'true' : 'false');
    });
    if (accToggle) { accToggle.style.background = fillOf(pair[0]); accToggle.title = uiText('ui_accent'); }
    try { localStorage.setItem('kbench-accent', name); } catch (e) {}
    updateLogos();
  }
  function updateLogos() {
    var theme = root.getAttribute('data-theme');
    var brandBase = document.body.getAttribute('data-brand-base') || 'brand';
    var src;
    if (THEMED.indexOf(theme) >= 0) {
      // Fixed-color themes carry their own wordmark art, colored with the theme's in-app
      // PrimaryBrush (make-logo-svgs.py --themes).
      src = brandBase + '/killerbench-logo-' + theme + '.svg';
    } else {
      var variant = theme === 'light' ? 'light' : theme === 'hc' ? 'black' : 'dark';
      var color = (NEUTRAL.indexOf(theme) >= 0) ? curAccent : 'yellow';
      src = brandBase + '/killerbench-logo-' + variant + '-' + color + '.svg';
    }
    var imgs = document.querySelectorAll('img.wm-logo');
    for (var i = 0; i < imgs.length; i++) imgs[i].src = src;
  }

  function setTheme(name) {
    if (THEMES.indexOf(name) < 0) name = 'dark';
    root.setAttribute('data-theme', name);
    try { localStorage.setItem('kbench-theme', name); } catch (e) {}
    swatches.forEach(function (s) { s.setAttribute('aria-pressed', s.dataset.theme === name ? 'true' : 'false'); });
    syncThemePreview(name);
    if (accentSwitch) accentSwitch.hidden = NEUTRAL.indexOf(name) < 0;
    applyAccent(curAccent);
  }

  swatches.forEach(function (s) { s.addEventListener('click', function () { setTheme(s.dataset.theme); if (NEUTRAL.indexOf(s.dataset.theme) >= 0) showAccentBar(); else hideAccentBar(); }); });
  // Build a drop-down accent bar under the toolbar (moves the swatches out of the small header popup).
  var accentBar = null;
  var topbarEl = document.querySelector('.topbar');
  if (topbarEl && accDots.length) {
    accentBar = document.createElement('div');
    accentBar.className = 'accent-bar';
    var pill = document.createElement('div'); pill.className = 'pill';
    var grip = document.createElement('span'); grip.className = 'grip'; grip.setAttribute('aria-hidden', 'true');
    pill.appendChild(grip);
    var blbl = document.createElement('span'); blbl.className = 'lbl'; blbl.setAttribute('data-i18n', 'accent_label'); blbl.textContent = document.body.getAttribute('data-ui-accent-label') || 'accent:';
    pill.appendChild(blbl);
    accDots.forEach(function (d) { pill.appendChild(d); });
    var bx = document.createElement('button'); bx.className = 'x'; bx.setAttribute('aria-label', document.body.getAttribute('data-ui-close') || 'Close'); bx.innerHTML = '&times;';
    bx.addEventListener('click', hideAccentBar);
    pill.appendChild(bx);
    accentBar.appendChild(pill);
    topbarEl.parentNode.insertBefore(accentBar, topbarEl.nextSibling);
    if (accPop) accPop.remove();

    // Drag the strip sideways by its grip, clamped so it stays inside the content pane (the frame).
    var dragDx = 0, dragging = false, dragStartX = 0, dragStartDx = 0;
    function dragClamp(v) {
      var vw = window.innerWidth, pw = pill.offsetWidth, pad = 6, left = 8, right = vw - 8;
      var f = document.querySelector('.content');
      // Extra inset on the right so the pill clears the content scrollbar at its max position.
      if (f) { var fr = f.getBoundingClientRect(); if (fr.width > 0) { left = fr.left + pad; right = fr.right - pad - 12; } }
      var centerLeft = vw / 2 - pw / 2, min = left - centerLeft, max = right - pw - centerLeft;
      if (min > max) return 0;
      return Math.max(min, Math.min(max, v));
    }
    grip.addEventListener('mousedown', function (e) {
      dragging = true; dragStartX = e.clientX; dragStartDx = dragDx;
      document.body.style.userSelect = 'none'; e.preventDefault();
    });
    window.addEventListener('mousemove', function (e) {
      if (!dragging) return;
      dragDx = dragClamp(dragStartDx + (e.clientX - dragStartX));
      pill.style.transform = 'translateX(' + dragDx + 'px)';
    });
    window.addEventListener('mouseup', function () {
      if (!dragging) return; dragging = false; document.body.style.userSelect = '';
    });
    function dockAccentBar() {
      var contentPane = document.querySelector('.content');
      if (contentPane) accentBar.style.top = Math.round(contentPane.getBoundingClientRect().top) + 'px';
    }
    window.addEventListener('resize', function () { dockAccentBar(); dragDx = dragClamp(dragDx); pill.style.transform = 'translateX(' + dragDx + 'px)'; });
    // Default position: top-right corner, nearest the theme picker (still draggable from there).
    requestAnimationFrame(function () { dragDx = dragClamp(1e6); pill.style.transform = 'translateX(' + dragDx + 'px)'; });
  }
  function showAccentBar() { if (accentBar && NEUTRAL.indexOf(root.getAttribute('data-theme')) >= 0) { var contentPane = document.querySelector('.content'); if (contentPane) accentBar.style.top = Math.round(contentPane.getBoundingClientRect().top) + 'px'; accentBar.classList.add('show'); if (accToggle) accToggle.setAttribute('aria-expanded', 'true'); } }
  function hideAccentBar() { if (accentBar) { accentBar.classList.remove('show'); if (accToggle) accToggle.setAttribute('aria-expanded', 'false'); } }
  accDots.forEach(function (d) { d.addEventListener('click', function () { applyAccent(d.dataset.accent); }); });
  if (accToggle) {
    accToggle.addEventListener('click', function (e) { e.stopPropagation(); if (accentBar && accentBar.classList.contains('show')) hideAccentBar(); else showAccentBar(); });
  }
  document.addEventListener('click', function (e) { if (accentBar && accentBar.classList.contains('show') && !e.target.closest('.accent-bar') && !e.target.closest('#accentToggle')) hideAccentBar(); });

  function uiText(key) { return key === 'ui_accent' ? (document.body.getAttribute('data-ui-accent') || 'Accent color') : key; }
  swatches.concat(accDots).forEach(function(button){button.setAttribute('aria-label',button.title);});
  var savedTheme = 'dark', savedAccent = 'yellow';
  try { savedTheme = localStorage.getItem('kbench-theme') || savedTheme; savedAccent = localStorage.getItem('kbench-accent') || savedAccent; } catch(e) {}
  curAccent = savedAccent;
  setTheme(savedTheme);
  document.addEventListener('keydown', function(e){if(e.key === 'Escape') hideAccentBar();});
})();
/* Top bar fit: the bar never wraps. When it runs out of room, the Microsoft Store and KillerMCP
   buttons shrink to their logos first, then the navigation links move into a More menu from the
   end, then the Download button drops its label, and last the Store and KillerMCP buttons join
   the menu. Re-measured on resize, font load and language changes. */
(function () {
  var bar = document.querySelector('.topbar');
  if (!bar || !window.MutationObserver) return;
  var nav = bar.querySelector('.tb-nav');
  var more = document.createElement('div');
  more.className = 'tb-more';
  more.hidden = true;
  var btn = document.createElement('button');
  btn.type = 'button';
  btn.className = 'tb-more-toggle';
  btn.setAttribute('aria-haspopup', 'true');
  btn.setAttribute('aria-expanded', 'false');
  btn.setAttribute('aria-label', document.body.getAttribute('data-ui-more') || 'More');
  btn.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="5" cy="12" r="2"/><circle cx="12" cy="12" r="2"/><circle cx="19" cy="12" r="2"/></svg>';
  var menu = document.createElement('div');
  menu.className = 'lang-menu tb-more-menu';
  menu.hidden = true;
  more.appendChild(btn);
  more.appendChild(menu);
  if (nav && nav.parentNode === bar) bar.insertBefore(more, nav.nextSibling); else bar.appendChild(more);

  function mark(el) {
    if (!el) return null;
    var m = document.createComment('tb-slot');
    el.parentNode.insertBefore(m, el.nextSibling);
    return { el: el, slot: m };
  }
  var links = nav ? [].slice.call(nav.children).map(mark) : [];
  var extras = [bar.querySelector('.tb-mcp-link'), bar.querySelector('.tb-store')].filter(function (e) { return e && e.parentNode === bar; }).map(mark);
  var all = links.concat(extras);
  var busy = false;

  function over() { return bar.scrollWidth > bar.clientWidth + 1; }
  function close() { menu.hidden = true; btn.setAttribute('aria-expanded', 'false'); }
  function fit() {
    busy = true;
    close();
    all.forEach(function (m) { m.slot.parentNode.insertBefore(m.el, m.slot); });
    bar.classList.remove('tb-compact', 'tb-dl-icon');
    more.hidden = true;
    if (over()) bar.classList.add('tb-compact');
    if (over()) {
      more.hidden = false;
      for (var i = links.length - 1; i >= 0 && over(); i--) menu.insertBefore(links[i].el, menu.firstChild);
      if (over()) bar.classList.add('tb-dl-icon');
      for (var j = 0; j < extras.length && over(); j++) menu.appendChild(extras[j].el);
      if (!menu.children.length) more.hidden = true;
    }
    observer.takeRecords();
    busy = false;
  }
  var queued = false;
  function queue() {
    if (queued) return;
    queued = true;
    requestAnimationFrame(function () { queued = false; fit(); });
  }
  var observer = new MutationObserver(function () { if (!busy) queue(); });
  observer.observe(bar, { subtree: true, childList: true, characterData: true });
  window.addEventListener('resize', queue);
  // Logos, flags and fonts change widths without a DOM change, so watch every item's size too.
  if (window.ResizeObserver) {
    var sizes = new ResizeObserver(function () { if (!busy) queue(); });
    sizes.observe(bar);
    [].forEach.call(bar.children, function (c) { sizes.observe(c); });
  }
  window.addEventListener('load', queue);
  if (document.fonts && document.fonts.ready) document.fonts.ready.then(queue);
  btn.addEventListener('click', function (e) {
    e.stopPropagation();
    var open = menu.hidden;
    menu.hidden = !open;
    btn.setAttribute('aria-expanded', open ? 'true' : 'false');
  });
  document.addEventListener('click', function (e) { if (!menu.hidden && !more.contains(e.target)) close(); });
  document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && !menu.hidden) { close(); btn.focus(); } });
  fit();
})();
/* Screenshot strip: the home page in every language carries the thumbnails and their translated
   captions in its markup. This fades the strip's bottom edge while more shots are below and opens
   a thumbnail full size in the lightbox with its caption. */
(function () {
  var strip = document.getElementById('sbThumbs');
  var box = document.getElementById('lightbox');
  if (!strip || !box) return;
  var big = document.getElementById('lightboxImg'), caption = document.getElementById('lightboxCaption');
  var lastTrigger = null;
  var inertSiblings = [];
  function setPageInert() {
    Array.prototype.forEach.call(document.body.children, function (element) {
      if (element === box) return;
      inertSiblings.push({ element: element, wasInert: element.inert });
      element.inert = true;
    });
  }
  function restorePage() {
    inertSiblings.forEach(function (entry) { entry.element.inert = entry.wasInert; });
    inertSiblings = [];
  }
  function refade() { strip.classList.toggle('faded', strip.scrollHeight - strip.clientHeight - strip.scrollTop > 4); }
  Array.prototype.forEach.call(strip.querySelectorAll('img'), function (img) {
    img.addEventListener('load', refade);
    img.addEventListener('error', refade);
  });
  strip.addEventListener('scroll', refade, { passive: true });
  window.addEventListener('resize', refade);
  refade();
  strip.addEventListener('click', function (e) {
    var thumb = e.target.closest ? e.target.closest('.sb-thumb') : null;
    if (!thumb) return;
    var img = thumb.querySelector('img');
    lastTrigger = thumb;
    big.src = img.src;
    big.alt = img.alt;
    caption.textContent = img.alt;
    box.setAttribute('aria-hidden', 'false');
    box.classList.add('show');
    setPageInert();
    box.focus();
  });
  function closeBox() {
    if (!box.classList.contains('show')) return;
    box.classList.remove('show');
    box.setAttribute('aria-hidden', 'true');
    restorePage();
    if (lastTrigger && lastTrigger.isConnected) lastTrigger.focus();
  }
  box.addEventListener('click', closeBox);
  document.addEventListener('keydown', function (e) {
    if (!box.classList.contains('show')) return;
    if (e.key === 'Escape') { closeBox(); e.preventDefault(); }
    if (e.key === 'Tab') { e.preventDefault(); box.focus(); }
  });
  document.addEventListener('focusin', function (e) {
    if (box.classList.contains('show') && !box.contains(e.target)) box.focus();
  });
})();
/* Film Grain: the same switch as the app's theme menu and KillerTools, on the family Alt+Shift+G chord.
   On by default; off sets data-grain="off", which empties --grain on every grained surface. */
(function () {
  'use strict';
  var root = document.documentElement;
  var KEY = 'film-grain';
  var LABELS = {
    en: 'Film Grain', bn: 'ফিল্ম গ্রেইন', cs: 'Filmové zrno', de: 'Filmkorn', es: 'Grano de película',
    fr: 'Grain de film', hu: 'Filmszemcse', it: 'Grana pellicola', ja: 'フィルムグレイン',
    kk: 'Пленка түйіршігі', nb: 'Filmkorn', no: 'Filmkorn', pl: 'Ziarno filmowe', pt: 'Granulação de filme',
    ru: 'Зернистость плёнки', sl: 'Filmska zrnatost', tr: 'Film greni', uk: 'Зернистість плівки',
    vi: 'Hạt phim', 'zh-hant': '底片顆粒', 'zh-hans': '胶片颗粒'
  };
  var box = null, text = null;
  function apply(on) {
    root.setAttribute('data-grain', on ? 'on' : 'off');
    if (box) box.checked = on;
  }
  function set(on) {
    apply(on);
    try { localStorage.setItem(KEY, on ? 'on' : 'off'); } catch (e) {}
  }
  function label() {
    var lang = (root.getAttribute('lang') || 'en').toLowerCase();
    if (lang.indexOf('zh') === 0) lang = /hant|tw|hk/.test(lang) ? 'zh-hant' : 'zh-hans';
    else lang = lang.split('-')[0];
    return LABELS[lang] || LABELS.en;
  }
  function localize() {
    if (!text) return;
    text.textContent = label();
    text.parentNode.title = label() + ' (Alt+Shift+G)';
  }
  var saved = 'on';
  try { saved = localStorage.getItem(KEY) || 'on'; } catch (e) {}
  apply(saved !== 'off');

  var group = document.querySelector('.topbar .tgrp');
  if (group) {
    var sep = document.createElement('div');
    sep.className = 'tgrp-sep';
    sep.setAttribute('aria-hidden', 'true');
    var row = document.createElement('label');
    row.className = 'tgrp-grain';
    box = document.createElement('input');
    box.type = 'checkbox';
    box.checked = saved !== 'off';
    box.addEventListener('change', function () { set(box.checked); });
    text = document.createElement('span');
    row.appendChild(box);
    row.appendChild(text);
    group.appendChild(sep);
    group.appendChild(row);
    localize();
    new MutationObserver(localize).observe(root, { attributes: true, attributeFilter: ['lang'] });
  }
  document.addEventListener('keydown', function (e) {
    if (e.altKey && e.shiftKey && !e.ctrlKey && !e.metaKey && e.code === 'KeyG') {
      e.preventDefault();
      if (!e.repeat) set(root.getAttribute('data-grain') === 'off');
    }
  });
})();
