/* ═══════════════════════════════════════════════════════════════════════════
   RestaurantMS — Shifts Module  (shifts.js)
   ═══════════════════════════════════════════════════════════════════════════ */

'use strict';

const ShiftsModule = (() => {

    let _calendar   = null;
    let _activeShiftId = null;

    // ── CSRF / fetch helpers ──────────────────────────────────────────────────
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

    function escHtml(s) {
        return String(s ?? '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    // ── FullCalendar ──────────────────────────────────────────────────────────
    function initCalendar(isArStr) {
        const isAr = isArStr === 'true';
        const el   = document.getElementById('shiftCalendar');
        if (!el || typeof FullCalendar === 'undefined') return;

        _calendar = new FullCalendar.Calendar(el, {
            initialView:       'timeGridWeek',
            locale:            isAr ? 'ar' : 'en',
            direction:         isAr ? 'rtl' : 'ltr',
            headerToolbar: {
                start:  'prev,next today',
                center: 'title',
                end:    'dayGridMonth,timeGridWeek,timeGridDay',
            },
            events: (info, success, failure) => {
                fetch(`/Shift/GetCalendarEvents?start=${info.startStr}&end=${info.endStr}`)
                    .then(r => r.json())
                    .then(success)
                    .catch(failure);
            },
            eventClick: (info) => showShiftDetail(info.event, isAr),
            editable:   User_IsManager ?? false,    // set by Razor below if manager
            eventDrop:  (info) => {
                const e    = info.event;
                const data = e.extendedProps;
                assignShift({
                    employeeId: data.employeeId,
                    templateId: data.templateId,
                    shiftDate:  e.start.toISOString().split('T')[0],
                });
            },
            eventContent: (arg) => renderShiftEvent(arg),
        });

        _calendar.render();
    }

    function renderShiftEvent(arg) {
        const p    = arg.event.extendedProps;
        const statusIcons = { 1: '⬜', 2: '🟢', 3: '✅', 4: '🔴' };
        return {
            html: `<div style="padding:2px 4px;font-size:.78rem;overflow:hidden">
                ${statusIcons[p.status] ?? ''} ${escHtml(arg.event.title)}
                ${p.clockIn ? `<br><small>↳ ${new Date(p.clockIn).toLocaleTimeString()}</small>` : ''}
            </div>`,
        };
    }

    function showShiftDetail(event, isAr) {
        const p   = event.extendedProps;
        const fmt = (iso) => iso ? new Date(iso).toLocaleString() : '—';

        document.getElementById('detailTitle').textContent = event.title;
        document.getElementById('detailBody').innerHTML = `
            <dl class="row mb-0">
                <dt class="col-5">${isAr ? 'الحالة' : 'Status'}</dt>
                <dd class="col-7">${['','Scheduled','Active','Completed','Absent'][p.status] ?? p.status}</dd>
                <dt class="col-5">${isAr ? 'الدخول' : 'Clock In'}</dt>
                <dd class="col-7">${fmt(p.clockIn)}</dd>
                <dt class="col-5">${isAr ? 'الخروج' : 'Clock Out'}</dt>
                <dd class="col-7">${fmt(p.clockOut)}</dd>
                <dt class="col-5">${isAr ? 'وقت إضافي' : 'Overtime'}</dt>
                <dd class="col-7">${p.overtime ? p.overtime + ' min' : '—'}</dd>
            </dl>
        `;
        new bootstrap.Modal(document.getElementById('shiftDetailModal')).show();
    }

    // ── Assign shift ──────────────────────────────────────────────────────────
    async function assignShift(data) {
        if (!data.employeeId || !data.templateId || !data.shiftDate) {
            App.warning('Please fill all fields');
            return;
        }

        App.showLoading();
        try {
            const result = await postJson('/Shift/Assign', data);
            App.hideLoading();

            if (result.success) {
                bootstrap.Modal.getInstance(document.getElementById('assignModal'))?.hide();
                App.success(window.AppStrings?.shiftAssigned ?? 'Shift assigned');
                _calendar?.refetchEvents();
            } else {
                App.error(result.message ?? 'Failed to assign shift');
            }
        } catch (_) {
            App.hideLoading();
            App.error('Network error');
        }
    }

    // ── Clock In page ─────────────────────────────────────────────────────────
    function initClockIn(isArStr) {
        const isAr = isArStr === 'true';

        // Live clock
        function tick() {
            const now = new Date();
            document.getElementById('clockDisplay').textContent =
                now.toLocaleTimeString(isAr ? 'ar-SA' : 'en-US');
            document.getElementById('dateDisplay').textContent =
                now.toLocaleDateString(isAr ? 'ar-SA' : 'en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
        }
        tick();
        setInterval(tick, 1000);

        // Load shift templates (static options for demo; replace with real endpoint)
        const templateSel = document.getElementById('templateSelect');
        const templates = [
            { id: 1, nameEn: 'Morning (8:00 - 16:00)', nameAr: 'صباح (8 - 4)' },
            { id: 2, nameEn: 'Afternoon (14:00 - 22:00)', nameAr: 'مساء (2 - 10)' },
            { id: 3, nameEn: 'Night (22:00 - 06:00)', nameAr: 'ليل (10 - 6)' },
        ];
        templates.forEach(t => {
            const opt   = document.createElement('option');
            opt.value   = t.id;
            opt.textContent = isAr ? t.nameAr : t.nameEn;
            templateSel.appendChild(opt);
        });

        // Employee search (client-side typeahead via small user API)
        let employees = [];
        fetch('/api/users?role=all')
            .catch(() => { /* optional endpoint; skip if not available */ });

        const searchInput   = document.getElementById('employeeSearch');
        const suggestions   = document.getElementById('employeeSuggestions');
        const selectedIdEl  = document.getElementById('selectedEmployeeId');

        searchInput?.addEventListener('input', function () {
            const q = this.value.trim().toLowerCase();
            if (!q || !employees.length) { suggestions.style.display = 'none'; return; }
            const hits = employees.filter(e => e.name.toLowerCase().includes(q)).slice(0, 6);
            if (!hits.length) { suggestions.style.display = 'none'; return; }
            suggestions.innerHTML = hits.map(e =>
                `<button class="list-group-item list-group-item-action" data-id="${e.id}" data-name="${escHtml(e.name)}">
                    <i class="fas fa-user me-2 text-muted"></i>${escHtml(e.name)}
                 </button>`
            ).join('');
            suggestions.style.display = '';
            suggestions.querySelectorAll('button').forEach(btn => {
                btn.addEventListener('click', () => {
                    searchInput.value      = btn.dataset.name;
                    selectedIdEl.value     = btn.dataset.id;
                    suggestions.style.display = 'none';
                });
            });
        });

        // Check for own active shift
        checkActiveShift();

        // Clock-In button
        document.getElementById('clockInBtn')?.addEventListener('click', async () => {
            const empId    = parseInt(selectedIdEl?.value) || 0;
            const tmplId   = parseInt(templateSel?.value)  || 0;
            if (!tmplId) { App.warning(isAr ? 'اختر الوردية' : 'Select a shift template'); return; }
            await clockIn(empId || getCurrentUserId(), tmplId, isAr);
        });

        // Clock-Out button
        document.getElementById('clockOutBtn')?.addEventListener('click', async () => {
            if (!_activeShiftId) return;
            await clockOut(_activeShiftId, isAr);
        });

        loadRecentClockIns(isAr);
    }

    function getCurrentUserId() {
        // Reads from meta tag set by layout
        return parseInt(document.querySelector('meta[name="user-id"]')?.content ?? 0);
    }

    async function checkActiveShift() {
        try {
            const res = await fetch('/Shift/GetActiveShift');
            const data = await res.json();
            if (data.success && data.data) {
                const s = data.data;
                _activeShiftId = s.id;
                document.getElementById('activeShiftName').textContent =
                    `${s.employeeNameEn} — ${s.templateNameEn}`;
                document.getElementById('activeClockIn').textContent =
                    s.clockIn ? new Date(s.clockIn).toLocaleTimeString() : '—';
                document.getElementById('activeShiftPanel').style.display = '';
                document.getElementById('clockInPanel').style.display      = 'none';
            }
        } catch (_) { /* silent */ }
    }

    async function clockIn(employeeId, templateId, isAr) {
        App.showLoading();
        try {
            const data = await postJson('/Shift/ClockIn', { employeeId, templateId });
            App.hideLoading();
            if (data.success) {
                App.success(isAr ? 'تم تسجيل الدخول' : 'Clocked in successfully');
                _activeShiftId = data.data?.id;
                document.getElementById('activeShiftPanel').style.display = '';
                document.getElementById('clockInPanel').style.display      = 'none';
                loadRecentClockIns(isAr);
            } else {
                App.error(data.message ?? 'Clock-in failed');
            }
        } catch (_) {
            App.hideLoading();
            App.error('Network error');
        }
    }

    async function clockOut(shiftId, isAr) {
        App.showLoading();
        try {
            const data = await postJson('/Shift/ClockOut', { shiftId });
            App.hideLoading();
            if (data.success) {
                const s        = data.data;
                const overtime = s?.overtimeMinutes ?? 0;
                App.success(
                    (isAr ? 'تم تسجيل الخروج' : 'Clocked out') +
                    (overtime > 0 ? ` — ${isAr ? 'وقت إضافي:' : 'Overtime:'} ${overtime} min` : '')
                );
                _activeShiftId = null;
                document.getElementById('activeShiftPanel').style.display = 'none';
                document.getElementById('clockInPanel').style.display      = '';
                loadRecentClockIns(isAr);
            } else {
                App.error(data.message ?? 'Clock-out failed');
            }
        } catch (_) {
            App.hideLoading();
            App.error('Network error');
        }
    }

    async function loadRecentClockIns(isAr) {
        const ul = document.getElementById('recentClockIns');
        if (!ul) return;

        try {
            const now  = new Date();
            const from = new Date(now); from.setDate(from.getDate() - 1);
            const res  = await fetch(`/Shift/GetMyShifts?from=${from.toISOString()}&to=${now.toISOString()}`);
            const data = await res.json();
            const shifts = (data.data ?? []).slice(0, 8);

            if (!shifts.length) {
                ul.innerHTML = `<li class="list-group-item text-muted text-center">
                    ${isAr ? 'لا توجد سجلات' : 'No recent records'}</li>`;
                return;
            }

            ul.innerHTML = shifts.map(s => {
                const statusCss = { 1: 'secondary', 2: 'success', 3: 'primary', 4: 'danger' };
                const css = statusCss[s.status] ?? 'secondary';
                return `<li class="list-group-item d-flex justify-content-between align-items-center py-2">
                    <div>
                        <div class="fw-semibold">${escHtml(s.employeeNameEn)}</div>
                        <small class="text-muted">${escHtml(s.templateNameEn)}</small>
                    </div>
                    <div class="text-end">
                        <span class="badge bg-${css} mb-1">${['','Scheduled','Active','Completed','Absent'][s.status]}</span>
                        <div class="small text-muted">${s.clockIn ? new Date(s.clockIn).toLocaleTimeString() : '—'}</div>
                    </div>
                </li>`;
            }).join('');
        } catch (_) { /* silent */ }
    }

    // ── My Shifts table ───────────────────────────────────────────────────────
    async function loadMyShifts(from, to, isArStr) {
        const isAr  = isArStr === 'true';
        const tbody = document.getElementById('myShiftsTbody');
        if (!tbody) return;

        tbody.innerHTML = `<tr><td colspan="7" class="text-center py-4 text-muted">
            <i class="fas fa-spinner fa-spin me-2"></i>Loading...</td></tr>`;

        try {
            const res  = await fetch(`/Shift/GetMyShifts?from=${from}&to=${to}`);
            const data = await res.json();
            const rows = data.data ?? [];

            if (!rows.length) {
                tbody.innerHTML = `<tr><td colspan="7" class="text-center text-muted py-4">
                    ${isAr ? 'لا توجد ورديات' : 'No shifts found'}</td></tr>`;
                return;
            }

            const statusMap = {
                1: ['secondary', isAr ? 'مجدول'   : 'Scheduled'],
                2: ['success',   isAr ? 'نشط'      : 'Active'],
                3: ['primary',   isAr ? 'مكتمل'    : 'Completed'],
                4: ['danger',    isAr ? 'غائب'     : 'Absent'],
            };

            tbody.innerHTML = rows.map(s => {
                const [scol, slabel] = statusMap[s.status] ?? ['secondary', s.status];
                const fmtTime = (iso) => iso ? new Date(iso).toLocaleTimeString() : '—';
                const planned = `${fmtTime(s.plannedStart)} → ${fmtTime(s.plannedEnd)}`;

                return `<tr>
                    <td>${new Date(s.shiftDate).toLocaleDateString()}</td>
                    <td>${escHtml(isAr ? s.templateNameAr : s.templateNameEn)}</td>
                    <td class="text-muted small">${planned}</td>
                    <td>${fmtTime(s.clockIn)}</td>
                    <td>${fmtTime(s.clockOut)}</td>
                    <td>${s.overtimeMinutes > 0 ? `<span class="text-warning fw-bold">${s.overtimeMinutes} min</span>` : '—'}</td>
                    <td><span class="badge bg-${scol}">${slabel}</span></td>
                </tr>`;
            }).join('');
        } catch (_) {
            tbody.innerHTML = `<tr><td colspan="7" class="text-center text-danger py-3">
                Failed to load data</td></tr>`;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────
    return { initCalendar, assignShift, initClockIn, clockIn, clockOut, loadMyShifts };

})();
