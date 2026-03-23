/* ═══════════════════════════════════════════════════════════════════════════
   RestaurantMS — Reports Module  (reports.js)
   ═══════════════════════════════════════════════════════════════════════════ */

'use strict';

const ReportsModule = (() => {

    let _categoryChart = null;
    let _trendChart    = null;
    let _heatmapChart  = null;

    const CHART_COLORS = [
        '#3498db','#2ecc71','#e74c3c','#f39c12','#9b59b6',
        '#1abc9c','#e67e22','#34495e','#e91e63','#00bcd4',
    ];

    // ── Init dashboard ────────────────────────────────────────────────────────
    async function initDashboard(from, to, isArStr) {
        const isAr = isArStr === 'true';
        await Promise.all([
            loadSalesMetrics(to, isAr),
            loadBestSellers(from, to, isAr),
            loadHeatmap(to, isAr),
        ]);
    }

    // ── Sales metrics cards ───────────────────────────────────────────────────
    async function loadSalesMetrics(date, isAr) {
        try {
            const res  = await fetch(`/Report/GetDailySalesData?date=${date}`);
            const data = await res.json();
            const r    = data.data;
            if (!r) return;

            setText('metricSales',  r.netSales?.toFixed(3)   ?? '—');
            setText('metricOrders', r.totalOrders?.toString() ?? '—');
            setText('metricAvg',    r.totalOrders > 0
                ? (r.netSales / r.totalOrders).toFixed(3)
                : '0.000');
            setText('metricTax',    r.taxAmount?.toFixed(3)   ?? '—');

            // Sales by category chart from TopItems
            loadSalesChart(r.topItems ?? [], isAr);
            loadTrendHourly(r.hourlySales ?? [], isAr);
        } catch (_) { /* silent */ }
    }

    // ── Category bar chart ────────────────────────────────────────────────────
    function loadSalesChart(topItems, isAr) {
        const canvas = document.getElementById('categoryChart');
        if (!canvas || typeof Chart === 'undefined') return;

        _categoryChart?.destroy();

        const labels = topItems.map(i => isAr ? i.nameAr : i.nameEn);
        const values = topItems.map(i => i.totalRevenue);

        _categoryChart = new Chart(canvas, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: isAr ? 'الإيراد' : 'Revenue',
                    data:  values,
                    backgroundColor: CHART_COLORS.slice(0, labels.length),
                    borderRadius: 6,
                }],
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true } },
            },
        });
    }

    // ── Hourly trend line chart ───────────────────────────────────────────────
    function loadTrendHourly(hourlySales, isAr) {
        const canvas = document.getElementById('trendChart');
        if (!canvas || typeof Chart === 'undefined') return;

        _trendChart?.destroy();

        const labels = hourlySales.map(h => `${String(h.hour).padStart(2,'0')}:00`);
        const values = hourlySales.map(h => h.totalAmount);

        _trendChart = new Chart(canvas, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label:           isAr ? 'المبيعات' : 'Sales',
                    data:            values,
                    borderColor:     '#3498db',
                    backgroundColor: 'rgba(52,152,219,0.1)',
                    fill:            true,
                    tension:         0.4,
                    pointRadius:     3,
                }],
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: { legend: { display: false } },
                scales: { y: { beginAtZero: true } },
            },
        });
    }

    // ── Hourly heatmap (bubble/scatter) ───────────────────────────────────────
    async function loadHeatmap(date, isAr) {
        try {
            const res  = await fetch(`/Report/GetHourlySales?date=${date}`);
            const data = await res.json();
            renderHeatmap(data.data ?? [], isAr);
        } catch (_) { /* silent */ }
    }

    function renderHeatmap(hourlySales, isAr) {
        const canvas = document.getElementById('heatmapChart');
        if (!canvas || typeof Chart === 'undefined') return;

        _heatmapChart?.destroy();

        const maxOrders = Math.max(1, ...hourlySales.map(h => h.orderCount));

        const bubbles = hourlySales.map(h => ({
            x: h.hour,
            y: 1,
            r: Math.max(4, Math.round((h.orderCount / maxOrders) * 28)),
        }));

        _heatmapChart = new Chart(canvas, {
            type: 'bubble',
            data: {
                datasets: [{
                    label:           isAr ? 'الطلبات' : 'Orders',
                    data:            bubbles,
                    backgroundColor: bubbles.map(b =>
                        `rgba(52,152,219,${Math.min(1, 0.2 + (b.r / 30))})`),
                }],
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    x: { min: 0, max: 23, ticks: { callback: (v) => `${String(v).padStart(2,'0')}:00` } },
                    y: { display: false },
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: (ctx) => {
                                const h = hourlySales.find(x => x.hour === ctx.raw.x);
                                return h ? `${h.orderCount} orders — ${h.totalAmount.toFixed(3)}` : '';
                            },
                        },
                    },
                },
            },
        });
    }

    // ── Best sellers table ────────────────────────────────────────────────────
    async function loadBestSellers(from, to, isAr) {
        const tbody = document.getElementById('bestSellersTbody');
        if (!tbody) return;

        try {
            const res  = await fetch(`/Report/GetBestSellers?from=${from}&to=${to}&top=10`);
            const data = await res.json();
            renderBestSellers(data.data ?? [], tbody, isAr);
        } catch (_) {
            if (tbody) tbody.innerHTML = `<tr><td colspan="5" class="text-center text-danger py-3">
                Failed to load</td></tr>`;
        }
    }

    function renderBestSellers(items, tbody, isAr) {
        if (!items.length) {
            tbody.innerHTML = `<tr><td colspan="5" class="text-center text-muted py-3">
                ${isAr ? 'لا توجد بيانات' : 'No data'}</td></tr>`;
            return;
        }

        tbody.innerHTML = items.map((item, i) => `
            <tr>
                <td><span class="badge bg-secondary">${i + 1}</span></td>
                <td>${escHtml(isAr ? item.nameAr : item.nameEn)}</td>
                <td class="fw-bold">${item.totalQuantity}</td>
                <td class="text-success fw-bold">${parseFloat(item.totalRevenue).toFixed(3)}</td>
                <td>${item.orderCount}</td>
            </tr>
        `).join('');
    }

    // ── Export Excel ──────────────────────────────────────────────────────────
    function exportExcel(date) {
        const url = `/Report/ExportExcel?date=${date ?? new Date().toISOString().split('T')[0]}`;
        window.location.href = url;
    }

    // ── Print Z-Report ────────────────────────────────────────────────────────
    function printZReport(date) {
        const url = `/Report/ZReport?date=${date}`;
        const win = window.open(url, '_blank', 'width=500,height=700');
        win?.focus();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    function setText(id, val) {
        const el = document.getElementById(id);
        if (el) el.textContent = val;
    }

    function escHtml(str) {
        return String(str ?? '')
            .replace(/&/g, '&amp;').replace(/</g, '&lt;')
            .replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    // ── Public API ────────────────────────────────────────────────────────────
    return { initDashboard, loadSalesChart, loadHeatmap, loadBestSellers, exportExcel, printZReport };

})();
