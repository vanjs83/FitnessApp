const Settings = {
    me: null,
    allTrainers: [],
    trainerSearch: '',
    myRequest: null,
    viewingTrainerId: null,

    init() {
        document.getElementById('settingsBtn').addEventListener('click', () => this.open());
        document.getElementById('closeSettingsBtn').addEventListener('click', () => this.close());
        document.getElementById('settingsModal').addEventListener('click', e => {
            if (e.target.id === 'settingsModal') this.close();
        });
        document.getElementById('saveProfileBtn').addEventListener('click', () => this.saveProfile());
        document.getElementById('changePasswordBtn').addEventListener('click', () => this.changePassword());
        const supportBtn = document.getElementById('sendSupportBtn');
        if (supportBtn) supportBtn.addEventListener('click', () => this.sendSupport());

        document.getElementById('settingsTrainerSearch').addEventListener('input', e => {
            this.trainerSearch = e.target.value.toLowerCase();
            this.renderTrainerCards();
        });

        const closeTp = document.getElementById('closeTrainerProfileBtn');
        if (closeTp) closeTp.addEventListener('click', () => this.closeTrainerProfile());
        const tpReq = document.getElementById('trainerProfileRequestBtn');
        if (tpReq) tpReq.addEventListener('click', () => this.sendRequest(this.viewingTrainerId));
    },

    async open() {
        document.getElementById('settingsModal').classList.remove('hidden');
        document.getElementById('profileMsg').textContent = '';
        document.getElementById('passwordMsg').textContent = '';
        document.getElementById('trainerChangeMsg').textContent = '';

        try {
            this.me = await API.get('/auth/me');
            document.getElementById('profileEmailLabel').textContent = `Email: ${this.me.email}`;
            document.getElementById('profileRoleLabel').textContent = `Uloga: ${App.roleLabel(this.me.role)}`;
            document.getElementById('profileTrainerLabel').textContent = '';
            document.getElementById('profileFullName').value = this.me.fullName || '';

            const trainerSection = document.getElementById('trainerChangeSection');
            if (this.me.role === 'Client') {
                trainerSection.classList.remove('hidden');
                await this.loadTrainerState();
            } else {
                trainerSection.classList.add('hidden');
            }
        } catch (err) {
            document.getElementById('profileMsg').textContent = err.message;
        }
    },

    // Decides what the trainer section shows: current trainer + disconnect,
    // a pending request + cancel, or the trainer picker (with optional rejected note).
    async loadTrainerState() {
        const statusEl = document.getElementById('trainerRequestStatus');
        const pickWrap = document.getElementById('trainerPickWrap');
        const label = document.getElementById('currentTrainerLabel');
        document.getElementById('trainerProfilePanel').classList.add('hidden');
        document.getElementById('trainerChangeMsg').textContent = '';
        this.viewingTrainerId = null;

        if (this.me.trainerId) {
            label.textContent = `Trenutni trener: ${this.me.trainerName || ''}`;
            statusEl.innerHTML = '<button class="secondary" id="disconnectTrainerBtn">Odspoji se od trenera</button>';
            pickWrap.classList.add('hidden');
            document.getElementById('disconnectTrainerBtn').addEventListener('click', () => this.disconnect());
            return;
        }

        label.textContent = 'Trenutno: bez trenera';

        this.myRequest = null;
        try { this.myRequest = await API.get('/trainer-requests/mine'); } catch (err) { /* ignore */ }

        if (this.myRequest && this.myRequest.status === 'Pending') {
            statusEl.innerHTML = `
                <div class="info-banner">
                    Zahtjev poslan treneru <strong>${this.escape(this.myRequest.trainerName)}</strong> — čeka odobrenje.
                    <button class="secondary small-btn" id="cancelRequestBtn">Otkaži zahtjev</button>
                </div>`;
            pickWrap.classList.add('hidden');
            document.getElementById('cancelRequestBtn').addEventListener('click', () => this.cancelRequest(this.myRequest.id));
            return;
        }

        statusEl.innerHTML = (this.myRequest && this.myRequest.status === 'Rejected')
            ? `<p class="muted small">Prethodni zahtjev treneru ${this.escape(this.myRequest.trainerName)} je odbijen. Možeš poslati novi.</p>`
            : '';

        pickWrap.classList.remove('hidden');
        await this.loadTrainerOptions();
    },

    async loadTrainerOptions() {
        try {
            this.allTrainers = await API.get('/trainers');
            this.renderTrainerCards();
        } catch (err) {
            console.warn(err);
        }
    },

    renderTrainerCards() {
        const container = document.getElementById('settingsTrainersList');
        const filtered = this.allTrainers.filter(t =>
            !this.trainerSearch ||
            (t.fullName || '').toLowerCase().includes(this.trainerSearch) ||
            (t.email || '').toLowerCase().includes(this.trainerSearch)
        );

        const cards = filtered.map(t => `
            <div class="trainer-pick-card">
                ${Profile.avatarHtml(t.profileImageUrl, 'avatar-mini')}
                <div class="info">
                    <span class="name">${this.escape(t.fullName || t.email)}</span>
                    <span class="email">${this.escape(t.email)}</span>
                </div>
                <span class="planned-actions">
                    <button class="secondary small-btn" data-profile-id="${t.id}">Profil</button>
                    <button data-request-id="${t.id}">Pošalji zahtjev</button>
                </span>
            </div>
        `).join('');

        const empty = filtered.length === 0
            ? `<p class="muted small">${this.trainerSearch ? 'Nema rezultata za pretragu.' : 'Trenutno nema dostupnih trenera.'}</p>`
            : '';

        container.innerHTML = cards + empty;

        container.querySelectorAll('button[data-profile-id]').forEach(btn => {
            btn.addEventListener('click', () => this.viewProfile(btn.dataset.profileId));
        });
        container.querySelectorAll('button[data-request-id]').forEach(btn => {
            btn.addEventListener('click', () => this.sendRequest(btn.dataset.requestId));
        });
    },

    async viewProfile(trainerId) {
        this.viewingTrainerId = trainerId;
        const panel = document.getElementById('trainerProfilePanel');
        const body = document.getElementById('trainerProfileBody');
        const t = this.allTrainers.find(x => x.id === trainerId);
        document.getElementById('trainerProfileTitle').textContent = `Profil: ${t ? (t.fullName || t.email) : ''}`;
        document.getElementById('trainerProfileMsg').textContent = '';
        body.innerHTML = '<p class="muted small">Učitavanje…</p>';
        panel.classList.remove('hidden');
        document.getElementById('trainerPickWrap').classList.add('hidden');
        try {
            const p = await API.get(`/trainers/${trainerId}/profile`);
            Profile.renderReadOnly(body, p);
        } catch (err) {
            body.innerHTML = `<p class="muted small">${this.escape(err.message)}</p>`;
        }
    },

    closeTrainerProfile() {
        document.getElementById('trainerProfilePanel').classList.add('hidden');
        this.viewingTrainerId = null;
        if (this.me && !this.me.trainerId && !(this.myRequest && this.myRequest.status === 'Pending')) {
            document.getElementById('trainerPickWrap').classList.remove('hidden');
        }
    },

    async sendRequest(trainerId) {
        if (!trainerId) return;
        const inPanel = !document.getElementById('trainerProfilePanel').classList.contains('hidden');
        const msg = document.getElementById(inPanel ? 'trainerProfileMsg' : 'trainerChangeMsg');
        msg.textContent = '';
        msg.style.color = '';

        try {
            await API.post('/trainer-requests', {
                trainerId,
                language: (typeof I18n !== 'undefined' && I18n.lang) ? I18n.lang : 'hr'
            });
            this.me = await API.get('/auth/me');
            await this.loadTrainerState();
            const s = document.getElementById('trainerChangeMsg');
            s.style.color = '#51cf66';
            s.textContent = 'Zahtjev poslan. Čeka odobrenje trenera.';
        } catch (err) {
            msg.textContent = err.message;
        }
    },

    async cancelRequest(id) {
        const msg = document.getElementById('trainerChangeMsg');
        msg.textContent = '';
        msg.style.color = '';
        try {
            await API.delete(`/trainer-requests/${id}`);
            this.me = await API.get('/auth/me');
            await this.loadTrainerState();
        } catch (err) {
            msg.textContent = err.message;
        }
    },

    async disconnect() {
        if (!confirm('Odspojiti se od trenera? Tvoji postojeći planovi ostaju, ali trener te više neće voditi.')) return;
        const msg = document.getElementById('trainerChangeMsg');
        msg.textContent = '';
        msg.style.color = '';
        try {
            await API.put('/auth/trainer', { trainerId: null });
            this.me = await API.get('/auth/me');
            await this.loadTrainerState();
            if (typeof App !== 'undefined' && App.loadTrainerBanner) App.loadTrainerBanner();

            const myPlansView = document.getElementById('myPlansView');
            if (myPlansView && !myPlansView.classList.contains('hidden')) {
                Plans.showMyPlansList();
                Plans.loadMine(this.me.id);
            }

            msg.style.color = '#51cf66';
            msg.textContent = 'Odspojen od trenera.';
        } catch (err) {
            msg.textContent = err.message;
        }
    },

    close() {
        document.getElementById('settingsModal').classList.add('hidden');
        document.getElementById('currentPassword').value = '';
        document.getElementById('newPassword').value = '';
        const supSubj = document.getElementById('supportSubject');
        const supBody = document.getElementById('supportBody');
        const supMsg = document.getElementById('supportMsg');
        if (supSubj) supSubj.value = '';
        if (supBody) supBody.value = '';
        if (supMsg) { supMsg.textContent = ''; supMsg.style.color = ''; }
    },

    async sendSupport() {
        const msg = document.getElementById('supportMsg');
        msg.textContent = '';
        msg.style.color = '';

        const subject = document.getElementById('supportSubject').value.trim();
        const body = document.getElementById('supportBody').value.trim();
        if (!subject) { msg.textContent = I18n.t('support.subjectRequired'); return; }
        if (!body) { msg.textContent = I18n.t('support.bodyRequired'); return; }

        try {
            await API.post('/support/contact', {
                subject,
                body,
                language: (typeof I18n !== 'undefined' && I18n.lang) ? I18n.lang : 'hr'
            });
            msg.style.color = '#51cf66';
            msg.textContent = I18n.t('settings.supportSent');
            document.getElementById('supportSubject').value = '';
            document.getElementById('supportBody').value = '';
        } catch (err) {
            msg.textContent = err.message;
        }
    },

    async saveProfile() {
        const msg = document.getElementById('profileMsg');
        msg.textContent = '';
        msg.style.color = '';

        try {
            await API.put('/auth/profile', {
                fullName: document.getElementById('profileFullName').value || null
            });
            msg.style.color = '#51cf66';
            msg.textContent = 'Profil spremljen.';
        } catch (err) {
            msg.textContent = err.message;
        }
    },

    async changePassword() {
        const msg = document.getElementById('passwordMsg');
        msg.textContent = '';
        msg.style.color = '';

        const current = document.getElementById('currentPassword').value;
        const next = document.getElementById('newPassword').value;
        if (!current || next.length < 6) {
            msg.textContent = 'Popuni trenutnu lozinku i novu (min 6 znakova).';
            return;
        }

        try {
            await API.post('/auth/change-password', {
                currentPassword: current,
                newPassword: next
            });
            msg.style.color = '#51cf66';
            msg.textContent = 'Lozinka promijenjena.';
            document.getElementById('currentPassword').value = '';
            document.getElementById('newPassword').value = '';
        } catch (err) {
            msg.textContent = err.message;
        }
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
