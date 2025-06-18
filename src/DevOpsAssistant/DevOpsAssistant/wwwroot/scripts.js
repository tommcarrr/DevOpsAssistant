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

window.blazorCulture = {
    get: function () {
        return localStorage['BlazorCulture'];
    },
    set: function (value) {
        localStorage['BlazorCulture'] = value;
    }
};

if (window.matchMedia && window.matchMedia('(prefers-contrast: more)').matches) {
    document.body.classList.add('high-contrast');
}
