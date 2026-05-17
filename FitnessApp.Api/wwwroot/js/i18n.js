const I18n = {
    lang: 'hr',
    dict: {},
    supported: ['hr', 'en'],

    async init() {
        const stored = localStorage.getItem('lang');
        this.lang = this.supported.includes(stored) ? stored : 'hr';
        await this.loadDict(this.lang);
        this.apply();
        document.documentElement.lang = this.lang;
    },

    async loadDict(lang) {
        const res = await fetch(`/i18n/${lang}.json`);
        this.dict = await res.json();
    },

    t(key, fallback) {
        return this.dict[key] ?? fallback ?? key;
    },

    async setLang(lang) {
        if (!this.supported.includes(lang)) return;
        this.lang = lang;
        localStorage.setItem('lang', lang);
        await this.loadDict(lang);
        document.documentElement.lang = lang;
        this.apply();
        document.dispatchEvent(new CustomEvent('langchange', { detail: { lang } }));
    },

    apply(root) {
        const scope = root || document;
        scope.querySelectorAll('[data-i18n]').forEach(el => {
            const key = el.getAttribute('data-i18n');
            el.textContent = this.t(key);
        });
        scope.querySelectorAll('[data-i18n-ph]').forEach(el => {
            const key = el.getAttribute('data-i18n-ph');
            el.setAttribute('placeholder', this.t(key));
        });
        scope.querySelectorAll('[data-i18n-title]').forEach(el => {
            const key = el.getAttribute('data-i18n-title');
            el.setAttribute('title', this.t(key));
        });
    }
};
