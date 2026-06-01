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
    },

    updateRoleUI() {
        // Role selection no longer toggles a trainer picker — clients connect to a
        // trainer later from their profile by sending a request the trainer accepts.
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

        try {
            const res = await API.post('/auth/register', {
                email: document.getElementById('registerEmail').value,
                password: document.getElementById('registerPassword').value,
                fullName: document.getElementById('registerFullName').value,
                role
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
        if (typeof FirebasePush !== 'undefined') {
            FirebasePush.autoRegisterIfGranted();
        }
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
