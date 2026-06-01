const App = {
    async init() {
        await I18n.init();
        this.bindLangSwitcher();

        Auth.init();
        Workouts.init();
        Exercises.init();
        Trainers.init();
        Plans.init();
        Templates.init();
        Nutrition.init();
        MyNutrition.init();
        EditPlanModal.init();
        Admin.init();
        Settings.init();
        ClientTrainers.init();
        Stats.init();
        Profile.init();
        Chat.init();

        const closeTpBtn = document.getElementById('closeTrainerProfileBtn');
        if (closeTpBtn) closeTpBtn.addEventListener('click', () => {
            document.getElementById('trainerProfileModal').classList.add('hidden');
        });
        const tpModal = document.getElementById('trainerProfileModal');
        if (tpModal) tpModal.addEventListener('click', e => {
            if (e.target === tpModal) tpModal.classList.add('hidden');
        });

        document.querySelectorAll('.nav-btn').forEach(btn => {
            btn.addEventListener('click', () => this.switchClientView(btn.dataset.view));
        });

        document.querySelectorAll('.trainer-nav-btn').forEach(btn => {
            btn.addEventListener('click', () => this.switchTrainerView(btn.dataset.trainerView));
        });

        if (Auth.isLoggedIn()) {
            this.showApp(localStorage.getItem('userEmail') || '', localStorage.getItem('userRole') || 'Client');
            if (typeof FirebasePush !== 'undefined') {
                FirebasePush.autoRegisterIfGranted();
            }
        }
    },

    showApp(email, role) {
        document.getElementById('authSection').classList.add('hidden');
        document.getElementById('userInfo').classList.remove('hidden');
        document.getElementById('userEmail').textContent = email;

        const roleBadge = document.getElementById('userRole');
        roleBadge.textContent = this.roleLabel(role);
        roleBadge.className = 'badge ' + (role === 'SuperAdmin' ? 'admin' : role === 'Trainer' ? 'global' : '');

        document.getElementById('adminPanel').classList.add('hidden');
        document.getElementById('trainerPanel').classList.add('hidden');
        document.getElementById('clientPanel').classList.add('hidden');

        this.loadMeAndHeader();

        if (role === 'SuperAdmin') {
            document.getElementById('adminPanel').classList.remove('hidden');
            Admin.load();
        } else if (role === 'Trainer') {
            document.getElementById('trainerPanel').classList.remove('hidden');
            Trainers.load();
            Chat.start();
        } else {
            document.getElementById('clientPanel').classList.remove('hidden');
            Workouts.load();
            this.loadTrainerBanner();
            Chat.start();
        }
    },

    async loadMeAndHeader() {
        try {
            const me = await API.get('/auth/me');
            this.refreshHeaderAvatar(me.profileImageUrl);
        } catch (err) { /* ignore */ }
    },

    bindLangSwitcher() {
        const btns = document.querySelectorAll('.lang-btn');
        const updateVisibility = () => btns.forEach(b => {
            const isCurrent = b.dataset.lang === I18n.lang;
            b.classList.toggle('hidden', isCurrent);
            b.classList.toggle('active', isCurrent);
        });
        updateVisibility();
        btns.forEach(b => b.addEventListener('click', async () => {
            await I18n.setLang(b.dataset.lang);
            updateVisibility();
            const roleBadge = document.getElementById('userRole');
            const currentRole = localStorage.getItem('userRole');
            if (currentRole && roleBadge.textContent) {
                roleBadge.textContent = this.roleLabel(currentRole);
            }
            if (currentRole === 'Client') this.loadTrainerBanner();
        }));
    },

    refreshHeaderAvatar(url) {
        const img = document.getElementById('headerAvatarImg');
        const placeholder = document.getElementById('headerAvatarPlaceholder');
        if (!img || !placeholder) return;
        if (url) {
            img.src = url + (url.includes('?') ? '&' : '?') + 't=' + Date.now();
            img.classList.remove('hidden');
            placeholder.classList.add('hidden');
        } else {
            img.removeAttribute('src');
            img.classList.add('hidden');
            placeholder.classList.remove('hidden');
        }
    },

    async loadTrainerBanner() {
        try {
            const me = await API.get('/auth/me');
            const banner = document.getElementById('clientTrainerBanner');
            banner.classList.remove('hidden');

            if (me.trainerName) {
                banner.classList.remove('no-trainer');
                banner.innerHTML = `
                    ${Profile.avatarHtml(me.trainerImageUrl, 'avatar-mini')}
                    <div class="info">
                        <span class="label">${I18n.t('banner.yourTrainer')}</span>
                        <a href="#" class="name trainer-link" id="trainerProfileLink">${this.escape(me.trainerName)}</a>
                    </div>
                    <button class="secondary small-btn" id="changeTrainerBannerBtn">${I18n.t('common.change')}</button>
                `;
                document.getElementById('trainerProfileLink').addEventListener('click', e => {
                    e.preventDefault();
                    this.openTrainerProfile(me.trainerId, me.trainerName);
                });
            } else {
                banner.classList.add('no-trainer');
                banner.innerHTML = `
                    <div class="info">
                        <span class="label">${I18n.t('banner.noTrainer')}</span>
                        <span class="name">${I18n.t('banner.noTrainerHint')}</span>
                    </div>
                `;
            }

            // The "change trainer" button (only shown when a trainer exists) leads to
            // the Treneri tab, where trainer selection now lives.
            const changeBtn = document.getElementById('changeTrainerBannerBtn');
            if (changeBtn) changeBtn.addEventListener('click', () => this.switchClientView('trainers'));
        } catch (err) {
            console.warn(err);
        }
    },

    async openTrainerProfile(trainerId, trainerName) {
        const modal = document.getElementById('trainerProfileModal');
        const body = document.getElementById('trainerProfileBody');
        document.getElementById('trainerProfileTitle').textContent = `${I18n.t('trainer.profileTitle')} — ${trainerName || I18n.t('role.trainer')}`;
        body.innerHTML = `<p class="muted small">${I18n.t('app.loading')}</p>`;
        modal.classList.remove('hidden');
        try {
            const p = await API.get(`/trainers/${trainerId}/profile`);
            Profile.renderReadOnly(body, p);
        } catch (err) {
            body.innerHTML = `<p class="muted small">${I18n.t('app.errorPrefix')}${this.escape(err.message)}</p>`;
        }
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    },

    roleLabel(role) {
        if (role === 'SuperAdmin') return I18n.t('role.admin');
        if (role === 'Trainer') return I18n.t('role.trainer');
        return I18n.t('role.client');
    },

    async switchClientView(view) {
        document.querySelectorAll('.nav-btn').forEach(b => b.classList.toggle('active', b.dataset.view === view));
        document.getElementById('workoutsView').classList.toggle('hidden', view !== 'workouts');
        document.getElementById('myPlansView').classList.toggle('hidden', view !== 'myPlans');
        const myNut = document.getElementById('myNutritionView');
        if (myNut) myNut.classList.toggle('hidden', view !== 'myNutrition');
        document.getElementById('exercisesView').classList.toggle('hidden', view !== 'exercises');
        document.getElementById('statsView').classList.toggle('hidden', view !== 'stats');
        document.getElementById('profileView').classList.toggle('hidden', view !== 'profile');
        document.getElementById('chatView').classList.toggle('hidden', view !== 'chat');
        const trainersV = document.getElementById('trainersView');
        if (trainersV) trainersV.classList.toggle('hidden', view !== 'trainers');

        if (view === 'trainers') ClientTrainers.load();
        if (view === 'chat') Chat.load();
        if (view === 'workouts') Workouts.load();
        if (view === 'exercises') Exercises.load();
        if (view === 'stats') Stats.load();
        if (view === 'profile') Profile.load();
        if (view === 'myPlans') {
            Plans.showMyPlansList();
            try {
                const me = await API.get('/auth/me');
                Plans.loadMine(me.id);
            } catch (err) { console.error(err); }
        }
        if (view === 'myNutrition') {
            try {
                const me = await API.get('/auth/me');
                MyNutrition.loadMine(me.id);
            } catch (err) { console.error(err); }
        }
    },

    switchTrainerView(view) {
        document.querySelectorAll('.trainer-nav-btn').forEach(b => b.classList.toggle('active', b.dataset.trainerView === view));
        document.getElementById('clientsView').classList.toggle('hidden', view !== 'clients');
        document.getElementById('clientDetail').classList.add('hidden');
        document.getElementById('plansView').classList.toggle('hidden', view !== 'plans');
        const tplView = document.getElementById('templatesView');
        if (tplView) tplView.classList.toggle('hidden', view !== 'templates');
        const nutView = document.getElementById('nutritionView');
        if (nutView) nutView.classList.toggle('hidden', view !== 'nutrition');
        document.getElementById('profileView').classList.toggle('hidden', view !== 'profile');
        document.getElementById('chatView').classList.toggle('hidden', view !== 'chat');

        if (view === 'chat') Chat.load();
        if (view === 'clients') Trainers.load();
        if (view === 'plans') {
            Exercises.load();
            Plans.load();
        }
        if (view === 'templates') {
            Exercises.load();
            Templates.load();
        }
        if (view === 'nutrition') Nutrition.load();
        if (view === 'profile') Profile.load();
    }
};

document.addEventListener('DOMContentLoaded', () => App.init());
