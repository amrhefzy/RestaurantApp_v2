/* ═══════════════════════════════════════════════════════════════════════════
   RestaurantMS — Standard POS Module  (pos.js)
   ═══════════════════════════════════════════════════════════════════════════ */

'use strict';

// ── State ─────────────────────────────────────────────────────────────────────
const PosState = {
    orderId:      null,
    items:        [],        // { itemId, menuItemId, name, price, quantity, notes }
    categories:   [],
    allItems:     [],
    filteredItems:[],
    activeCatId:  '',
    subtotal:     0,
    tax:          0,
    discount:     0,
    total:        0,
    paymentMode:  'cash',   // 'cash'|'card'|'split'
    cashEntered:  '',
    splitCount:   2,
    heldOrders:   [],
    isProcessing: false,
};

const TAX_RATE = 0.14;

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

// ── Currency ──────────────────────────────────────────────────────────────────
function fmt(n) {
    return parseFloat(n || 0).toFixed(3);
}

// ── Boot ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', () => {
    initPOS();
});

async function initPOS() {
    await loadMenu();
    bindCartEvents();
    bindSearchEvents();
    bindPaymentModal();
    bindHeldOrdersModal();
    bindKeyboardShortcuts();
    await createNewOrder();
}

// ── Menu ──────────────────────────────────────────────────────────────────────
async function loadMenu() {
    try {
        const res = await fetch('/Pos/GetMenu');
        const data = await res.json();
        if (!data.success) return;

        PosState.allItems      = data.data ?? [];
        PosState.filteredItems = [...PosState.allItems];

        buildCategoryTabs();
        renderMenuGrid(PosState.allItems);
    } catch (e) {
        App.error(window.AppStrings?.error ?? 'Error loading menu');
    }
}

function buildCategoryTabs() {
    const bar  = document.getElementById('categoryTabs');
    const seen = new Set();

    // Keep the "All" tab
    [...bar.querySelectorAll('.category-tab:not([data-id=""])')].forEach(el => el.remove());

    PosState.allItems.forEach(item => {
        if (item.categoryId && !seen.has(item.categoryId)) {
            seen.add(item.categoryId);
            const btn = document.createElement('button');
            btn.className    = 'category-tab';
            btn.dataset.id   = item.categoryId;
            btn.dataset.name = item.categoryName ?? '';
            btn.innerHTML    = `<i class="fas fa-tag"></i> ${escHtml(item.categoryName ?? '')}`;
            btn.addEventListener('click', () => filterByCategory(item.categoryId));
            bar.appendChild(btn);
        }
    });

    bar.querySelector('[data-id=""]')?.addEventListener('click', () => filterByCategory(''));
}

function filterByCategory(catId) {
    PosState.activeCatId   = catId;
    PosState.filteredItems = catId
        ? PosState.allItems.filter(i => String(i.categoryId) === String(catId))
        : [...PosState.allItems];

    document.querySelectorAll('.category-tab').forEach(b => {
        b.classList.toggle('active', String(b.dataset.id) === String(catId));
    });

    const search = document.getElementById('menuSearch').value.trim().toLowerCase();
    renderMenuGrid(search
        ? PosState.filteredItems.filter(i => (i.name ?? '').toLowerCase().includes(search))
        : PosState.filteredItems);
}

function renderMenuGrid(items) {
    const grid = document.getElementById('menuGrid');
    grid.innerHTML = '';

    if (!items.length) {
        grid.innerHTML = `<div class="col-span-3 text-center text-muted py-4">
            <i class="fas fa-search fa-2x mb-2 d-block opacity-50"></i>
            ${window.AppStrings?.noItems ?? 'No items found'}
        </div>`;
        return;
    }

    items.forEach(item => {
        const card = document.createElement('div');
        card.className  = 'menu-item-card' + (item.isAvailable === false ? ' unavailable' : '');
        card.dataset.id = item.id;
        card.innerHTML  = `
            ${item.imageUrl
                ? `<img src="${escHtml(item.imageUrl)}" alt="" class="item-img">`
                : `<span class="item-icon">${escHtml(item.icon ?? '🍽️')}</span>`}
            <div class="item-name">${escHtml(item.name ?? '')}</div>
            <div class="item-price">${fmt(item.price)}</div>
        `;
        card.addEventListener('click', () => addToCart(item));
        grid.appendChild(card);
    });
}

// ── Order lifecycle ───────────────────────────────────────────────────────────
async function createNewOrder() {
    try {
        const orderType = parseInt(document.getElementById('orderType').value, 10) || 1;
        const tableNum  = document.getElementById('tableNumber').value.trim();
        const data      = await postJson('/Pos/CreateOrder', { orderType, tableNumber: tableNum });
        if (data.success) {
            PosState.orderId = data.data.id;
            document.getElementById('orderNumberDisplay').textContent = data.data.orderNumber ?? '';
        }
    } catch (e) {
        App.error('Failed to create order');
    }
}

// ── Cart ──────────────────────────────────────────────────────────────────────
async function addToCart(item) {
    if (!PosState.orderId) await createNewOrder();
    if (PosState.isProcessing) return;
    PosState.isProcessing = true;

    try {
        const existing = PosState.items.find(i => i.menuItemId === item.id && !i.notes);
        if (existing) {
            await updateQuantity(existing.itemId, existing.quantity + 1);
        } else {
            const data = await postJson('/Pos/AddItem', {
                orderId:    PosState.orderId,
                menuItemId: item.id,
                quantity:   1,
                notes:      '',
            });
            if (data.success) {
                PosState.items.push({
                    itemId:     data.data.id,
                    menuItemId: item.id,
                    name:       item.name,
                    price:      item.price,
                    quantity:   1,
                    notes:      '',
                });
                recalculateTotals();
                renderCart();
            } else {
                App.error(data.message ?? 'Failed to add item');
            }
        }
    } finally {
        PosState.isProcessing = false;
    }
}

async function removeFromCart(itemId) {
    if (!PosState.orderId) return;
    const data = await postJson('/Pos/RemoveItem', { orderId: PosState.orderId, itemId });
    if (data.success) {
        PosState.items = PosState.items.filter(i => i.itemId !== itemId);
        recalculateTotals();
        renderCart();
    } else {
        App.error(data.message ?? 'Failed to remove item');
    }
}

async function updateQuantity(itemId, qty) {
    if (!PosState.orderId) return;
    if (qty <= 0) { await removeFromCart(itemId); return; }

    const data = await postJson('/Pos/UpdateQty', { orderId: PosState.orderId, itemId, quantity: qty });
    if (data.success) {
        const row = PosState.items.find(i => i.itemId === itemId);
        if (row) row.quantity = qty;
        recalculateTotals();
        renderCart();
    } else {
        App.error(data.message ?? 'Failed to update quantity');
    }
}

function recalculateTotals() {
    const discount = parseFloat(document.getElementById('discountInput').value) || 0;
    PosState.discount = discount;
    PosState.subtotal = PosState.items.reduce((s, i) => s + i.price * i.quantity, 0);
    PosState.tax      = PosState.subtotal * TAX_RATE;
    PosState.total    = Math.max(0, PosState.subtotal + PosState.tax - PosState.discount);

    document.getElementById('subtotalDisplay').textContent = fmt(PosState.subtotal);
    document.getElementById('taxDisplay').textContent      = fmt(PosState.tax);
    document.getElementById('totalDisplay').textContent    = fmt(PosState.total);
}

function renderCart() {
    const area  = document.getElementById('cartItems');
    const empty = document.getElementById('cartEmptyMsg');
    area.querySelectorAll('.cart-item-row').forEach(el => el.remove());

    if (!PosState.items.length) {
        empty.style.display = '';
        document.getElementById('orderStatusBadge').style.display = 'none';
        return;
    }

    empty.style.display = 'none';

    PosState.items.forEach(item => {
        const row = document.createElement('div');
        row.className      = 'cart-item-row';
        row.dataset.itemId = item.itemId;
        row.innerHTML      = `
            <div class="cart-item-name" title="${escHtml(item.name)}">${escHtml(item.name)}</div>
            <div class="qty-stepper">
                <button class="btn btn-outline-secondary btn-sm qty-btn-minus" data-id="${item.itemId}">−</button>
                <span class="qty-val">${item.quantity}</span>
                <button class="btn btn-outline-secondary btn-sm qty-btn-plus" data-id="${item.itemId}">+</button>
            </div>
            <div class="cart-item-price">${fmt(item.price * item.quantity)}</div>
            <button class="btn-remove-item" data-id="${item.itemId}"><i class="fas fa-times"></i></button>
        `;
        area.insertBefore(row, empty);
    });

    // Badge count
    document.getElementById('orderStatusBadge').style.display = 'none';

    // Scroll to bottom
    area.scrollTop = area.scrollHeight;
}

function bindCartEvents() {
    const area = document.getElementById('cartItems');

    area.addEventListener('click', async e => {
        const minus  = e.target.closest('.qty-btn-minus');
        const plus   = e.target.closest('.qty-btn-plus');
        const remove = e.target.closest('.btn-remove-item');

        if (minus) {
            const id   = parseInt(minus.dataset.id, 10);
            const item = PosState.items.find(i => i.itemId === id);
            if (item) await updateQuantity(id, item.quantity - 1);
        } else if (plus) {
            const id   = parseInt(plus.dataset.id, 10);
            const item = PosState.items.find(i => i.itemId === id);
            if (item) await updateQuantity(id, item.quantity + 1);
        } else if (remove) {
            const id = parseInt(remove.dataset.id, 10);
            await removeFromCart(id);
        }
    });

    document.getElementById('discountInput').addEventListener('input', recalculateTotals);

    document.getElementById('btnClear').addEventListener('click', clearCart);
    document.getElementById('btnHold').addEventListener('click',  holdOrder);
    document.getElementById('btnVoid').addEventListener('click',  voidOrder);
    document.getElementById('btnKitchen').addEventListener('click', sendToKitchen);

    document.getElementById('btnCash').addEventListener('click',  () => openPaymentModal('cash'));
    document.getElementById('btnCard').addEventListener('click',  () => openPaymentModal('card'));
    document.getElementById('btnSplit').addEventListener('click', () => openPaymentModal('split'));
}

// ── Search ────────────────────────────────────────────────────────────────────
function bindSearchEvents() {
    const search  = document.getElementById('menuSearch');
    const barcode = document.getElementById('barcodeInput');

    search.addEventListener('input', () => {
        const q = search.value.trim().toLowerCase();
        const base = PosState.activeCatId
            ? PosState.allItems.filter(i => String(i.categoryId) === String(PosState.activeCatId))
            : [...PosState.allItems];
        renderMenuGrid(q ? base.filter(i => (i.name ?? '').toLowerCase().includes(q)) : base);
    });

    barcode.addEventListener('keydown', async e => {
        if (e.key !== 'Enter') return;
        const code = barcode.value.trim();
        if (!code) return;
        barcode.value = '';
        await barcodeSearch(code);
    });
}

async function barcodeSearch(code) {
    try {
        const res  = await fetch(`/Pos/GetItemByBarcode?barcode=${encodeURIComponent(code)}`);
        const data = await res.json();
        if (data.success && data.data) {
            await addToCart(data.data);
        } else {
            App.warning(`Barcode not found: ${code}`);
        }
    } catch (e) {
        App.error('Barcode lookup failed');
    }
}

// ── Clear / Hold / Void / Kitchen ─────────────────────────────────────────────
async function clearCart() {
    if (!PosState.items.length) return;
    const ok = await App.confirm(window.AppStrings?.confirmClear ?? 'Clear all items?');
    if (!ok) return;
    PosState.items    = [];
    PosState.discount = 0;
    document.getElementById('discountInput').value = 0;
    recalculateTotals();
    renderCart();
    await createNewOrder();
}

async function holdOrder() {
    if (!PosState.orderId || !PosState.items.length) return;
    const data = await postJson('/Pos/HoldOrder', { orderId: PosState.orderId });
    if (data.success) {
        App.success(window.AppStrings?.orderHeld ?? 'Order held');
        resetCart();
        await createNewOrder();
        refreshHeldCount();
    } else {
        App.error(data.message ?? 'Hold failed');
    }
}

async function voidOrder() {
    if (!PosState.orderId) return;
    const reason = await App.pinPrompt(window.AppStrings?.voidReason ?? 'Void reason + Manager PIN');
    if (!reason) return;

    const data = await postJson('/Pos/VoidOrder', {
        orderId:    PosState.orderId,
        reason:     reason.reason ?? reason,
        managerPin: reason.pin ?? '',
    });
    if (data.success) {
        App.success(window.AppStrings?.orderVoided ?? 'Order voided');
        resetCart();
        await createNewOrder();
    } else {
        App.error(data.message ?? 'Void failed');
    }
}

async function sendToKitchen() {
    App.info(window.AppStrings?.sentToKitchen ?? 'Sent to kitchen');
}

function resetCart() {
    PosState.orderId  = null;
    PosState.items    = [];
    PosState.discount = 0;
    document.getElementById('discountInput').value = 0;
    document.getElementById('orderNumberDisplay').textContent =
        window.AppStrings?.newOrder ?? 'New Order';
    recalculateTotals();
    renderCart();
}

// ── Payment modal ─────────────────────────────────────────────────────────────
function bindPaymentModal() {
    const modal = document.getElementById('paymentModal');

    // Numpad
    document.getElementById('cashNumpad').addEventListener('click', e => {
        const btn = e.target.closest('.numpad-btn');
        if (!btn) return;
        const key = btn.dataset.key;
        if (key === 'back') {
            PosState.cashEntered = PosState.cashEntered.slice(0, -1);
        } else {
            if (PosState.cashEntered.includes('.') && key === '.') return;
            if (PosState.cashEntered.length >= 10) return;
            PosState.cashEntered += key;
        }
        updateCashDisplay();
    });

    // Quick cash buttons
    document.querySelectorAll('.quick-cash-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            if (btn.dataset.exact === 'true') {
                PosState.cashEntered = fmt(PosState.total);
            } else {
                const add = parseFloat(btn.dataset.add || 0);
                const cur = parseFloat(PosState.cashEntered || PosState.total);
                PosState.cashEntered = fmt(cur + add);
            }
            updateCashDisplay();
        });
    });

    // Split controls
    document.getElementById('splitMinus').addEventListener('click', () => {
        if (PosState.splitCount > 2) { PosState.splitCount--; renderSplitAmounts(); }
    });
    document.getElementById('splitPlus').addEventListener('click', () => {
        PosState.splitCount++;
        renderSplitAmounts();
    });

    // Confirm
    document.getElementById('confirmPaymentBtn').addEventListener('click', processPayment);
}

function openPaymentModal(mode) {
    if (!PosState.orderId || !PosState.items.length) {
        App.warning(window.AppStrings?.cartEmpty ?? 'Cart is empty');
        return;
    }

    PosState.paymentMode  = mode;
    PosState.cashEntered  = '';

    document.getElementById('cashSection').style.display  = mode === 'cash'  ? '' : 'none';
    document.getElementById('cardSection').style.display  = mode === 'card'  ? '' : 'none';
    document.getElementById('splitSection').style.display = mode === 'split' ? '' : 'none';

    const titles = { cash: 'CASH', card: 'CARD', split: 'SPLIT' };
    document.getElementById('paymentModalTitle').textContent = titles[mode] ?? 'Payment';

    if (mode === 'cash') {
        document.getElementById('cashDisplay').textContent = fmt(PosState.total);
        document.getElementById('changeDisplay').style.display = 'none';
    } else if (mode === 'card') {
        document.getElementById('cardTotalDisplay').textContent = fmt(PosState.total);
    } else {
        PosState.splitCount = 2;
        renderSplitAmounts();
    }

    const bsModal = new bootstrap.Modal(document.getElementById('paymentModal'));
    bsModal.show();
}

function updateCashDisplay() {
    const entered = parseFloat(PosState.cashEntered) || 0;
    document.getElementById('cashDisplay').textContent = PosState.cashEntered || '0.000';

    const change = entered - PosState.total;
    const cDisp  = document.getElementById('changeDisplay');
    if (entered > 0) {
        cDisp.style.display = '';
        document.getElementById('changeAmount').textContent = fmt(Math.max(0, change));
    } else {
        cDisp.style.display = 'none';
    }
}

function renderSplitAmounts() {
    document.getElementById('splitCount').textContent = PosState.splitCount;
    const each = PosState.total / PosState.splitCount;
    const html  = Array.from({ length: PosState.splitCount }, (_, i) =>
        `<div class="d-flex justify-content-between border-bottom py-1">
            <span>${window.AppStrings?.person ?? 'Person'} ${i + 1}</span>
            <strong>${fmt(each)}</strong>
        </div>`
    ).join('');
    document.getElementById('splitAmounts').innerHTML = html;
}

async function processPayment() {
    if (PosState.isProcessing) return;
    PosState.isProcessing = true;
    App.showLoading();

    try {
        const methodMap = { cash: 1, card: 2, split: 3 };
        const payload   = {
            orderId:        PosState.orderId,
            paymentMethod:  methodMap[PosState.paymentMode] ?? 1,
            cashReceived:   PosState.paymentMode === 'cash' ? (parseFloat(PosState.cashEntered) || PosState.total) : null,
            discountAmount: PosState.discount,
            managerPin:     '',
        };

        const data = await postJson('/Pos/ProcessPayment', payload);
        App.hideLoading();

        if (data.success) {
            bootstrap.Modal.getInstance(document.getElementById('paymentModal'))?.hide();
            await showPaymentSuccess(data.data);
            resetCart();
            await createNewOrder();
        } else {
            App.error(data.message ?? 'Payment failed');
        }
    } catch (e) {
        App.hideLoading();
        App.error('Payment error');
    } finally {
        PosState.isProcessing = false;
    }
}

async function showPaymentSuccess(result) {
    const change = result?.changeGiven ?? 0;
    await App.confirm(
        `${window.AppStrings?.paymentSuccess ?? 'Payment successful!'}\n` +
        (change > 0 ? `${window.AppStrings?.change ?? 'Change'}: ${fmt(change)}` : ''),
        { confirmOnly: true }
    );
}

// ── Held orders ───────────────────────────────────────────────────────────────
function bindHeldOrdersModal() {
    document.getElementById('held-orders-btn').addEventListener('click', openHeldOrdersModal);
}

async function openHeldOrdersModal() {
    try {
        const res  = await fetch('/Pos/GetActiveOrders');
        const data = await res.json();
        const list = document.getElementById('heldOrdersList');

        const held = (data.data ?? []).filter(o => o.status === 4); // Held = 4
        PosState.heldOrders = held;
        updateHeldBadge(held.length);

        if (!held.length) {
            list.innerHTML = `<p class="text-muted text-center">${window.AppStrings?.noHeldOrders ?? 'No held orders'}</p>`;
        } else {
            list.innerHTML = held.map(o => `
                <div class="d-flex align-items-center justify-content-between mb-2 p-2 border rounded">
                    <div>
                        <div class="fw-bold">${escHtml(o.orderNumber ?? '')}</div>
                        <small class="text-muted">${escHtml(o.tableNumber ?? '')} · ${escHtml(o.itemCount ?? 0)} items</small>
                    </div>
                    <button class="btn btn-sm btn-success recall-btn" data-id="${o.id}">
                        <i class="fas fa-play"></i> Recall
                    </button>
                </div>
            `).join('');

            list.querySelectorAll('.recall-btn').forEach(btn => {
                btn.addEventListener('click', async () => {
                    await recallOrder(parseInt(btn.dataset.id, 10));
                    bootstrap.Modal.getInstance(document.getElementById('heldOrdersModal'))?.hide();
                });
            });
        }

        new bootstrap.Modal(document.getElementById('heldOrdersModal')).show();
    } catch (e) {
        App.error('Failed to load held orders');
    }
}

async function recallOrder(orderId) {
    const data = await postJson('/Pos/RecallOrder', { orderId });
    if (data.success) {
        PosState.orderId = orderId;
        // Reload order details into cart
        const det = await (await fetch(`/Pos/GetOrderDetails?orderId=${orderId}`)).json();
        if (det.success) {
            PosState.items = (det.data?.items ?? []).map(i => ({
                itemId:     i.id,
                menuItemId: i.menuItemId,
                name:       i.name,
                price:      i.unitPrice,
                quantity:   i.quantity,
                notes:      i.notes ?? '',
            }));
            document.getElementById('orderNumberDisplay').textContent = det.data.orderNumber ?? '';
            recalculateTotals();
            renderCart();
        }
        refreshHeldCount();
    } else {
        App.error(data.message ?? 'Recall failed');
    }
}

async function refreshHeldCount() {
    try {
        const res  = await fetch('/Pos/GetActiveOrders');
        const data = await res.json();
        const cnt  = (data.data ?? []).filter(o => o.status === 4).length;
        updateHeldBadge(cnt);
    } catch (_) { /* silent */ }
}

function updateHeldBadge(count) {
    const badge = document.getElementById('held-count');
    if (count > 0) {
        badge.textContent    = count;
        badge.style.display  = '';
    } else {
        badge.style.display  = 'none';
    }
}

// ── Keyboard shortcuts ────────────────────────────────────────────────────────
function bindKeyboardShortcuts() {
    document.addEventListener('keydown', e => {
        if (e.target.matches('input, textarea, select')) return;
        switch (e.key) {
            case 'F1': e.preventDefault(); openPaymentModal('cash');  break;
            case 'F2': e.preventDefault(); openPaymentModal('card');  break;
            case 'F5': e.preventDefault(); holdOrder();               break;
            case 'Escape':                 clearCart();               break;
        }
    });
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
