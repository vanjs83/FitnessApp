const Auth = {
    init() {
        document.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => this.switchTab(tab.dataset.tab));
        });

        document.getElementById('loginForm').addEventListener('submit', e => this.handleLogin(e));
        document.getElementById('registerForm').addEventListener('submit', e => this.handleRegister(e));
        document.getElementById('logoutBtn').addEventListener('click', () => this.logout());

        this.initGoogle();
    },

    // Sets up "Sign in / up with Google" buttons. No-op when the feature is not
    // configured on the server (empty Google:ClientId) so basic login still works.
    async initGoogle() {
        let clientId = '';
        try {
            const cfg = await API.get('/auth/google-config');
            clientId = cfg && cfg.clientId;
        } catch (e) { /* feature unavailable */ }
        if (!clientId) return;

        // The GSI script loads async — wait briefly for it to be ready.
        const start = Date.now();
        while (!(window.google && google.accounts && google.accounts.id)) {
            if (Date.now() - start > 5000) return;
            await new Promise(r => setTimeout(r, 100));
        }

        google.accounts.id.initialize({
            client_id: clientId,
            callback: resp => this.handleGoogleCredential(resp)
        });

        const locale = (typeof I18n !== 'undefined' && I18n.lang) ? I18n.lang : 'hr';
        // Rectangular + fixed width so both buttons match each other and the app's own buttons.
        const common = { theme: 'filled_blue', size: 'large', shape: 'rectangular', width: 336, locale };
        const loginEl = document.getElementById('googleLoginBtn');
        const regEl = document.getElementById('googleRegisterBtn');
        if (loginEl) google.accounts.id.renderButton(loginEl, { ...common, text: 'signin_with' });
        if (regEl) google.accounts.id.renderButton(regEl, { ...common, text: 'signup_with' });
    },

    // Shared callback for both buttons. The active tab decides whether we send a
    // role: only the Register tab carries the Client/Trainer choice for new accounts.
    async handleGoogleCredential(response) {
        const isRegister = !document.getElementById('registerForm').classList.contains('hidden');
        const errorEl = document.getElementById(isRegister ? 'registerError' : 'loginError');
        if (errorEl) errorEl.textContent = '';

        const body = { credential: response.credential };
        if (isRegister) {
            const checked = document.querySelector('input[name="role"]:checked');
            body.role = checked ? checked.value : null;
        }

        try {
            const res = await API.post('/auth/google', body);
            this.onLoginSuccess(res);
        } catch (err) {
            if (errorEl) errorEl.textContent = err.message;
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
            errorEl.textContent = this.loginErrorMessage(err);
        }
    },

    loginErrorMessage(err) {
        const code = err && err.data && err.data.code;
        if (code === 'user_not_found') return I18n.t('auth.error.userNotFound');
        if (code === 'wrong_password') return I18n.t('auth.error.wrongPassword');
        return err.message;
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
    }
};
