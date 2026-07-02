const Auth = {
    init() {
        document.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => this.switchTab(tab.dataset.tab));
        });

        document.getElementById('loginForm').addEventListener('submit', e => this.handleLogin(e));
        document.getElementById('registerForm').addEventListener('submit', e => this.handleRegister(e));
        document.getElementById('logoutBtn').addEventListener('click', () => this.logout());

        // Forgot / reset password flow.
        document.getElementById('forgotPasswordLink').addEventListener('click', e => { e.preventDefault(); this.showAuthForm('forgot'); });
        document.getElementById('backToLoginLink').addEventListener('click', e => { e.preventDefault(); this.showAuthForm('login'); });
        document.getElementById('resetBackToLoginLink').addEventListener('click', e => { e.preventDefault(); this.clearResetQuery(); this.showAuthForm('login'); });
        document.getElementById('forgotPasswordForm').addEventListener('submit', e => this.handleForgot(e));
        document.getElementById('resetPasswordForm').addEventListener('submit', e => this.handleReset(e));

        // A reset link from the email lands here as /?reset=TOKEN&email=...
        this.detectResetLink();

        this.initGoogle();
    },

    // Shows exactly one auth form. The login/register tabs are only relevant for those
    // two; the forgot/reset forms hide them so the screen stays focused.
    showAuthForm(which) {
        const tabs = document.querySelector('#authSection .tabs');
        if (tabs) tabs.classList.toggle('hidden', which === 'forgot' || which === 'reset');
        document.getElementById('loginForm').classList.toggle('hidden', which !== 'login');
        document.getElementById('registerForm').classList.toggle('hidden', which !== 'register');
        document.getElementById('forgotPasswordForm').classList.toggle('hidden', which !== 'forgot');
        document.getElementById('resetPasswordForm').classList.toggle('hidden', which !== 'reset');
        if (which === 'login' || which === 'register') {
            document.querySelectorAll('.tab').forEach(t => t.classList.toggle('active', t.dataset.tab === which));
        }
    },

    detectResetLink() {
        const params = new URLSearchParams(window.location.search);
        const token = params.get('reset');
        const email = params.get('email');
        if (token && email) {
            this._resetToken = token;
            this._resetEmail = email;
            this.showAuthForm('reset');
        }
    },

    clearResetQuery() {
        this._resetToken = null;
        this._resetEmail = null;
        history.replaceState(null, '', window.location.pathname);
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
        const loginEl = document.getElementById('googleLoginBtn');
        const regEl = document.getElementById('googleRegisterBtn');
        // Match the button to the form width instead of a fixed 336px (which overflowed
        // narrow phones). GSI only accepts 200–400px, so clamp to that range. The login
        // form is the visible one at init, so measure it and use the same width for both.
        const refEl = loginEl || regEl;
        // Fallback to 336 (the form's content width) when clientWidth is 0 at init —
        // parentElement.clientWidth would add the 2rem padding (→400px) and overflow.
        const containerWidth = (refEl && refEl.clientWidth) || 336;
        const width = Math.round(Math.min(400, Math.max(200, containerWidth)));
        // Rectangular so both buttons match each other and the app's own buttons.
        const common = { theme: 'filled_blue', size: 'large', shape: 'rectangular', width, locale };
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
        this.showAuthForm(tab);
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

    async handleForgot(e) {
        e.preventDefault();
        const msgEl = document.getElementById('forgotMsg');
        msgEl.style.color = '';
        msgEl.textContent = '';

        const btn = e.target.querySelector('button[type="submit"]');
        btn.disabled = true;
        try {
            await API.post('/auth/forgot-password', {
                email: document.getElementById('forgotEmail').value.trim(),
                language: I18n.lang
            });
            // Always a generic success — the server never reveals whether the email exists.
            msgEl.style.color = '#52c452';
            msgEl.textContent = I18n.t('auth.forgotSent');
        } catch (err) {
            msgEl.style.color = '#d9534f';
            msgEl.textContent = err.message;
        } finally {
            btn.disabled = false;
        }
    },

    async handleReset(e) {
        e.preventDefault();
        const msgEl = document.getElementById('resetMsg');
        msgEl.style.color = '';
        msgEl.textContent = '';

        const pw = document.getElementById('resetPassword').value;
        const confirm = document.getElementById('resetPasswordConfirm').value;
        if (pw !== confirm) {
            msgEl.style.color = '#d9534f';
            msgEl.textContent = I18n.t('auth.resetMismatch');
            return;
        }

        const btn = e.target.querySelector('button[type="submit"]');
        btn.disabled = true;
        try {
            await API.post('/auth/reset-password', {
                email: this._resetEmail,
                token: this._resetToken,
                newPassword: pw,
                language: I18n.lang
            });
            this.clearResetQuery();
            msgEl.style.color = '#52c452';
            msgEl.textContent = I18n.t('auth.resetDone');
            e.target.reset();
            setTimeout(() => this.showAuthForm('login'), 1500);
        } catch (err) {
            msgEl.style.color = '#d9534f';
            msgEl.textContent = err.message;
        } finally {
            btn.disabled = false;
        }
    },

    onLoginSuccess(res) {
        API.setToken(res.token);
        if (res.refreshToken) API.setRefreshToken(res.refreshToken);
        localStorage.setItem('userEmail', res.email);
        localStorage.setItem('userRole', res.role);
        App.showApp(res.email, res.role);
        if (typeof FirebasePush !== 'undefined') {
            FirebasePush.autoRegisterIfGranted();
        }
    },

    async logout() {
        // Best-effort server-side revoke; a failure here must not block the local logout.
        const rt = API.getRefreshToken();
        if (rt) {
            try { await API.post('/auth/logout', { refreshToken: rt }); } catch (e) { /* ignore */ }
        }
        API.clearToken();
        window.location.reload();
    },

    isLoggedIn() {
        return !!API.getToken();
    }
};
