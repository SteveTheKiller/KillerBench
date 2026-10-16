(function () {
  'use strict';

  var docId = document.body.getAttribute('data-doc') || 'overview';
  var pages = [
    { id: 'overview', title: 'Overview', file: 'index.html', group: 'Engine' },
    { id: 'getting-started', title: 'Getting started', file: 'getting-started.html', group: 'Start here' },
    { id: 'workflows', title: 'Diagnostic workflows', file: 'workflows.html', group: 'Use the Engine' },
    { id: 'evidence', title: 'Evidence and results', file: 'evidence.html', group: 'Use the Engine' },
    { id: 'repairs', title: 'Repairs and safety', file: 'repairs.html', group: 'Use the Engine' },
    { id: 'cli', title: 'CLI guide', file: 'cli.html', group: 'Reference' },
    { id: 'api', title: 'API guide', file: 'api.html', group: 'Reference' },
    { id: 'architecture', title: 'Architecture', file: 'architecture.html', group: 'Reference' },
    { id: 'knowledge-packs', title: 'Knowledge packs', file: 'knowledge-packs.html', group: 'Reference' }
  ];

  function escapeHtml(value) {
    return String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  function inline(value) {
    var code = [];
    var text = escapeHtml(value).replace(/`([^`]+)`/g, function (_, body) {
      code.push('<code>' + body + '</code>');
      return '\u0000' + (code.length - 1) + '\u0000';
    });
    text = text.replace(/\[([^\]]+)\]\(([^\s)]+)\)/g, '<a href="$2">$1</a>');
    text = text.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    return text.replace(/\u0000(\d+)\u0000/g, function (_, index) { return code[Number(index)]; });
  }

  function slug(value) {
    return value.toLowerCase().replace(/<[^>]*>/g, '').replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
  }

  function cells(line) {
    return line.trim().replace(/^\||\|$/g, '').split('|').map(function (cell) { return cell.trim(); });
  }

  function renderMarkdown(markdown) {
    var lines = markdown.replace(/\r\n?/g, '\n').split('\n');
    var output = [], paragraph = [], i = 0;
    function flush() {
      if (!paragraph.length) return;
      output.push('<p' + (output.length ? '' : ' class="doc-lede"') + '>' + inline(paragraph.join(' ')) + '</p>');
      paragraph = [];
    }
    while (i < lines.length) {
      var line = lines[i];
      if (/^```/.test(line)) {
        flush();
        var language = line.slice(3).trim(), body = [];
        i++;
        while (i < lines.length && !/^```/.test(lines[i])) { body.push(lines[i]); i++; }
        output.push('<pre data-language="' + escapeHtml(language) + '"><code>' + escapeHtml(body.join('\n')) + '</code></pre>');
        i++;
        continue;
      }
      var heading = /^(#{1,3})\s+(.+)$/.exec(line);
      if (heading) {
        flush();
        var level = heading[1].length;
        output.push('<h' + level + ' id="' + slug(heading[2]) + '">' + inline(heading[2]) + '</h' + level + '>');
        i++;
        continue;
      }
      if (line.indexOf('|') >= 0 && i + 1 < lines.length && /^\s*\|?\s*:?-+/.test(lines[i + 1])) {
        flush();
        var headers = cells(line), rows = [];
        i += 2;
        while (i < lines.length && lines[i].trim() && lines[i].indexOf('|') >= 0) { rows.push(cells(lines[i])); i++; }
        output.push('<table><thead><tr>' + headers.map(function (cell) { return '<th>' + inline(cell) + '</th>'; }).join('') + '</tr></thead><tbody>' + rows.map(function (row) { return '<tr>' + row.map(function (cell) { return '<td>' + inline(cell) + '</td>'; }).join('') + '</tr>'; }).join('') + '</tbody></table>');
        continue;
      }
      var unordered = /^\s*[-+]\s+(.+)$/.exec(line);
      var ordered = /^\s*\d+\.\s+(.+)$/.exec(line);
      if (unordered || ordered) {
        flush();
        var tag = unordered ? 'ul' : 'ol', items = [];
        while (i < lines.length) {
          var match = tag === 'ul' ? /^\s*[-+]\s+(.+)$/.exec(lines[i]) : /^\s*\d+\.\s+(.+)$/.exec(lines[i]);
          if (!match) break;
          items.push('<li>' + inline(match[1]) + '</li>');
          i++;
        }
        output.push('<' + tag + '>' + items.join('') + '</' + tag + '>');
        continue;
      }
      if (!line.trim()) flush(); else paragraph.push(line.trim());
      i++;
    }
    flush();
    return output.join('\n');
  }

  function themeButtons() {
    return ['dark','light','hc','blood','greed','cyanotic','ectoplasm','decay','malaise','sepulchre','delirium','mourning'].map(function (name) {
      var title = name === 'hc' ? 'Black' : name.charAt(0).toUpperCase() + name.slice(1);
      return '<button class="swatch sw-' + name + '" data-theme="' + name + '" title="' + title + '"></button>';
    }).join('');
  }

  function navigation() {
    var groups = [];
    pages.forEach(function (page) {
      var group = groups.find(function (item) { return item.name === page.group; });
      if (!group) { group = { name: page.group, pages: [] }; groups.push(group); }
      group.pages.push(page);
    });
    return groups.map(function (group) {
      return '<div class="docs-nav-group"><span class="docs-nav-label">' + group.name + '</span>' + group.pages.map(function (page) {
        var active = page.id === docId ? ' class="on" aria-current="page"' : '';
        return '<a href="' + page.file + '"' + active + '>' + page.title + '</a>' + (page.id === docId ? '<div class="page-outline" id="pageOutline"></div>' : '');
      }).join('') + '</div>';
    }).join('');
  }

  function shell() {
    document.body.setAttribute('data-brand-base', '../brand');
    document.body.innerHTML = '<header class="topbar"><a class="tb-home" href="../index.html"><img class="wm-logo tb-wm" src="../brand/killerbench-logo-dark-yellow.svg" alt="KillerBench"></a><span class="tb-spacer"></span><nav class="tb-nav"><a href="../index.html#workbench">Workbench</a><a href="../index.html#features">Features</a><a class="on" href="index.html">Engine</a><a href="../index.html#about">About</a></nav><div class="tgrp" role="group" aria-label="Theme">' + themeButtons() + '</div><div class="acc-fly accent-switch" id="accentSwitch"><button class="acc-toggle" id="accentToggle" aria-haspopup="true" aria-expanded="false" title="Accent color"></button><div class="acc-pop" id="accentPop" hidden role="group" aria-label="Accent color"><span class="acc-pop-label">accent:</span><button class="acc" data-accent="red" style="background:#DD504B;color:#DD504B" title="Red"></button><button class="acc" data-accent="orange" style="background:#E8962C;color:#E8962C" title="Orange"></button><button class="acc" data-accent="yellow" style="background:#EAD900;color:#EAD900" title="Yellow"></button><button class="acc" data-accent="green" style="background:#1EA54C;color:#1EA54C" title="Green"></button><button class="acc" data-accent="teal" style="background:#1FB8A8;color:#1FB8A8" title="Teal"></button><button class="acc" data-accent="blue" style="background:#50AEE8;color:#50AEE8" title="Blue"></button><button class="acc" data-accent="purple" style="background:#B982E3;color:#B982E3" title="Purple"></button><button class="acc" data-accent="magenta" style="background:#FF52C9;color:#FF52C9" title="Magenta"></button></div></div></header><div class="shell engine-shell"><aside class="sidebar engine-sidebar"><div class="sb-tabs"><span class="on">Developer guide</span></div><nav class="docs-nav" aria-label="Engine documentation">' + navigation() + '</nav></aside><main class="content"><div class="content-scroll"><div class="engine-doc-wrap"><div class="engine-doc-hero"><img class="wm-logo" src="../brand/killerbench-logo-dark-yellow.svg" alt="The KillerBench.Engine"></div><article class="engine-doc" id="engineDoc"><p>Loading documentation...</p></article><nav class="doc-pager" id="docPager" aria-label="Documentation pages"></nav></div></div></main></div><footer class="statusbar"><span class="left"><a href="https://github.com/SteveTheKiller/KillerBench/tree/main/Engine">Engine source</a> &middot; In development</span><span class="right">v1.0.0 &middot; &copy; 2026 <b><a href="https://thekiller.net">Steve the Killer</a></b></span></footer>';
  }

  function pager() {
    var index = pages.findIndex(function (page) { return page.id === docId; });
    var previous = index > 0 ? pages[index - 1] : null;
    var next = index >= 0 && index < pages.length - 1 ? pages[index + 1] : null;
    document.getElementById('docPager').innerHTML = (previous ? '<a href="' + previous.file + '"><small>Previous</small><span>' + previous.title + '</span></a>' : '<span></span>') + (next ? '<a href="' + next.file + '"><small>Next</small><span>' + next.title + '</span></a>' : '<span></span>');
  }

  function outline() {
    var container = document.getElementById('pageOutline');
    if (!container) return;
    container.innerHTML = Array.prototype.map.call(document.querySelectorAll('#engineDoc h2'), function (heading) {
      return '<a href="#' + heading.id + '">' + heading.textContent + '</a>';
    }).join('');
  }

  function show(markdown) {
    document.getElementById('engineDoc').innerHTML = renderMarkdown(markdown);
    outline();
  }

  function loadBehavior() {
    var script = document.createElement('script');
    script.src = '../bench.js?v=6';
    document.body.appendChild(script);
  }

  shell();
  pager();
  if (window.KillerBenchEngineDocs && window.KillerBenchEngineDocs[docId]) {
    show(window.KillerBenchEngineDocs[docId]);
  } else {
    document.getElementById('engineDoc').innerHTML = '<div class="doc-error"><strong>The guide content is unavailable.</strong><p>Reload this page with the complete Engine documentation files.</p></div>';
  }
  loadBehavior();
})();
