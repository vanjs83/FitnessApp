const Settings = {
    me: null,
    allTrainers: [],
    trainerSearch: '',

    init() {
        document.getElementById('settingsBtn').addEventListener('click', () => this.open());
        document.getElementById('closeSettingsBtn').addEventListener('click', () => this.close());
        document.getElementById('settingsModal').addEventListener('click', e => {
            if (e.target.id === 'settingsModal') this.close();
        });
        document.getElementById('saveProfileBtn').addEventListener('click', () => this.saveProfile());
        document.getElementById('changePasswordBtn').addEventListener('click', () => this.changePassword());

        document.getElementById('settingsTrainerSearch').addEventListener('input', e => {
            this.trainerSearch = e.target.value.toLowerCase();
            this.renderTrainerCards();
        });
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
                await this.loadTrainerOptions();
            } else {
                trainerSection.classList.add('hidden');
            }
        } catch (err) {
            document.getElementById('profileMsg').textContent = err.message;
        }
    },

    async loadTrainerOptions() {
        try {
            this.allTrainers = await API.get('/trainers');
            this.updateCurrentTrainerLabel();
            this.renderTrainerCards();
        } catch (err) {
            console.warn(err);
        }
    },

    updateCurrentTrainerLabel() {
        const label = document.getElementById('currentTrainerLabel');
        label.textContent = this.me.trainerName ? `Trenutni trener: ${this.me.trainerName}` : 'Trenutno: bez trenera';
    },

    renderTrainerCards() {
        const container = document.getElementById('settingsTrainersList');
        const filtered = this.allTrainers.filter(t =>
            !this.trainerSearch ||
            (t.fullName || '').toLowerCase().includes(this.trainerSearch) ||
            (t.email || '').toLowerCase().includes(this.trainerSearch)
        );

        const noneCardCurrent = !this.me.trainerId;
        const noneCard = `
            <div class="trainer-pick-card none-option ${noneCardCurrent ? 'current' : ''}">
                <div class="info">
                    <span class="name">— Bez trenera —</span>
                    <span class="email">Treniraj samostalno</span>
                </div>
                ${noneCardCurrent
                    ? '<span class="current-tag">✓ Trenutno</span>'
                    : '<button class="secondary" data-trainer-id="">Odaberi</button>'}
            </div>
        `;

        const cards = filtered.map(t => {
            const isCurrent = this.me.trainerId === t.id;
            return `
                <div class="trainer-pick-card ${isCurrent ? 'current' : ''}">
                    ${Profile.avatarHtml(t.profileImageUrl, 'avatar-mini')}
                    <div class="info">
                        <span class="name">${this.escape(t.fullName || t.email)}</span>
                        <span class="email">${this.escape(t.email)}</span>
                    </div>
                    ${isCurrent
                        ? '<span class="current-tag">✓ Trenutno</span>'
                        : `<button data-trainer-id="${t.id}">Odaberi</button>`}
                </div>
            `;
        }).join('');

        const empty = filtered.length === 0 && this.trainerSearch
            ? '<p class="muted small">Nema rezultata za pretragu.</p>'
            : '';

        container.innerHTML = (this.trainerSearch ? '' : noneCard) + cards + empty;

        container.querySelectorAll('button[data-trainer-id]').forEach(btn => {
            btn.addEventListener('click', () => this.pickTrainer(btn.dataset.trainerId || null));
        });
    },

    async pickTrainer(trainerId) {
        const msg = document.getElementById('trainerChangeMsg');
        msg.textContent = '';
        msg.style.color = '';

        try {
            await API.put('/auth/trainer', { trainerId });
            this.me = await API.get('/auth/me');
            this.updateCurrentTrainerLabel();
            this.renderTrainerCards();
            if (typeof App !== 'undefined' && App.loadTrainerBanner) App.loadTrainerBanner();

            const myPlansView = document.getElementById('myPlansView');
            if (myPlansView && !myPlansView.classList.contains('hidden')) {
                Plans.showMyPlansList();
                Plans.loadMine(this.me.id);
            }

            msg.style.color = '#51cf66';
            msg.textContent = trainerId ? 'Trener postavljen.' : 'Trener uklonjen.';
        } catch (err) {
            msg.textContent = err.message;
        }
    },

    close() {
        document.getElementById('settingsModal').classList.add('hidden');
        document.getElementById('currentPassword').value = '';
        document.getElementById('newPassword').value = '';
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
