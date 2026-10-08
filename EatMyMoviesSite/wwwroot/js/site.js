document.addEventListener('DOMContentLoaded', () => {
    const navbarBurgers = Array.from(document.querySelectorAll('.navbar-burger'));
    const listDropdowns = Array.from(document.querySelectorAll('.nav-list-dropdown'));

    const closeDropdowns = () => {
        listDropdowns.forEach((dropdown) => {
            dropdown.classList.remove('is-active');
            dropdown.querySelector('.nav-list-toggle')?.setAttribute('aria-expanded', 'false');
        });
    };

    const setBurgerState = (burger, target, isActive) => {
        burger.classList.toggle('is-active', isActive);
        target.classList.toggle('is-active', isActive);
        burger.setAttribute('aria-expanded', isActive.toString());
        burger.setAttribute('aria-label', isActive ? 'Close menu' : 'Open menu');

        if (!isActive) {
            closeDropdowns();
        }
    };

    navbarBurgers.forEach((burger) => {
        const targetId = burger.dataset.target;
        const target = targetId ? document.getElementById(targetId) : null;

        if (!target) {
            return;
        }

        burger.addEventListener('click', () => {
            setBurgerState(burger, target, !burger.classList.contains('is-active'));
        });

        burger.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' || event.key === ' ') {
                event.preventDefault();
                setBurgerState(burger, target, !burger.classList.contains('is-active'));
            }

            if (event.key === 'Escape') {
                setBurgerState(burger, target, false);
            }
        });
    });

    listDropdowns.forEach((dropdown) => {
        const toggle = dropdown.querySelector('.nav-list-toggle');

        if (!toggle) {
            return;
        }

        const toggleDropdown = (event) => {
            if (!window.matchMedia('(max-width: 1023px)').matches) {
                return;
            }

            if (dropdown.classList.contains('is-active')) return;
            event.preventDefault();
            const isActive = dropdown.classList.toggle('is-active');
            toggle.setAttribute('aria-expanded', isActive.toString());
        };

        toggle.addEventListener('click', toggleDropdown);
        toggle.addEventListener('keydown', (event) => {
            if (event.key === 'Enter' || event.key === ' ') {
                toggleDropdown(event);
            }

            if (event.key === 'Escape') {
                dropdown.classList.remove('is-active');
                toggle.setAttribute('aria-expanded', 'false');
            }
        });
    });
});

// Progressive enhancement leaves the complete biography readable without JavaScript.
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-director-biography]').forEach((section) => {
        const paragraph = section.querySelector('p');
        const toggle = section.querySelector('.biography-toggle');
        const fullText = paragraph?.textContent.trim();
        if (!fullText || fullText.length <= 300 || !toggle) return;

        const prefix = fullText.slice(0, 300);
        const lastBoundary = prefix.search(/\s+\S*$/);
        // A single very long word should remain intact rather than be split.
        const firstBoundary = fullText.search(/\s/);
        const boundary = lastBoundary > 0 ? lastBoundary : firstBoundary;
        if (boundary <= 0 || boundary >= fullText.length) return;
        const excerpt = fullText.slice(0, boundary).trimEnd() + '…';
        paragraph.textContent = excerpt;
        toggle.hidden = false;
        toggle.addEventListener('click', () => {
            const expanded = toggle.getAttribute('aria-expanded') !== 'true';
            paragraph.textContent = expanded ? fullText : excerpt;
            toggle.setAttribute('aria-expanded', String(expanded));
            toggle.textContent = expanded ? 'Show less' : 'Read more';
            if (!expanded && toggle.getBoundingClientRect().top < 0) {
                toggle.scrollIntoView({ behavior: 'instant', block: 'nearest' });
            }
        });
    });
});
