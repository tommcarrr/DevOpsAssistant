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

window.setHighContrast = function (enabled) {
    if (enabled)
        document.body.classList.add('high-contrast');
    else
        document.body.classList.remove('high-contrast');
};

if (window.matchMedia && window.matchMedia('(prefers-contrast: more)').matches) {
    window.setHighContrast(true);
}

(function () {
    const cheat = 'iddqd';
    let buffer = '';

    function activateDoomTheme() {
        if (document.body.classList.contains('doom-theme'))
            return;

        let link = document.getElementById('doom-stylesheet');
        if (!link) {
            link = document.createElement('link');
            link.id = 'doom-stylesheet';
            link.rel = 'stylesheet';
            link.href = 'css/doom.css';
            document.head.appendChild(link);
        }

        document.body.classList.add('doom-theme');
        localStorage['doom-theme'] = '1';
    }

    function removeDoomTheme() {
        const link = document.getElementById('doom-stylesheet');
        if (link)
            link.remove();
        document.body.classList.remove('doom-theme');
        localStorage.removeItem('doom-theme');
    }

    document.addEventListener('keydown', function (e) {
        buffer += e.key.toLowerCase();
        if (buffer.length > cheat.length)
            buffer = buffer.slice(-cheat.length);
        if (buffer === cheat)
            activateDoomTheme();
    });

    if (localStorage['doom-theme'] === '1')
        activateDoomTheme();

    window.activateDoomTheme = activateDoomTheme;
    window.removeDoomTheme = removeDoomTheme;
})();
