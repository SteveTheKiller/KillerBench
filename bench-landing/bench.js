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
    dark:  { red:['#DD504B','#5E1C1C'], orange:['#E8962C','#F29A28'], yellow:['#EAD900','#F2E500'], green:['#1EA54C','#1C5E38'], teal:['#1FB8A8','#1C5E5C'], blue:['#50AEE8','#1C3B5E'], purple:['#B982E3','#411C5E'], magenta:['#FF52C9','#FF2BBD'] },
    light: { red:['#931A1A','#931A1A'], orange:['#C7710F','#C7710F'], yellow:['#766600','#766600'], green:['#1B5E20','#1B5E20'], teal:['#0D827E','#0D827E'], blue:['#18608E','#18608E'], purple:['#5A1690','#5A1690'], magenta:['#A60070','#A60070'] },
    hc:    { red:['#FF2929','#FF2929'], orange:['#FF910A','#FF910A'], yellow:['#FFEB00','#FFEB00'], green:['#00FF66','#00FF66'], teal:['#0AFFE7','#0AFFE7'], blue:['#298DFF','#298DFF'], purple:['#B829FF','#B829FF'], magenta:['#FF2BBD','#FF2BBD'] }
  };
  // SelectionBg (muted accent) for inactive tab/card edges; brightens to accent on hover. Mirrors KillerTools themes.ts.
  var SEL = {
    dark:  { red:'#5E1C1C', orange:'#5E3B16', yellow:'#2A2908', green:'#1C5E38', teal:'#1C5E5C', blue:'#1C3B5E', purple:'#411C5E', magenta:'#2A0D24' },
    light: { red:'#931A1A', orange:'#C7710F', yellow:'#766600', green:'#1B5E20', teal:'#0D827E', blue:'#18608E', purple:'#5A1690', magenta:'#A60070' },
    hc:    { red:'#380000', orange:'#4E2900', yellow:'#2E2B00', green:'#003314', teal:'#003832', blue:'#0A2C50', purple:'#250038', magenta:'#2E0022' }
  };
  function famFor(t) { return t === 'light' ? 'light' : t === 'hc' ? 'hc' : 'dark'; }
  // Text on an accent fill uses white or near-black, whichever contrasts more with that fill.
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
        dot.style.setProperty('--sw-accent', neutralTheme === 'light' && name === 'yellow' ? 'linear-gradient(#FFF5A3, #FFD43B)' : preview[0]);
      });
    });
    curAccent = name;
    var pair = ACCENTS[fam][name];
    var neutral = NEUTRAL.indexOf(theme) >= 0;
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
      if (p) { d.style.background = p[0]; d.style.color = p[0]; }
      d.setAttribute('aria-pressed', d.dataset.accent === name ? 'true' : 'false');
    });
    if (accToggle) { accToggle.style.background = pair[0]; accToggle.title = uiText('ui_accent'); }
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
