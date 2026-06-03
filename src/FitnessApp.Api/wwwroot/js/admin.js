const Admin = {
    trainers: [],
    clients: [],
    trainerSearch: '',
    clientSearch: '',
    recipients: [],
    msgSearch: '',
    msgSelectedIds: new Set(),
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

        document.getElementById('adminMsgSearch').addEventListener('input', e => {
            this.msgSearch = e.target.value.toLowerCase();
            this.renderRecipients();
        });
        document.getElementById('adminMsgSelectAll').addEventListener('click', () => this.msgSelectFiltered(true));
        document.getElementById('adminMsgSelectNone').addEventListener('click', () => this.msgSelectFiltered(false));
        document.getElementById('adminMsgSelectNoTrainer').addEventListener('click', () => this.msgSelectClientsWithoutTrainer());
        document.getElementById('adminEmailSendBtn').addEventListener('click', () => this.sendEmail());
        document.getElementById('adminEmailResetBtn').addEventListener('click', () => this.resetEmail());
        document.getElementById('adminPushSendBtn').addEventListener('click', () => this.sendPush());
        document.getElementById('adminPushResetBtn').addEventListener('click', () => this.resetPush());
    },

    async load() {
        await Promise.all([this.loadStats(), this.loadTrainers()]);
    },

    switchTab(tab) {
        document.querySelectorAll('.admin-tab').forEach(t => t.classList.toggle('active', t.dataset.adminTab === tab));
        document.getElementById('adminTrainersTab').classList.toggle('hidden', tab !== 'trainers');
        document.getElementById('adminClientsTab').classList.toggle('hidden', tab !== 'clients');
        document.getElementById('adminMailTab').classList.toggle('hidden', tab !== 'mail');

        if (tab === 'trainers') this.loadTrainers();
        if (tab === 'clients') this.loadClients();
        if (tab === 'mail') this.loadMail();
    },

    async loadStats() {
        try {
            const s = await API.get('/admin/stats');
            document.getElementById('adminStats').innerHTML = `
                <div class="stat-card"><div class="stat-value">${s.trainersCount}</div><div class="stat-label">${I18n.t('admin.stats.trainers')}</div></div>
                <div class="stat-card"><div class="stat-value">${s.clientsCount}</div><div class="stat-label">${I18n.t('admin.stats.clients')}</div></div>
                <div class="stat-card"><div class="stat-value">${s.clientsWithoutTrainer}</div><div class="stat-label">${I18n.t('admin.stats.clientsNoTrainer')}</div></div>
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
            statusEl.textContent = I18n.t('admin.smtp.notConfigured') + ' (push svejedno radi)';
            statusEl.style.color = '#d9534f';
        }

        if (this.trainers.length === 0) await this.loadTrainers();
        if (this.clients.length === 0) await this.loadClients();
        this.buildRecipients();
        this.renderRecipients();
    },

    // Trainers + clients combined into one selectable recipient list.
    buildRecipients() {
        const fromTrainers = this.trainers.map(t => ({
            id: t.id, name: t.fullName || t.email, email: t.email, role: 'Trainer', hasTrainer: true
        }));
        const fromClients = this.clients.map(c => ({
            id: c.id, name: c.fullName || c.email, email: c.email, role: 'Client', hasTrainer: !!c.trainerName
        }));
        this.recipients = [...fromTrainers, ...fromClients];
    },

    filteredRecipients() {
        const q = this.msgSearch;
        return this.recipients.filter(r =>
            !q ||
            (r.name || '').toLowerCase().includes(q) ||
            (r.email || '').toLowerCase().includes(q) ||
            (r.role === 'Trainer' ? 'trener' : 'klijent').includes(q)
        );
    },

    renderRecipients() {
        const container = document.getElementById('adminMsgRecipientsList');
        const filtered = this.filteredRecipients();

        if (filtered.length === 0) {
            container.innerHTML = `<p class="muted">${this.recipients.length === 0 ? 'Nema korisnika.' : 'Nema rezultata.'}</p>`;
            this.updateSelectedCount();
            return;
        }

        container.innerHTML = filtered.map(r => {
            const checked = this.msgSelectedIds.has(r.id);
            const roleLabel = r.role === 'Trainer' ? 'Trener' : 'Klijent';
            const noTrainer = r.role === 'Client' && !r.hasTrainer ? ' · <span class="recipient-flag">bez trenera</span>' : '';
            return `
                <label class="recipient-item${checked ? ' selected' : ''}">
                    <input type="checkbox" class="msg-recipient-cb" value="${this.escape(r.id)}" ${checked ? 'checked' : ''}>
                    <span class="recipient-info">
                        <span class="recipient-name">${this.escape(r.name)}</span>
                        <span class="recipient-meta">${this.escape(r.email)} · <span class="recipient-role">${roleLabel}</span>${noTrainer}</span>
                    </span>
                </label>
            `;
        }).join('');

        container.querySelectorAll('.msg-recipient-cb').forEach(cb => {
            cb.addEventListener('change', e => {
                if (e.target.checked) this.msgSelectedIds.add(e.target.value);
                else this.msgSelectedIds.delete(e.target.value);
                e.target.closest('.recipient-item').classList.toggle('selected', e.target.checked);
                this.updateSelectedCount();
            });
        });
        this.updateSelectedCount();
    },

    updateSelectedCount() {
        const el = document.getElementById('adminMsgSelectedCount');
        if (el) el.textContent = `Odabrano: ${this.msgSelectedIds.size}`;
    },

    msgSelectFiltered(all) {
        this.filteredRecipients().forEach(r => {
            if (all) this.msgSelectedIds.add(r.id);
            else this.msgSelectedIds.delete(r.id);
        });
        this.renderRecipients();
    },

    msgSelectClientsWithoutTrainer() {
        this.recipients
            .filter(r => r.role === 'Client' && !r.hasTrainer)
            .forEach(r => this.msgSelectedIds.add(r.id));
        this.renderRecipients();
    },

    // Shared sender for both email and push panels.
    async _sendMessage({ url, subjectId, bodyId, btnId, msgId, subjectLabel }) {
        const msgEl = document.getElementById(msgId);
        msgEl.style.color = '';
        msgEl.style.whiteSpace = 'pre-wrap';
        msgEl.textContent = '';

        const userIds = Array.from(this.msgSelectedIds);
        const subject = document.getElementById(subjectId).value.trim();
        const body = document.getElementById(bodyId).value.trim();

        if (userIds.length === 0) { msgEl.textContent = 'Odaberi barem jednog primatelja.'; return; }
        if (!subject) { msgEl.textContent = `Unesi ${subjectLabel}.`; return; }
        if (!body) { msgEl.textContent = 'Unesi poruku.'; return; }

        const btn = document.getElementById(btnId);
        btn.disabled = true;
        try {
            const lang = (typeof I18n !== 'undefined' && I18n.lang) ? I18n.lang : 'hr';
            const result = await API.post(url, { userIds, subject, body, language: lang });
            const sent = (result.sent || []).length;
            const failed = result.failed || [];
            if (failed.length === 0) {
                msgEl.style.color = '#52c452';
                msgEl.textContent = `Poslano: ${sent}.`;
            } else {
                msgEl.style.color = '#d9534f';
                const lines = failed.map(f => `${f.recipient || f.userId}: ${f.error}`).join('\n');
                msgEl.textContent = `Poslano: ${sent}, palo: ${failed.length}.\n${lines}`;
            }
        } catch (err) {
            msgEl.style.color = '#d9534f';
            msgEl.textContent = err.message;
        } finally {
            btn.disabled = false;
        }
    },

    sendEmail() {
        return this._sendMessage({
            url: '/admin/email/send-to-users',
            subjectId: 'adminEmailSubject', bodyId: 'adminEmailBody',
            btnId: 'adminEmailSendBtn', msgId: 'adminEmailMsg', subjectLabel: 'predmet'
        });
    },

    sendPush() {
        return this._sendMessage({
            url: '/admin/push/send',
            subjectId: 'adminPushTitle', bodyId: 'adminPushBody',
            btnId: 'adminPushSendBtn', msgId: 'adminPushMsg', subjectLabel: 'naslov'
        });
    },

    resetEmail() {
        document.getElementById('adminEmailSubject').value = '';
        document.getElementById('adminEmailBody').value = '';
        const m = document.getElementById('adminEmailMsg'); m.textContent = ''; m.style.color = '';
    },

    resetPush() {
        document.getElementById('adminPushTitle').value = '';
        document.getElementById('adminPushBody').value = '';
        const m = document.getElementById('adminPushMsg'); m.textContent = ''; m.style.color = '';
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
