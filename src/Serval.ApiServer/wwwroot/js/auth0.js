// adapted from https://community.auth0.com/t/auth0-swagger/19954
var f = window.fetch;
window.fetch = function (url, opts) {
    if (opts && opts.body && typeof opts.body === 'string' && opts.body.indexOf('client_credentials') !== -1) {
        // We know the audience - just add it.
        opts.body += '&audience=https://machine.sil.org';
    }
    return f(url, opts);
};
