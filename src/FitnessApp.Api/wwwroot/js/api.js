const API = {
    baseUrl: '/api/v1',

    getToken() {
        return localStorage.getItem('token');
    },

    setToken(token) {
        localStorage.setItem('token', token);
    },

    clearToken() {
        localStorage.removeItem('token');
        localStorage.removeItem('userEmail');
        localStorage.removeItem('userRole');
    },

    async request(method, path, body) {
        const headers = { 'Content-Type': 'application/json' };
        const token = this.getToken();
        if (token) headers['Authorization'] = `Bearer ${token}`;

        const options = { method, headers };
        if (body !== undefined) options.body = JSON.stringify(body);

        const res = await fetch(this.baseUrl + path, options);

        // Auto-logout only when an existing session expires (token present).
        // During login there is no token, so let the 401 fall through and surface
        // the server's message instead of silently reloading the page.
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
