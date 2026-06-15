const API = {
    baseUrl: '/api/v1',

    getToken() {
        return localStorage.getItem('token');
    },

    setToken(token) {
        localStorage.setItem('token', token);
    },

    getRefreshToken() {
        return localStorage.getItem('refreshToken');
    },

    setRefreshToken(token) {
        localStorage.setItem('refreshToken', token);
    },

    clearToken() {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('userEmail');
        localStorage.removeItem('userRole');
    },

    // Exchanges the refresh token for a fresh token pair. Concurrent 401s share a single
    // in-flight request so we never fire multiple refreshes (which would rotate each other out).
    async tryRefresh() {
        const rt = this.getRefreshToken();
        if (!rt) return false;
        if (this._refreshing) return this._refreshing;

        this._refreshing = (async () => {
            try {
                const res = await fetch(this.baseUrl + '/auth/refresh', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ refreshToken: rt })
                });
                if (!res.ok) return false;
                const data = await res.json();
                this.setToken(data.token);
                this.setRefreshToken(data.refreshToken);
                return true;
            } catch (e) {
                return false;
            } finally {
                this._refreshing = null;
            }
        })();

        return this._refreshing;
    },

    async request(method, path, body, _retry = false) {
        const headers = { 'Content-Type': 'application/json' };
        const token = this.getToken();
        if (token) headers['Authorization'] = `Bearer ${token}`;

        const options = { method, headers };
        if (body !== undefined) options.body = JSON.stringify(body);

        const res = await fetch(this.baseUrl + path, options);

        // Expired access token (token present): try a one-time refresh and replay the request.
        // During login there is no token, so the 401 falls through and surfaces the server's
        // message instead of silently reloading.
        if (res.status === 401 && this.getToken() && !_retry && path !== '/auth/refresh') {
            if (await this.tryRefresh()) return this.request(method, path, body, true);
            this.clearToken();
            window.location.reload();
            return;
        }
        if (res.status === 401 && this.getToken()) {
            this.clearToken();
            window.location.reload();
            return;
        }

        const text = await res.text();
        const data = text ? JSON.parse(text) : null;

        if (!res.ok) {
            let msg = data?.message;
            if (!msg && data?.errors) {
                if (Array.isArray(data.errors)) msg = data.errors.join(', ');
                else if (typeof data.errors === 'object') {
                    msg = Object.values(data.errors).flat().join(', ');
                }
            }
            if (!msg && data?.title) msg = data.title;
            const err = new Error(msg || `HTTP ${res.status}`);
            err.status = res.status;
            err.data = data;
            throw err;
        }

        return data;
    },

    get(path) { return this.request('GET', path); },
    post(path, body) { return this.request('POST', path, body); },
    put(path, body) { return this.request('PUT', path, body); },
    delete(path) { return this.request('DELETE', path); }
};
