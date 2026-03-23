/* ═══════════════════════════════════════════════════════════════════════════
   RestaurantMS — Touch POS Module  (pos-touch.js)
   ═══════════════════════════════════════════════════════════════════════════ */

'use strict';

// ── State ─────────────────────────────────────────────────────────────────────
const TouchState = {
    orderId:       null,
    items:         [],       // { itemId, menuItemId, name, price, quantity, notes }
    allItems:      [],
    activeCatId:   '',
    subtotal:      0,
    tax:           0,
    discount:      0,
    total:         0,
    heldCount:     0,
    isProcessing:  false,

    // Numpad context
    numpadMode:    'qty',    // 'qty' | 'cash'
    numpadTarget:  null,     // itemId when mode='qty'
    numpadValue:   '',

    // Modifier popup context
    pendingItem:   null,     // menu item waiting for modifier confirmation

    // Swipe tracking
    swipeStartX:   0,
    swipeEl:       null,
};

const TAX_RATE = 0.14;
const LONG_PRESS_MS = 500;

// ── CSRF helper ───────────────────────────────────────────────────────────────
function getCsrf() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
}

async function postJson(url, body) {
    const res = await fetch(url, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN':  getCsrf(),
        },
        body: JSON.stringify(body),
    });
    return res.json();
}

function fmt(n) {
    return parseFloat(n || 0).toFixed(3);
}

// ── Boot ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    initTouchPOS();
});

async function initTouchPOS() {
    tryFullscreen();
    await loadMenu();
    bindCartEvents();
    bindNumpad();
    bindModifierPopup();
    bindTopbarButtons();
    await createNewOrder();
    refreshHeldCount();
}

function tryFullscreen() {
    const el = document.documentElement;
    if (el.requestFullscreen) {
        el.requestFullscreen().catch(() => { /* user may deny */ });
    } else if (el.webkitRequestFullscreen) {
        el.webkitRequestFullscreen();
    }
}

// ── Menu ──────────────────────────────────────────────────────────────────────
async function loadMenu() {
    try {
        const res  = await fetch('/Pos/GetMenu');
        const data = await res.json();
        if (!data.success) return;

        TouchState.allItems = data.data ?? [];
        buildCategoryTabs();
        renderMenuGrid(TouchState.allItems);
    } catch (_) { /* silent */ }
}

function buildCategoryTabs() {
    const bar  = document.getElementById('touch-cat-bar');
    const seen = new Set();

    [...bar.querySelectorAll('.touch-cat-tab:not([data-id=""])')].forEach(el => el.remove());

    TouchState.allItems.forEach(item => {
        if (item.categoryId && !seen.has(item.categoryId)) {
            seen.add(item.categoryId);
            const btn = document.createElement('button');
            btn.className  = 'touch-cat-tab';
            btn.dataset.id = item.categoryId;
            btn.innerHTML  = `<i class="fas fa-tag"></i> ${escHtml(item.categoryName ?? '')}`;
            btn.addEventListener('click', () => filterByCategory(item.categoryId));
            bar.appendChild(btn);
        }
    });

    bar.querySelector('[data-id=""]')?.addEventListener('click', () => filterByCategory(''));
}

function filterByCategory(catId) {
    TouchState.activeCatId = catId;
    const filtered = catId
        ? TouchState.allItems.filter(i => String(i.categoryId) === String(catId))
        : [...TouchState.allItems];

    document.querySelectorAll('.touch-cat-tab').forEach(b => {
        b.classList.toggle('active', String(b.dataset.id) === String(catId));
    });

    const q = document.getElementById('touch-search').value.trim().toLowerCase();
    renderMenuGrid(q ? filtered.filter(i => (i.name ?? '').toLowerCase().includes(q)) : filtered);
}

function renderMenuGrid(items) {
    const grid = document.getElementById('touch-menu-grid');
    grid.innerHTML = '';

    items.forEach(item => {
        const card = document.createElement('div');
        card.className  = 'touch-item-card' + (item.isAvailable === false ? ' unavailable' : '');
        card.dataset.id = item.id;
        card.innerHTML  = `
            ${item.imageUrl
                ? `<img src="${escHtml(item.imageUrl)}" alt="" class="touch-item-img">`
                : `<span class="touch-item-icon">${escHtml(item.icon ?? '🍽️')}</span>`}
            <div class="touch-item-name">${escHtml(item.name ?? '')}</div>
            <div class="touch-item-price">${fmt(item.price)}</div>
        `;

        // Long-press → modifier popup; tap → add directly
        attachLongPress(card, item);
        card.addEventListener('click', () => addToCart(item, ''));
        grid.appendChild(card);
    });
}

// ── Long press ────────────────────────────────────────────────────────────────
function attachLongPress(el, item) {
    let timer = null;
    let fired  = false;

    function startPress(e) {
        fired = false;
        timer = setTimeout(() => {
            fired = true;
            el.classList.add('long-press');
            openModifierPopup(item);
        }, LONG_PRESS_MS);
    }

    function endPress(e) {
        clearTimeout(timer);
        setTimeout(() => el.classList.remove('long-press'), 200);
        if (fired) {
            // Prevent the click event from also firing
            e.stopImmediatePropagation();
            fired = false;
        }
    }

    el.addEventListener('touchstart',  startPress, { passive: true });
    el.addEventListener('touchend',    endPress);
    el.addEventListener('touchcancel', endPress);
    el.addEventListener('mousedown',   startPress);
    el.addEventListener('mouseup',     endPress);
    el.addEventListener('mouseleave',  endPress);
}

// ── Modifier popup ────────────────────────────────────────────────────────────
function openModifierPopup(item) {
    TouchState.pendingItem = item;
    document.getElementById('modifier-title').textContent = item.name ?? '';
    document.getElementById('modifier-notes').value       = '';
    document.getElementById('modifierPopup').style.display = '';
}

function bindModifierPopup() {
    document.getElementById('modifier-cancel').addEventListener('click', () => {
        document.getElementById('modifierPopup').style.display = 'none';
        TouchState.pendingItem = null;
    });

    document.getElementById('modifier-confirm').addEventListener('click', async () => {
        document.getElementById('modifierPopup').style.display = 'none';
        if (!TouchState.pendingItem) return;
        const notes = document.getElementById('modifier-notes').value.trim();
        await addToCart(TouchState.pendingItem, notes);
        TouchState.pendingItem = null;
    });
}

// ── Search ────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('touch-search')?.addEventListener('input', function () {
        const q    = this.value.trim().toLowerCase();
        const base = TouchState.activeCatId
            ? TouchState.allItems.filter(i => String(i.categoryId) === String(TouchState.activeCatId))
            : [...TouchState.allItems];
        renderMenuGrid(q ? base.filter(i => (i.name ?? '').toLowerCase().includes(q)) : base);
    });
});

// ── Order lifecycle ───────────────────────────────────────────────────────────
async function createNewOrder() {
    try {
        const orderType = parseInt(document.getElementById('touch-order-type').value, 10) || 1;
        const tableNum  = document.getElementById('touch-table-num').value.trim();
        const data      = await postJson('/Pos/CreateOrder', { orderType, tableNumber: tableNum });
        if (data.success) {
            TouchState.orderId = data.data.id;
            document.getElementById('touch-order-num').textContent = data.data.orderNumber ?? '';
        }
    } catch (_) { /* silent */ }
}

// ── Cart ──────────────────────────────────────────────────────────────────────
async function addToCart(item, notes) {
    if (!TouchState.orderId) await createNewOrder();
    if (TouchState.isProcessing) return;
    TouchState.isProcessing = true;

    try {
        const existing = notes === ''
            ? TouchState.items.find(i => i.menuItemId === item.id && !i.notes)
            : null;

        if (existing) {
            await updateQuantity(existing.itemId, existing.quantity + 1);
        } else {
            const data = await postJson('/Pos/AddItem', {
                orderId:    TouchState.orderId,
                menuItemId: item.id,
                quantity:   1,
                notes:      notes,
            });
            if (data.success) {
                TouchState.items.push({
                    itemId:     data.data.id,
                    menuItemId: item.id,
                    name:       item.name,
                    price:      item.price,
                    quantity:   1,
                    notes:      notes,
                });
                recalcTotals();
                renderCart();
            }
        }
    } finally {
        TouchState.isProcessing = false;
    }
}

async function removeFromCart(itemId) {
    if (!TouchState.orderId) return;
    const el = document.querySelector(`.cart-item[data-item-id="${itemId}"]`);
    if (el) {
        el.classList.add('removing');
        await new Promise(r => setTimeout(r, 220));
    }
    const data = await postJson('/Pos/RemoveItem', { orderId: TouchState.orderId, itemId });
    if (data.success) {
        TouchState.items = TouchState.items.filter(i => i.itemId !== itemId);
        recalcTotals();
        renderCart();
    }
}

async function updateQuantity(itemId, qty) {
    if (!TouchState.orderId) return;
    if (qty <= 0) { await removeFromCart(itemId); return; }
    const data = await postJson('/Pos/UpdateQty', { orderId: TouchState.orderId, itemId, quantity: qty });
    if (data.success) {
        const row = TouchState.items.find(i => i.itemId === itemId);
        if (row) row.quantity = qty;
        recalcTotals();
        renderCart();
    }
}

function recalcTotals() {
    TouchState.subtotal = TouchState.items.reduce((s, i) => s + i.price * i.quantity, 0);
    TouchState.tax      = TouchState.subtotal * TAX_RATE;
    TouchState.total    = Math.max(0, TouchState.subtotal + TouchState.tax - TouchState.discount);

    document.getElementById('touch-subtotal').textContent = fmt(TouchState.subtotal);
    document.getElementById('touch-tax').textContent      = fmt(TouchState.tax);
    document.getElementById('touch-discount').textContent = fmt(TouchState.discount);
    document.getElementById('touch-total').textContent    = fmt(TouchState.total);

    const cnt = TouchState.items.reduce((s, i) => s + i.quantity, 0);
    document.getElementById('touch-item-count').textContent = cnt;
}

function renderCart() {
    const area  = document.getElementById('touch-cart-items');
    const empty = document.getElementById('touch-cart-empty');
    area.querySelectorAll('.cart-item').forEach(el => el.remove());

    if (!TouchState.items.length) {
        empty.style.display = '';
        return;
    }
    empty.style.display = 'none';

    TouchState.items.forEach(item => {
        const row = document.createElement('div');
        row.className         = 'cart-item';
        row.dataset.itemId    = item.itemId;
        row.innerHTML         = `
            <div class="swipe-delete-reveal"><i class="fas fa-trash"></i></div>
            <div class="cart-item-name-t" title="${escHtml(item.name)}">${escHtml(item.name)}</div>
            <div class="touch-qty-stepper">
                <button class="touch-qty-btn t-minus" data-id="${item.itemId}">−</button>
                <span class="touch-qty-val">${item.quantity}</span>
                <button class="touch-qty-btn t-plus"  data-id="${item.itemId}">+</button>
            </div>
            <div class="cart-item-price-t">${fmt(item.price * item.quantity)}</div>
        `;

        // Tap on qty value → open numpad
        row.querySelector('.touch-qty-val').addEventListener('click', () => {
            openNumpad('qty', item.itemId, item.quantity);
        });

        attachSwipeDelete(row, item.itemId);
        area.insertBefore(row, empty);
    });

    area.scrollTop = area.scrollHeight;
}

// ── Swipe-to-delete ───────────────────────────────────────────────────────────
function attachSwipeDelete(el, itemId) {
    let startX = 0;
    let startY = 0;

    el.addEventListener('touchstart', e => {
        startX = e.touches[0].clientX;
        startY = e.touches[0].clientY;
    }, { passive: true });

    el.addEventListener('touchmove', e => {
        const dx = e.touches[0].clientX - startX;
        const dy = e.touches[0].clientY - startY;
        if (Math.abs(dy) > Math.abs(dx)) return; // vertical scroll priority
        if (dx < -10) {
            el.classList.add('swiping-left');
        } else {
            el.classList.remove('swiping-left');
        }
    }, { passive: true });

    el.addEventListener('touchend', async e => {
        const dx = e.changedTouches[0].clientX - startX;
        if (dx < -60) {
            await removeFromCart(itemId);
        } else {
            el.classList.remove('swiping-left');
        }
    });
}

// ── Cart event delegation ─────────────────────────────────────────────────────
function bindCartEvents() {
    const area = document.getElementById('touch-cart-items');

    area.addEventListener('click', async e => {
        const minus  = e.target.closest('.t-minus');
        const plus   = e.target.closest('.t-plus');
        if (minus) {
            const id   = parseInt(minus.dataset.id, 10);
            const item = TouchState.items.find(i => i.itemId === id);
            if (item) await updateQuantity(id, item.quantity - 1);
        } else if (plus) {
            const id   = parseInt(plus.dataset.id, 10);
            const item = TouchState.items.find(i => i.itemId === id);
            if (item) await updateQuantity(id, item.quantity + 1);
        }
    });

    // Payment buttons
    document.getElementById('touch-cash-btn').addEventListener('click', () => openCashNumpad());
    document.getElementById('touch-card-btn').addEventListener('click', () => payCard());
    document.getElementById('touch-split-btn').addEventListener('click', () => paySplit());

    // Action buttons
    document.getElementById('touch-clear-btn').addEventListener('click', clearCart);
    document.getElementById('touch-hold-btn').addEventListener('click',  holdOrder);
    document.getElementById('touch-void-btn').addEventListener('click',  voidOrder);
}

// ── Topbar buttons ────────────────────────────────────────────────────────────
function bindTopbarButtons() {
    document.getElementById('touch-held-btn').addEventListener('click', openHeldOrders);
}

// ── Numpad ────────────────────────────────────────────────────────────────────
function openNumpad(mode, targetId, initial) {
    TouchState.numpadMode   = mode;
    TouchState.numpadTarget = targetId;
    TouchState.numpadValue  = String(initial ?? '');

    const overlay = document.getElementById('numpadOverlay');
    const title   = document.getElementById('numpad-title');
    const display = document.getElementById('numpad-display');
    const cashBtns = document.getElementById('cash-quick-btns');

    if (mode === 'cash') {
        title.textContent = window.AppStrings?.enterAmount ?? 'Enter Amount';
        cashBtns.style.display = 'grid';
        TouchState.numpadValue = fmt(TouchState.total);
    } else {
        title.textContent = window.AppStrings?.enterQty ?? 'Enter Quantity';
        cashBtns.style.display = 'none';
    }

    display.textContent = TouchState.numpadValue || '0';
    overlay.style.display = '';
}

function openCashNumpad() {
    if (!TouchState.items.length) return;
    openNumpad('cash', null, TouchState.total);
}

function bindNumpad() {
    const grid    = document.querySelector('.numpad-grid');
    const display = document.getElementById('numpad-display');
    const overlay = document.getElementById('numpadOverlay');

    grid.addEventListener('click', e => {
        const btn = e.target.closest('.numpad-btn');
        if (!btn) return;
        const k = btn.dataset.k;

        if (k === 'clear') {
            TouchState.numpadValue = '';
        } else if (k === 'back') {
            TouchState.numpadValue = TouchState.numpadValue.slice(0, -1);
        } else {
            if (TouchState.numpadValue.length >= 8) return;
            TouchState.numpadValue += k;
        }
        display.textContent = TouchState.numpadValue || '0';
    });

    // Quick cash shortcuts
    document.querySelectorAll('[data-quick]').forEach(btn => {
        btn.addEventListener('click', () => {
            TouchState.numpadValue = btn.dataset.quick;
            display.textContent    = TouchState.numpadValue;
        });
    });

    document.getElementById('numpad-close').addEventListener('click', () => {
        overlay.style.display = 'none';
    });

    document.getElementById('numpad-confirm').addEventListener('click', async () => {
        overlay.style.display = 'none';
        const val = parseFloat(TouchState.numpadValue) || 0;

        if (TouchState.numpadMode === 'qty') {
            if (val <= 0) {
                await removeFromCart(TouchState.numpadTarget);
            } else {
                await updateQuantity(TouchState.numpadTarget, val);
            }
        } else if (TouchState.numpadMode === 'cash') {
            await processPayment('cash', val);
        }
    });
}

// ── Payment ───────────────────────────────────────────────────────────────────
async function payCard() {
    if (!TouchState.orderId || !TouchState.items.length) return;
    const ok = await touchConfirm(`CARD\n${window.AppStrings?.total ?? 'Total'}: ${fmt(TouchState.total)}`);
    if (!ok) return;
    await processPayment('card', null);
}

async function paySplit() {
    if (!TouchState.orderId || !TouchState.items.length) return;
    await processPayment('split', null);
}

async function processPayment(mode, cashReceived) {
    if (TouchState.isProcessing) return;
    TouchState.isProcessing = true;

    try {
        const methodMap = { cash: 1, card: 2, split: 3 };
        const data = await postJson('/Pos/ProcessPayment', {
            orderId:        TouchState.orderId,
            paymentMethod:  methodMap[mode] ?? 1,
            cashReceived:   cashReceived,
            discountAmount: TouchState.discount,
            managerPin:     '',
        });

        if (data.success) {
            const change = data.data?.changeGiven ?? 0;
            if (change > 0) {
                await touchConfirm(`${window.AppStrings?.change ?? 'Change'}: ${fmt(change)}`, true);
            } else {
                showToast(window.AppStrings?.paymentSuccess ?? 'Payment complete!', 'success');
            }
            resetCart();
            await createNewOrder();
        } else {
            showToast(data.message ?? 'Payment failed', 'error');
        }
    } finally {
        TouchState.isProcessing = false;
    }
}

// ── Clear / Hold / Void ───────────────────────────────────────────────────────
async function clearCart() {
    if (!TouchState.items.length) return;
    const ok = await touchConfirm(window.AppStrings?.confirmClear ?? 'Clear cart?');
    if (!ok) return;
    resetCart();
    await createNewOrder();
}

async function holdOrder() {
    if (!TouchState.orderId || !TouchState.items.length) return;
    const data = await postJson('/Pos/HoldOrder', { orderId: TouchState.orderId });
    if (data.success) {
        showToast(window.AppStrings?.orderHeld ?? 'Order held', 'success');
        resetCart();
        await createNewOrder();
        refreshHeldCount();
    } else {
        showToast(data.message ?? 'Hold failed', 'error');
    }
}

async function voidOrder() {
    if (!TouchState.orderId) return;
    const ok = await touchConfirm(window.AppStrings?.confirmVoid ?? 'Void this order?');
    if (!ok) return;
    const data = await postJson('/Pos/VoidOrder', {
        orderId:    TouchState.orderId,
        reason:     'Touch POS void',
        managerPin: '',
    });
    if (data.success) {
        showToast(window.AppStrings?.orderVoided ?? 'Order voided', 'success');
        resetCart();
        await createNewOrder();
    } else {
        showToast(data.message ?? 'Void failed', 'error');
    }
}

function resetCart() {
    TouchState.orderId  = null;
    TouchState.items    = [];
    TouchState.discount = 0;
    document.getElementById('touch-order-num').textContent =
        window.AppStrings?.newOrder ?? 'New Order';
    recalcTotals();
    renderCart();
}

// ── Held orders ───────────────────────────────────────────────────────────────
async function openHeldOrders() {
    try {
        const res  = await fetch('/Pos/GetActiveOrders');
        const data = await res.json();
        const held = (data.data ?? []).filter(o => o.status === 4);

        if (!held.length) {
            showToast(window.AppStrings?.noHeldOrders ?? 'No held orders', 'info');
            return;
        }

        // Build a simple inline picker
        const listHtml = held.map(o =>
            `<button class="touch-btn btn btn-outline-light w-100 mb-2 recall-touch-btn"
                     data-id="${o.id}" style="min-height:56px;justify-content:flex-start;gap:12px">
                <i class="fas fa-receipt"></i>
                <span>${escHtml(o.orderNumber ?? '')} — ${escHtml(o.tableNumber ?? '')} (${o.itemCount ?? 0} items)</span>
             </button>`
        ).join('');

        const overlay = document.createElement('div');
        overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.8);z-index:2000;display:flex;align-items:center;justify-content:center;padding:20px';
        overlay.innerHTML = `
            <div style="background:#16213e;border-radius:16px;padding:20px;width:100%;max-width:400px;max-height:80vh;overflow-y:auto">
                <div class="d-flex justify-content-between align-items-center mb-3">
                    <h5 class="mb-0 text-white">${window.AppStrings?.heldOrders ?? 'Held Orders'}</h5>
                    <button class="btn btn-sm btn-outline-light" id="close-held-overlay"><i class="fas fa-times"></i></button>
                </div>
                ${listHtml}
            </div>
        `;

        document.body.appendChild(overlay);

        overlay.querySelector('#close-held-overlay').addEventListener('click', () => overlay.remove());

        overlay.querySelectorAll('.recall-touch-btn').forEach(btn => {
            btn.addEventListener('click', async () => {
                overlay.remove();
                await recallOrder(parseInt(btn.dataset.id, 10));
            });
        });

    } catch (_) {
        showToast('Failed to load held orders', 'error');
    }
}

async function recallOrder(orderId) {
    const data = await postJson('/Pos/RecallOrder', { orderId });
    if (data.success) {
        TouchState.orderId = orderId;
        const det = await (await fetch(`/Pos/GetOrderDetails?orderId=${orderId}`)).json();
        if (det.success) {
            TouchState.items = (det.data?.items ?? []).map(i => ({
                itemId:     i.id,
                menuItemId: i.menuItemId,
                name:       i.name,
                price:      i.unitPrice,
                quantity:   i.quantity,
                notes:      i.notes ?? '',
            }));
            document.getElementById('touch-order-num').textContent = det.data.orderNumber ?? '';
            recalcTotals();
            renderCart();
        }
        refreshHeldCount();
    } else {
        showToast(data.message ?? 'Recall failed', 'error');
    }
}

async function refreshHeldCount() {
    try {
        const res  = await fetch('/Pos/GetActiveOrders');
        const data = await res.json();
        const cnt  = (data.data ?? []).filter(o => o.status === 4).length;
        TouchState.heldCount = cnt;
        const badge = document.getElementById('touch-held-count');
        if (cnt > 0) {
            badge.textContent   = cnt;
            badge.style.display = '';
        } else {
            badge.style.display = 'none';
        }
    } catch (_) { /* silent */ }
}

// ── Utility UI ────────────────────────────────────────────────────────────────
function touchConfirm(msg, okOnly = false) {
    return new Promise(resolve => {
        const overlay = document.createElement('div');
        overlay.style.cssText = 'position:fixed;inset:0;background:rgba(0,0,0,0.75);z-index:3000;display:flex;align-items:center;justify-content:center;padding:20px';
        overlay.innerHTML = `
            <div style="background:#1a1a2e;border-radius:16px;padding:24px;min-width:260px;max-width:320px;text-align:center;color:#fff">
                <p style="font-size:1.1rem;margin-bottom:20px;white-space:pre-line">${escHtml(msg)}</p>
                <div style="display:flex;gap:10px;justify-content:center">
                    ${okOnly ? '' : `<button class="touch-btn btn btn-secondary flex-fill" id="tc-no" style="min-height:52px">
                        ${window.AppStrings?.no ?? 'No'}
                    </button>`}
                    <button class="touch-btn btn btn-success flex-fill" id="tc-yes" style="min-height:52px">
                        ${window.AppStrings?.yes ?? 'OK'}
                    </button>
                </div>
            </div>
        `;
        document.body.appendChild(overlay);
        overlay.querySelector('#tc-yes').addEventListener('click', () => { overlay.remove(); resolve(true); });
        overlay.querySelector('#tc-no')?.addEventListener('click', () => { overlay.remove(); resolve(false); });
    });
}

function showToast(msg, type = 'success') {
    if (window.toastr) {
        toastr[type]?.(msg) ?? toastr.info(msg);
        return;
    }
    // Fallback
    const el = document.createElement('div');
    el.style.cssText = `position:fixed;bottom:20px;left:50%;transform:translateX(-50%);
        background:${type === 'success' ? '#27ae60' : type === 'error' ? '#e74c3c' : '#2980b9'};
        color:#fff;padding:12px 24px;border-radius:10px;z-index:9999;font-weight:600;font-size:15px`;
    el.textContent = msg;
    document.body.appendChild(el);
    setTimeout(() => el.remove(), 2500);
}

// ── XSS-safe HTML escape ──────────────────────────────────────────────────────
function escHtml(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#39;');
}
