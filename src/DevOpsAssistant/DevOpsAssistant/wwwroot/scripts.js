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

document.addEventListener('DOMContentLoaded', function () {
    const progressSvg = document.querySelector('.loading-progress');
    const progressText = document.querySelector('.loading-progress-text');
    if (!progressSvg || !progressText) return;

    const update = function () {
        const styles = getComputedStyle(document.documentElement);
        const percent = parseFloat(styles.getPropertyValue('--blazor-load-percentage') || '0');
        const text = styles.getPropertyValue('--blazor-load-percentage-text') || 'Loading';
        progressSvg.setAttribute('aria-valuenow', percent.toString());
        progressText.textContent = text.replace(/"/g, '').trim();
    };

    update();
    const observer = new MutationObserver(update);
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['style'] });
});
