// Validate every maintained landing page and its loaded translation dictionaries.
const fs = require('fs');
const path = require('path');
const vm = require('vm');
const root = path.resolve(__dirname, '..', 'bench-landing');
function htmlFiles(folder) {
  return fs.readdirSync(folder, {withFileTypes:true}).flatMap(entry =>
    entry.isDirectory() ? htmlFiles(path.join(folder, entry.name)) :
      entry.name.endsWith('.html') ? [path.join(folder, entry.name)] : []);
}
const pages = htmlFiles(root);
const failures = [];
let translatedBindings = 0;
for (const file of pages) {
  const html = fs.readFileSync(file, 'utf8');
  const relative = path.relative(root, file);
  const ids = [...html.matchAll(/\bid="([^"]+)"/g)].map(match => match[1]);
  if (new Set(ids).size !== ids.length) failures.push(relative + ': duplicate IDs');
  for (const match of html.matchAll(/\b(?:href|src)="([^"]+)"/g)) {
    const reference = match[1];
    if (/^(?:https?:|mailto:|data:|javascript:)/i.test(reference) || reference === '#') continue;
    const [pathname, fragment] = reference.split('#');
    const target = pathname.split('?')[0];
    const resolved = target ? path.resolve(path.dirname(file), decodeURIComponent(target)) : file;
    if (!resolved.startsWith(root + path.sep) && resolved !== root) continue;
    if (!fs.existsSync(resolved)) { failures.push(relative + ': missing ' + reference); continue; }
    if (fragment && resolved.endsWith('.html')) {
      const targetHtml = resolved === file ? html : fs.readFileSync(resolved, 'utf8');
      const anchors = [...targetHtml.matchAll(/\bid="([^"]+)"/g)].map(item => item[1]);
      if (!anchors.includes(decodeURIComponent(fragment))) failures.push(relative + ': missing anchor ' + reference);
    }
  }
  const context = vm.createContext({});
  context.window = context;
  const scripts = [...html.matchAll(/<script[^>]+src="([^"]+)"/g)].map(match => match[1]);
  for (const script of scripts.filter(script => /i18n[^/]*\.js(?:\?|$)/.test(script))) {
    const scriptFile = path.resolve(path.dirname(file), script.split('?')[0]);
    if (fs.existsSync(scriptFile)) vm.runInContext(fs.readFileSync(scriptFile, 'utf8'), context, {filename:scriptFile});
  }
  const keys = [...new Set([...html.matchAll(/\bdata-i18n(?:-[a-z-]+)?="([A-Za-z0-9_-]+)"/g)].map(match => match[1]))];
  const locales = Object.keys(context.I18N || {});
  if (locales.length !== 19) failures.push(relative + ': expected nineteen translated dictionaries, found ' + locales.length);
  for (const locale of locales) {
    for (const key of keys) {
      const value = context.I18N[locale][key];
      if (typeof value !== 'string' || !value.trim()) failures.push(relative + ': ' + locale + ' missing ' + key);
      else translatedBindings++;
    }
  }
  for (const required of ['help.html', 'technical.html']) {
    if (!html.includes('href="' + (relative.includes(path.sep) ? '../' : '') + required + '"'))
      failures.push(relative + ': missing shared navigation to ' + required);
  }
}
if (failures.length) {
  console.error(failures.join('\n'));
  process.exitCode = 1;
} else console.log('PASS: ' + pages.length + ' pages, ' + translatedBindings + ' translated bindings, local assets, anchors and shared navigation.');
