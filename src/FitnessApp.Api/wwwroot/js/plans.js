const Plans = {
    list: [],
    currentPlan: null,
    progressionData: [],
    progressionChart: null,
    myProgressionChart: null,
    dayNames: {
        Sunday: 'Nedjelja', Monday: 'Ponedjeljak', Tuesday: 'Utorak',
        Wednesday: 'Srijeda', Thursday: 'ÄŚetvrtak', Friday: 'Petak', Saturday: 'Subota',
        0: 'Nedjelja', 1: 'Ponedjeljak', 2: 'Utorak',
        3: 'Srijeda', 4: 'ÄŚetvrtak', 5: 'Petak', 6: 'Subota'
    },
    dayOrder: { Sunday: 0, Monday: 1, Tuesday: 2, Wednesday: 3, Thursday: 4, Friday: 5, Saturday: 6 },

    init() {
        document.getElementById('newPlanBtn').addEventListener('click', () => this.showNewPlanForm());
        document.getElementById('cancelPlanBtn').addEventListener('click', () => this.resetNewPlanForm());
        document.getElementById('savePlanBtn').addEventListener('click', () => this.create());
        document.getElementById('backToPlansBtn').addEventListener('click', () => this.showPlansList());
        document.getElementById('addDayBtn').addEventListener('click', () => this.addDay());
        document.getElementById('quickExerciseBtn').addEventListener('click', () => this.quickCreateExercise());

        const saveAsTplBtn = document.getElementById('saveAsTemplateBtn');
        if (saveAsTplBtn) saveAsTplBtn.addEventListener('click', () => this.saveAsTemplate());

        const trainerPdfBtn = document.getElementById('trainerPlanPdfBtn');
        if (trainerPdfBtn) trainerPdfBtn.addEventListener('click', () => this.downloadTrainerPdf());
        const trainerQrBtn = document.getElementById('trainerPlanQrBtn');
        if (trainerQrBtn) trainerQrBtn.addEventListener('click', () => this.openTrainerQr());
        const notifyBtn = document.getElementById('trainerPlanNotifyBtn');
        if (notifyBtn) notifyBtn.addEventListener('click', () => this.notifyClient());
        const pushBtn = document.getElementById('trainerPlanPushBtn');
        if (pushBtn) pushBtn.addEventListener('click', () => this.pushClient());
        const editBtn = document.getElementById('editPlanBtn');
        if (editBtn) editBtn.addEventListener('click', () => this.editCurrentPlan());
        const delBtn = document.getElementById('deletePlanBtn');
        if (delBtn) delBtn.addEventListener('click', () => this.deleteCurrentPlan());

        const newPlanForClientBtn = document.getElementById('trainerNewClientPlanBtn');
        if (newPlanForClientBtn) {
            newPlanForClientBtn.addEventListener('click', () => this.openForClient(Trainers.currentClient));
        }

        const backMy = document.getElementById('backToMyPlansBtn');
        if (backMy) backMy.addEventListener('click', () => this.showMyPlansList());

        const pdfBtn = document.getElementById('myPlanPdfBtn');
        if (pdfBtn) pdfBtn.addEventListener('click', () => this.downloadMyPlanPdf());

        const qrBtn = document.getElementById('myPlanQrBtn');
        if (qrBtn) qrBtn.addEventListener('click', () => this.openQrShare());

        const closeQr = document.getElementById('closeQrShareBtn');
        if (closeQr) closeQr.addEventListener('click', () => this.closeQrShare());
        const qrModal = document.getElementById('qrShareModal');
        if (qrModal) qrModal.addEventListener('click', e => { if (e.target === qrModal) this.closeQrShare(); });
    },

    async openForClient(client) {
        if (!client) return;
        App.switchTrainerView('plans');
        await this.showNewPlanForm(client.id);
    },

    async loadMine(clientId) {
        try {
            const list = await API.get(`/training-plans/client/${clientId}`);
            const container = document.getElementById('myPlansList');
            if (!list.length) {
                container.innerHTML = '<p class="muted">Trener ti joĹˇ nije zadao plan.</p>';
                return;
            }
            container.innerHTML = list.map(p => `
                <div class="list-item" data-id="${p.id}">
                    <div>
                        <h4>${this.escape(p.name)}</h4>
                        <div class="meta">
                            ${this.formatDate(p.startDate)} â†’ ${this.formatDate(p.endDate)}
                            Â· ${p.dayCount} ${p.dayCount === 1 ? 'dan' : 'dana'}
                            Â· ${this.formatPrice(p)}
                            Â· <span class="${this.paymentBadgeClass(p.paymentStatus)}">${this.paymentStatusLabel(p.paymentStatus)}</span>
                        </div>
                    </div>
                    <span class="muted">â†’</span>
                </div>
            `).join('');
            container.querySelectorAll('.list-item').forEach(el => {
                el.addEventListener('click', () => this.showMyPlanDetail(parseInt(el.dataset.id)));
            });
        } catch (err) {
            console.error(err);
        }
    },

    async showMyPlanDetail(planId) {
        try {
            if (!Exercises.list.length) await Exercises.load();
            const plan = await API.get(`/training-plans/${planId}`);
            this.currentMyPlanId = planId;
            document.getElementById('myPlansList').classList.add('hidden');
            document.getElementById('myPlanDetail').classList.remove('hidden');
            document.getElementById('myPlanName').textContent = plan.name;
            document.getElementById('myPlanMeta').textContent =
                `${this.formatDate(plan.startDate)} â†’ ${this.formatDate(plan.endDate)}`;

            this.renderClientPaymentBox(plan);

            const exportRow = document.getElementById('myPlanExportRow');
            if (exportRow) exportRow.classList.toggle('hidden', plan.isLocked);

            if (plan.isLocked) {
                document.getElementById('myPlanExpectations').textContent = '';
                document.getElementById('myPlanDays').innerHTML =
                    '<p class="muted">đź”’ SadrĹľaj plana je zakljuÄŤan dok trener ne odobri uplatu.</p>';
                return;
            }

            document.getElementById('myPlanExpectations').textContent =
                plan.trainerExpectations ? `OÄŤekivanja trenera: ${plan.trainerExpectations}` : '';

            const days = document.getElementById('myPlanDays');
            if (!plan.days.length) {
                days.innerHTML = '<p class="muted">Plan nema definirane dane.</p>';
                return;
            }
            const exOf = (exId) => Exercises.list.find(x => x.id === exId) || {};
            const hasVideo = (exId) => !!exOf(exId).videoUrl;
            days.innerHTML = plan.days.map(d => `
                <div class="exercise-block">
                    <h4>${this.dayNames[d.dayOfWeek]} â€” ${this.escape(d.label)}</h4>
                    ${d.exercises.length === 0
                        ? '<p class="muted small">Bez vjeĹľbi.</p>'
                        : d.exercises.map(pe => `
                            <div class="planned-row ${pe.isCompletedToday ? 'completed' : ''}">
                                <span class="planned-name exercise-link" data-exercise-id="${pe.exerciseId}" title="PrikaĹľi video / detalje vjeĹľbe">${Exercises.thumbHtml(exOf(pe.exerciseId), 'exercise-thumb exercise-thumb-sm')}<strong>${this.escape(pe.exerciseName)}</strong>${hasVideo(pe.exerciseId) ? ' <span class="badge video">â–¶</span>' : ''}</span>
                                <span class="planned-target">${this.planTargetLabel(pe)}</span>
                                ${pe.restSeconds ? `<span class="planned-rest">${pe.restSeconds}s</span>` : ''}
                                <span class="planned-actions">
                                    <button class="btn-toggle toggle-pe-btn" data-pe-id="${pe.id}" title="${pe.isCompletedToday ? 'PoniĹˇti odraÄ‘eno' : 'OznaÄŤi odraÄ‘eno'}">
                                        ${pe.isCompletedToday ? 'âś… OdraÄ‘eno' : 'âś“ Odradi'}
                                    </button>
                                </span>
                            </div>
                            <div class="sets-log">
                                ${(pe.recentPerformedSets || []).map(s => `
                                    <div class="set-row">
                                        <span class="set-num">#${s.setNumber}</span>
                                        <span>${s.actualWeightKg} kg</span>
                                        <span>${s.actualReps} rep</span>
                                        <span class="muted small">${this.formatDate(s.performedAt)}</span>
                                        <button class="icon-btn delete-set-btn" data-set-id="${s.id}" title="ObriĹˇi seriju">Ă—</button>
                                    </div>
                                `).join('')}
                                <div class="add-set-form" data-pe-id="${pe.id}">
                                    <input type="number" class="set-num-input" placeholder="#" min="1" value="${(pe.recentPerformedSets || []).length + 1}">
                                    <input type="number" class="weight-input" placeholder="kg" step="0.5" min="0">
                                    <input type="number" class="reps-input" placeholder="rep" min="1">
                                    <button class="add-set-btn">+ BiljeĹľi</button>
                                </div>
                            </div>
                        `).join('')}
                </div>
            `).join('');

            days.querySelectorAll('.toggle-pe-btn').forEach(btn => {
                btn.addEventListener('click', () => this.toggleCompletion(parseInt(btn.dataset.peId), planId));
            });

            days.querySelectorAll('.add-set-btn').forEach(btn => {
                btn.addEventListener('click', e => this.logSet(e.target.closest('.add-set-form'), planId));
            });

            days.querySelectorAll('.delete-set-btn').forEach(btn => {
                btn.addEventListener('click', () => this.deletePerformedSet(parseInt(btn.dataset.setId), planId));
            });

            days.querySelectorAll('.exercise-link').forEach(el => {
                el.addEventListener('click', () => Exercises.showDetail(parseInt(el.dataset.exerciseId)));
            });
        } catch (err) {
            alert(err.message);
        }
    },

    async loadProgression(planId, selectId, canvasId, summaryId, chartKey) {
        const select = document.getElementById(selectId);
        const summary = document.getElementById(summaryId);
        if (!select) return;
        try {
            this.progressionData = await API.get(`/training-plans/${planId}/weight-progression`);
        } catch (err) {
            this.progressionData = [];
            if (summary) summary.textContent = err.message;
        }
        if (!this.progressionData.length) {
            select.innerHTML = '<option value="">â€” plan nema vjeĹľbi â€”</option>';
            this.clearProgressionChart(chartKey);
            if (summary) summary.textContent = 'Plan nema definirane vjeĹľbe.';
            return;
        }
        select.innerHTML = this.progressionData.map(p =>
            `<option value="${p.exerciseId}">${this.escape(p.exerciseName)}${p.muscleGroup ? ' (' + this.escape(p.muscleGroup) + ')' : ''}</option>`
        ).join('');
        this.renderProgression(parseInt(select.value), canvasId, summaryId, chartKey);
    },

    renderProgression(exerciseId, canvasId, summaryId, chartKey) {
        const item = this.progressionData.find(p => p.exerciseId === exerciseId);
        const summary = document.getElementById(summaryId);
        if (!item) { this.clearProgressionChart(chartKey); if (summary) summary.textContent = ''; return; }

        if (!item.points.length) {
            this.clearProgressionChart(chartKey);
            if (summary) summary.textContent = `Target ${item.targetSets}Ă—${item.targetReps} @ ${item.targetWeightKg} kg Â· klijent joĹˇ nije radio ovu vjeĹľbu u periodu plana.`;
            return;
        }

        const dateCounts = {};
        item.points.forEach(d => {
            const key = new Date(d.date).toDateString();
            dateCounts[key] = (dateCounts[key] || 0) + 1;
        });
        const labels = item.points.map(d => {
            const dateStr = new Date(d.date).toLocaleDateString('hr-HR', { day: '2-digit', month: '2-digit', year: '2-digit' });
            return dateCounts[new Date(d.date).toDateString()] > 1 ? `${dateStr} Â· S${d.setNumber}` : dateStr;
        });
        const maxWeights = item.points.map(d => d.maxWeight);
        const target = Number(item.targetWeightKg);
        const targetLine = item.points.map(() => target);

        if (this[chartKey]) this[chartKey].destroy();
        const ctx = document.getElementById(canvasId).getContext('2d');
        this[chartKey] = new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [
                    {
                        label: 'TeĹľina (kg)',
                        data: maxWeights,
                        borderColor: '#4dabf7',
                        backgroundColor: 'rgba(77,171,247,0.15)',
                        tension: 0.25,
                        pointRadius: 4
                    },
                    {
                        label: `Target (${target} kg)`,
                        data: targetLine,
                        borderColor: '#fc74b1',
                        borderDash: [6, 4],
                        pointRadius: 0,
                        backgroundColor: 'transparent',
                        tension: 0
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { intersect: false, mode: 'index' },
                plugins: {
                    legend: { labels: { color: '#e6e8eb' } },
                    tooltip: { backgroundColor: '#1a2028', borderColor: '#2a3340', borderWidth: 1 }
                },
                scales: {
                    x: { ticks: { color: '#a0a8b0' }, grid: { color: '#2a3340' } },
                    y: { beginAtZero: true, ticks: { color: '#e6e8eb' }, grid: { color: '#2a3340' }, title: { display: true, text: 'kg', color: '#a0a8b0' } }
                }
            }
        });

        const last = item.points[item.points.length - 1].maxWeight;
        const first = item.points[0].maxWeight;
        const delta = (last - first).toFixed(1);
        const setCount = item.points.length;
        const targetDelta = (last - target).toFixed(1);
        if (summary) {
            summary.innerHTML =
                `${setCount} ${setCount === 1 ? 'serija' : 'serija'} Â· ` +
                `Zadnja teĹľina: <strong>${last} kg</strong> Â· ` +
                `Î” od poÄŤetka: <strong>${delta >= 0 ? '+' : ''}${delta} kg</strong> Â· ` +
                `Target: ${target} kg (${targetDelta >= 0 ? '+' : ''}${targetDelta} kg vs target)`;
        }
    },

    clearProgressionChart(chartKey) {
        if (this[chartKey]) { this[chartKey].destroy(); this[chartKey] = null; }
    },

    async logSet(form, planId) {
        const peId = parseInt(form.dataset.peId);
        const setNumber = parseInt(form.querySelector('.set-num-input').value);
        const weight = parseFloat(form.querySelector('.weight-input').value);
        const reps = parseInt(form.querySelector('.reps-input').value);

        if (!setNumber || isNaN(weight) || !reps) {
            alert('Popuni broj serije, teĹľinu i ponavljanja.');
            return;
        }

        try {
            await API.post(`/training-plans/exercises/${peId}/performed-sets`, {
                setNumber,
                actualReps: reps,
                actualWeightKg: weight
            });
            await this.showMyPlanDetail(planId);
        } catch (err) {
            alert(err.message);
        }
    },

    async deletePerformedSet(setId, planId) {
        if (!confirm('Obrisati seriju?')) return;
        try {
            await API.delete(`/training-plans/performed-sets/${setId}`);
            await this.showMyPlanDetail(planId);
        } catch (err) {
            alert(err.message);
        }
    },

    async toggleCompletion(peId, planId) {
        try {
            await API.post(`/training-plans/exercises/${peId}/toggle-today`);
            await this.showMyPlanDetail(planId);
        } catch (err) {
            alert(err.message);
        }
    },

    showMyPlansList() {
        document.getElementById('myPlanDetail').classList.add('hidden');
        document.getElementById('myPlansList').classList.remove('hidden');
    },

    async load() {
        try {
            this.list = await API.get('/training-plans/mine');
            this.render();
        } catch (err) {
            console.error(err);
        }
    },

    render() {
        const container = document.getElementById('plansList');
        if (this.list.length === 0) {
            container.innerHTML = '<p class="muted">JoĹˇ nemaĹˇ kreiranih planova. Klikni "+ Novi plan".</p>';
            return;
        }
        container.innerHTML = this.list.map(p => `
            <div class="list-item" data-id="${p.id}">
                <div>
                    <h4>${this.escape(p.name)}</h4>
                    <div class="meta">
                        ${this.escape(p.clientName)}
                        Â· ${this.formatDate(p.startDate)} â†’ ${this.formatDate(p.endDate)}
                        Â· ${p.dayCount} ${p.dayCount === 1 ? 'dan' : 'dana'}
                        Â· ${this.formatPrice(p)}
                        Â· <span class="${this.paymentBadgeClass(p.paymentStatus)}">${this.paymentStatusLabel(p.paymentStatus)}</span>
                    </div>
                </div>
                <span class="planned-actions">
                    <button class="btn-delete-icon delete-plan-btn" data-plan-id="${p.id}" data-plan-name="${this.escape(p.name)}" title="ObriĹˇi plan">đź—‘</button>
                    <span class="muted">â†’</span>
                </span>
            </div>
        `).join('');

        container.querySelectorAll('.list-item').forEach(el => {
            el.addEventListener('click', e => {
                if (e.target.closest('.delete-plan-btn')) return;
                this.showPlanDetail(parseInt(el.dataset.id));
            });
        });

        container.querySelectorAll('.delete-plan-btn').forEach(btn => {
            btn.addEventListener('click', async e => {
                e.stopPropagation();
                const planId = parseInt(btn.dataset.planId);
                const name = btn.dataset.planName;
                if (!confirm(`Obrisati plan "${name}"? Svi odraÄ‘eni setovi i oznake odraÄ‘enog se briĹˇu trajno.`)) return;
                try {
                    await API.delete(`/training-plans/${planId}`);
                    await this.load();
                } catch (err) {
                    alert(err.message);
                }
            });
        });
    },

    async showNewPlanForm(preselectClientId) {
        if (!Trainers.clients || Trainers.clients.length === 0) {
            await Trainers.load();
        }
        const select = document.getElementById('planClientSelect');
        if (Trainers.clients.length === 0) {
            select.innerHTML = '<option value="">â€” nema klijenata, prvo dodaj klijenta â€”</option>';
        } else {
            select.innerHTML = Trainers.clients.map(c =>
                `<option value="${c.id}">${this.escape(c.fullName || c.email)}</option>`
            ).join('');
            if (preselectClientId) select.value = preselectClientId;
        }

        const tplSel = document.getElementById('planTemplateSelect');
        if (tplSel) {
            try {
                const templates = await API.get('/training-plans/templates');
                tplSel.innerHTML = '<option value="">â€” bez predloĹˇka (prazan plan) â€”</option>' +
                    templates.map(t => `<option value="${t.id}">${this.escape(t.name)} (${t.dayCount} ${t.dayCount === 1 ? 'dan' : 'dana'})</option>`).join('');
            } catch (err) {
                console.error(err);
                tplSel.innerHTML = '<option value="">â€” bez predloĹˇka â€”</option>';
            }
        }

        document.getElementById('newPlanForm').classList.remove('hidden');
    },

    resetNewPlanForm() {
        document.getElementById('planName').value = '';
        document.getElementById('planStartDate').value = '';
        document.getElementById('planEndDate').value = '';
        document.getElementById('planExpectations').value = '';
        document.getElementById('planPrice').value = '0';
        document.getElementById('planCurrency').value = 'EUR';
        const tplSel = document.getElementById('planTemplateSelect');
        if (tplSel) tplSel.value = '';
        document.getElementById('newPlanError').textContent = '';
        document.getElementById('newPlanForm').classList.add('hidden');
    },

    async create() {
        const errorEl = document.getElementById('newPlanError');
        errorEl.textContent = '';

        const clientId = document.getElementById('planClientSelect').value;
        const name = document.getElementById('planName').value.trim();
        const startDate = document.getElementById('planStartDate').value;
        const endDate = document.getElementById('planEndDate').value;
        const expectations = document.getElementById('planExpectations').value.trim();
        const tplSel = document.getElementById('planTemplateSelect');
        const templateId = tplSel ? parseInt(tplSel.value) : 0;

        if (!clientId) { errorEl.textContent = 'Odaberi klijenta.'; return; }
        if (!name) { errorEl.textContent = 'Naziv je obavezan.'; return; }
        if (!startDate || !endDate) { errorEl.textContent = 'Period je obavezan.'; return; }

        const price = parseFloat(document.getElementById('planPrice').value) || 0;
        const currency = document.getElementById('planCurrency').value || 'EUR';

        const payload = {
            clientId,
            name,
            startDate: new Date(startDate).toISOString(),
            endDate: new Date(endDate).toISOString(),
            trainerExpectations: expectations || null,
            price,
            currency
        };

        try {
            const url = templateId
                ? `/training-plans/templates/${templateId}/clone`
                : '/training-plans';
            const plan = await API.post(url, payload);
            this.resetNewPlanForm();
            await this.load();
            await this.showPlanDetail(plan.id);
        } catch (err) {
            errorEl.textContent = err.message;
        }
    },

    async saveAsTemplate() {
        if (!this.currentPlan) return;
        const defaultName = `${this.currentPlan.name} (predloĹľak)`;
        const name = prompt('Naziv predloĹˇka:', defaultName);
        if (!name || !name.trim()) return;
        try {
            await API.post(`/training-plans/${this.currentPlan.id}/save-as-template`, {
                name: name.trim(),
                trainerExpectations: this.currentPlan.trainerExpectations || null
            });
            alert('PredloĹľak spremljen. NaÄ‡i Ä‡eĹˇ ga u tabu "PredloĹˇci".');
        } catch (err) {
            alert(err.message);
        }
    },

    async downloadTrainerPdf() {
        if (!this.currentPlan) return;
        const token = API.getToken();
        try {
            const res = await fetch(`/api/v1/training-plans/${this.currentPlan.id}/pdf`, {
                headers: token ? { 'Authorization': `Bearer ${token}` } : {}
            });
            if (!res.ok) {
                const text = await res.text();
                let msg = text;
                try { msg = JSON.parse(text)?.message || text; } catch { /* ignore */ }
                alert(msg); return;
            }
            const blob = await res.blob();
            const url = URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url; a.download = `plan_${this.currentPlan.id}.pdf`;
            document.body.appendChild(a); a.click(); a.remove();
            URL.revokeObjectURL(url);
        } catch (err) { alert(err.message); }
    },

    async deleteCurrentPlan() {
        if (!this.currentPlan) return;
        if (!confirm(`Obrisati plan "${this.currentPlan.name}"? Svi odraÄ‘eni setovi i oznake odraÄ‘enog se briĹˇu trajno.`)) return;
        try {
            await API.delete(`/training-plans/${this.currentPlan.id}`);
            this.showPlansList();
        } catch (err) { alert(err.message); }
    },

    async editCurrentPlan() {
        if (!this.currentPlan) return;
        EditPlanModal.open({
            title: 'Uredi plan treninga',
            notesLabel: 'OÄŤekivanja trenera',
            plan: this.currentPlan,
            onSave: async (data) => {
                await API.put(`/training-plans/${this.currentPlan.id}`, {
                    name: data.name,
                    startDate: new Date(data.startDate).toISOString(),
                    endDate: new Date(data.endDate).toISOString(),
                    trainerExpectations: data.notes || null,
                    price: data.price,
                    currency: data.currency
                });
                await this.showPlanDetail(this.currentPlan.id);
            }
        });
    },

    async notifyClient() {
        if (!this.currentPlan) return;
        if (!confirm(`Poslati email klijentu (${this.escape(this.currentPlan.clientName)}) da je plan "${this.escape(this.currentPlan.name)}" spreman?`)) return;
        try {
            await API.post('/email/notify-plan-ready', {
                clientId: this.currentPlan.clientId,
                planName: this.currentPlan.name,
                planType: 'training',
                language: (typeof I18n !== 'undefined' && I18n.lang) ? I18n.lang : 'hr'
            });
            alert('âś“ Email poslan klijentu.');
        } catch (err) { alert(err.message); }
    },

    async pushClient() {
        if (!this.currentPlan) return;
        if (!confirm(`Poslati push notifikaciju klijentu (${this.escape(this.currentPlan.clientName)}) za plan "${this.escape(this.currentPlan.name)}"?`)) return;
        try {
            const res = await API.post('/notifications/notify-client-plan', {
                clientId: this.currentPlan.clientId,
                planName: this.currentPlan.name,
                planType: 'training'
            });
            if (res && res.activeTokens === 0) {
                alert('âš  Klijent nema registriran ureÄ‘aj za push notifikacije.');
            } else {
                alert('âś“ Push poslan klijentu.');
            }
        } catch (err) { alert(err.message); }
    },

    async openTrainerQr() {
        if (!this.currentPlan) return;
        const modal = document.getElementById('qrShareModal');
        const imgWrap = document.getElementById('qrShareImage');
        const urlEl = document.getElementById('qrShareUrl');
        const msg = document.getElementById('qrShareMsg');

        imgWrap.innerHTML = `<p class="muted small">${I18n.t('common.loading')}</p>`;
        urlEl.textContent = '';
        msg.textContent = '';
        modal.classList.remove('hidden');

        try {
            const data = await API.post(`/training-plans/${this.currentPlan.id}/share`);
            imgWrap.innerHTML = `<img src="data:image/png;base64,${data.qrPngBase64}" alt="QR">`;
            urlEl.textContent = data.url;
        } catch (err) {
            imgWrap.innerHTML = '';
            msg.textContent = err.message || I18n.t('qr.error');
        }
    },

    paymentStatusLabel(status) {
        if (status === 'Approved') return 'Odobreno';
        if (status === 'PaymentClaimed') return 'Klijent je platio â€” ÄŤeka odobrenje';
        return 'ÄŚeka plaÄ‡anje';
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
        const box = document.getElementById('planDetailPayment');
        if (!box) return;
        if (parseFloat(plan.price || 0) === 0) {
            box.innerHTML = `<span class="muted small">Besplatan plan â€” odmah dostupan klijentu.</span>`;
            return;
        }
        const statusLabel = this.paymentStatusLabel(plan.paymentStatus);
        const cls = this.paymentBadgeClass(plan.paymentStatus);
        let action = '';
        if (plan.paymentStatus === 'Approved') {
            action = `<button id="revokeApprovalBtn" class="secondary" data-plan-id="${plan.id}">đź”’ ZakljuÄŤaj nazad</button>`;
        } else {
            action = `<button id="approvePaymentBtn" data-plan-id="${plan.id}">Odobri plan</button>`;
        }
        box.innerHTML = `
            <div><strong>Cijena:</strong> ${this.formatPrice(plan)} Â· <span class="${cls}">${statusLabel}</span></div>
            ${action}
        `;
        const approveBtn = document.getElementById('approvePaymentBtn');
        if (approveBtn) approveBtn.addEventListener('click', () => this.approvePayment(plan.id));
        const revokeBtn = document.getElementById('revokeApprovalBtn');
        if (revokeBtn) revokeBtn.addEventListener('click', () => this.revokeApproval(plan.id));
    },

    async approvePayment(planId) {
        try {
            await API.post(`/training-plans/${planId}/approve-payment`, {});
            await this.showPlanDetail(planId);
        } catch (err) {
            alert(err.message);
        }
    },

    async revokeApproval(planId) {
        if (!confirm('ZakljuÄŤati plan nazad? Klijent Ä‡e ponovo morati platiti.')) return;
        try {
            await API.post(`/training-plans/${planId}/revoke-approval`, {});
            await this.showPlanDetail(planId);
        } catch (err) {
            alert(err.message);
        }
    },

    renderClientPaymentBox(plan) {
        const box = document.getElementById('myPlanPayment');
        if (!box) return;
        if (parseFloat(plan.price || 0) === 0) {
            box.innerHTML = `<span class="muted small">Besplatan plan.</span>`;
            return;
        }
        const statusLabel = this.paymentStatusLabel(plan.paymentStatus);
        const cls = this.paymentBadgeClass(plan.paymentStatus);
        let action = '';
        if (plan.paymentStatus === 'Pending') {
            action = `<button id="claimPaymentBtn" data-plan-id="${plan.id}">Plati ${this.formatPrice(plan)}</button>`;
        } else if (plan.paymentStatus === 'PaymentClaimed') {
            action = `<span class="muted small">ÄŚeka da trener potvrdi uplatu.</span>`;
        }
        box.innerHTML = `
            <div><strong>Cijena:</strong> ${this.formatPrice(plan)} Â· <span class="${cls}">${statusLabel}</span></div>
            ${action}
        `;
        const btn = document.getElementById('claimPaymentBtn');
        if (btn) btn.addEventListener('click', () => this.claimPayment(plan.id));
    },

    async claimPayment(planId) {
        try {
            await API.post(`/training-plans/${planId}/claim-payment`, {});
            await this.showMyPlanDetail(planId);
        } catch (err) {
            alert(err.message);
        }
    },

    async showPlanDetail(planId) {
        try {
            this.currentPlan = await API.get(`/training-plans/${planId}`);
            document.getElementById('plansList').classList.add('hidden');
            document.getElementById('newPlanBtn').classList.add('hidden');
            document.getElementById('planDetail').classList.remove('hidden');

            document.getElementById('planDetailName').textContent = this.currentPlan.name;
            document.getElementById('planDetailMeta').textContent =
                `${this.escape(this.currentPlan.clientName)} Â· ${this.formatDate(this.currentPlan.startDate)} â†’ ${this.formatDate(this.currentPlan.endDate)}`;
            document.getElementById('planDetailExpectations').textContent =
                this.currentPlan.trainerExpectations
                    ? `OÄŤekivanja: ${this.currentPlan.trainerExpectations}`
                    : 'OÄŤekivanja: â€”';

            this.renderTrainerPaymentBox(this.currentPlan);

            if (!Exercises.list.length) await Exercises.load();
            this.renderDays();
            this.renderMyExercises();
        } catch (err) {
            alert(err.message);
        }
    },

    renderMyExercises() {
        const container = document.getElementById('myExercisesList');
        if (!container) return;
        const mine = (Exercises.list || []);
        if (mine.length === 0) {
            container.innerHTML = '<p class="muted small" style="margin-top:0.5rem">JoĹˇ nemaĹˇ vlastitih vjeĹľbi.</p>';
            return;
        }
        const grouped = this.groupExercisesByTypeAndMuscle(mine);
        container.innerHTML = `
            <div class="muted small" style="margin-top:0.5rem">Moje vjeĹľbe â€” klikni za ureÄ‘ivanje (opis, video), ili obriĹˇi one koje nigdje ne koristiĹˇ:</div>
            ${grouped.map(g => `
                <div class="exercise-group-header">${ExerciseTypeLabels[g.type] || g.type}</div>
                ${g.muscleGroups.map(mg => `
                    <div class="exercise-subgroup-header">${this.escape(mg.muscleGroup || 'â€” bez miĹˇiÄ‡ne grupe â€”')}</div>
                    ${mg.items.map(e => `
                        <div class="planned-row">
                            <span class="planned-name exercise-link" data-exercise-id="${e.id}" title="Uredi vjeĹľbu (opis, YouTube link)">${Exercises.thumbHtml(e, 'exercise-thumb exercise-thumb-sm')}<span>${this.escape(e.name)}${e.videoUrl ? ' <span class="badge video">â–¶</span>' : ''}</span></span>
                            <span class="planned-actions">
                                <button class="btn-delete delete-ex-btn" data-ex-id="${e.id}" title="ObriĹˇi vjeĹľbu iz kataloga">đź—‘ ObriĹˇi</button>
                            </span>
                        </div>
                    `).join('')}
                `).join('')}
            `).join('')}
        `;
        container.querySelectorAll('.delete-ex-btn').forEach(btn => {
            btn.addEventListener('click', e => {
                e.stopPropagation();
                this.deleteCatalogExercise(parseInt(btn.dataset.exId));
            });
        });
        container.querySelectorAll('.exercise-link').forEach(el => {
            el.addEventListener('click', () => Exercises.showDetail(parseInt(el.dataset.exerciseId)));
        });
    },

    groupExercisesByTypeAndMuscle(list) {
        const typeOrder = ['Strength', 'Power', 'HIIT', 'Kardio', 'FullBody', 'Funkcionalni', 'Cicle'];
        const byType = {};
        for (const e of list) {
            const t = e.type || 'Other';
            const mg = e.muscleGroup || '';
            byType[t] = byType[t] || {};
            (byType[t][mg] = byType[t][mg] || []).push(e);
        }
        const typesSorted = Object.keys(byType).sort((a, b) => {
            const ai = typeOrder.indexOf(a); const bi = typeOrder.indexOf(b);
            return (ai === -1 ? 999 : ai) - (bi === -1 ? 999 : bi);
        });
        return typesSorted.map(t => {
            const mgKeys = Object.keys(byType[t]).sort((a, b) => {
                if (a === '' && b !== '') return 1;
                if (b === '' && a !== '') return -1;
                return a.localeCompare(b, 'hr');
            });
            return {
                type: t,
                muscleGroups: mgKeys.map(mg => ({
                    muscleGroup: mg,
                    items: byType[t][mg].slice().sort((a, b) => (a.name || '').localeCompare(b.name || '', 'hr'))
                }))
            };
        });
    },

    groupExercisesByType(list) {
        const typeOrder = ['Strength', 'Power', 'HIIT', 'Kardio', 'FullBody', 'Funkcionalni', 'Cicle'];
        const groups = {};
        for (const e of list) {
            const t = e.type || 'Other';
            (groups[t] = groups[t] || []).push(e);
        }
        for (const t in groups) {
            groups[t].sort((a, b) => {
                const mg = (a.muscleGroup || '').localeCompare(b.muscleGroup || '', 'hr');
                if (mg !== 0) return mg;
                return (a.name || '').localeCompare(b.name || '', 'hr');
            });
        }
        return Object.keys(groups)
            .sort((a, b) => {
                const ai = typeOrder.indexOf(a); const bi = typeOrder.indexOf(b);
                return (ai === -1 ? 999 : ai) - (bi === -1 ? 999 : bi);
            })
            .map(t => ({ type: t, items: groups[t] }));
    },

    async deleteCatalogExercise(exId) {
        if (!confirm('Obrisati ovu vjeĹľbu iz kataloga?')) return;
        try {
            await API.delete(`/exercises/${exId}`);
            await Exercises.load();
            await this.showPlanDetail(this.currentPlan.id);
        } catch (err) {
            alert(err.message);
        }
    },

    showPlansList() {
        document.getElementById('planDetail').classList.add('hidden');
        document.getElementById('plansList').classList.remove('hidden');
        document.getElementById('newPlanBtn').classList.remove('hidden');
        this.currentPlan = null;
        this.load();
    },

    renderDays() {
        const container = document.getElementById('planDaysList');
        if (!this.currentPlan.days.length) {
            container.innerHTML = '<p class="muted">Bez dana. Dodaj prvi dan iznad (npr. Ponedjeljak â€” Push Day).</p>';
            return;
        }

        container.innerHTML = this.currentPlan.days.map(d => `
            <div class="exercise-block" data-day-id="${d.id}">
                <div class="row" style="justify-content: space-between; align-items: center;">
                    <h4>${this.dayNames[d.dayOfWeek]} â€” ${this.escape(d.label)}</h4>
                    <button class="icon-btn delete-day-btn" data-day-id="${d.id}" title="ObriĹˇi dan">Ă—</button>
                </div>
                ${d.exercises.length === 0
                    ? '<p class="muted small">Bez vjeĹľbi.</p>'
                    : d.exercises.map((pe, idx) => `
                        <div class="planned-row">
                            <span class="planned-name"><strong>${this.escape(pe.exerciseName)}</strong></span>
                            <span class="planned-target">${this.planTargetLabel(pe)}</span>
                            ${pe.restSeconds ? `<span class="planned-rest">${pe.restSeconds}s</span>` : ''}
                            <span class="completion-badge" title="${pe.lastCompletedAt ? 'Zadnji put: ' + this.formatDate(pe.lastCompletedAt) : 'JoĹˇ nije odraÄ‘eno'}">
                                ${pe.completionsCount > 0 ? `âś… ${pe.completionsCount}Ă—` : 'â€” 0Ă—'}
                            </span>
                            <span class="planned-actions">
                                <button class="move-pe-btn" data-pe-id="${pe.id}" data-direction="up" ${idx === 0 ? 'disabled' : ''} title="Pomakni gore">â–˛</button>
                                <button class="move-pe-btn" data-pe-id="${pe.id}" data-direction="down" ${idx === d.exercises.length - 1 ? 'disabled' : ''} title="Pomakni dolje">â–Ľ</button>
                                <button class="btn-delete delete-pe-btn" data-pe-id="${pe.id}" title="ObriĹˇi vjeĹľbu iz dana">đź—‘</button>
                            </span>
                        </div>
                    `).join('')}
                <div class="add-set-form add-pe-form" data-mode="reps">
                    <select class="day-exercise-select"></select>
                    <input type="number" class="pe-sets" placeholder="serije" min="1" value="3" title="Broj serija">
                    <input type="number" class="pe-reps" placeholder="rep" min="1" value="10" title="Ponavljanja">
                    <input type="number" class="pe-duration hidden" placeholder="min" step="0.1" min="0.1" value="2" title="Trajanje (min, decimalno)">
                    <input type="number" class="pe-weight" placeholder="kg" step="0.5" min="0" value="0" title="TeĹľina">
                    <input type="number" class="pe-rest" placeholder="odmor s" min="0" max="3600" title="Odmor izmeÄ‘u serija">
                    <div class="pe-mode-toggle">
                        <label><input type="radio" name="peMode-${d.id}" value="reps" checked> Pon.</label>
                        <label><input type="radio" name="peMode-${d.id}" value="time"> Vrij.</label>
                    </div>
                    <button class="add-pe-btn">+</button>
                </div>
            </div>
        `).join('');

        container.querySelectorAll('.day-exercise-select').forEach(sel => {
            if (!Exercises.list.length) {
                sel.innerHTML = '<option value="">â€” nema vjeĹľbi â€”</option>';
                return;
            }
            const grouped = this.groupExercisesByType(Exercises.list);
            sel.innerHTML = grouped.map(g => `
                <optgroup label="${ExerciseTypeLabels[g.type] || g.type}">
                    ${g.items.map(e =>
                        `<option value="${e.id}">${this.escape(e.name)}${e.muscleGroup ? ' â€” ' + this.escape(e.muscleGroup) : ''}</option>`
                    ).join('')}
                </optgroup>
            `).join('');
        });

        container.querySelectorAll('.delete-day-btn').forEach(btn => {
            btn.addEventListener('click', () => this.deleteDay(parseInt(btn.dataset.dayId)));
        });
        container.querySelectorAll('.delete-pe-btn').forEach(btn => {
            btn.addEventListener('click', () => this.deletePlannedExercise(parseInt(btn.dataset.peId)));
        });
        container.querySelectorAll('.move-pe-btn').forEach(btn => {
            btn.addEventListener('click', () => this.movePlannedExercise(parseInt(btn.dataset.peId), btn.dataset.direction));
        });
        container.querySelectorAll('.add-pe-btn').forEach(btn => {
            btn.addEventListener('click', e => this.addPlannedExercise(e.target.closest('.exercise-block')));
        });
        container.querySelectorAll('.add-pe-form').forEach(form => {
            const repsInput = form.querySelector('.pe-reps');
            const durInput = form.querySelector('.pe-duration');
            form.querySelectorAll('input[type="radio"]').forEach(r => {
                r.addEventListener('change', () => {
                    const mode = form.querySelector('input[type="radio"]:checked').value;
                    form.dataset.mode = mode;
                    repsInput.classList.toggle('hidden', mode !== 'reps');
                    durInput.classList.toggle('hidden', mode !== 'time');
                });
            });
        });
    },

    async addDay() {
        if (!this.currentPlan) return;
        const dow = document.getElementById('newDayOfWeek').value;
        const label = document.getElementById('newDayLabel').value.trim();
        if (!label) { alert('Unesi oznaku dana (npr. Push Day).'); return; }

        try {
            await API.post(`/training-plans/${this.currentPlan.id}/days`, {
                dayOfWeek: dow,
                label
            });
            document.getElementById('newDayLabel').value = '';
            await this.showPlanDetail(this.currentPlan.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async deleteDay(dayId) {
        if (!confirm('Obrisati ovaj dan i sve vjeĹľbe u njemu?')) return;
        try {
            await API.delete(`/training-plans/days/${dayId}`);
            await this.showPlanDetail(this.currentPlan.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async addPlannedExercise(block) {
        const dayId = parseInt(block.dataset.dayId);
        const form = block.querySelector('.add-pe-form');
        const mode = form.dataset.mode || 'reps';
        const exerciseId = parseInt(form.querySelector('.day-exercise-select').value);
        const sets = parseInt(form.querySelector('.pe-sets').value);
        const weight = parseFloat(form.querySelector('.pe-weight').value);
        const restRaw = form.querySelector('.pe-rest').value;
        const rest = restRaw === '' ? null : parseInt(restRaw);

        if (!exerciseId) { alert('Odaberi vjeĹľbu.'); return; }
        if (!sets) { alert('Popuni broj serija.'); return; }

        const payload = {
            exerciseId,
            order: 0,
            targetSets: sets,
            targetReps: 0,
            targetWeightKg: isNaN(weight) ? 0 : weight,
            restSeconds: Number.isFinite(rest) ? rest : null
        };

        if (mode === 'time') {
            const minutes = parseFloat(form.querySelector('.pe-duration').value);
            if (!minutes || minutes <= 0) { alert('Unesi trajanje u minutama (npr. 2 ili 1.5).'); return; }
            payload.targetDurationSeconds = Math.round(minutes * 60);
        } else {
            const reps = parseInt(form.querySelector('.pe-reps').value);
            if (!reps) { alert('Unesi broj ponavljanja.'); return; }
            payload.targetReps = reps;
        }

        const day = this.currentPlan.days.find(d => d.id === dayId);
        payload.order = day ? day.exercises.length + 1 : 1;

        try {
            await API.post(`/training-plans/days/${dayId}/exercises`, payload);
            await this.showPlanDetail(this.currentPlan.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async deletePlannedExercise(peId) {
        if (!confirm('Obrisati vjeĹľbu iz plana?')) return;
        try {
            await API.delete(`/training-plans/exercises/${peId}`);
            await this.showPlanDetail(this.currentPlan.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async movePlannedExercise(peId, direction) {
        try {
            await API.put(`/training-plans/exercises/${peId}/move?direction=${direction}`);
            await this.showPlanDetail(this.currentPlan.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async quickCreateExercise() {
        const name = document.getElementById('quickExerciseName').value.trim();
        const muscle = document.getElementById('quickExerciseMuscle').value.trim();
        const type = document.getElementById('quickExerciseType').value;
        const description = document.getElementById('quickExerciseDescription').value.trim();
        const videoUrl = document.getElementById('quickExerciseVideoUrl').value.trim();
        const fileInput = document.getElementById('quickExerciseVideoFile');
        const videoFile = fileInput && fileInput.files && fileInput.files[0];
        if (!name) { alert('Unesi naziv vjeĹľbe.'); return; }

        try {
            const created = await API.post('/exercises', {
                name,
                muscleGroup: muscle || null,
                type,
                videoUrl: videoFile ? null : (videoUrl || null),
                description: description || null
            });
            if (videoFile && created && created.id) {
                try {
                    await Exercises.uploadVideo(created.id, videoFile);
                } catch (uploadErr) {
                    alert(I18n.t('exercises.videoUploadFailed') + ' ' + uploadErr.message);
                }
            }
            document.getElementById('quickExerciseName').value = '';
            document.getElementById('quickExerciseMuscle').value = '';
            document.getElementById('quickExerciseDescription').value = '';
            document.getElementById('quickExerciseVideoUrl').value = '';
            if (fileInput) fileInput.value = '';
            await Exercises.load();
            await this.showPlanDetail(this.currentPlan.id);
        } catch (err) {
            alert(err.message);
        }
    },

    formatDate(s) {
        return new Date(s).toLocaleDateString('hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    },

    formatDuration(seconds) {
        if (seconds < 60) return `${seconds}s`;
        const minutes = seconds / 60;
        if (seconds % 60 === 0) return `${Math.round(minutes)} min`;
        return `${minutes.toFixed(1).replace('.', ',')} min`;
    },

    planTargetLabel(pe) {
        if (pe.targetDurationSeconds) {
            const weight = (pe.targetWeightKg && pe.targetWeightKg > 0) ? ` @ ${pe.targetWeightKg} kg` : '';
            return `${pe.targetSets} Ă— ${this.formatDuration(pe.targetDurationSeconds)}${weight}`;
        }
        return `${pe.targetSets} Ă— ${pe.targetReps} @ ${pe.targetWeightKg} kg`;
    },

    async downloadMyPlanPdf() {
        if (!this.currentMyPlanId) return;
        const token = API.getToken();
        try {
            const res = await fetch(`/api/v1/training-plans/${this.currentMyPlanId}/pdf`, {
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
            a.download = `plan_${this.currentMyPlanId}.pdf`;
            document.body.appendChild(a);
            a.click();
            a.remove();
            URL.revokeObjectURL(url);
        } catch (err) {
            alert(err.message);
        }
    },

    async openQrShare() {
        if (!this.currentMyPlanId) return;
        const modal = document.getElementById('qrShareModal');
        const imgWrap = document.getElementById('qrShareImage');
        const urlEl = document.getElementById('qrShareUrl');
        const msg = document.getElementById('qrShareMsg');

        imgWrap.innerHTML = `<p class="muted small">${I18n.t('common.loading')}</p>`;
        urlEl.textContent = '';
        msg.textContent = '';
        modal.classList.remove('hidden');

        try {
            const data = await API.post(`/training-plans/${this.currentMyPlanId}/share`);
            imgWrap.innerHTML = `<img src="data:image/png;base64,${data.qrPngBase64}" alt="QR">`;
            urlEl.textContent = data.url;
        } catch (err) {
            imgWrap.innerHTML = '';
            msg.textContent = err.message || I18n.t('qr.error');
        }
    },

    closeQrShare() {
        document.getElementById('qrShareModal').classList.add('hidden');
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
