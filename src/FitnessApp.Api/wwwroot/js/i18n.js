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

    tf(key, vars) {
        let s = this.t(key);
        if (vars) for (const k in vars) s = s.split(`{${k}}`).join(vars[k]);
        return s;
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
    },

    dayName(day) {
        const map = {
            Monday: 'day.monday', Tuesday: 'day.tuesday', Wednesday: 'day.wednesday',
            Thursday: 'day.thursday', Friday: 'day.friday', Saturday: 'day.saturday', Sunday: 'day.sunday',
            0: 'day.monday', 1: 'day.tuesday', 2: 'day.wednesday',
            3: 'day.thursday', 4: 'day.friday', 5: 'day.saturday', 6: 'day.sunday'
        };
        const key = map[day];
        return key ? this.t(key) : (day ?? '');
    },

    mealLabel(meal) {
        const map = {
            Breakfast: 'meal.breakfast', Snack1: 'meal.snack1', Lunch: 'meal.lunch',
            Snack2: 'meal.snack2', Dinner: 'meal.dinner', LateSnack: 'meal.lateSnack', Other: 'meal.other',
            0: 'meal.breakfast', 1: 'meal.snack1', 2: 'meal.lunch',
            3: 'meal.snack2', 4: 'meal.dinner', 5: 'meal.lateSnack', 99: 'meal.other'
        };
        const key = map[meal];
        return key ? this.t(key) : (meal ?? '');
    },

    paymentStatus(status) {
        if (status === 'PaymentClaimed' || status === 1) return this.t('payment.claimed');
        if (status === 'Pending' || status === 0) return this.t('payment.pending');
        return '';
    }
};
