(() => {
    const key = 'emm-optional-tracking';
    const panel = document.getElementById('tracking-consent');
    const settings = document.getElementById('privacy-settings');
    const accept = document.getElementById('tracking-accept');
    const reject = document.getElementById('tracking-reject');
    if (!panel || !settings || !accept || !reject) return;

    let loaded = false;
    const readChoice = () => {
        try { return localStorage.getItem(key); }
        catch { return null; }
    };
    const saveChoice = (choice) => {
        try { localStorage.setItem(key, choice); }
        catch { /* A blocked store leaves tracking off on the next visit. */ }
    };
    const loadTracking = () => {
        if (loaded) return;
        loaded = true;
        window.dataLayer = window.dataLayer || [];
        window.gtag = function () { window.dataLayer.push(arguments); };
        window.gtag('js', new Date());
        window.gtag('config', 'G-QGZDZJ5HW2');
        const analytics = document.createElement('script');
        analytics.async = true;
        analytics.src = 'https://www.googletagmanager.com/gtag/js?id=G-QGZDZJ5HW2';
        document.head.appendChild(analytics);

        window.hj = window.hj || function () { (window.hj.q = window.hj.q || []).push(arguments); };
        window._hjSettings = { hjid: 5171442, hjsv: 6 };
        const hotjar = document.createElement('script');
        hotjar.async = true;
        hotjar.src = 'https://static.hotjar.com/c/hotjar-5171442.js?sv=6';
        document.head.appendChild(hotjar);
    };
    const clearFirstPartyTrackingCookies = () => {
        document.cookie.split(';').forEach((part) => {
            const name = part.split('=')[0].trim();
            if (!/^(_ga|_gid|_gat|_hj)/.test(name)) return;
            document.cookie = `${name}=; Max-Age=0; Path=/; SameSite=Lax`;
            document.cookie = `${name}=; Max-Age=0; Path=/; Domain=.eatmymovies.com; SameSite=Lax`;
        });
    };
    const choose = (choice) => {
        saveChoice(choice);
        panel.hidden = true;
        if (choice === 'accepted') loadTracking();
        else {
            clearFirstPartyTrackingCookies();
            if (loaded) window.location.reload();
        }
    };

    accept.addEventListener('click', () => choose('accepted'));
    reject.addEventListener('click', () => choose('rejected'));
    settings.addEventListener('click', () => {
        panel.hidden = false;
        reject.focus();
    });
    const choice = readChoice();
    if (choice === 'accepted') loadTracking();
    else if (choice !== 'rejected') panel.hidden = false;
})();
