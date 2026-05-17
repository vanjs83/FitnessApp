const API = {
    baseUrl: '/api',

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

        if (res.status === 401) {
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
            throw new Error(msg || `HTTP ${res.status}`);
        }

        return data;
    },

    get(path) { return this.request('GET', path); },
    post(path, body) { return this.request('POST', path, body); },
    put(path, body) { return this.request('PUT', path, body); },
    delete(path) { return this.request('DELETE', path); }
};
