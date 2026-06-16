const Nutrition = {
    plans: [],
    templates: [],
    plansPage: 1,
    plansMeta: null,
    current: null,
    currentTab: 'plans',
    dayNames: {
        Sunday: 'Nedjelja', Monday: 'Ponedjeljak', Tuesday: 'Utorak',
        Wednesday: 'Srijeda', Thursday: 'Četvrtak', Friday: 'Petak', Saturday: 'Subota',
        0: 'Nedjelja', 1: 'Ponedjeljak', 2: 'Utorak',
        3: 'Srijeda', 4: 'Četvrtak', 5: 'Petak', 6: 'Subota'
    },
    mealTypeLabels: {
        Breakfast: 'Doručak', Snack1: 'Užina 1', Lunch: 'Ručak',
        Snack2: 'Užina 2', Dinner: 'Večera', LateSnack: 'Kasna užina', Other: 'Ostalo',
        0: 'Doručak', 1: 'Užina 1', 2: 'Ručak', 3: 'Užina 2', 4: 'Večera', 5: 'Kasna užina', 99: 'Ostalo'
    },
    mealTypeOptions: ['Breakfast', 'Snack1', 'Lunch', 'Snack2', 'Dinner', 'LateSnack', 'Other'],

    init() {
        const newPlanBtn = document.getElementById('newNutritionPlanBtn');
        if (newPlanBtn) newPlanBtn.addEventListener('click', () => this.showNewPlanForm());
        const cancelPlan = document.getElementById('cancelNutritionPlanBtn');
        if (cancelPlan) cancelPlan.addEventListener('click', () => this.resetNewPlanForm());
        const savePlan = document.getElementById('saveNutritionPlanBtn');
        if (savePlan) savePlan.addEventListener('click', () => this.createPlan());

        const back = document.getElementById('backToNutritionListBtn');
        if (back) back.addEventListener('click', () => this.showList());

        const addDay = document.getElementById('addNutritionDayBtn');
        if (addDay) addDay.addEventListener('click', () => this.addDay());

        const saveAsTpl = document.getElementById('saveNutritionAsTemplateBtn');
        if (saveAsTpl) saveAsTpl.addEventListener('click', () => this.saveAsTemplate());

        const pdfBtn = document.getElementById('nutritionPdfBtn');
        if (pdfBtn) pdfBtn.addEventListener('click', () => this.downloadPdf());

        const qrBtn = document.getElementById('nutritionQrBtn');
        if (qrBtn) qrBtn.addEventListener('click', () => this.openQrShare());

        const notifyBtn = document.getElementById('nutritionNotifyBtn');
        if (notifyBtn) notifyBtn.addEventListener('click', () => this.notifyClient());

        const pushBtn = document.getElementById('nutritionPushBtn');
        if (pushBtn) pushBtn.addEventListener('click', () => this.pushClient());

        const editBtn = document.getElementById('editNutritionBtn');
        if (editBtn) editBtn.addEventListener('click', () => this.editCurrent());

        const delBtn = document.getElementById('deleteNutritionBtn');
        if (delBtn) delBtn.addEventListener('click', () => this.deleteCurrent());
    },

    async deleteCurrent() {
        if (!this.current) return;
        const label = this.current.isTemplate ? 'predložak' : 'plan prehrane';
        if (!confirm(`Obrisati ${label} "${this.current.name}"?`)) return;
        try {
            await API.delete(`/nutrition-plans/${this.current.id}`);
            this.showList();
        } catch (err) { alert(err.message); }
    },

    async editCurrent() {
        if (!this.current) return;
        if (this.current.isTemplate) {
            // template — just name + notes via modal too
            EditPlanModal.open({
                title: 'Uredi predložak prehrane',
                notesLabel: 'Napomene / opis',
                plan: { ...this.current, startDate: '', endDate: '', price: 0, currency: 'EUR' },
                onSave: async (data) => {
                    await API.put(`/nutrition-plans/templates/${this.current.id}`, {
                        name: data.name,
                        notes: data.notes || null
                    });
                    await this.showDetail(this.current.id);
                }
            });
            return;
        }
        EditPlanModal.open({
            title: 'Uredi plan prehrane',
            notesLabel: 'Napomene',
            plan: this.current,
            onSave: async (data) => {
                await API.put(`/nutrition-plans/${this.current.id}`, {
                    name: data.name,
                    startDate: new Date(data.startDate).toISOString(),
                    endDate: new Date(data.endDate).toISOString(),
                    notes: data.notes || null,
                    price: data.price,
                    currency: data.currency
                });
                await this.showDetail(this.current.id);
            }
        });
    },

    async notifyClient() {
        if (!this.current || this.current.isTemplate) return;
        if (!confirm(`Poslati email klijentu (${this.escape(this.current.clientName)}) da je plan prehrane "${this.escape(this.current.name)}" spreman?`)) return;
        try {
            await API.post('/email/notify-plan-ready', {
                clientId: this.current.clientId,
                planName: this.current.name,
                planType: 'nutrition',
                language: (typeof I18n !== 'undefined' && I18n.lang) ? I18n.lang : 'hr'
            });
            alert('✓ Email poslan klijentu.');
        } catch (err) { alert(err.message); }
    },

    async pushClient() {
        if (!this.current || this.current.isTemplate) return;
        if (!confirm(`Poslati push notifikaciju klijentu (${this.escape(this.current.clientName)}) za plan prehrane "${this.escape(this.current.name)}"?`)) return;
        try {
            const res = await API.post('/notifications/notify-client-plan', {
                clientId: this.current.clientId,
                planName: this.current.name,
                planType: 'nutrition'
            });
            if (res && res.activeTokens === 0) {
                alert('⚠ Klijent nema registriran uređaj za push notifikacije.');
            } else {
                alert('✓ Push poslan klijentu.');
            }
        } catch (err) { alert(err.message); }
    },

    async load() {
        // Always reset to list view when loaded (tab switch, refresh)
        const detail = document.getElementById('nutritionDetail');
        if (detail) detail.classList.add('hidden');
        const list = document.getElementById('nutritionPlansList');
        if (list) list.classList.remove('hidden');
        const newBtn = document.getElementById('newNutritionPlanBtn');
        if (newBtn) newBtn.classList.remove('hidden');
        this.current = null;

        try {
            const res = await API.get(`/nutrition-plans/mine?page=${this.plansPage}`);
            if (this.plansPage > 1 && res.totalPages > 0 && this.plansPage > res.totalPages) {
                this.plansPage = res.totalPages;
                return this.load();
            }
            this.plans = res.items;
            this.plansMeta = res;
            this.renderPlans();
        } catch (err) {
            console.error(err);
        }
    },

    renderPlansPager() {
        Pagination.render(document.getElementById('nutritionPlansListPager'), this.plansMeta, p => {
            this.plansPage = p;
            this.load();
        });
    },

    renderPlans() {
        const container = document.getElementById('nutritionPlansList');
        if (!this.plans.length) {
            container.innerHTML = '<p class="muted">Još nemaš planova prehrane. Klikni "+ Novi plan prehrane".</p>';
            this.renderPlansPager();
            return;
        }
        container.innerHTML = this.plans.map(p => `
            <div class="list-item" data-id="${p.id}">
                <div>
                    <h4>${this.escape(p.name)}</h4>
                    <div class="meta">
                        ${this.escape(p.clientName)}
                        · ${this.formatDate(p.startDate)} → ${this.formatDate(p.endDate)}
                        · ${p.dayCount} ${p.dayCount === 1 ? 'dan' : 'dana'}
                        · ${this.formatPrice(p)}
                        · <span class="${this.paymentBadgeClass(p.paymentStatus)}">${this.paymentStatusLabel(p.paymentStatus)}</span>
                    </div>
                </div>
                <span class="planned-actions">
                    <button class="btn-delete-icon delete-nut-btn" data-id="${p.id}" data-name="${this.escape(p.name)}" title="Obriši">🗑</button>
                    <span class="muted">→</span>
                </span>
            </div>
        `).join('');
        container.querySelectorAll('.list-item').forEach(el => {
            el.addEventListener('click', e => {
                if (e.target.closest('.delete-nut-btn')) return;
                this.showDetail(parseInt(el.dataset.id));
            });
        });
        container.querySelectorAll('.delete-nut-btn').forEach(btn => {
            btn.addEventListener('click', async e => {
                e.stopPropagation();
                const id = parseInt(btn.dataset.id);
                const name = btn.dataset.name;
                if (!confirm(`Obrisati "${name}"?`)) return;
                try {
                    await API.delete(`/nutrition-plans/${id}`);
                    await this.load();
                } catch (err) { alert(err.message); }
            });
        });

        this.renderPlansPager();
    },

    paymentStatusLabel(status) {
        if (status === 'Approved') return 'Odobreno';
        if (status === 'PaymentClaimed') return 'Klijent je platio — čeka odobrenje';
        return 'Čeka plaćanje';
    },
    paymentBadgeClass(status) {
        if (status === 'Approved') return 'badge approved';
        if (status === 'PaymentClaimed') return 'badge claimed';
        return 'badge pending';
    },
    formatPrice(plan) {
        const p = parseFloat(plan.price || 0);
        if (p === 0) return 'Besplatno';
        return `${p.toFixed(2)} ${plan.currency || 'EUR'}`;
    },

    renderTrainerPaymentBox(plan) {
        const box = document.getElementById('nutritionDetailPayment');
        if (!box) return;
        if (parseFloat(plan.price || 0) === 0) {
            box.innerHTML = `<span class="muted small">Besplatan plan — odmah dostupan klijentu.</span>`;
            return;
        }
        const statusLabel = this.paymentStatusLabel(plan.paymentStatus);
        const cls = this.paymentBadgeClass(plan.paymentStatus);
        let action = '';
        if (plan.paymentStatus === 'Approved') {
            action = `<button id="revokeNutApprovalBtn" class="secondary" data-plan-id="${plan.id}">🔒 Zaključaj nazad</button>`;
        } else {
            action = `<button id="approveNutPaymentBtn" data-plan-id="${plan.id}">Odobri plan</button>`;
        }
        box.innerHTML = `
            <div><strong>Cijena:</strong> ${this.formatPrice(plan)} · <span class="${cls}">${statusLabel}</span></div>
            ${action}
        `;
        const approveBtn = document.getElementById('approveNutPaymentBtn');
        if (approveBtn) approveBtn.addEventListener('click', () => this.approvePayment(plan.id));
        const revokeBtn = document.getElementById('revokeNutApprovalBtn');
        if (revokeBtn) revokeBtn.addEventListener('click', () => this.revokeApproval(plan.id));
    },

    async approvePayment(id) {
        try {
            await API.post(`/nutrition-plans/${id}/approve-payment`, {});
            await this.showDetail(id);
        } catch (err) { alert(err.message); }
    },
    async revokeApproval(id) {
        if (!confirm('Zaključati plan nazad? Klijent ga više neće vidjeti dok ponovo ne odobriš.')) return;
        try {
            await API.post(`/nutrition-plans/${id}/revoke-approval`, {});
            await this.showDetail(id);
        } catch (err) { alert(err.message); }
    },

    async showNewPlanForm(preselectClientId) {
        if (!Trainers.clients || !Trainers.clients.length) await Trainers.load();
        const select = document.getElementById('nutritionPlanClientSelect');
        if (!Trainers.clients.length) {
            select.innerHTML = '<option value="">— nema klijenata —</option>';
        } else {
            select.innerHTML = Trainers.clients.map(c =>
                `<option value="${c.id}">${this.escape(c.fullName || c.email)}</option>`
            ).join('');
            if (preselectClientId) select.value = preselectClientId;
        }

        const tplSel = document.getElementById('nutritionPlanTemplateSelect');
        try {
            const templates = await API.get('/nutrition-plans/templates');
            tplSel.innerHTML = '<option value="">— bez predloška (prazan plan) —</option>' +
                templates.map(t => `<option value="${t.id}">${this.escape(t.name)} (${t.dayCount} ${t.dayCount === 1 ? 'dan' : 'dana'})</option>`).join('');
        } catch (err) {
            console.error(err);
            tplSel.innerHTML = '<option value="">— bez predloška —</option>';
        }

        document.getElementById('newNutritionPlanForm').classList.remove('hidden');
    },

    resetNewPlanForm() {
        document.getElementById('nutritionPlanName').value = '';
        document.getElementById('nutritionPlanStartDate').value = '';
        document.getElementById('nutritionPlanEndDate').value = '';
        document.getElementById('nutritionPlanNotes').value = '';
        document.getElementById('nutritionPlanTemplateSelect').value = '';
        const priceEl = document.getElementById('nutritionPlanPrice');
        if (priceEl) priceEl.value = '0';
        const currEl = document.getElementById('nutritionPlanCurrency');
        if (currEl) currEl.value = 'EUR';
        document.getElementById('newNutritionPlanError').textContent = '';
        document.getElementById('newNutritionPlanForm').classList.add('hidden');
    },

    async createPlan() {
        const errorEl = document.getElementById('newNutritionPlanError');
        errorEl.textContent = '';
        const clientId = document.getElementById('nutritionPlanClientSelect').value;
        const name = document.getElementById('nutritionPlanName').value.trim();
        const startDate = document.getElementById('nutritionPlanStartDate').value;
        const endDate = document.getElementById('nutritionPlanEndDate').value;
        const notes = document.getElementById('nutritionPlanNotes').value.trim();
        const templateId = parseInt(document.getElementById('nutritionPlanTemplateSelect').value);
        const price = parseFloat(document.getElementById('nutritionPlanPrice')?.value) || 0;
        const currency = document.getElementById('nutritionPlanCurrency')?.value || 'EUR';

        if (!clientId) { errorEl.textContent = 'Odaberi klijenta.'; return; }
        if (!name) { errorEl.textContent = 'Naziv je obavezan.'; return; }
        if (!startDate || !endDate) { errorEl.textContent = 'Period je obavezan.'; return; }

        const payload = {
            clientId, name,
            startDate: new Date(startDate).toISOString(),
            endDate: new Date(endDate).toISOString(),
            notes: notes || null,
            price, currency
        };

        try {
            const url = templateId
                ? `/nutrition-plans/templates/${templateId}/clone`
                : '/nutrition-plans';
            const plan = await API.post(url, payload);
            this.resetNewPlanForm();
            await this.load();
            await this.showDetail(plan.id);
        } catch (err) {
            errorEl.textContent = err.message;
        }
    },

    async showDetail(id) {
        try {
            this.current = await API.get(`/nutrition-plans/${id}`);
            // If accessed from templates tab, hide nutrition list specifics
            const plansList = document.getElementById('nutritionPlansList');
            if (plansList) plansList.classList.add('hidden');
            const newBtn = document.getElementById('newNutritionPlanBtn');
            if (newBtn) newBtn.classList.add('hidden');
            document.getElementById('nutritionDetail').classList.remove('hidden');

            document.getElementById('nutritionDetailName').textContent = this.current.name;
            const metaEl = document.getElementById('nutritionDetailMeta');
            if (this.current.isTemplate) {
                metaEl.textContent = `Predložak · ${this.current.days.length} ${this.current.days.length === 1 ? 'dan' : 'dana'}`;
            } else {
                metaEl.innerHTML = `<a href="#" class="client-link" data-client-id="${this.current.clientId}">${this.escape(this.current.clientName)}</a>
                    · ${this.formatDate(this.current.startDate)} → ${this.formatDate(this.current.endDate)}`;
                const link = metaEl.querySelector('.client-link');
                if (link) link.addEventListener('click', e => {
                    e.preventDefault();
                    App.switchTrainerView('clients');
                    Trainers.showClientDetail(link.dataset.clientId);
                });
            }
            document.getElementById('nutritionDetailNotes').textContent =
                this.current.notes ? `Napomene: ${this.current.notes}` : '';

            const saveAs = document.getElementById('saveNutritionAsTemplateBtn');
            if (saveAs) saveAs.style.display = this.current.isTemplate ? 'none' : '';

            // Payment box (hide for template)
            const payBox = document.getElementById('nutritionDetailPayment');
            if (payBox) {
                if (this.current.isTemplate) payBox.innerHTML = '';
                else this.renderTrainerPaymentBox(this.current);
            }

            this.renderDays();
        } catch (err) { alert(err.message); }
    },

    showList() {
        document.getElementById('nutritionDetail').classList.add('hidden');
        const plansList = document.getElementById('nutritionPlansList');
        if (plansList) plansList.classList.remove('hidden');
        const newBtn = document.getElementById('newNutritionPlanBtn');
        if (newBtn) newBtn.classList.remove('hidden');
        this.current = null;
        this.load();
    },

    renderDays() {
        const container = document.getElementById('nutritionDaysList');
        if (!this.current.days.length) {
            container.innerHTML = '<p class="muted">Bez dana. Dodaj prvi dan iznad (npr. Ponedjeljak — Trening dan).</p>';
            return;
        }
        container.innerHTML = this.current.days.map(d => this.renderDay(d)).join('');
        this.bindDayEvents(container);
    },

    renderDay(d) {
        const totals = this.computeDayTotals(d);
        const targetLabel = d.totalCaloriesTarget
            ? ` · cilj <strong>${d.totalCaloriesTarget} kcal</strong>`
            : '';
        return `
            <div class="exercise-block" data-day-id="${d.id}">
                <div class="row" style="justify-content: space-between; align-items: center;">
                    <h4>${this.dayNames[d.dayOfWeek]} — ${this.escape(d.label)}</h4>
                    <button class="icon-btn delete-nut-day-btn" data-day-id="${d.id}" title="Obriši dan">×</button>
                </div>
                <div class="muted small">
                    Ukupno: <strong>${totals.calories} kcal</strong> · P ${totals.protein.toFixed(0)}g · UH ${totals.carbs.toFixed(0)}g · M ${totals.fat.toFixed(0)}g
                    ${targetLabel}
                </div>
                ${d.meals.length === 0
                    ? '<p class="muted small">Bez obroka.</p>'
                    : d.meals.map(m => this.renderMeal(m)).join('')}
                <div class="nutrition-add-form nf-meal-form add-meal-form" data-day-id="${d.id}">
                    <div class="nf-field">
                        <label>Tip obroka</label>
                        <select class="meal-type-select">
                            ${this.mealTypeOptions.map(mt => `<option value="${mt}">${this.mealTypeLabels[mt]}</option>`).join('')}
                        </select>
                    </div>
                    <div class="nf-field">
                        <label>Vrijeme</label>
                        <input type="time" class="meal-time-input">
                    </div>
                    <div class="nf-field">
                        <label>Napomena</label>
                        <input type="text" class="meal-notes-input" placeholder="opcionalno">
                    </div>
                    <button class="nf-btn add-meal-btn">+ Obrok</button>
                </div>
            </div>
        `;
    },

    renderMeal(m) {
        const totals = this.computeMealTotals(m);
        return `
            <div class="meal-block" data-meal-id="${m.id}" style="border-left: 3px solid #4dabf7; padding-left: 0.75rem; margin: 0.75rem 0;">
                <div class="row" style="justify-content: space-between; align-items: center;">
                    <strong>${this.mealTypeLabels[m.mealType] || m.mealType}${m.time ? ' · ' + m.time : ''}</strong>
                    <button class="icon-btn delete-meal-btn" data-meal-id="${m.id}" title="Obriši obrok">×</button>
                </div>
                ${m.notes ? `<div class="muted small">${this.escape(m.notes)}</div>` : ''}
                <div class="muted small">${totals.calories} kcal · P ${totals.protein.toFixed(0)}g · UH ${totals.carbs.toFixed(0)}g · M ${totals.fat.toFixed(0)}g</div>
                ${m.items.length === 0
                    ? '<p class="muted small">Bez stavki.</p>'
                    : `<table class="meal-items" style="width:100%; margin-top:0.4rem; font-size:0.9rem;">
                        <thead><tr style="text-align:left; color:#a0a8b0;">
                            <th>Namirnica</th><th>Količina</th><th>kcal</th><th>P</th><th>UH</th><th>M</th><th></th>
                        </tr></thead>
                        <tbody>
                            ${m.items.map(it => `
                                <tr>
                                    <td>${this.escape(it.description)}</td>
                                    <td>${this.escape(it.quantity || '')}</td>
                                    <td>${it.calories ?? ''}</td>
                                    <td>${it.proteinG ?? ''}</td>
                                    <td>${it.carbsG ?? ''}</td>
                                    <td>${it.fatG ?? ''}</td>
                                    <td><button class="btn-delete-icon delete-item-btn" data-item-id="${it.id}" title="Obriši">🗑</button></td>
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>`}
                <div class="nutrition-add-form nf-item-form add-item-form" data-meal-id="${m.id}">
                    <div class="nf-field">
                        <label>Namirnica</label>
                        <input type="text" class="item-desc" placeholder="npr. Pileća prsa">
                    </div>
                    <div class="nf-field">
                        <label>Količina (g)</label>
                        <input type="text" class="item-qty" placeholder="u gramima — npr. 150">
                    </div>
                    <div class="nf-field">
                        <label>kcal</label>
                        <input type="number" class="item-cal" min="0" max="20000">
                    </div>
                    <div class="nf-field nf-macros-wrap">
                        <label>Makronutrijenti (g) — P · UH · M</label>
                        <div class="nf-macros">
                            <input type="number" class="item-p" placeholder="P" step="0.1" min="0">
                            <input type="number" class="item-c" placeholder="UH" step="0.1" min="0">
                            <input type="number" class="item-f" placeholder="M" step="0.1" min="0">
                        </div>
                    </div>
                    <button class="nf-btn add-item-btn">+ Dodaj</button>
                </div>
            </div>
        `;
    },

    bindDayEvents(container) {
        container.querySelectorAll('.delete-nut-day-btn').forEach(btn => {
            btn.addEventListener('click', () => this.deleteDay(parseInt(btn.dataset.dayId)));
        });
        container.querySelectorAll('.delete-meal-btn').forEach(btn => {
            btn.addEventListener('click', () => this.deleteMeal(parseInt(btn.dataset.mealId)));
        });
        container.querySelectorAll('.delete-item-btn').forEach(btn => {
            btn.addEventListener('click', () => this.deleteItem(parseInt(btn.dataset.itemId)));
        });
        container.querySelectorAll('.add-meal-btn').forEach(btn => {
            btn.addEventListener('click', e => this.addMeal(e.target.closest('.add-meal-form')));
        });
        container.querySelectorAll('.add-item-btn').forEach(btn => {
            btn.addEventListener('click', e => this.addItem(e.target.closest('.add-item-form')));
        });
    },

    computeMealTotals(m) {
        const t = { calories: 0, protein: 0, carbs: 0, fat: 0 };
        for (const it of m.items) {
            t.calories += it.calories || 0;
            t.protein += parseFloat(it.proteinG) || 0;
            t.carbs += parseFloat(it.carbsG) || 0;
            t.fat += parseFloat(it.fatG) || 0;
        }
        return t;
    },

    computeDayTotals(d) {
        const t = { calories: 0, protein: 0, carbs: 0, fat: 0 };
        for (const m of d.meals) {
            const mt = this.computeMealTotals(m);
            t.calories += mt.calories;
            t.protein += mt.protein;
            t.carbs += mt.carbs;
            t.fat += mt.fat;
        }
        return t;
    },

    async addDay() {
        if (!this.current) return;
        const dow = document.getElementById('newNutritionDayOfWeek').value;
        const label = document.getElementById('newNutritionDayLabel').value.trim();
        const calStr = document.getElementById('newNutritionDayCalories').value;
        if (!label) { alert('Unesi oznaku dana.'); return; }
        try {
            await API.post(`/nutrition-plans/${this.current.id}/days`, {
                dayOfWeek: dow, label,
                totalCaloriesTarget: calStr ? parseInt(calStr) : null
            });
            document.getElementById('newNutritionDayLabel').value = '';
            document.getElementById('newNutritionDayCalories').value = '';
            await this.showDetail(this.current.id);
        } catch (err) { alert(err.message); }
    },

    async deleteDay(dayId) {
        if (!confirm('Obrisati ovaj dan i sve obroke?')) return;
        try {
            await API.delete(`/nutrition-plans/days/${dayId}`);
            await this.showDetail(this.current.id);
        } catch (err) { alert(err.message); }
    },

    async addMeal(form) {
        const dayId = parseInt(form.dataset.dayId);
        const mealType = form.querySelector('.meal-type-select').value;
        const time = form.querySelector('.meal-time-input').value;
        const notes = form.querySelector('.meal-notes-input').value.trim();
        try {
            await API.post(`/nutrition-plans/days/${dayId}/meals`, {
                mealType, time: time || null, notes: notes || null
            });
            await this.showDetail(this.current.id);
        } catch (err) { alert(err.message); }
    },

    async deleteMeal(mealId) {
        if (!confirm('Obrisati ovaj obrok?')) return;
        try {
            await API.delete(`/nutrition-plans/meals/${mealId}`);
            await this.showDetail(this.current.id);
        } catch (err) { alert(err.message); }
    },

    async addItem(form) {
        const mealId = parseInt(form.dataset.mealId);
        const desc = form.querySelector('.item-desc').value.trim();
        if (!desc) { alert('Unesi naziv namirnice.'); return; }
        const qty = form.querySelector('.item-qty').value.trim();
        const cal = form.querySelector('.item-cal').value;
        const p = form.querySelector('.item-p').value;
        const c = form.querySelector('.item-c').value;
        const f = form.querySelector('.item-f').value;
        try {
            await API.post(`/nutrition-plans/meals/${mealId}/items`, {
                description: desc,
                quantity: qty || null,
                calories: cal ? parseInt(cal) : null,
                proteinG: p ? parseFloat(p) : null,
                carbsG: c ? parseFloat(c) : null,
                fatG: f ? parseFloat(f) : null
            });
            await this.showDetail(this.current.id);
        } catch (err) { alert(err.message); }
    },

    async deleteItem(itemId) {
        try {
            await API.delete(`/nutrition-plans/items/${itemId}`);
            await this.showDetail(this.current.id);
        } catch (err) { alert(err.message); }
    },

    async downloadPdf() {
        if (!this.current) return;
        const token = API.getToken();
        try {
            const res = await fetch(`/api/v1/nutrition-plans/${this.current.id}/pdf`, {
                headers: token ? { 'Authorization': `Bearer ${token}` } : {}
            });
            if (!res.ok) {
                const text = await res.text();
                let msg = text;
                try { msg = JSON.parse(text)?.message || text; } catch { /* ignore */ }
                alert(msg);
                return;
            }
            const blob = await res.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `prehrana_${this.current.id}.pdf`;
            document.body.appendChild(a);
            a.click();
            a.remove();
            URL.revokeObjectURL(url);
        } catch (err) { alert(err.message); }
    },

    async openQrShare() {
        if (!this.current) return;
        if (this.current.isTemplate) { alert('Predlošci se ne dijele.'); return; }
        const modal = document.getElementById('qrShareModal');
        const imgWrap = document.getElementById('qrShareImage');
        const urlEl = document.getElementById('qrShareUrl');
        const msg = document.getElementById('qrShareMsg');

        imgWrap.innerHTML = `<p class="muted small">${I18n.t('common.loading')}</p>`;
        urlEl.textContent = '';
        msg.textContent = '';
        modal.classList.remove('hidden');

        try {
            const data = await API.post(`/nutrition-plans/${this.current.id}/share`);
            imgWrap.innerHTML = `<img src="data:image/png;base64,${data.qrPngBase64}" alt="QR">`;
            urlEl.textContent = data.url;
        } catch (err) {
            imgWrap.innerHTML = '';
            msg.textContent = err.message || I18n.t('qr.error');
        }
    },

    async saveAsTemplate() {
        if (!this.current) return;
        const defaultName = `${this.current.name} (predložak)`;
        const name = prompt('Naziv predloška prehrane:', defaultName);
        if (!name || !name.trim()) return;
        try {
            await API.post(`/nutrition-plans/${this.current.id}/save-as-template`, {
                name: name.trim(),
                notes: this.current.notes || null
            });
            alert('Predložak prehrane spremljen.');
        } catch (err) { alert(err.message); }
    },

    formatDate(s) {
        return new Date(s).toLocaleDateString('hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s || '';
        return div.innerHTML;
    }
};
