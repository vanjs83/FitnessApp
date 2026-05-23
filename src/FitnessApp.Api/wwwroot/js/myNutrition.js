const MyNutrition = {
    list: [],
    current: null,

    init() {
        const back = document.getElementById('backToMyNutritionBtn');
        if (back) back.addEventListener('click', () => this.showList());
        const pdfBtn = document.getElementById('myNutritionPdfBtn');
        if (pdfBtn) pdfBtn.addEventListener('click', () => this.downloadPdf());
        const qrBtn = document.getElementById('myNutritionQrBtn');
        if (qrBtn) qrBtn.addEventListener('click', () => this.openQrShare());
    },

    async openQrShare() {
        if (!this.current) return;
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

    async downloadPdf() {
        if (!this.current) return;
        const token = API.getToken();
        try {
            const res = await fetch(`/api/nutrition-plans/${this.current.id}/pdf`, {
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

    async loadMine(clientId) {
        const container = document.getElementById('myNutritionList');
        try {
            this.list = await API.get(`/nutrition-plans/client/${clientId}`);
            document.getElementById('myNutritionDetail').classList.add('hidden');
            container.classList.remove('hidden');
            if (!this.list.length) {
                container.innerHTML = `<p class="muted">${I18n.t('nutrition.empty.client')}</p>`;
                return;
            }
            container.innerHTML = this.list.map(p => `
                <div class="list-item" data-id="${p.id}">
                    <div>
                        <h4>${this.escape(p.name)}</h4>
                        <div class="meta">
                            ${this.formatDate(p.startDate)} → ${this.formatDate(p.endDate)}
                            · ${p.dayCount} ${I18n.t(p.dayCount === 1 ? 'nutrition.dayUnit.one' : 'nutrition.dayUnit.many')}
                        </div>
                    </div>
                    <span class="muted">→</span>
                </div>
            `).join('');
            container.querySelectorAll('.list-item').forEach(el => {
                el.addEventListener('click', () => this.showDetail(parseInt(el.dataset.id)));
            });
        } catch (err) {
            container.innerHTML = `<p class="muted">${I18n.t('app.errorPrefix')}${this.escape(err.message)}</p>`;
        }
    },

    async showDetail(id) {
        try {
            this.current = await API.get(`/nutrition-plans/${id}`);
            document.getElementById('myNutritionList').classList.add('hidden');
            document.getElementById('myNutritionDetail').classList.remove('hidden');
            document.getElementById('myNutritionName').textContent = this.current.name;
            document.getElementById('myNutritionMeta').textContent =
                `${this.formatDate(this.current.startDate)} → ${this.formatDate(this.current.endDate)}`;

            this.renderPaymentBox(this.current);

            if (this.current.isLocked) {
                document.getElementById('myNutritionNotes').textContent = '';
                document.getElementById('myNutritionDays').innerHTML =
                    `<p class="muted">${I18n.t('payment.locked')}</p>`;
                return;
            }

            document.getElementById('myNutritionNotes').textContent =
                this.current.notes ? `${I18n.t('nutrition.trainerNotesPrefix')} ${this.current.notes}` : '';

            this.renderDays();
        } catch (err) { alert(err.message); }
    },

    renderPaymentBox(plan) {
        const box = document.getElementById('myNutritionPayment');
        if (!box) return;
        if (parseFloat(plan.price || 0) === 0) {
            box.innerHTML = `<span class="muted small">${I18n.t('nutrition.freeplan')}</span>`;
            return;
        }
        const statusLabel = plan.paymentStatus === 'Approved' ? I18n.t('nutrition.paymentApproved')
            : I18n.paymentStatus(plan.paymentStatus);
        const cls = plan.paymentStatus === 'Approved' ? 'badge approved'
            : plan.paymentStatus === 'PaymentClaimed' ? 'badge claimed' : 'badge pending';
        let action = '';
        if (plan.paymentStatus === 'Pending') {
            const price = `${parseFloat(plan.price).toFixed(2)} ${plan.currency || 'EUR'}`;
            action = `<button id="claimNutPaymentBtn" data-plan-id="${plan.id}">${I18n.tf('nutrition.payBtn', { price })}</button>`;
        } else if (plan.paymentStatus === 'PaymentClaimed') {
            action = `<span class="muted small">${I18n.t('payment.waitingApproval')}</span>`;
        }
        box.innerHTML = `
            <div><strong>${I18n.t('nutrition.priceLabel')}</strong> ${parseFloat(plan.price).toFixed(2)} ${plan.currency || 'EUR'} · <span class="${cls}">${statusLabel}</span></div>
            ${action}
        `;
        const btn = document.getElementById('claimNutPaymentBtn');
        if (btn) btn.addEventListener('click', () => this.claimPayment(plan.id));
    },

    async claimPayment(id) {
        try {
            await API.post(`/nutrition-plans/${id}/claim-payment`, {});
            await this.showDetail(id);
        } catch (err) { alert(err.message); }
    },

    showList() {
        document.getElementById('myNutritionDetail').classList.add('hidden');
        document.getElementById('myNutritionList').classList.remove('hidden');
        this.current = null;
    },

    renderDays() {
        const container = document.getElementById('myNutritionDays');
        if (!this.current.days.length) {
            container.innerHTML = `<p class="muted">${I18n.t('nutrition.empty.days')}</p>`;
            return;
        }
        container.innerHTML = this.current.days.map(d => this.renderDay(d)).join('');
    },

    renderDay(d) {
        const totals = this.computeDayTotals(d);
        const targetLabel = d.totalCaloriesTarget
            ? ` · ${I18n.t('nutrition.dayCalorieTarget')} <strong>${d.totalCaloriesTarget} kcal</strong>`
            : '';
        const P = I18n.t('nutrition.col.protein'), C = I18n.t('nutrition.col.carbs'), F = I18n.t('nutrition.col.fat');
        return `
            <div class="exercise-block">
                <h4>${I18n.dayName(d.dayOfWeek)} — ${this.escape(d.label)}</h4>
                <div class="muted small">
                    ${I18n.t('nutrition.dayTotalsPrefix')} <strong>${totals.calories} kcal</strong> · ${P} ${totals.protein.toFixed(0)}g · ${C} ${totals.carbs.toFixed(0)}g · ${F} ${totals.fat.toFixed(0)}g
                    ${targetLabel}
                </div>
                ${d.meals.length === 0
                    ? `<p class="muted small">${I18n.t('nutrition.empty.meals')}</p>`
                    : d.meals.map(m => this.renderMeal(m)).join('')}
            </div>
        `;
    },

    renderMeal(m) {
        const totals = this.computeMealTotals(m);
        const P = I18n.t('nutrition.col.protein'), C = I18n.t('nutrition.col.carbs'), F = I18n.t('nutrition.col.fat');
        return `
            <div class="meal-block" style="border-left: 3px solid #4dabf7; padding-left: 0.75rem; margin: 0.75rem 0;">
                <strong>${I18n.mealLabel(m.mealType)}${m.time ? ' · ' + m.time : ''}</strong>
                ${m.notes ? `<div class="muted small">${this.escape(m.notes)}</div>` : ''}
                <div class="muted small">${totals.calories} kcal · ${P} ${totals.protein.toFixed(0)}g · ${C} ${totals.carbs.toFixed(0)}g · ${F} ${totals.fat.toFixed(0)}g</div>
                ${m.items.length === 0
                    ? `<p class="muted small">${I18n.t('nutrition.empty.items')}</p>`
                    : `<table class="meal-items" style="width:100%; margin-top:0.4rem; font-size:0.9rem;">
                        <thead><tr style="text-align:left; color:#a0a8b0;">
                            <th>${I18n.t('nutrition.col.food')}</th><th>${I18n.t('nutrition.col.amount')}</th><th>${I18n.t('nutrition.col.kcal')}</th><th>${P}</th><th>${C}</th><th>${F}</th>
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
                                </tr>
                            `).join('')}
                        </tbody>
                    </table>`}
            </div>
        `;
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

    formatDate(s) {
        return new Date(s).toLocaleDateString(I18n.lang === 'en' ? 'en-GB' : 'hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s || '';
        return div.innerHTML;
    }
};
