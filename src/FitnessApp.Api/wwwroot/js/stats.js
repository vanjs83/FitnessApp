const Stats = {
    plans: [],

    init() {
        const planSel = document.getElementById('statsPlanSelect');
        if (planSel) planSel.addEventListener('change', () => this.loadPlanProgression());

        const planExSel = document.getElementById('statsPlanExerciseSelect');
        if (planExSel) planExSel.addEventListener('change', e => Plans.renderProgression(parseInt(e.target.value), 'statsPlanChart', 'statsPlanSummary', 'statsPlanChartObj'));
    },

    async load() {
        await this.loadPlans();
    },

    async loadPlans() {
        const planSel = document.getElementById('statsPlanSelect');
        const summary = document.getElementById('statsPlanSummary');
        if (!planSel) return;
        try {
            const me = await API.get('/auth/me');
            this.plans = await API.get(`/training-plans/client/${me.id}`);
        } catch (err) {
            this.plans = [];
            if (summary) summary.textContent = err.message;
        }
        if (!this.plans.length) {
            planSel.innerHTML = `<option value="">${I18n.t('plans.noPlansOption')}</option>`;
            document.getElementById('statsPlanExerciseSelect').innerHTML = '';
            Plans.clearProgressionChart('statsPlanChartObj');
            if (summary) summary.textContent = I18n.t('plans.empty.client');
            return;
        }
        planSel.innerHTML = this.plans.map(p =>
            `<option value="${p.id}">${this.escape(p.name)} (${this.formatDate(p.startDate)} → ${this.formatDate(p.endDate)})</option>`
        ).join('');
        await this.loadPlanProgression();
    },

    async loadPlanProgression() {
        const planSel = document.getElementById('statsPlanSelect');
        const planId = parseInt(planSel.value);
        if (!planId) return;
        await Plans.loadProgression(planId, 'statsPlanExerciseSelect', 'statsPlanChart', 'statsPlanSummary', 'statsPlanChartObj');
    },

    formatDate(s) {
        return new Date(s).toLocaleDateString(I18n.lang === 'en' ? 'en-GB' : 'hr-HR', { day: '2-digit', month: '2-digit', year: '2-digit' });
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
