const Auth = {
    init() {
        document.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => this.switchTab(tab.dataset.tab));
        });

        document.getElementById('loginForm').addEventListener('submit', e => this.handleLogin(e));
        document.getElementById('registerForm').addEventListener('submit', e => this.handleRegister(e));
        document.getElementById('logoutBtn').addEventListener('click', () => this.logout());

        document.querySelectorAll('input[name="role"]').forEach(r => {
            r.addEventListener('change', () => this.updateRoleUI());
        });

        this.updateRoleUI();
        this.loadTrainersForPicker();
    },

    updateRoleUI() {
        const role = document.querySelector('input[name="role"]:checked').value;
        document.getElementById('trainerPickerWrap').classList.toggle('hidden', role !== 'Client');
    },

    async loadTrainersForPicker() {
        try {
            const trainers = await API.get('/trainers');
            const select = document.getElementById('registerTrainer');
            select.innerHTML = `<option value="">${I18n.t('auth.noTrainer')}</option>` +
                trainers.map(t =>
                    `<option value="${t.id}">${this.escape(t.fullName || t.email)}</option>`
                ).join('');
        } catch (err) {
            console.warn(I18n.t('app.cantLoadTrainers'), err);
        }
    },

    switchTab(tab) {
        document.querySelectorAll('.tab').forEach(t => t.classList.toggle('active', t.dataset.tab === tab));
        document.getElementById('loginForm').classList.toggle('hidden', tab !== 'login');
        document.getElementById('registerForm').classList.toggle('hidden', tab !== 'register');
    },

    async handleLogin(e) {
        e.preventDefault();
        const errorEl = document.getElementById('loginError');
        errorEl.textContent = '';

        try {
            const res = await API.post('/auth/login', {
                email: document.getElementById('loginEmail').value,
                password: document.getElementById('loginPassword').value
            });
            this.onLoginSuccess(res);
        } catch (err) {
            errorEl.textContent = err.message;
        }
    },

    async handleRegister(e) {
        e.preventDefault();
        const errorEl = document.getElementById('registerError');
        errorEl.textContent = '';

        const role = document.querySelector('input[name="role"]:checked').value;
        const trainerId = role === 'Client' ? document.getElementById('registerTrainer').value || null : null;

        try {
            const res = await API.post('/auth/register', {
                email: document.getElementById('registerEmail').value,
                password: document.getElementById('registerPassword').value,
                fullName: document.getElementById('registerFullName').value,
                role,
                trainerId
            });
            this.onLoginSuccess(res);
        } catch (err) {
            errorEl.textContent = err.message;
        }
    },

    onLoginSuccess(res) {
        API.setToken(res.token);
        localStorage.setItem('userEmail', res.email);
        localStorage.setItem('userRole', res.role);
        App.showApp(res.email, res.role);
    },

    logout() {
        API.clearToken();
        localStorage.removeItem('userRole');
        window.location.reload();
    },

    isLoggedIn() {
        return !!API.getToken();
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }
};
