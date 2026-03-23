/* ═══════════════════════════════════════════════════════════════════════════
   RestaurantMS — Cash Handover Module  (cashhandover.js)
   ═══════════════════════════════════════════════════════════════════════════ */

'use strict';

const HandoverModule = (() => {

    // ── CSRF helper ───────────────────────────────────────────────────────────
    function csrf() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? '';
    }

    async function postJson(url, body) {
        const res = await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf() },
            body: JSON.stringify(body),
        });
        return res.json();
    }

    function fmt(n) { return parseFloat(n || 0).toFixed(3); }

    // ── Open Handover ─────────────────────────────────────────────────────────
    async function openHandover(floatAmount) {
        App.showLoading();
        try {
            const data = await postJson('/CashHandover/Open', { openFloat: floatAmount });
            App.hideLoading();
            if (data.success) {
                App.success(window.AppStrings?.handoverOpened ?? 'Handover opened successfully');
                setTimeout(() => { window.location.href = '/Pos/Index'; }, 1200);
            } else {
                App.error(data.message ?? 'Failed to open handover');
            }
        } catch (_) {
            App.hideLoading();
            App.error('Network error');
        }
    }

    // ── Close page init ───────────────────────────────────────────────────────
    function initClose(isArStr, diffLabel, matchLabel) {
        const isAr      = isArStr === 'true';
        const expected  = parseFloat(document.getElementById('expectedCash')?.value  ?? 0);
        const threshold = parseFloat(document.getElementById('diffThreshold')?.value ?? 5);
        const handoverId = parseInt(document.getElementById('handoverId')?.value ?? 0);

        const actualInput  = document.getElementById('actualCashInput');
        const diffPanel    = document.getElementById('diffPanel');
        const diffValue    = document.getElementById('diffValue');
        const closeBtn     = document.getElementById('closeHandoverBtn');
        const pinModal     = document.getElementById('managerPinModal');
        const pinInput     = document.getElementById('managerPinInput');
        const skipBtn      = document.getElementById('skipApprovalBtn');
        const submitPinBtn = document.getElementById('submitWithPinBtn');

        let pendingPayload = null;

        function updateDiff() {
            const actual = parseFloat(actualInput.value) || 0;
            const diff   = actual - expected;
            diffValue.textContent = (diff >= 0 ? '+' : '') + fmt(diff);

            diffPanel.className = 'diff-panel mb-3';
            if (Math.abs(diff) <= 0.001) {
                diffPanel.classList.add('diff-ok');
            } else if (Math.abs(diff) <= threshold) {
                diffPanel.classList.add('diff-warn');
            } else {
                diffPanel.classList.add('diff-bad');
            }
        }

        actualInput?.addEventListener('input', updateDiff);
        updateDiff();

        async function submitClose(managerPin) {
            App.showLoading();
            try {
                const payload = {
                    handoverId:   handoverId,
                    actualCash:   parseFloat(actualInput.value) || 0,
                    cashierNotes: document.getElementById('cashierNotes')?.value ?? '',
                };

                const data = await postJson('/CashHandover/Close', payload);
                App.hideLoading();

                if (data.success) {
                    // If flagged and we have a PIN, auto-approve
                    if (managerPin && data.data?.status === 4) {
                        await approveHandover(handoverId, managerPin, '');
                    }
                    App.success(window.AppStrings?.handoverClosed ?? 'Handover closed');
                    setTimeout(() => { window.location.href = '/CashHandover/History'; }, 1400);
                } else {
                    App.error(data.message ?? 'Failed to close handover');
                }
            } catch (_) {
                App.hideLoading();
                App.error('Network error');
            }
        }

        closeBtn?.addEventListener('click', () => {
            const actual = parseFloat(actualInput.value) || 0;
            const diff   = Math.abs(actual - expected);

            if (diff > threshold) {
                // Show manager PIN modal
                const msg = `${diffLabel} ${(actual - expected >= 0 ? '+' : '')}${fmt(actual - expected)}`;
                document.getElementById('flaggedDiffMsg').textContent = msg;
                new bootstrap.Modal(pinModal).show();
            } else {
                submitClose('');
            }
        });

        skipBtn?.addEventListener('click', () => {
            bootstrap.Modal.getInstance(pinModal)?.hide();
            submitClose('');
        });

        submitPinBtn?.addEventListener('click', () => {
            const pin = pinInput?.value ?? '';
            if (!pin || pin.length !== 4) {
                App.warning('Enter a 4-digit PIN');
                return;
            }
            bootstrap.Modal.getInstance(pinModal)?.hide();
            submitClose(pin);
        });
    }

    // ── History page init ─────────────────────────────────────────────────────
    function initHistory(from, to, isArStr) {
        const isAr = isArStr === 'true';
        loadHistory(from, to, isAr);
    }

    async function loadHistory(from, to, isAr) {
        const tbody = document.getElementById('handoverTbody');
        if (!tbody) return;

        tbody.innerHTML = `<tr><td colspan="8" class="text-center py-4 text-muted">
            <i class="fas fa-spinner fa-spin me-2"></i>Loading...</td></tr>`;

        try {
            const res  = await fetch(`/CashHandover/GetHistory?from=${from}&to=${to}`);
            const data = await res.json();
            renderHistoryTable(data.data ?? [], tbody, isAr ?? false);
        } catch (_) {
            tbody.innerHTML = `<tr><td colspan="8" class="text-center text-danger py-3">
                Failed to load data</td></tr>`;
        }
    }

    function renderHistoryTable(rows, tbody, isAr) {
        if (!rows.length) {
            tbody.innerHTML = `<tr><td colspan="8" class="text-center text-muted py-4">
                ${isAr ? 'لا توجد بيانات' : 'No records found'}</td></tr>`;
            return;
        }

        const statusMap = {
            1: ['badge-open',   isAr ? 'مفتوح'       : 'Open'],
            2: ['badge-held',   isAr ? 'انتظار اعتماد' : 'Pending'],
            3: ['badge-paid',   isAr ? 'مغلق'         : 'Closed'],
            4: ['badge-void',   isAr ? 'فرق كبير'     : 'Flagged'],
        };

        tbody.innerHTML = rows.map(h => {
            const diff    = h.difference ?? 0;
            const diffCss = diff < -0.001 ? 'text-danger' : diff > 0.001 ? 'text-warning' : 'text-success';
            const [sCss, sLabel] = statusMap[h.status] ?? ['badge-open', h.status];
            const canApprove = (h.status === 2 || h.status === 4);

            return `<tr>
                <td>${new Date(h.openedAt).toLocaleString()}</td>
                <td>${escHtml(h.cashierName)}</td>
                <td>${parseFloat(h.openFloat).toFixed(3)}</td>
                <td>${parseFloat(h.expectedCash).toFixed(3)}</td>
                <td>${h.actualCash != null ? parseFloat(h.actualCash).toFixed(3) : '—'}</td>
                <td class="${diffCss} fw-bold">
                    ${h.difference != null ? (diff >= 0 ? '+' : '') + parseFloat(h.difference).toFixed(3) : '—'}
                </td>
                <td><span class="badge ${sCss}">${sLabel}</span></td>
                <td>
                    <a href="/CashHandover/Detail/${h.id}" class="btn btn-outline-secondary btn-xs me-1">
                        <i class="fas fa-eye"></i>
                    </a>
                    ${canApprove ? `<button class="btn btn-outline-primary btn-xs approve-btn" data-id="${h.id}">
                        <i class="fas fa-check"></i></button>` : ''}
                </td>
            </tr>`;
        }).join('');

        // Bind approve buttons
        tbody.querySelectorAll('.approve-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                document.getElementById('approveHandoverId').value = btn.dataset.id;
                document.getElementById('approvePinInput').value   = '';
                document.getElementById('approveNotes').value      = '';
                new bootstrap.Modal(document.getElementById('approveModal')).show();
            });
        });
    }

    // ── Approve Handover ──────────────────────────────────────────────────────
    async function approveHandover(handoverId, managerPin, notes) {
        if (!managerPin || managerPin.length !== 4) {
            App.warning('Enter a valid 4-digit Manager PIN');
            return;
        }

        App.showLoading();
        try {
            const data = await postJson('/CashHandover/Approve', {
                handoverId, managerPin, notes: notes ?? ''
            });
            App.hideLoading();

            if (data.success) {
                bootstrap.Modal.getInstance(document.getElementById('approveModal'))
                    ?.hide();
                bootstrap.Modal.getInstance(document.getElementById('detailApproveModal'))
                    ?.hide();
                App.success(window.AppStrings?.handoverApproved ?? 'Handover approved');
                setTimeout(() => location.reload(), 1000);
            } else {
                App.error(data.message ?? 'Approval failed');
            }
        } catch (_) {
            App.hideLoading();
            App.error('Network error');
        }
    }

    // ── XSS escape ────────────────────────────────────────────────────────────
    function escHtml(str) {
        return String(str ?? '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    // ── Public API ────────────────────────────────────────────────────────────
    return { openHandover, initClose, initHistory, loadHistory, approveHandover };

})();
