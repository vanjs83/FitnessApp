/**
 * Shared edit plan modal for training and nutrition plans.
 * Usage:
 *   EditPlanModal.open({
 *       title: 'Uredi plan treninga',
 *       notesLabel: 'Očekivanja trenera',
 *       plan: { name, startDate, endDate, trainerExpectations, price, currency },
 *       onSave: async ({ name, startDate, endDate, notes, price, currency }) => {
 *           // do API call here
 *       }
 *   });
 */
const EditPlanModal = {
    _onSave: null,

    init() {
        const close = document.getElementById('closeEditPlanBtn');
        if (close) close.addEventListener('click', () => this.close());
        const cancel = document.getElementById('cancelEditPlanBtn');
        if (cancel) cancel.addEventListener('click', () => this.close());
        const save = document.getElementById('saveEditPlanBtn');
        if (save) save.addEventListener('click', () => this.save());
        const modal = document.getElementById('editPlanModal');
        if (modal) modal.addEventListener('click', e => { if (e.target === modal) this.close(); });
    },

    open({ title, notesLabel, plan, onSave }) {
        document.getElementById('editPlanTitle').textContent = title || I18n.t('plans.edit.title');
        document.getElementById('editPlanNotesLabel').textContent = notesLabel || I18n.t('common.notes');
        document.getElementById('editPlanName').value = plan.name || '';
        document.getElementById('editPlanStart').value = (plan.startDate || '').substring(0, 10);
        document.getElementById('editPlanEnd').value = (plan.endDate || '').substring(0, 10);
        document.getElementById('editPlanNotes').value = plan.notes || plan.trainerExpectations || '';
        document.getElementById('editPlanPrice').value = plan.price ?? 0;
        document.getElementById('editPlanCurrency').value = plan.currency || 'EUR';
        document.getElementById('editPlanError').textContent = '';
        this._onSave = onSave;
        document.getElementById('editPlanModal').classList.remove('hidden');
    },

    close() {
        document.getElementById('editPlanModal').classList.add('hidden');
        this._onSave = null;
    },

    async save() {
        const errEl = document.getElementById('editPlanError');
        errEl.textContent = '';
        const data = {
            name: document.getElementById('editPlanName').value.trim(),
            startDate: document.getElementById('editPlanStart').value,
            endDate: document.getElementById('editPlanEnd').value,
            notes: document.getElementById('editPlanNotes').value.trim(),
            price: parseFloat(document.getElementById('editPlanPrice').value) || 0,
            currency: document.getElementById('editPlanCurrency').value || 'EUR'
        };
        if (!data.name) { errEl.textContent = I18n.t('editPlan.nameRequired'); return; }
        if (!data.startDate || !data.endDate) { errEl.textContent = I18n.t('editPlan.datesRequired'); return; }
        try {
            await this._onSave(data);
            this.close();
        } catch (err) {
            errEl.textContent = err.message || I18n.t('editPlan.errorSaving');
        }
    }
};
