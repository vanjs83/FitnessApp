const Settings = {
    me: null,

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
    },

    async open() {
        document.getElementById('settingsModal').classList.remove('hidden');
        document.getElementById('profileMsg').textContent = '';
        document.getElementById('passwordMsg').textContent = '';

        try {
            this.me = await API.get('/auth/me');
            document.getElementById('profileEmailLabel').textContent = `Email: ${this.me.email}`;
            document.getElementById('profileRoleLabel').textContent = `Uloga: ${App.roleLabel(this.me.role)}`;
            document.getElementById('profileFullName').value = this.me.fullName || '';
        } catch (err) {
            document.getElementById('profileMsg').textContent = err.message;
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
    }
};
