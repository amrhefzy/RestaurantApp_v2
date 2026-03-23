'use strict';

/** Default DataTables configuration applied to every table. */
const DT_DEFAULTS = {
    language:    window.AppStrings?.dataTable ?? {},
    responsive:  true,
    pageLength:  25,
    lengthMenu:  [[10, 25, 50, 100, -1], [10, 25, 50, 100, 'All']],
    dom:         "<'row'<'col-sm-12 col-md-6'B><'col-sm-12 col-md-6'f>>" +
                 "<'row'<'col-sm-12'tr>>" +
                 "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>",
    buttons: [
        {
            extend:    'excel',
            text:      `<i class="fas fa-file-excel"></i> ${window.AppStrings?.dataTable?.buttons?.excel ?? 'Excel'}`,
            className: 'btn btn-success btn-sm',
        },
        {
            extend:    'pdf',
            text:      `<i class="fas fa-file-pdf"></i> ${window.AppStrings?.dataTable?.buttons?.pdf ?? 'PDF'}`,
            className: 'btn btn-danger btn-sm',
        },
        {
            extend:    'csv',
            text:      `<i class="fas fa-file-csv"></i> ${window.AppStrings?.dataTable?.buttons?.csv ?? 'CSV'}`,
            className: 'btn btn-secondary btn-sm',
        },
        {
            extend:    'print',
            text:      `<i class="fas fa-print"></i> ${window.AppStrings?.dataTable?.buttons?.print ?? 'Print'}`,
            className: 'btn btn-info btn-sm',
        },
    ],
    order:       [[0, 'desc']],
    autoWidth:   false,
};

/**
 * Initialise a DataTable with merged defaults + caller-supplied options.
 *
 * @param {string} selector     jQuery / CSS selector for the <table>
 * @param {object} [customOptions]  Options to merge/override defaults
 * @returns {DataTables.Api}
 */
function initDataTable(selector, customOptions = {}) {
    const $table = $(selector);
    if (!$table.length) {
        console.warn(`[initDataTable] Element not found: "${selector}"`);
        return null;
    }

    // Destroy existing instance if re-initialising
    if ($.fn.DataTable.isDataTable($table)) {
        $table.DataTable().destroy();
    }

    const options = $.extend(true, {}, DT_DEFAULTS, customOptions);
    return $table.DataTable(options);
}

/**
 * Refresh a DataTable without resetting the current page.
 * Falls back to page reload if the table has no server-side source.
 *
 * @param {string} tableId  The bare `id` attribute value (no `#`)
 */
function reloadDataTable(tableId) {
    const $t = $(`#${tableId}`);
    if ($t.length && $.fn.DataTable.isDataTable($t)) {
        $t.DataTable().ajax.reload(null, false);
    } else {
        window.location.reload();
    }
}
