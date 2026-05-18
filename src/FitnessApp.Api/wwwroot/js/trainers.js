const Trainers = {
    clients: [],
    currentClient: null,
    currentClientWorkouts: [],
    currentWorkout: null,
    clientSearch: '',
    statsChart: null,

    init() {
        document.getElementById('backToClientsBtn').addEventListener('click', () => this.showClientsList());
        document.getElementById('backToClientWorkoutsBtn').addEventListener('click', () => this.showClientWorkouts());

        document.getElementById('trainerNewClientWorkoutBtn').addEventListener('click', () => {
            document.getElementById('trainerNewClientWorkoutForm').classList.remove('hidden');
        });
        document.getElementById('trainerCancelWorkoutBtn').addEventListener('click', () => this.resetNewWorkoutForm());
        document.getElementById('trainerSaveWorkoutBtn').addEventListener('click', () => this.createWorkout());
        document.getElementById('trainerAddExerciseBtn').addEventListener('click', () => this.addExercise());

        const emailBtn = document.getElementById('emailClientBtn');
        if (emailBtn) emailBtn.addEventListener('click', () => this.openEmailModal());

        const newNutBtn = document.getElementById('trainerNewClientNutritionBtn');
        if (newNutBtn) newNutBtn.addEventListener('click', () => this.openNutritionForClient());
        const closeEmail = document.getElementById('closeEmailModalBtn');
        if (closeEmail) closeEmail.addEventListener('click', () => this.closeEmailModal());
        const cancelEmail = document.getElementById('cancelEmailBtn');
        if (cancelEmail) cancelEmail.addEventListener('click', () => this.closeEmailModal());
        const sendEmail = document.getElementById('sendEmailBtn');
        if (sendEmail) sendEmail.addEventListener('click', () => this.sendEmail());
        const emailModal = document.getElementById('emailClientModal');
        if (emailModal) emailModal.addEventListener('click', e => { if (e.target === emailModal) this.closeEmailModal(); });

        document.getElementById('trainerClientsSearch').addEventListener('input', e => {
            this.clientSearch = e.target.value.toLowerCase();
            this.renderClients();
        });

        document.getElementById('trainerStatsExerciseSelect').addEventListener('change', e => {
            this.loadClientProgress(parseInt(e.target.value));
        });

        const planSel = document.getElementById('trainerStatsPlanSelect');
        if (planSel) planSel.addEventListener('change', () => this.loadPlanProgression());

        const planExSel = document.getElementById('trainerStatsPlanExerciseSelect');
        if (planExSel) planExSel.addEventListener('change', e => Plans.renderProgression(parseInt(e.target.value), 'trainerStatsPlanChart', 'trainerStatsPlanSummary', 'trainerStatsPlanChartObj'));

        document.getElementById('newClientBtn').addEventListener('click', () => {
            document.getElementById('newClientForm').classList.remove('hidden');
        });
        document.getElementById('cancelClientBtn').addEventListener('click', () => this.resetNewClientForm());
        document.getElementById('saveClientBtn').addEventListener('click', () => this.createClient());
    },

    async createClient() {
        const errorEl = document.getElementById('newClientError');
        errorEl.textContent = '';

        const email = document.getElementById('newClientEmail').value.trim();
        if (!email) { errorEl.textContent = 'Email je obavezan.'; return; }

        try {
            await API.post('/trainers/me/clients', {
                email,
                fullName: document.getElementById('newClientFullName').value || null,
                language: (typeof I18n !== 'undefined' && I18n.lang) ? I18n.lang : 'hr'
            });
            alert(`✓ Klijent kreiran. Mail s pristupnim podacima poslan na ${email}.`);
            this.resetNewClientForm();
            await this.load();
        } catch (err) {
            errorEl.textContent = err.message;
        }
    },

    resetNewClientForm() {
        document.getElementById('newClientFullName').value = '';
        document.getElementById('newClientEmail').value = '';
        document.getElementById('newClientError').textContent = '';
        const resultBox = document.getElementById('newClientResultBox');
        if (resultBox) resultBox.classList.add('hidden');
        document.getElementById('newClientForm').classList.add('hidden');
    },

    async load() {
        try {
            this.clients = await API.get('/trainers/me/clients');
            this.renderClients();
        } catch (err) {
            console.error(err);
        }
    },

    renderClients() {
        const container = document.getElementById('clientsList');
        const filtered = this.clients.filter(c =>
            !this.clientSearch ||
            (c.fullName || '').toLowerCase().includes(this.clientSearch) ||
            (c.email || '').toLowerCase().includes(this.clientSearch)
        );
        if (filtered.length === 0) {
            container.innerHTML = `<p class="muted">${this.clients.length === 0 ? 'Još nemaš klijenata.' : 'Nema rezultata za pretragu.'}</p>`;
            return;
        }

        container.innerHTML = filtered.map(c => `
            <div class="list-item" data-id="${c.id}">
                <div class="list-item-main">
                    ${Profile.avatarHtml(c.profileImageUrl, 'avatar-mini')}
                    <div>
                        <h4>${this.escape(c.fullName || c.email)}</h4>
                        <div class="meta">
                            ${this.escape(c.email)}
                            · ${c.workoutCount} ${c.workoutCount === 1 ? 'trening' : 'treninga'}
                        </div>
                    </div>
                </div>
                <span class="muted">→</span>
            </div>
        `).join('');

        container.querySelectorAll('.list-item').forEach(el => {
            el.addEventListener('click', () => this.showClientDetail(el.dataset.id));
        });
    },

    async showClientDetail(clientId) {
        const client = this.clients.find(c => c.id === clientId);
        if (!client) return;

        this.currentClient = client;
        document.getElementById('clientsView').classList.add('hidden');
        document.getElementById('clientDetail').classList.remove('hidden');
        const nameEl = document.getElementById('clientDetailName');
        nameEl.innerHTML = `${Profile.avatarHtml(client.profileImageUrl, 'avatar-mini')} <span>${this.escape(client.fullName || client.email)}</span>`;
        document.getElementById('clientDetailMeta').textContent = client.email;
        document.getElementById('clientWorkoutDetail').classList.add('hidden');
        this.resetNewWorkoutForm();

        await this.loadClientProfile();
        await this.loadClientPlans();
        await this.loadClientNutritionPlans();
        await this.loadClientWorkouts();
        await this.loadStatsExercises();
        await this.loadPlansForStats();
    },

    async loadPlansForStats() {
        const planSel = document.getElementById('trainerStatsPlanSelect');
        const summary = document.getElementById('trainerStatsPlanSummary');
        if (!planSel || !this.currentClient) return;
        let plans = [];
        try {
            plans = await API.get(`/training-plans/client/${this.currentClient.id}`);
        } catch (err) {
            if (summary) summary.textContent = err.message;
        }
        if (!plans.length) {
            planSel.innerHTML = '<option value="">— klijent nema planova —</option>';
            document.getElementById('trainerStatsPlanExerciseSelect').innerHTML = '';
            Plans.clearProgressionChart('trainerStatsPlanChartObj');
            if (summary) summary.textContent = 'Klijent još nema plan.';
            return;
        }
        planSel.innerHTML = plans.map(p =>
            `<option value="${p.id}">${this.escape(p.name)} (${this.formatDate(p.startDate)} → ${this.formatDate(p.endDate)})</option>`
        ).join('');
        await this.loadPlanProgression();
    },

    async loadPlanProgression() {
        const planSel = document.getElementById('trainerStatsPlanSelect');
        const planId = parseInt(planSel.value);
        if (!planId) return;
        await Plans.loadProgression(planId, 'trainerStatsPlanExerciseSelect', 'trainerStatsPlanChart', 'trainerStatsPlanSummary', 'trainerStatsPlanChartObj');
    },

    async loadClientProfile() {
        const container = document.getElementById('clientDetailProfile');
        if (!container) return;
        try {
            const p = await API.get(`/trainers/me/clients/${this.currentClient.id}/profile`);
            Profile.renderReadOnly(container, p);
        } catch (err) {
            container.innerHTML = `<p class="muted small">Greška pri dohvaćanju profila: ${this.escape(err.message)}</p>`;
        }
    },

    async loadClientPlans() {
        const container = document.getElementById('clientPlansList');
        try {
            const plans = await API.get(`/training-plans/client/${this.currentClient.id}`);
            if (!plans.length) {
                container.innerHTML = '<p class="muted">Klijent još nema plan. Klikni "+ Novi plan".</p>';
                return;
            }
            container.innerHTML = plans.map(p => `
                <div class="list-item" data-plan-id="${p.id}">
                    <div>
                        <h4>${this.escape(p.name)}</h4>
                        <div class="meta">
                            ${this.formatDate(p.startDate)} → ${this.formatDate(p.endDate)}
                            · ${p.dayCount} ${p.dayCount === 1 ? 'dan' : 'dana'}
                        </div>
                    </div>
                    <span class="planned-actions">
                        <button class="btn-delete-icon delete-client-plan-btn" data-plan-id="${p.id}" data-plan-name="${this.escape(p.name)}" title="Obriši plan">🗑</button>
                        <span class="muted">→</span>
                    </span>
                </div>
            `).join('');

            container.querySelectorAll('.list-item').forEach(el => {
                el.addEventListener('click', e => {
                    if (e.target.closest('.delete-client-plan-btn')) return;
                    App.switchTrainerView('plans');
                    Plans.showPlanDetail(parseInt(el.dataset.planId));
                });
            });

            container.querySelectorAll('.delete-client-plan-btn').forEach(btn => {
                btn.addEventListener('click', async e => {
                    e.stopPropagation();
                    const planId = parseInt(btn.dataset.planId);
                    const name = btn.dataset.planName;
                    if (!confirm(`Obrisati plan "${name}"? Svi odrađeni setovi i oznake odrađenog se brišu trajno.`)) return;
                    try {
                        await API.delete(`/training-plans/${planId}`);
                        await this.loadClientPlans();
                    } catch (err) {
                        alert(err.message);
                    }
                });
            });
        } catch (err) {
            console.error(err);
            container.innerHTML = `<p class="muted">Greška: ${this.escape(err.message)}</p>`;
        }
    },

    async loadClientNutritionPlans() {
        const container = document.getElementById('clientNutritionPlansList');
        if (!container) return;
        try {
            const plans = await API.get(`/nutrition-plans/client/${this.currentClient.id}`);
            if (!plans.length) {
                container.innerHTML = '<p class="muted">Klijent još nema plan prehrane.</p>';
                return;
            }
            container.innerHTML = plans.map(p => `
                <div class="list-item" data-plan-id="${p.id}">
                    <div>
                        <h4>${this.escape(p.name)}</h4>
                        <div class="meta">
                            ${this.formatDate(p.startDate)} → ${this.formatDate(p.endDate)}
                            · ${p.dayCount} ${p.dayCount === 1 ? 'dan' : 'dana'}
                        </div>
                    </div>
                    <span class="planned-actions">
                        <button class="btn-delete-icon delete-client-nut-btn" data-plan-id="${p.id}" data-plan-name="${this.escape(p.name)}" title="Obriši">🗑</button>
                        <span class="muted">→</span>
                    </span>
                </div>
            `).join('');

            container.querySelectorAll('.list-item').forEach(el => {
                el.addEventListener('click', e => {
                    if (e.target.closest('.delete-client-nut-btn')) return;
                    App.switchTrainerView('nutrition');
                    Nutrition.showDetail(parseInt(el.dataset.planId));
                });
            });
            container.querySelectorAll('.delete-client-nut-btn').forEach(btn => {
                btn.addEventListener('click', async e => {
                    e.stopPropagation();
                    const id = parseInt(btn.dataset.planId);
                    const name = btn.dataset.planName;
                    if (!confirm(`Obrisati plan prehrane "${name}"?`)) return;
                    try {
                        await API.delete(`/nutrition-plans/${id}`);
                        await this.loadClientNutritionPlans();
                    } catch (err) { alert(err.message); }
                });
            });
        } catch (err) {
            container.innerHTML = `<p class="muted">Greška: ${this.escape(err.message)}</p>`;
        }
    },

    formatNutPrice(p) {
        const v = parseFloat(p.price || 0);
        if (v === 0) return 'Besplatno';
        return `${v.toFixed(2)} ${p.currency || 'EUR'}`;
    },
    nutPaymentLabel(s) {
        if (s === 'Approved') return 'Odobreno';
        if (s === 'PaymentClaimed') return 'Klijent je platio — čeka odobrenje';
        return 'Čeka plaćanje';
    },
    nutPaymentClass(s) {
        if (s === 'Approved') return 'badge approved';
        if (s === 'PaymentClaimed') return 'badge claimed';
        return 'badge pending';
    },

    async openNutritionForClient() {
        if (!this.currentClient) return;
        App.switchTrainerView('nutrition');
        await Nutrition.showNewPlanForm(this.currentClient.id);
    },

    async loadStatsExercises() {
        const select = document.getElementById('trainerStatsExerciseSelect');
        let trained = [];
        try {
            trained = await API.get(`/trainers/me/clients/${this.currentClient.id}/stats/my-exercises`);
        } catch (err) {
            console.error(err);
        }
        if (trained.length === 0) {
            select.innerHTML = '<option value="">— klijent još nema odrađenih vježbi —</option>';
            this.renderStatsEmpty('Klijent nema niti jednu vježbu sa serijom.');
            return;
        }
        select.innerHTML = trained.map(e =>
            `<option value="${e.id}">${this.escape(e.name)}${e.muscleGroup ? ' (' + this.escape(e.muscleGroup) + ')' : ''}</option>`
        ).join('');
        await this.loadClientProgress(parseInt(select.value));
    },

    async loadClientProgress(exerciseId) {
        if (!exerciseId || !this.currentClient) return;
        try {
            const data = await API.get(`/trainers/me/clients/${this.currentClient.id}/stats/exercise-progress/${exerciseId}`);
            if (!data.length) {
                this.renderStatsEmpty('Klijent nije radio ovu vježbu.');
                return;
            }
            this.renderStatsChart(data);
            this.renderStatsSummary(data);
        } catch (err) {
            this.renderStatsEmpty(err.message);
        }
    },

    renderStatsChart(data) {
        const labels = data.map(d => new Date(d.date).toLocaleDateString('hr-HR', { day: '2-digit', month: '2-digit', year: '2-digit' }));
        const maxWeights = data.map(d => Number(d.maxWeight));
        const totalReps = data.map(d => d.totalReps);

        if (this.statsChart) this.statsChart.destroy();

        const ctx = document.getElementById('trainerProgressChart').getContext('2d');
        this.statsChart = new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [
                    {
                        label: 'Max težina (kg)',
                        data: maxWeights,
                        borderColor: '#4dabf7',
                        backgroundColor: 'rgba(77,171,247,0.15)',
                        tension: 0.25,
                        yAxisID: 'y'
                    },
                    {
                        label: 'Ukupno ponavljanja',
                        data: totalReps,
                        borderColor: '#fc74b1',
                        backgroundColor: 'rgba(252,116,177,0.1)',
                        tension: 0.25,
                        yAxisID: 'y1'
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
                    y: { position: 'left', beginAtZero: true, ticks: { color: '#4dabf7' }, grid: { color: '#2a3340' }, title: { display: true, text: 'Max kg', color: '#4dabf7' } },
                    y1: { position: 'right', beginAtZero: true, ticks: { color: '#fc74b1' }, grid: { drawOnChartArea: false }, title: { display: true, text: 'Ponavljanja', color: '#fc74b1' } }
                }
            }
        });
    },

    renderStatsSummary(data) {
        const max = data.length ? Math.max(...data.map(d => Number(d.maxWeight))) : 0;
        const totalSets = data.reduce((a, d) => a + d.setCount, 0);
        const sessions = data.length;
        document.getElementById('trainerStatsSummary').innerHTML =
            `${sessions} sesija · ${totalSets} serija · rekord: <strong>${max} kg</strong>`;
    },

    renderStatsEmpty(msg) {
        if (this.statsChart) { this.statsChart.destroy(); this.statsChart = null; }
        document.getElementById('trainerStatsSummary').textContent = msg;
    },

    async loadClientWorkouts() {
        try {
            this.currentClientWorkouts = await API.get(`/trainers/me/clients/${this.currentClient.id}/workouts`);
            this.renderClientWorkouts();
        } catch (err) {
            alert(err.message);
        }
    },

    renderClientWorkouts() {
        const container = document.getElementById('clientWorkoutsList');
        if (this.currentClientWorkouts.length === 0) {
            container.innerHTML = '<p class="muted">Ovaj klijent još nema treninga.</p>';
            return;
        }

        container.innerHTML = this.currentClientWorkouts.map(w => `
            <div class="list-item" data-id="${w.id}">
                <div>
                    <h4>${this.escape(w.name)}</h4>
                    <div class="meta">
                        ${this.formatDateTime(w.performedAt)}
                        ${w.durationMinutes ? '· ' + w.durationMinutes + ' min' : ''}
                        · ${w.exerciseCount} ${w.exerciseCount === 1 ? 'vježba' : 'vježbi'}
                    </div>
                </div>
                <span class="muted">→</span>
            </div>
        `).join('');

        container.querySelectorAll('.list-item').forEach(el => {
            el.addEventListener('click', () => this.showWorkoutDetail(parseInt(el.dataset.id)));
        });
    },

    async createWorkout() {
        const name = document.getElementById('trainerWorkoutName').value.trim();
        if (!name) return;

        const dateInput = document.getElementById('trainerWorkoutDate').value;
        const duration = document.getElementById('trainerWorkoutDuration').value;

        try {
            await API.post(`/trainers/me/clients/${this.currentClient.id}/workouts`, {
                name,
                notes: document.getElementById('trainerWorkoutNotes').value || null,
                performedAt: dateInput ? new Date(dateInput).toISOString() : null,
                durationMinutes: duration ? parseInt(duration) : null
            });
            this.resetNewWorkoutForm();
            await this.loadClientWorkouts();
        } catch (err) {
            alert(err.message);
        }
    },

    resetNewWorkoutForm() {
        document.getElementById('trainerWorkoutName').value = '';
        document.getElementById('trainerWorkoutDate').value = '';
        document.getElementById('trainerWorkoutDuration').value = '';
        document.getElementById('trainerWorkoutNotes').value = '';
        document.getElementById('trainerNewClientWorkoutForm').classList.add('hidden');
    },

    async showWorkoutDetail(workoutId) {
        try {
            this.currentWorkout = await API.get(`/trainers/me/clients/${this.currentClient.id}/workouts/${workoutId}`);
            document.getElementById('clientWorkoutsList').classList.add('hidden');
            document.getElementById('trainerNewClientWorkoutBtn').classList.add('hidden');
            document.getElementById('clientWorkoutDetail').classList.remove('hidden');
            document.getElementById('clientWorkoutName').textContent = this.currentWorkout.name;
            document.getElementById('clientWorkoutMeta').textContent =
                `${this.formatDateTime(this.currentWorkout.performedAt)}${this.currentWorkout.durationMinutes ? ' · ' + this.currentWorkout.durationMinutes + ' min' : ''}${this.currentWorkout.notes ? ' · ' + this.currentWorkout.notes : ''}`;

            this.renderExercises();
            await this.populateExerciseSelect();
        } catch (err) {
            alert(err.message);
        }
    },

    renderExercises() {
        const container = document.getElementById('clientWorkoutExercises');
        if (!this.currentWorkout.exercises.length) {
            container.innerHTML = '<p class="muted">Bez vježbi. Dodaj prvu vježbu ispod.</p>';
            return;
        }

        container.innerHTML = this.currentWorkout.exercises.map(we => `
            <div class="exercise-block" data-we-id="${we.id}">
                <h4>${this.escape(we.exerciseName)}</h4>
                ${we.sets.map(s => `
                    <div class="set-row">
                        <span class="set-num">#${s.setNumber}</span>
                        <span>${s.weight} kg</span>
                        <span>${s.reps} rep</span>
                        <button class="icon-btn" data-set-id="${s.id}" title="Obriši">×</button>
                    </div>
                `).join('')}
                <div class="add-set-form">
                    <input type="number" class="set-num-input" placeholder="#" value="${we.sets.length + 1}" min="1">
                    <input type="number" class="weight-input" placeholder="kg" step="0.5" min="0">
                    <input type="number" class="reps-input" placeholder="rep" min="0">
                    <button class="add-set-btn">+</button>
                </div>
            </div>
        `).join('');

        container.querySelectorAll('.add-set-btn').forEach(btn => {
            btn.addEventListener('click', e => this.addSet(e.target.closest('.exercise-block')));
        });
        container.querySelectorAll('.icon-btn').forEach(btn => {
            btn.addEventListener('click', e => this.deleteSet(parseInt(e.target.dataset.setId)));
        });
    },

    async populateExerciseSelect() {
        if (!Exercises.list.length) await Exercises.load();
        const select = document.getElementById('trainerExerciseSelect');
        select.innerHTML = Exercises.list.map(e =>
            `<option value="${e.id}">${this.escape(e.name)}${e.muscleGroup ? ' (' + this.escape(e.muscleGroup) + ')' : ''}</option>`
        ).join('');
    },

    async addExercise() {
        const exerciseId = parseInt(document.getElementById('trainerExerciseSelect').value);
        if (!exerciseId) return;

        try {
            await API.post(`/trainers/me/clients/${this.currentClient.id}/workouts/${this.currentWorkout.id}/exercises`, {
                exerciseId,
                order: this.currentWorkout.exercises.length + 1
            });
            await this.showWorkoutDetail(this.currentWorkout.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async addSet(block) {
        const weId = parseInt(block.dataset.weId);
        const setNum = parseInt(block.querySelector('.set-num-input').value);
        const weight = parseFloat(block.querySelector('.weight-input').value);
        const reps = parseInt(block.querySelector('.reps-input').value);

        if (!setNum || isNaN(weight) || !reps) {
            alert('Popuni broj serije, težinu i ponavljanja.');
            return;
        }

        try {
            await API.post(`/trainers/me/clients/${this.currentClient.id}/workouts/${this.currentWorkout.id}/sets`, {
                workoutExerciseId: weId,
                setNumber: setNum,
                weight,
                reps
            });
            await this.showWorkoutDetail(this.currentWorkout.id);
        } catch (err) {
            alert(err.message);
        }
    },

    async deleteSet(setId) {
        if (!confirm('Obrisati seriju?')) return;
        try {
            await API.delete(`/trainers/me/clients/${this.currentClient.id}/sets/${setId}`);
            await this.showWorkoutDetail(this.currentWorkout.id);
        } catch (err) {
            alert(err.message);
        }
    },

    showClientsList() {
        document.getElementById('clientDetail').classList.add('hidden');
        document.getElementById('clientsView').classList.remove('hidden');
        this.currentClient = null;
        this.load();
    },

    showClientWorkouts() {
        document.getElementById('clientWorkoutDetail').classList.add('hidden');
        document.getElementById('clientWorkoutsList').classList.remove('hidden');
        document.getElementById('trainerNewClientWorkoutBtn').classList.remove('hidden');
        this.currentWorkout = null;
        this.loadClientWorkouts();
    },

    openEmailModal() {
        if (!this.currentClient) return;
        document.getElementById('emailClientTo').textContent =
            `Prima: ${this.currentClient.fullName || this.currentClient.email} <${this.currentClient.email}>`;
        document.getElementById('emailSubject').value = '';
        document.getElementById('emailBody').value = '';
        document.getElementById('emailSendMsg').textContent = '';
        document.getElementById('emailClientModal').classList.remove('hidden');
    },

    closeEmailModal() {
        document.getElementById('emailClientModal').classList.add('hidden');
    },

    async sendEmail() {
        const subject = document.getElementById('emailSubject').value.trim();
        const body = document.getElementById('emailBody').value.trim();
        const msg = document.getElementById('emailSendMsg');
        msg.textContent = '';
        if (!subject) { msg.textContent = 'Predmet je obavezan.'; return; }
        if (!body) { msg.textContent = 'Poruka je obavezna.'; return; }
        try {
            await API.post('/email/send-to-client', {
                clientId: this.currentClient.id, subject, body
            });
            msg.style.color = '#52c452';
            msg.textContent = '✓ Email poslan!';
            setTimeout(() => this.closeEmailModal(), 1200);
        } catch (err) {
            msg.style.color = '';
            msg.textContent = err.message;
        }
    },

    formatDate(s) {
        return new Date(s).toLocaleDateString('hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    },

    formatDateTime(s) {
        const d = new Date(s);
        return d.toLocaleString('hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
