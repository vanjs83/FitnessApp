const Admin = {
    trainers: [],
    clients: [],
    workouts: [],
    trainerSearch: '',
    clientSearch: '',
    workoutSearch: '',
    mailTrainerSearch: '',
    mailSelectedIds: new Set(),
    mailConfigured: null,

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

        document.getElementById('adminMailTrainerSearch').addEventListener('input', e => {
            this.mailTrainerSearch = e.target.value.toLowerCase();
            this.renderMailTrainers();
        });
        document.getElementById('adminMailSelectAll').addEventListener('click', () => this.mailSelectAll(true));
        document.getElementById('adminMailSelectNone').addEventListener('click', () => this.mailSelectAll(false));
        document.getElementById('adminMailSendBtn').addEventListener('click', () => this.sendMail());
        document.getElementById('adminMailResetBtn').addEventListener('click', () => this.resetMailForm());
    },

    async load() {
        await Promise.all([this.loadStats(), this.loadTrainers()]);
    },

    switchTab(tab) {
        document.querySelectorAll('.admin-tab').forEach(t => t.classList.toggle('active', t.dataset.adminTab === tab));
        document.getElementById('adminTrainersTab').classList.toggle('hidden', tab !== 'trainers');
        document.getElementById('adminClientsTab').classList.toggle('hidden', tab !== 'clients');
        document.getElementById('adminWorkoutsTab').classList.toggle('hidden', tab !== 'workouts');
        document.getElementById('adminMailTab').classList.toggle('hidden', tab !== 'mail');

        if (tab === 'trainers') this.loadTrainers();
        if (tab === 'clients') this.loadClients();
        if (tab === 'workouts') this.loadWorkouts();
        if (tab === 'mail') this.loadMail();
    },

    async loadStats() {
        try {
            const s = await API.get('/admin/stats');
            document.getElementById('adminStats').innerHTML = `
                <div class="stat-card"><div class="stat-value">${s.trainersCount}</div><div class="stat-label">${I18n.t('admin.stats.trainers')}</div></div>
                <div class="stat-card"><div class="stat-value">${s.clientsCount}</div><div class="stat-label">${I18n.t('admin.stats.clients')}</div></div>
                <div class="stat-card"><div class="stat-value">${s.clientsWithoutTrainer}</div><div class="stat-label">${I18n.t('admin.stats.clientsNoTrainer')}</div></div>
                <div class="stat-card"><div class="stat-value">${s.workoutsCount}</div><div class="stat-label">${I18n.t('admin.stats.workouts')}</div></div>
                <div class="stat-card"><div class="stat-value">${s.exercisesCount}</div><div class="stat-label">${I18n.t('admin.stats.exerciseLabel')}</div></div>
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
            container.innerHTML = `<p class="muted">${I18n.t(this.trainers.length === 0 ? 'admin.empty.trainers' : 'admin.empty.search')}</p>`;
            return;
        }

        container.innerHTML = filtered.map(t => `
            <div class="list-item">
                <div>
                    <h4>${this.escape(t.fullName || t.email)}</h4>
                    <div class="meta">
                        ${this.escape(t.email)}
                        · ${t.clientCount} ${I18n.t(t.clientCount === 1 ? 'admin.clientUnit.one' : 'admin.clientUnit.many')}
                        · ${I18n.t('admin.metaFromDate')} ${this.formatDate(t.createdAt)}
                    </div>
                </div>
                <span class="planned-actions">
                    <button class="btn-delete-icon delete-trainer-btn" data-id="${t.id}" title="${I18n.t('admin.trainers.deleteBtn')}">🗑</button>
                </span>
            </div>
        `).join('');

        container.querySelectorAll('.delete-trainer-btn').forEach(btn => {
            btn.addEventListener('click', e => {
                e.stopPropagation();
                const name = btn.closest('.list-item').querySelector('h4').textContent;
                this.deleteTrainer(btn.dataset.id, name);
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
            container.innerHTML = `<p class="muted">${I18n.t(this.clients.length === 0 ? 'admin.empty.clients' : 'admin.empty.search')}</p>`;
            return;
        }

        container.innerHTML = filtered.map(c => `
            <div class="list-item">
                <div>
                    <h4>${this.escape(c.fullName || c.email)}</h4>
                    <div class="meta">
                        ${this.escape(c.email)}
                        · ${I18n.t('admin.metaTrainerLabel')} ${c.trainerName ? this.escape(c.trainerName) : `<em>${I18n.t('admin.metaNoTrainer')}</em>`}
                        · ${c.workoutCount} ${I18n.t(c.workoutCount === 1 ? 'admin.workoutUnit.one' : 'admin.workoutUnit.many')}
                        · ${I18n.t('admin.metaFromDate')} ${this.formatDate(c.createdAt)}
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
            container.innerHTML = `<p class="muted">${I18n.t(this.workouts.length === 0 ? 'admin.empty.workouts' : 'admin.empty.search')}</p>`;
            return;
        }

        container.innerHTML = filtered.map(w => `
            <div class="list-item">
                <div>
                    <h4>${this.escape(w.name)}</h4>
                    <div class="meta">
                        ${I18n.t('admin.metaClientLabel')} ${this.escape(w.clientName)}
                        ${w.trainerName ? '· ' + I18n.t('admin.metaTrainerLabel') + ' ' + this.escape(w.trainerName) : ''}
                        · ${this.formatDate(w.performedAt)}
                        ${w.durationMinutes ? '· ' + w.durationMinutes + ' min' : ''}
                        · ${w.exerciseCount} ${I18n.t(w.exerciseCount === 1 ? 'admin.workouts.exerciseUnit.one' : 'admin.workouts.exerciseUnit.many')}
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
        if (!email) { errorEl.textContent = I18n.t('admin.trainer.emailRequired'); return; }
        if (!password) { errorEl.textContent = I18n.t('admin.trainer.passwordRequired'); return; }
        if (password.length < 6) { errorEl.textContent = I18n.t('admin.trainer.passwordMin'); return; }

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
        if (!confirm(I18n.tf('admin.trainer.deleteConfirm', { name }))) return;

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

    async loadMail() {
        try {
            const status = await API.get('/admin/email/status');
            this.mailConfigured = !!status.configured;
        } catch (err) {
            this.mailConfigured = false;
        }
        const statusEl = document.getElementById('adminMailStatus');
        if (this.mailConfigured) {
            statusEl.textContent = '';
            statusEl.style.color = '';
        } else {
            statusEl.textContent = I18n.t('admin.smtp.notConfigured');
            statusEl.style.color = '#d9534f';
        }

        if (this.trainers.length === 0) await this.loadTrainers();
        this.renderMailTrainers();
    },

    renderMailTrainers() {
        const container = document.getElementById('adminMailTrainersList');
        const filtered = this.trainers.filter(t =>
            !this.mailTrainerSearch ||
            (t.fullName || '').toLowerCase().includes(this.mailTrainerSearch) ||
            (t.email || '').toLowerCase().includes(this.mailTrainerSearch)
        );

        if (filtered.length === 0) {
            container.innerHTML = `<p class="muted">${I18n.t(this.trainers.length === 0 ? 'admin.empty.trainers' : 'admin.empty.searchShort')}</p>`;
            return;
        }

        container.innerHTML = filtered.map(t => {
            const checked = this.mailSelectedIds.has(t.id) ? 'checked' : '';
            return `
                <label class="list-item" style="cursor:pointer;">
                    <input type="checkbox" class="admin-mail-trainer-cb" value="${this.escape(t.id)}" ${checked} style="margin-right:0.6rem;">
                    <div>
                        <h4 style="margin:0;">${this.escape(t.fullName || t.email)}</h4>
                        <div class="meta">${this.escape(t.email)}</div>
                    </div>
                </label>
            `;
        }).join('');

        container.querySelectorAll('.admin-mail-trainer-cb').forEach(cb => {
            cb.addEventListener('change', e => {
                if (e.target.checked) this.mailSelectedIds.add(e.target.value);
                else this.mailSelectedIds.delete(e.target.value);
            });
        });
    },

    mailSelectAll(all) {
        const filtered = this.trainers.filter(t =>
            !this.mailTrainerSearch ||
            (t.fullName || '').toLowerCase().includes(this.mailTrainerSearch) ||
            (t.email || '').toLowerCase().includes(this.mailTrainerSearch)
        );
        if (all) filtered.forEach(t => this.mailSelectedIds.add(t.id));
        else filtered.forEach(t => this.mailSelectedIds.delete(t.id));
        this.renderMailTrainers();
    },

    async sendMail() {
        const msgEl = document.getElementById('adminMailMsg');
        msgEl.style.color = '';
        msgEl.textContent = '';

        const trainerIds = Array.from(this.mailSelectedIds);
        const subject = document.getElementById('adminMailSubject').value.trim();
        const body = document.getElementById('adminMailBody').value.trim();

        if (trainerIds.length === 0) { msgEl.textContent = I18n.t('admin.mail.pickTrainer'); return; }
        if (!subject) { msgEl.textContent = I18n.t('admin.mail.subjectRequired'); return; }
        if (!body) { msgEl.textContent = I18n.t('admin.mail.bodyRequired'); return; }

        const btn = document.getElementById('adminMailSendBtn');
        btn.disabled = true;
        try {
            const lang = (typeof I18n !== 'undefined' && I18n.lang) ? I18n.lang : 'hr';
            const result = await API.post('/admin/email/send-to-trainers', {
                trainerIds, subject, body, language: lang
            });
            const sentCount = (result.sent || []).length;
            const failedCount = (result.failed || []).length;
            if (failedCount === 0) {
                msgEl.style.color = '#52c452';
                msgEl.textContent = I18n.tf('admin.mail.sentOk', {
                    count: sentCount,
                    unit: I18n.t(sentCount === 1 ? 'admin.mail.recipientUnit.one' : 'admin.mail.recipientUnit.many')
                });
                this.resetMailForm(false);
            } else {
                msgEl.style.color = '#d9534f';
                const failures = result.failed.map(f => `${f.email || f.trainerId}: ${f.error}`).join('\n');
                msgEl.style.whiteSpace = 'pre-wrap';
                msgEl.textContent = I18n.tf('admin.mail.sentMixed', { sent: sentCount, failed: failedCount }) + '\n' + failures;
            }
        } catch (err) {
            msgEl.style.color = '#d9534f';
            msgEl.textContent = err.message;
        } finally {
            btn.disabled = false;
        }
    },

    resetMailForm(clearMessage = true) {
        document.getElementById('adminMailSubject').value = '';
        document.getElementById('adminMailBody').value = '';
        this.mailSelectedIds.clear();
        this.renderMailTrainers();
        if (clearMessage) {
            const msgEl = document.getElementById('adminMailMsg');
            msgEl.textContent = '';
            msgEl.style.color = '';
        }
    },

    formatDate(s) {
        const d = new Date(s);
        return d.toLocaleDateString(I18n.lang === 'en' ? 'en-GB' : 'hr-HR', { day: '2-digit', month: '2-digit', year: 'numeric' });
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
