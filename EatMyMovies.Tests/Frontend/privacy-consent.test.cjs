const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const source = fs.readFileSync(path.join(__dirname, '../../EatMyMoviesSite/wwwroot/js/privacy-consent.js'), 'utf8');

function start(storedChoice = null) {
    const handlers = {};
    const scripts = [];
    const storage = new Map(storedChoice ? [['emm-optional-tracking', storedChoice]] : []);
    const elements = Object.fromEntries(['tracking-consent', 'privacy-settings', 'tracking-accept', 'tracking-reject']
        .map(id => [id, { hidden: true, addEventListener: (_, callback) => { handlers[id] = callback; }, focus() {} }]));
    let reloads = 0;
    const document = {
        cookie: '',
        getElementById: id => elements[id],
        createElement: () => ({}),
        head: { appendChild: script => scripts.push(script) }
    };
    const window = { location: { reload: () => { reloads++; } } };
    const localStorage = { getItem: key => storage.get(key) ?? null, setItem: (key, value) => storage.set(key, value) };
    vm.runInNewContext(source, { document, window, localStorage, Date });
    return { elements, handlers, scripts, storage, get reloads() { return reloads; } };
}

test('tracking stays unloaded without consent and after rejection', () => {
    const page = start();
    assert.equal(page.elements['tracking-consent'].hidden, false);
    assert.equal(page.scripts.length, 0);
    page.handlers['tracking-reject']();
    assert.equal(page.scripts.length, 0);
    assert.equal(page.storage.get('emm-optional-tracking'), 'rejected');
    assert.equal(start('rejected').scripts.length, 0);
});

test('acceptance loads each service once and withdrawal reloads without them', () => {
    const page = start();
    page.handlers['tracking-accept']();
    assert.equal(page.scripts.length, 2);
    assert.match(page.scripts[0].src, /googletagmanager/);
    assert.match(page.scripts[1].src, /hotjar/);
    page.handlers['privacy-settings']();
    page.handlers['tracking-reject']();
    assert.equal(page.reloads, 1);
    assert.equal(start(page.storage.get('emm-optional-tracking')).scripts.length, 0);
});
