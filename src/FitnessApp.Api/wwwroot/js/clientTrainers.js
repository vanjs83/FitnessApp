// Client-facing "Treneri" view: browse all trainers, open a full profile,
// and send a coaching request (which the trainer must accept).
const ClientTrainers = {
    all: [],
    search: '',
    me: null,
    myRequest: null,
    modalTrainerId: null,

    init() {
        const s = document.getElementById('clientTrainersSearch');
        if (s) s.addEventListener('input', e => { this.search = e.target.value.toLowerCase(); this.renderList(); });
        const b = document.getElementById('modalSendRequestBtn');
        if (b) b.addEventListener('click', () => this.sendRequest(this.modalTrainerId));
    },

    async load() {
        try {
            this.me = await API.get('/auth/me');
            this.myRequest = null;
            try { this.myRequest = await API.get('/trainer-requests/mine'); } catch (e) { /* ignore */ }
            this.all = await API.get('/trainers');
        } catch (err) {
            console.error(err);
        }
        this.renderStatus();
        this.renderList();
    },

    renderStatus() {
        const el = document.getElementById('clientTrainerStatus');
        if (!el) return;
        if (this.me && this.me.trainerId) {
            el.innerHTML = `
                <div class="info-banner">
                    Tvoj trener: <strong>${this.esc(this.me.trainerName || '')}</strong>
                    <button class="secondary small-btn" id="clientDisconnectBtn">Odspoji se</button>
                </div>`;
            const btn = document.getElementById('clientDisconnectBtn');
            if (btn) btn.addEventListener('click', () => this.disconnect());
        } else if (this.myRequest && this.myRequest.status === 'Pending') {
            el.innerHTML = `<div class="info-banner">Zahtjev poslan treneru <strong>${this.esc(this.myRequest.trainerName)}</strong> — čeka odobrenje.</div>`;
        } else if (this.myRequest && this.myRequest.status === 'Rejected') {
            el.innerHTML = `<p class="muted small">Prethodni zahtjev treneru ${this.esc(this.myRequest.trainerName)} je odbijen. Možeš poslati novi.</p>`;
        } else {
            el.innerHTML = '<p class="muted small">Odaberi trenera, pogledaj profil i pošalji zahtjev za suradnju.</p>';
        }
    },

    canRequest() {
        return this.me && !this.me.trainerId && !(this.myRequest && this.myRequest.status === 'Pending');
    },

    renderList() {
        const c = document.getElementById('clientTrainersList');
        if (!c) return;
        const f = this.all.filter(t =>
            !this.search ||
            (t.fullName || '').toLowerCase().includes(this.search) ||
            (t.email || '').toLowerCase().includes(this.search)
        );
        if (!f.length) {
            c.innerHTML = `<p class="muted">${this.search ? 'Nema rezultata za pretragu.' : 'Trenutno nema dostupnih trenera.'}</p>`;
            return;
        }
        c.innerHTML = f.map(t => `
            <div class="list-item" data-id="${t.id}">
                <div class="list-item-main">
                    ${Profile.avatarHtml(t.profileImageUrl, 'avatar-mini')}
                    <div>
                        <h4>${this.esc(t.fullName || t.email)}</h4>
                        <div class="meta">${this.esc(t.email)}</div>
                    </div>
                </div>
                <span class="muted">Profil →</span>
            </div>
        `).join('');
        c.querySelectorAll('.list-item').forEach(el => el.addEventListener('click', () => this.openProfile(el.dataset.id)));
    },

    async openProfile(trainerId) {
        this.modalTrainerId = trainerId;
        const t = this.all.find(x => x.id === trainerId);
        const msg = document.getElementById('modalSendRequestMsg');
        if (msg) { msg.textContent = ''; msg.style.color = ''; }
        const actions = document.getElementById('trainerProfileModalActions');
        if (actions) actions.classList.toggle('hidden', !this.canRequest());
        App.openTrainerProfile(trainerId, t ? (t.fullName || t.email) : '');
    },

    async sendRequest(trainerId) {
        if (!trainerId) return;
        const msg = document.getElementById('modalSendRequestMsg');
        if (msg) { msg.textContent = ''; msg.style.color = ''; }
        try {
            await API.post('/trainer-requests', {
                trainerId,
                language: (typeof I18n !== 'undefined' && I18n.lang) ? I18n.lang : 'hr'
            });
            if (msg) { msg.style.color = '#51cf66'; msg.textContent = 'Zahtjev poslan. Čeka odobrenje trenera.'; }
            const actions = document.getElementById('trainerProfileModalActions');
            if (actions) actions.classList.add('hidden');
            await this.load();
            if (typeof App !== 'undefined' && App.loadTrainerBanner) App.loadTrainerBanner();
        } catch (err) {
            if (msg) msg.textContent = err.message;
        }
    },

    async disconnect() {
        if (!confirm('Odspojiti se od trenera? Tvoji postojeći planovi ostaju, ali trener te više neće voditi.')) return;
        try {
            await API.put('/auth/trainer', { trainerId: null });
            await this.load();
            if (typeof App !== 'undefined' && App.loadTrainerBanner) App.loadTrainerBanner();
        } catch (err) {
            alert(err.message);
        }
    },

    esc(s) {
        const d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    }
};
