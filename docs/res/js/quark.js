/* Quark docs — shared behavior (theme, copy toast, filters) */

(function () {
  const saved = localStorage.getItem('quark-theme');
  if (saved) document.documentElement.dataset.theme = saved;
})();

function toggleTheme() {
  const root = document.documentElement;
  const next = root.dataset.theme === 'light' ? '' : 'light';
  if (next) root.dataset.theme = next; else delete root.dataset.theme;
  localStorage.setItem('quark-theme', next);
}

function toast(msg) {
  let t = document.getElementById('toast');
  if (!t) { t = document.createElement('div'); t.id = 'toast'; document.body.appendChild(t); }
  t.textContent = msg;
  t.classList.add('show');
  clearTimeout(t._h);
  t._h = setTimeout(() => t.classList.remove('show'), 1200);
}

function copy(text) {
  navigator.clipboard.writeText(text);
  toast(text + ' copiado');
}

/* Filter items: matches query against data-k, and active chip's data-f against data-g */
function wireFilter(searchId, gridSel, countId) {
  const search = document.getElementById(searchId);
  const chips = document.querySelectorAll('.chip[data-f]');
  const apply = () => {
    const q = (search ? search.value : '').trim().toLowerCase();
    const active = document.querySelector('.chip[data-f].active');
    const f = active ? active.dataset.f : '';
    let shown = 0;
    document.querySelectorAll(gridSel + ' [data-k]').forEach(el => {
      const hit = (!q || el.dataset.k.toLowerCase().includes(q)) && (!f || el.dataset.g === f);
      el.classList.toggle('hidden', !hit);
      if (hit) shown++;
    });
    const c = document.getElementById(countId);
    if (c) c.textContent = shown + ' resultados';
  };
  if (search) search.addEventListener('input', apply);
  chips.forEach(ch => ch.addEventListener('click', () => {
    chips.forEach(o => o.classList.toggle('active', o === ch && !ch.classList.contains('active')));
    apply();
  }));
  apply();
}
