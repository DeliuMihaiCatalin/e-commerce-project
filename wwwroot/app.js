const $ = id => document.getElementById(id);
const state = { page: 1, pageSize: 12 };

const money = n => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n);
const esc = s => String(s ?? '').replace(/[&<>"']/g, c =>
  ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

async function loadFilters() {
  const res = await fetch('/api/products/filters');
  const f = await res.json();
  fill($('category'), f.categories);
  fill($('brand'), f.brands);
  fill($('caliber'), f.calibers);
}

function fill(select, values) {
  for (const v of values) {
    const o = document.createElement('option');
    o.value = v; o.textContent = v;
    select.appendChild(o);
  }
}

function buildQuery() {
  const p = new URLSearchParams();
  const add = (k, v) => { if (v !== '' && v != null) p.set(k, v); };
  add('q', $('q').value.trim());
  add('category', $('category').value);
  add('brand', $('brand').value);
  add('caliber', $('caliber').value);
  add('minPrice', $('minPrice').value);
  add('maxPrice', $('maxPrice').value);
  if ($('inStockOnly').checked) p.set('inStockOnly', 'true');
  p.set('sort', $('sort').value);
  p.set('page', state.page);
  p.set('pageSize', state.pageSize);
  return p.toString();
}

async function loadProducts() {
  const res = await fetch('/api/products?' + buildQuery());
  if (!res.ok) { $('grid').innerHTML = '<p class="empty">Could not load products.</p>'; return; }
  const data = await res.json();
  render(data);
}

function render(data) {
  $('count').textContent = `${data.totalCount} product${data.totalCount === 1 ? '' : 's'}`;
  const grid = $('grid');

  if (data.items.length === 0) {
    grid.innerHTML = '<p class="empty">No products match your search.</p>';
  } else {
    grid.innerHTML = data.items.map(p => `
      <article class="card" data-id="${p.id}">
        <img src="${esc(p.imageUrl)}" alt="${esc(p.name)}" loading="lazy">
        <div class="body">
          <span class="brand">${esc(p.brand)}</span>
          <span class="name">${esc(p.name)}</span>
          <span>
            <span class="tag">${esc(p.category)}</span>
            ${p.caliber ? `<span class="tag">${esc(p.caliber)}</span>` : ''}
            <span class="tag ${p.inStock ? 'ok' : 'out'}">${p.inStock ? 'In stock' : 'Out of stock'}</span>
          </span>
          <span class="price">${money(p.price)}</span>
        </div>
      </article>`).join('');
  }

  const totalPages = Math.max(1, data.totalPages);
  $('pager').innerHTML = `
    <button id="prev" ${data.page <= 1 ? 'disabled' : ''}>‹ Prev</button>
    <span>Page ${data.page} of ${totalPages}</span>
    <button id="next" ${data.page >= totalPages ? 'disabled' : ''}>Next ›</button>`;
  $('prev').onclick = () => { state.page--; loadProducts(); window.scrollTo(0, 0); };
  $('next').onclick = () => { state.page++; loadProducts(); window.scrollTo(0, 0); };
}

async function showDetail(id) {
  const res = await fetch('/api/products/' + id);
  if (!res.ok) return;
  const p = await res.json();
  const d = $('detail');
  d.innerHTML = `
    <img src="${esc(p.imageUrl)}" alt="${esc(p.name)}">
    <h2>${esc(p.name)}</h2>
    <p><span class="tag">${esc(p.category)}</span>
       ${p.caliber ? `<span class="tag">${esc(p.caliber)}</span>` : ''}
       <span class="tag">${esc(p.brand)}</span>
       <span class="tag ${p.inStock ? 'ok' : 'out'}">${p.inStock ? p.stockQuantity + ' in stock' : 'Out of stock'}</span></p>
    <p>${esc(p.description)}</p>
    <p class="price" style="font-size:1.4rem;color:var(--accent);font-weight:700">${money(p.price)}</p>
    ${p.requiresLicence ? '<p class="notice">⚠ A valid firearms licence is required to purchase this item.</p>' : ''}
    <button id="closeDetail">Close</button>`;
  $('closeDetail').onclick = () => d.close();
  d.showModal();
}

// ---- events ----
function refresh() { state.page = 1; loadProducts(); }

$('searchForm').addEventListener('submit', e => { e.preventDefault(); refresh(); });
for (const id of ['category', 'brand', 'caliber', 'inStockOnly', 'sort'])
  $(id).addEventListener('change', refresh);
for (const id of ['minPrice', 'maxPrice'])
  $(id).addEventListener('change', refresh);

$('reset').addEventListener('click', () => {
  $('q').value = '';
  for (const id of ['category', 'brand', 'caliber', 'minPrice', 'maxPrice']) $(id).value = '';
  $('inStockOnly').checked = false;
  $('sort').value = 'name';
  refresh();
});

$('grid').addEventListener('click', e => {
  const card = e.target.closest('.card');
  if (card) showDetail(card.dataset.id);
});
$('detail').addEventListener('click', e => { if (e.target === $('detail')) $('detail').close(); });

// live search while typing (debounced)
let t;
$('q').addEventListener('input', () => { clearTimeout(t); t = setTimeout(refresh, 300); });

loadFilters().then(loadProducts);
