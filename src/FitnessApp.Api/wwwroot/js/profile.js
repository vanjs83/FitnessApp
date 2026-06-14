const Profile = {
    currentRole: null,

    init() {
        const saveBtn = document.getElementById('saveProfileFullBtn');
        if (saveBtn) saveBtn.addEventListener('click', () => this.save());

        const uploadBtn = document.getElementById('pfAvatarUploadBtn');
        const fileInput = document.getElementById('pfAvatarFile');
        const removeBtn = document.getElementById('pfAvatarRemoveBtn');

        if (uploadBtn && fileInput) {
            uploadBtn.addEventListener('click', () => fileInput.click());
            fileInput.addEventListener('change', () => this.uploadAvatar(fileInput.files[0]));
        }
        if (removeBtn) removeBtn.addEventListener('click', () => this.removeAvatar());

        const notifChk = document.getElementById('pfReceiveNotifications');
        if (notifChk) notifChk.addEventListener('change', () => this.toggleNotifications(notifChk.checked));
    },

    refreshNotifUi() {
        const chk = document.getElementById('pfReceiveNotifications');
        const status = document.getElementById('pfNotifStatus');
        if (!chk || !status || typeof FirebasePush === 'undefined') return;

        if (!FirebasePush.isSupported()) {
            chk.checked = false;
            chk.disabled = true;
            status.textContent = I18n.t('profile.notif.unsupported');
            return;
        }
        const perm = FirebasePush.permission();
        chk.disabled = (perm === 'denied');
        if (perm === 'granted') {
            chk.checked = !!localStorage.getItem(FirebasePush.LS_KEY);
            status.textContent = chk.checked ? I18n.t('profile.notif.granted') : I18n.t('profile.notif.default');
        } else if (perm === 'denied') {
            chk.checked = false;
            status.textContent = I18n.t('profile.notif.denied');
        } else {
            chk.checked = false;
            status.textContent = I18n.t('profile.notif.default');
        }
    },

    async toggleNotifications(enable) {
        const status = document.getElementById('pfNotifStatus');
        if (typeof FirebasePush === 'undefined') return;
        if (enable) {
            const res = await FirebasePush.requestAndRegister();
            if (!res.ok) {
                document.getElementById('pfReceiveNotifications').checked = false;
                if (status) status.textContent = (res.reason === 'denied')
                    ? I18n.t('profile.notif.denied')
                    : I18n.t('profile.notif.requestFailed');
                return;
            }
            if (status) status.textContent = I18n.t('profile.notif.granted');
        } else {
            await FirebasePush.unregister();
            if (status) status.textContent = I18n.t('profile.notif.default');
        }
    },

    async load() {
        try {
            const p = await API.get('/auth/personal-profile');
            this.fill(p);
        } catch (err) {
            const msg = document.getElementById('profileFullMsg');
            if (msg) msg.textContent = err.message;
        }
    },

    fill(p) {
        this.currentRole = p.role || localStorage.getItem('userRole') || 'Client';
        const isTrainer = this.currentRole === 'Trainer';

        document.getElementById('pfFullName').value = p.fullName || '';
        const emailEl = document.getElementById('pfEmail');
        if (emailEl) emailEl.value = p.email || '';
        document.getElementById('pfBirthDate').value = p.birthDate ? p.birthDate.substring(0, 10) : '';
        document.getElementById('pfGender').value = p.gender || '';
        document.getElementById('pfHeight').value = p.heightCm ?? '';
        document.getElementById('pfWeight').value = p.weightKg ?? '';
        document.getElementById('pfActivityLevel').value = p.activityLevel || '';
        document.getElementById('pfPhone').value = p.phone || '';
        document.getElementById('pfGoal').value = p.goal || '';
        document.getElementById('pfHealthNotes').value = p.healthNotes || '';

        document.getElementById('pfHeightWeightRow').classList.toggle('hidden', isTrainer);
        document.getElementById('pfActivityCol').classList.toggle('hidden', isTrainer);
        document.getElementById('pfGoalBlock').classList.toggle('hidden', isTrainer);
        document.getElementById('pfHealthBlock').classList.toggle('hidden', isTrainer);

        const prefsBlock = document.getElementById('pfClientPrefs');
        const isClient = this.currentRole === 'Client';
        if (prefsBlock) prefsBlock.style.display = isClient ? '' : 'none';
        document.getElementById('pfWeeklyCount').value = p.preferredWeeklyTrainingCount ?? '';
        document.getElementById('pfTrainingType').value = p.preferredTrainingType || '';

        this.setAvatar(p.profileImageUrl);
        this.refreshNotifUi();

        const msg = document.getElementById('profileFullMsg');
        if (msg) msg.textContent = '';
        const avMsg = document.getElementById('pfAvatarMsg');
        if (avMsg) avMsg.textContent = '';
    },

    setAvatar(url) {
        const img = document.getElementById('pfAvatarImg');
        const placeholder = document.getElementById('pfAvatarPlaceholder');
        const removeBtn = document.getElementById('pfAvatarRemoveBtn');
        if (url) {
            img.src = url + '?t=' + Date.now();
            img.classList.remove('hidden');
            placeholder.classList.add('hidden');
            if (removeBtn) removeBtn.classList.remove('hidden');
        } else {
            img.removeAttribute('src');
            img.classList.add('hidden');
            placeholder.classList.remove('hidden');
            if (removeBtn) removeBtn.classList.add('hidden');
        }
        App.refreshHeaderAvatar(url);
    },

    async uploadAvatar(file) {
        const msg = document.getElementById('pfAvatarMsg');
        msg.textContent = '';
        msg.style.color = '';
        if (!file) return;

        if (file.size > 5 * 1024 * 1024) {
            msg.textContent = I18n.t('profile.imageTooLarge');
            return;
        }

        const form = new FormData();
        form.append('file', file);
        const token = API.getToken();
        try {
            const res = await fetch('/api/v1/auth/profile-image', {
                method: 'POST',
                headers: token ? { 'Authorization': `Bearer ${token}` } : {},
                body: form
            });
            const text = await res.text();
            const data = text ? JSON.parse(text) : null;
            if (!res.ok) {
                throw new Error(data?.message || (data?.errors && Object.values(data.errors).flat().join(', ')) || `HTTP ${res.status}`);
            }
            this.setAvatar(data.profileImageUrl);
            msg.style.color = '#4dc878';
            msg.textContent = I18n.t('profile.imageSaved');
        } catch (err) {
            msg.textContent = err.message;
        } finally {
            document.getElementById('pfAvatarFile').value = '';
        }
    },

    async removeAvatar() {
        const msg = document.getElementById('pfAvatarMsg');
        msg.textContent = '';
        try {
            await API.delete('/auth/profile-image');
            this.setAvatar(null);
            msg.style.color = '#4dc878';
            msg.textContent = I18n.t('profile.imageRemoved');
        } catch (err) {
            msg.textContent = err.message;
        }
    },

    async save() {
        const msg = document.getElementById('profileFullMsg');
        msg.textContent = '';
        msg.style.color = '';

        const isTrainer = this.currentRole === 'Trainer';
        const weeklyRaw = document.getElementById('pfWeeklyCount').value;
        const weekly = weeklyRaw === '' ? null : parseInt(weeklyRaw);

        const body = {
            fullName: document.getElementById('pfFullName').value.trim() || null,
            birthDate: document.getElementById('pfBirthDate').value || null,
            gender: document.getElementById('pfGender').value || null,
            phone: document.getElementById('pfPhone').value.trim() || null,
            heightCm: isTrainer ? null : (parseInt(document.getElementById('pfHeight').value) || null),
            weightKg: isTrainer ? null : (parseFloat(document.getElementById('pfWeight').value) || null),
            activityLevel: isTrainer ? null : (document.getElementById('pfActivityLevel').value || null),
            goal: isTrainer ? null : (document.getElementById('pfGoal').value.trim() || null),
            healthNotes: isTrainer ? null : (document.getElementById('pfHealthNotes').value.trim() || null),
            preferredWeeklyTrainingCount: isTrainer ? null : (Number.isFinite(weekly) ? weekly : null),
            preferredTrainingType: isTrainer ? null : (document.getElementById('pfTrainingType').value || null)
        };

        try {
            await API.put('/auth/personal-profile', body);
            msg.style.color = '#4dc878';
            msg.textContent = I18n.t('profile.saved');
        } catch (err) {
            msg.textContent = err.message;
        }
    },

    renderReadOnly(container, p) {
        const isTrainer = p.role === 'Trainer';
        const dash = '—';
        const dateLocale = I18n.lang === 'en' ? 'en-GB' : 'hr-HR';
        const rows = [
            [I18n.t('common.fullName'), p.fullName],
            [I18n.t('common.email'), p.email],
            [I18n.t('profile.birthDate'), p.birthDate ? new Date(p.birthDate).toLocaleDateString(dateLocale) : null],
            [I18n.t('profile.gender'), p.gender === 'M' ? I18n.t('profile.gender.male') : p.gender === 'F' ? I18n.t('profile.gender.female') : null],
            [I18n.t('common.phone'), p.phone],
            !isTrainer && [I18n.t('profile.readonly.height'), p.heightCm ? `${p.heightCm} cm` : null],
            !isTrainer && [I18n.t('profile.readonly.weight'), p.weightKg != null ? `${p.weightKg} kg` : null],
            !isTrainer && [I18n.t('profile.readonly.activity'), this.activityLabel(p.activityLevel)],
            !isTrainer && [I18n.t('profile.readonly.weeklyTrainings'), p.preferredWeeklyTrainingCount != null ? `${p.preferredWeeklyTrainingCount}×` : null],
            !isTrainer && [I18n.t('profile.readonly.preferredType'), this.trainingTypeLabel(p.preferredTrainingType)],
            !isTrainer && [I18n.t('profile.readonly.goal'), p.goal],
            !isTrainer && [I18n.t('profile.readonly.healthNotes'), p.healthNotes]
        ].filter(Boolean);

        const avatarHtml = p.profileImageUrl
            ? `<div class="profile-avatar-row"><div class="avatar-preview"><img src="${this.escape(p.profileImageUrl)}" alt=""></div></div>`
            : '';

        container.innerHTML = avatarHtml + rows.map(([k, v]) => {
            const isEmpty = v === null || v === undefined || v === '';
            const valHtml = isEmpty
                ? `<span class="profile-val muted">${dash}</span>`
                : `<span class="profile-val">${this.escape(String(v))}</span>`;
            return `<div class="profile-row"><span class="profile-key">${k}</span>${valHtml}</div>`;
        }).join('');
    },

    activityLabel(v) {
        if (v === 'Beginner') return I18n.t('profile.activity.beginner');
        if (v === 'Intermediate') return I18n.t('profile.activity.intermediate');
        if (v === 'Advanced') return I18n.t('profile.activity.advanced');
        return null;
    },

    trainingTypeLabel(v) {
        if (!v) return null;
        return ExerciseTypeLabels[v] ?? v;
    },

    avatarHtml(url, sizeClass = 'avatar-sm') {
        if (url) {
            return `<span class="${sizeClass}"><img src="${this.escape(url)}" alt=""></span>`;
        }
        return `<span class="${sizeClass}"><span>👤</span></span>`;
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
