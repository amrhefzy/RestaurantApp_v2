'use strict';

// Configure Toastr defaults
toastr.options = {
    closeButton:       true,
    progressBar:       true,
    positionClass:     document.dir === 'rtl' ? 'toast-bottom-left' : 'toast-bottom-right',
    timeOut:           4000,
    extendedTimeOut:   2000,
    newestOnTop:       true,
    preventDuplicates: true,
};

const App = {

    // ── Toastr notifications ──────────────────────────────────────────────
    success(msg, title) {
        toastr.success(msg, title ?? AppStrings.success);
    },

    error(msg, title) {
        toastr.error(msg, title ?? AppStrings.error);
    },

    warning(msg, title) {
        toastr.warning(msg, title ?? AppStrings.warning);
    },

    info(msg, title) {
        toastr.info(msg, title ?? AppStrings.info);
    },

    // ── SweetAlert2 helpers ───────────────────────────────────────────────

    /**
     * Generic yes/no confirmation.
     * @returns {Promise<boolean>}
     */
    async confirm(title, text = '') {
        const result = await Swal.fire({
            title,
            text,
            icon:              'question',
            showCancelButton:  true,
            confirmButtonText: AppStrings.yesBtn,
            cancelButtonText:  AppStrings.cancelBtn,
            reverseButtons:    document.dir === 'rtl',
            customClass: {
                confirmButton: 'btn btn-primary px-4',
                cancelButton:  'btn btn-secondary px-4 ms-2',
            },
            buttonsStyling: false,
        });
        return result.isConfirmed;
    },

    /**
     * Delete confirmation with red confirm button.
     * @param {string} itemName  Human-readable name of the item being deleted.
     * @returns {Promise<boolean>}
     */
    async confirmDelete(itemName = '') {
        const result = await Swal.fire({
            title:             AppStrings.confirmDelete,
            text:              itemName
                ? `"${itemName}" — ${AppStrings.deleteWarning}`
                : AppStrings.deleteWarning,
            icon:              'warning',
            showCancelButton:  true,
            confirmButtonText: AppStrings.deleteBtn,
            cancelButtonText:  AppStrings.cancelBtn,
            reverseButtons:    document.dir === 'rtl',
            customClass: {
                confirmButton: 'btn btn-danger px-4',
                cancelButton:  'btn btn-secondary px-4 ms-2',
            },
            buttonsStyling: false,
        });
        return result.isConfirmed;
    },

    /**
     * Prompt for Manager PIN (password input, 4 digits).
     * @returns {Promise<string|null>}  PIN string or null if cancelled.
     */
    async pinPrompt() {
        const result = await Swal.fire({
            title:             AppStrings.requireManagerPin,
            input:             'password',
            inputPlaceholder:  AppStrings.pinPlaceholder,
            inputAttributes: {
                maxlength:    '4',
                autocomplete: 'off',
                pattern:      '\\d{4}',
                inputmode:    'numeric',
            },
            showCancelButton:  true,
            confirmButtonText: AppStrings.yesBtn,
            cancelButtonText:  AppStrings.cancelBtn,
            reverseButtons:    document.dir === 'rtl',
            customClass: {
                confirmButton: 'btn btn-warning px-4',
                cancelButton:  'btn btn-secondary px-4 ms-2',
                input:         'form-control text-center fs-4 letter-spacing-4',
            },
            buttonsStyling: false,
            inputValidator(value) {
                if (!/^\d{4}$/.test(value))
                    return AppStrings.pinInvalid;
            },
        });
        return result.isConfirmed ? result.value : null;
    },

    /**
     * Show a non-dismissible loading spinner.
     * @param {string} [msg]
     */
    showLoading(msg) {
        Swal.fire({
            title:             msg ?? AppStrings.loading,
            allowOutsideClick: false,
            allowEscapeKey:    false,
            showConfirmButton:  false,
            didOpen() {
                Swal.showLoading();
            },
        });
    },

    /** Close whatever SweetAlert2 dialog is open. */
    hideLoading() {
        Swal.close();
    },
};
