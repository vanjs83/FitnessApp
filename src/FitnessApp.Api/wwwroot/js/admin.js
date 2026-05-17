const Admin = {
    trainers: [],
    clients: [],
    workouts: [],
    trainerSearch: '',
    clientSearch: '',
    workoutSearch: '',

    init() {
        document.getElementById('newTrainerBtn').addEventListener('click', () => {
            document.getElementById('newTrainerForm').classList.remove('hidden');
        });
        document.getElementById('cancelTrainerBtn').addEventListener('click', () => this.resetForm());
        document.getElementById('saveTrainerBtn').addEventListener('click', () => this.saveTrainer());

        document.querySelectorAll('.admin-tab').forEach(tab => {
            tab.addEventListener('click', () => this.switchTab(tab.dataset.adminTab));
        });

        document.getElementById('adminTrainersSearch').addEventListener('input', e => {
            this.trainerSearch = e.target.value.toLowerCase();
            this.renderTrainers();
        });
        document.getElementById('adminClientsSearch').addEventListener('input', e => {
            this.clientSearch = e.target.value.toLowerCase();
            this.renderClients();
        });
        document.getElementById('adminWorkoutsSearch').addEventListener('input', e => {
            this.workoutSearch = e.target.value.toLowerCase();
            this.renderWorkouts();
        });
    },

    async load() {
        await Promise.all([this.loadStats(), this.loadTrainers()]);
    },

    switchTab(tab) {
        document.querySelectorAll('.admin-tab').forEach(t => t.classList.toggle('active', t.dataset.adminTab === tab));
        document.getElementById('adminTrainersTab').classList.toggle('hidden', tab !== 'trainers');
        document.getElementById('adminClientsTab').classList.toggle('hidden', tab !== 'clients');
        document.getElementById('adminWorkoutsTab').classList.toggle('hidden', tab !== 'workouts');

        if (tab === 'trainers') this.loadTrainers();
        if (tab === 'clients') this.loadClients();
        if (tab === 'workouts') this.loadWorkouts();
    },

    async loadStats() {
        try {
            const s = await API.get('/admin/stats');
            document.getElementById('adminStats').innerHTML = `
                <div class="stat-card"><div class="stat-value">${s.trainersCount}</div><div class="stat-label">Treneri</div></div>
                <div class="stat-card"><div class="stat-value">${s.clientsCount}</div><div class="stat-label">Klijenti</div></div>
                <div class="stat-card"><div class="stat-value">${s.clientsWithoutTrainer}</div><div class="stat-label">Bez trenera</div></div>
                <div class="stat-card"><div class="stat-value">${s.workoutsCount}</div><div class="stat-label">Treninzi</div></div>
                <div class="stat-card"><div class="stat-value">${s.exercisesCount}</div><div class="stat-label">Vježbe</div></div>
            `;
        } catch (err) {
            console.error(err);
        }
    },

    async loadTrainers() {
        try {
            this.trainers = await API.get('/admin/trainers');
            this.renderTrainers();
        } catch (err) {
            console.error(err);
        }
    },

    renderTrainers() {
        const container = document.getElementById('adminTrainersList');
        const filtered = this.trainers.filter(t =>
            !this.trainerSearch ||
            (t.fullName || '').toLowerCase().includes(this.trainerSearch) ||
            (t.email || '').toLowerCase().includes(this.trainerSearch)
        );
        if (filtered.length === 0) {
            container.innerHTML = `<p class="muted">${this.trainers.length === 0 ? 'Nema trenera.' : 'Nema rezultata za pretragu.'}</p>`;
            return;
        }

        container.innerHTML = filtered.map(t => `
            <div class="list-item">
                <div>
                    <h4>${this.escape(t.fullName || t.email)}</h4>
                    <div class="meta">
                        ${this.escape(t.email)}
                        · ${t.clientCount} ${t.clientCount === 1 ? 'klijent' : 'klijenata'}
                        · od ${this.formatDate(t.createdAt)}
                    </div>
                </div>
                <button class="danger small-btn" data-id="${t.id}">Obriši</button>
            </div>
        `).join('');

        container.querySelectorAll('button.danger').forEach(btn => {
            btn.addEventListener('click', e => {
                const name = e.target.closest('.list-item').querySelector('h4').textContent;
                this.deleteTrainer(e.target.dataset.id, name);
            });
        });
    },

    async loadClients() {
        try {
            this.clients = await API.get('/admin/clients');
            this.renderClients();
        } catch (err) {
            console.error(err);
        }
    },

    renderClients() {
        const container = document.getElementById('adminClientsList');
        const filtered = this.clients.filter(c =>
            !this.clientSearch ||
            (c.fullName || '').toLowerCase().includes(this.clientSearch) ||
            (c.email || '').toLowerCase().includes(this.clientSearch) ||
            (c.trainerName || '').toLowerCase().includes(this.clientSearch)
        );

        if (filtered.length === 0) {
            container.innerHTML = `<p class="muted">${this.clients.length === 0 ? 'Nema klijenata.' : 'Nema rezultata za pretragu.'}</p>`;
            return;
        }

        container.innerHTML = filtered.map(c => `
            <div class="list-item">
                <div>
                    <h4>${this.escape(c.fullName || c.email)}</h4>
                    <div class="meta">
                        ${this.escape(c.email)}
                        · trener: ${c.trainerName ? this.escape(c.trainerName) : '<em>nema</em>'}
                        · ${c.workoutCount} ${c.workoutCount === 1 ? 'trening' : 'treninga'}
                        · od ${this.formatDate(c.createdAt)}
                    </div>
                </div>
            </div>
        `).join('');
    },

    async loadWorkouts() {
        try {
            this.workouts = await API.get('/admin/workouts');
            this.renderWorkouts();
        } catch (err) {
            console.error(err);
        }
    },

    renderWorkouts() {
        const container = document.getElementById('adminWorkoutsList');
        const filtered = this.workouts.filter(w =>
            !this.workoutSearch ||
            (w.name || '').toLowerCase().includes(this.workoutSearch) ||
            (w.clientName || '').toLowerCase().includes(this.workoutSearch) ||
            (w.trainerName || '').toLowerCase().includes(this.workoutSearch)
        );

        if (filtered.length === 0) {
            container.innerHTML = `<p class="muted">${this.workouts.length === 0 ? 'Nema treninga.' : 'Nema rezultata za pretragu.'}</p>`;
            return;
        }

        container.innerHTML = filtered.map(w => `
            <div class="list-item">
                <div>
                    <h4>${this.escape(w.name)}</h4>
                    <div class="meta">
                        klijent: ${this.escape(w.clientName)}
                        ${w.trainerName ? '· trener: ' + this.escape(w.trainerName) : ''}
                        · ${this.formatDate(w.performedAt)}
                        ${w.durationMinutes ? '· ' + w.durationMinutes + ' min' : ''}
                        · ${w.exerciseCount} ${w.exerciseCount === 1 ? 'vježba' : 'vježbi'}
                    </div>
                </div>
            </div>
        `).join('');
    },

    async saveTrainer() {
        const errorEl = document.getElementById('adminTrainerError');
        errorEl.textContent = '';

        const email = document.getElementById('adminTrainerEmail').value.trim();
        const password = document.getElementById('adminTrainerPassword').value;
        if (!email) { errorEl.textContent = 'Email je obavezan.'; return; }
        if (!password) { errorEl.textContent = 'Lozinka je obavezna.'; return; }
        if (password.length < 6) { errorEl.textContent = 'Lozinka mora imati barem 6 znakova.'; return; }

        try {
            await API.post('/admin/trainers', {
                email,
                password,
                fullName: document.getElementById('adminTrainerFullName').value || null
            });
            this.resetForm();
            await Promise.all([this.loadTrainers(), this.loadStats()]);
        } catch (err) {
            errorEl.textContent = err.message;
        }
    },

    async deleteTrainer(id, name) {
        if (!confirm(`Obrisati trenera "${name}"? Klijenti ostaju u sistemu, samo bez trenera.`)) return;

        try {
            await API.delete(`/admin/trainers/${id}`);
            await Promise.all([this.loadTrainers(), this.loadStats()]);
        } catch (err) {
            alert(err.message);
        }
    },

    resetForm() {
        document.getElementById('adminTrainerFullName').value = '';
        document.getElementById('adminTrainerEmail').value = '';
        document.getElementById('adminTrainerPassword').value = '';
        document.getElementById('adminTrainerError').textContent = '';
        document.getElementById('newTrainerForm').classList.add('hidden');
    },

    formatDate(s) {
        const d = new Date(s);
        return d.toLocaleDateString('hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
