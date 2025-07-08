window.copyText = function (text) {
    navigator.clipboard.writeText(text);
};

window.downloadCsv = function (filename, text) {
    const blob = new Blob([text], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
};

window.downloadText = function (filename, text) {
    const blob = new Blob([text], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
};

window.setErrorDismissLabel = function (label) {
    const el = document.querySelector('#blazor-error-ui .dismiss');
    if (el) {
        el.setAttribute('aria-label', label);
    }
};

window.blazorCulture = {
    get: function () {
        return localStorage['BlazorCulture'];
    },
    set: function (value) {
        localStorage['BlazorCulture'] = value;
    }
};

window.scrollToId = function (id) {
    const el = document.getElementById(id);
    if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
};

window.themeShortcut = {
    init: function (dotnetHelper) {
        let buffer = '';
        document.addEventListener('keydown', function (e) {
            if (e.key.length === 1) {
                buffer += e.key.toLowerCase();
                if (buffer.length > 5)
                    buffer = buffer.slice(-5);
                if (buffer === 'iddqd') {
                    dotnetHelper.invokeMethodAsync('ToggleDoom');
                    buffer = '';
                }
            }
        });
    },
    setDoom: function (isDoom) {
        if (isDoom)
            document.body.classList.add('doom-theme');
        else
            document.body.classList.remove('doom-theme');
    }
};

