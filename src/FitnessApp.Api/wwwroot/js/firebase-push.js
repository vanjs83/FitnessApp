const FirebasePush = {
    config: {
        apiKey: "AIzaSyCDwPhjlnP4huCfOX5gkMVrr1pV824JopI",
        authDomain: "fitnessapp-6655e.firebaseapp.com",
        projectId: "fitnessapp-6655e",
        storageBucket: "fitnessapp-6655e.firebasestorage.app",
        messagingSenderId: "707739351150",
        appId: "1:707739351150:web:2b83dd0fc067944826ce7f"
    },
    vapidKey: "BDr1AvIGMBD5zYmhorTFDJ-0nRaxLjKM5bdY6zCA2x614vWx3DxbPDeDyUQzCv5234o28lFJy9favX7VcanawNA",
    LS_KEY: 'fcm_token',
    _app: null,
    _messaging: null,
    _initialized: false,

    isSupported() {
        return 'serviceWorker' in navigator && 'Notification' in window && 'PushManager' in window;
    },

    permission() {
        return ('Notification' in window) ? Notification.permission : 'denied';
    },

    async _ensureInit() {
        if (this._initialized) return true;
        if (!this.isSupported()) return false;
        if (typeof firebase === 'undefined') {
            console.warn('Firebase SDK nije učitan.');
            return false;
        }
        try {
            this._app = firebase.initializeApp(this.config);
        } catch (e) {
            this._app = firebase.app();
        }
        this._messaging = firebase.messaging();

        this._messaging.onMessage((payload) => {
            const title = (payload.notification && payload.notification.title) || 'FitnessApp';
            const body = (payload.notification && payload.notification.body) || '';
            try { new Notification(title, { body, icon: '/favicon.ico' }); }
            catch (_) { console.log('[push foreground]', title, body); }
        });

        this._initialized = true;
        return true;
    },

    async _getSwRegistration() {
        return await navigator.serviceWorker.register('/firebase-messaging-sw.js');
    },

    async _fetchToken() {
        const reg = await this._getSwRegistration();
        return await this._messaging.getToken({
            vapidKey: this.vapidKey,
            serviceWorkerRegistration: reg
        });
    },

    async _sendToBackend(token) {
        if (!token) return;
        try {
            await API.post('/devices', { token, platform: 'web' });
            localStorage.setItem(this.LS_KEY, token);
        } catch (err) {
            console.warn('Spremanje device tokena nije uspjelo:', err.message);
        }
    },

    async autoRegisterIfGranted() {
        if (!this.isSupported()) return;
        if (this.permission() !== 'granted') return;
        if (!Auth.isLoggedIn()) return;
        const ok = await this._ensureInit();
        if (!ok) return;
        try {
            const token = await this._fetchToken();
            if (!token) return;
            const saved = localStorage.getItem(this.LS_KEY);
            if (token !== saved) await this._sendToBackend(token);
        } catch (err) {
            console.warn('autoRegisterIfGranted greška:', err);
        }
    },

    async requestAndRegister() {
        if (!this.isSupported()) {
            return { ok: false, reason: 'unsupported' };
        }
        const perm = await Notification.requestPermission();
        if (perm !== 'granted') {
            return { ok: false, reason: perm };
        }
        const ok = await this._ensureInit();
        if (!ok) return { ok: false, reason: 'init-failed' };
        try {
            const token = await this._fetchToken();
            if (!token) return { ok: false, reason: 'no-token' };
            await this._sendToBackend(token);
            return { ok: true, token };
        } catch (err) {
            console.warn(err);
            return { ok: false, reason: 'error', error: err.message };
        }
    },

    async unregister() {
        const token = localStorage.getItem(this.LS_KEY);
        if (!token) return;
        try {
            await API.delete('/devices/' + encodeURIComponent(token));
        } catch (_) { /* ignore */ }
        try {
            const ok = await this._ensureInit();
            if (ok) await this._messaging.deleteToken();
        } catch (_) { /* ignore */ }
        localStorage.removeItem(this.LS_KEY);
    }
};
