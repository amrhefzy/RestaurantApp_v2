'use strict';

const ModalManager = {

    _modal: null,

    _getModal() {
        if (!this._modal) {
            const el = document.getElementById('ajaxModal');
            if (el) this._modal = new bootstrap.Modal(el);
        }
        return this._modal;
    },

    // ── Open modal and load remote content ───────────────────────────────
    /**
     * Fetch HTML from `url` and inject it into #ajaxModal, then show the modal.
     * @param {string} url    Controller action URL
     * @param {string} [title]  Override the modal title
     */
    async open(url, title) {
        try {
            App.showLoading();

            const resp = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
            });

            if (!resp.ok) {
                App.hideLoading();
                App.error(`${AppStrings.error} (${resp.status})`);
                return;
            }

            const html = await resp.text();
            App.hideLoading();

            const container = document.getElementById('ajaxModalContent');
            if (container) container.innerHTML = html;

            if (title) {
                const titleEl = document.getElementById('ajaxModalLabel');
                if (titleEl) titleEl.textContent = title;
            }

            const modal = this._getModal();
            if (modal) modal.show();

        } catch (err) {
            App.hideLoading();
            App.error(AppStrings.error);
            console.error('[ModalManager.open]', err);
        }
    },

    // ── Close the modal ───────────────────────────────────────────────────
    close() {
        const modal = this._getModal();
        if (modal) modal.hide();
    },

    // ── Submit a form via AJAX and handle ApiResponse<T> ─────────────────
    /**
     * @param {string}   formSelector    CSS selector for the <form>
     * @param {Function} [successCallback]  Called with the response data on success
     */
    async submitForm(formSelector, successCallback) {
        const form = document.querySelector(formSelector);
        if (!form) return;

        // Clear previous validation errors
        form.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));
        form.querySelectorAll('.invalid-feedback').forEach(el => el.textContent = '');

        const formData = new FormData(form);
        const body     = {};
        formData.forEach((v, k) => { body[k] = v; });

        // Get antiforgery token from form or meta tag
        const token = form.querySelector('[name="__RequestVerificationToken"]')?.value
                   ?? document.querySelector('meta[name="csrf-token"]')?.content ?? '';

        App.showLoading(AppStrings.saving);

        try {
            const resp = await fetch(form.action || window.location.href, {
                method:  form.method?.toUpperCase() || 'POST',
                headers: {
                    'Content-Type':  'application/json',
                    'X-CSRF-TOKEN':  token,
                    'X-Requested-With': 'XMLHttpRequest',
                },
                body: JSON.stringify(body),
            });

            App.hideLoading();

            let apiResp;
            try {
                apiResp = await resp.json();
            } catch {
                App.error(AppStrings.error);
                return;
            }

            if (apiResp.success) {
                App.success(apiResp.message || AppStrings.success);
                this.close();
                if (typeof successCallback === 'function') successCallback(apiResp.data);
            } else if (apiResp.errors?.length) {
                // Map validation errors to form fields
                apiResp.errors.forEach(err => {
                    // Try matching to a field — errors may be "FieldName: message" format
                    const colon = err.indexOf(':');
                    if (colon > -1) {
                        const field   = err.substring(0, colon).trim();
                        const message = err.substring(colon + 1).trim();
                        const input   = form.querySelector(`[name="${field}"]`);
                        if (input) {
                            input.classList.add('is-invalid');
                            const feedback = input.nextElementSibling;
                            if (feedback?.classList.contains('invalid-feedback'))
                                feedback.textContent = message;
                        }
                    }
                });
                App.error(apiResp.message || AppStrings.error);
            } else {
                App.error(apiResp.message || AppStrings.error);
            }

        } catch (err) {
            App.hideLoading();
            App.error(AppStrings.error);
            console.error('[ModalManager.submitForm]', err);
        }
    },

    // ── Refresh a DataTable by element ID ─────────────────────────────────
    /**
     * @param {string} tableId  The `id` attribute of the <table> element (without #)
     */
    refresh(tableId) {
        const table = $(`#${tableId}`);
        if (table.length && $.fn.DataTable.isDataTable(table)) {
            table.DataTable().ajax.reload(null, false);
        } else {
            // Full page reload fallback
            window.location.reload();
        }
    },
};
