var f = window.fetch;
window.fetch = function (url, opts) {
    if (opts && opts.body && opts.body.indexOf('client_credentials') !== -1) {
        // Copy from Query string to body
        const urlParams = new URLSearchParams(opts.url.split('?')[1]);
        const audience = urlParams.get('audience');
        opts.body += '&audience=' + audience;
    }
    return f(url, opts);
};
