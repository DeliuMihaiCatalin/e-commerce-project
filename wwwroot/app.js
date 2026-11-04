const $ = id => document.getElementById(id);
const state = { page: 1, pageSize: 12 };

const money = n => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(n);
const esc = s => String(s ?? '').replace(/[&<>"']/g, c =>
  ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

async function loadFilters() {
  const res = await fetch('/api/products/filters');
  const f = await res.json();
  const parents = f.categories.filter(c => c.parentId == null);
  for (const parent of parents) {
    $('category').appendChild(new Option(parent.name, parent.id));
    for (const child of f.categories.filter(c => c.parentId === parent.id))
      $('category').appendChild(new Option('\u00A0\u00A0— ' + child.name, child.id));
  }
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
  add('categoryId', $('category').value);
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
          <button class="add" data-add="${p.id}" ${p.inStock ? '' : 'disabled'}>${p.inStock ? 'Add to cart' : 'Out of stock'}</button>
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
    <p><button id="addDetail" ${p.inStock ? '' : 'disabled'}>${p.inStock ? 'Add to cart' : 'Out of stock'}</button>
       <button id="closeDetail">Close</button></p>`;
  $('closeDetail').onclick = () => d.close();
  $('addDetail').onclick = () => addToCart(p.id);
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
  const addBtn = e.target.closest('[data-add]');
  if (addBtn) { addToCart(Number(addBtn.dataset.add)); return; }
  const card = e.target.closest('.card');
  if (card) showDetail(card.dataset.id);
});
$('detail').addEventListener('click', e => { if (e.target === $('detail')) $('detail').close(); });

// live search while typing (debounced)
let t;
$('q').addEventListener('input', () => { clearTimeout(t); t = setTimeout(refresh, 300); });

// ================= CART =================
let cart = { items: [], itemCount: 0, subtotal: 0, requiresLicence: false };

async function cartFetch(path, method = 'GET', body) {
  const res = await fetch('/api/cart' + path, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: body ? JSON.stringify(body) : undefined
  });
  const data = await res.json().catch(() => null);
  if (!res.ok) throw new Error(data?.message || 'Something went wrong with the cart.');
  return data;
}

let toastTimer;
function toast(msg, isError = false) {
  const t = $('toast');
  t.textContent = msg;
  t.className = 'toast show' + (isError ? ' error' : '');
  clearTimeout(toastTimer);
  toastTimer = setTimeout(() => t.className = 'toast', 2500);
}

function setCart(c) {
  cart = c;
  $('cartCount').textContent = c.itemCount;
  if ($('cart').open) renderCart();
}

async function loadCart() {
  try { setCart(await cartFetch('')); } catch { /* ignore on startup */ }
}

async function addToCart(productId, quantity = 1) {
  try {
    setCart(await cartFetch('/items', 'POST', { productId, quantity }));
    toast('Added to cart');
  } catch (err) { toast(err.message, true); }
}

async function setQuantity(productId, quantity) {
  try { setCart(await cartFetch('/items/' + productId, 'PUT', { quantity })); }
  catch (err) { toast(err.message, true); }
}

async function removeItem(productId) {
  try { setCart(await cartFetch('/items/' + productId, 'DELETE')); }
  catch (err) { toast(err.message, true); }
}

async function clearCart() {
  try { setCart(await cartFetch('', 'DELETE')); }
  catch (err) { toast(err.message, true); }
}

function renderCart() {
  const d = $('cart');
  if (cart.items.length === 0) {
    d.innerHTML = `<h2>Your cart</h2><p class="empty">Your cart is empty.</p>
      <div class="cart-actions"><button data-close>Continue shopping</button></div>`;
    return;
  }
  d.innerHTML = `
    <h2>Your cart (${cart.itemCount} item${cart.itemCount === 1 ? '' : 's'})</h2>
    ${cart.items.map(i => `
      <div class="line" data-pid="${i.productId}">
        <img src="${esc(i.imageUrl)}" alt="">
        <div>${esc(i.name)}<br><small>${esc(i.brand)} · ${money(i.unitPrice)} each</small></div>
        <div class="qty">
          <button data-dec aria-label="Decrease">−</button>
          <span>${i.quantity}</span>
          <button data-inc aria-label="Increase" ${i.quantity >= i.stockQuantity ? 'disabled' : ''}>+</button>
        </div>
        <div class="total">${money(i.lineTotal)}</div>
        <button class="remove" data-remove aria-label="Remove">✕</button>
      </div>`).join('')}
    <div class="cart-footer">
      <span>Subtotal</span><span class="subtotal">${money(cart.subtotal)}</span>
    </div>
    ${cart.requiresLicence ? '<p class="notice">⚠ A valid firearms licence is required for firearms and ammunition in your cart.</p>' : ''}
    <div class="cart-actions">
      <button data-clear style="background:#6b2222;color:#fff">Empty cart</button>
      <button data-close>Continue shopping</button>
      <button disabled title="Coming in week 5">Checkout (soon)</button>
    </div>`;
}

$('cartBtn').addEventListener('click', () => { renderCart(); $('cart').showModal(); });
$('cart').addEventListener('click', e => {
  if (e.target === $('cart') || e.target.closest('[data-close]')) { $('cart').close(); return; }
  if (e.target.closest('[data-clear]')) { clearCart(); return; }
  const line = e.target.closest('.line');
  if (!line) return;
  const pid = Number(line.dataset.pid);
  const item = cart.items.find(i => i.productId === pid);
  if (!item) return;
  if (e.target.closest('[data-inc]')) setQuantity(pid, item.quantity + 1);
  else if (e.target.closest('[data-dec]')) setQuantity(pid, item.quantity - 1);
  else if (e.target.closest('[data-remove]')) removeItem(pid);
});

loadCart();
loadFilters().then(loadProducts);
