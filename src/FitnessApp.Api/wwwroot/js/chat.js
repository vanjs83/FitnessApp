const Chat = {
    conn: null,
    conversations: [],
    activePartnerId: null,
    activePartnerName: null,
    meId: null,
    started: false,

    init() {
        const composer = document.getElementById('chatComposer');
        if (composer) composer.addEventListener('submit', e => {
            e.preventDefault();
            this.send();
        });

        const input = document.getElementById('chatInput');
        if (input) {
            // Enter sends, Shift+Enter newline. Auto-grow.
            input.addEventListener('keydown', e => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    this.send();
                }
            });
            input.addEventListener('input', () => this.autoGrow(input));
        }

        const back = document.getElementById('chatBackBtn');
        if (back) back.addEventListener('click', () => this.closeThread());

        document.addEventListener('langchange', () => {
            if (!document.getElementById('chatView').classList.contains('hidden')) this.renderConversations();
        });
    },

    // Called once after login: connect real-time channel and prime the unread badge.
    async start() {
        if (this.started) return;
        this.started = true;
        try {
            const me = await API.get('/auth/me');
            this.meId = me.id;
        } catch (err) { /* ignore */ }
        await this.connect();
        this.refreshBadge();
    },

    stop() {
        this.started = false;
        this.activePartnerId = null;
        if (this.conn) {
            this.conn.stop();
            this.conn = null;
        }
    },

    async connect() {
        if (typeof signalR === 'undefined') { console.warn('SignalR client not loaded'); return; }
        if (this.conn) return;
        try {
            this.conn = new signalR.HubConnectionBuilder()
                .withUrl('/hubs/chat', { accessTokenFactory: () => API.getToken() })
                .withAutomaticReconnect()
                .build();

            this.conn.on('ReceiveMessage', msg => this.onMessage(msg));
            this.conn.onreconnected(() => this.refreshBadge());

            await this.conn.start();
        } catch (err) {
            console.warn('Chat hub connection failed', err);
            this.conn = null;
        }
    },

    onMessage(msg) {
        // The DTO carries only senderId; a message belongs to the open thread when its
        // sender is the active partner (incoming) or me (echo from another tab).
        const inActiveThread = this.activePartnerId
            && (msg.senderId === this.activePartnerId || msg.senderId === this.meId);

        if (inActiveThread) {
            this.appendMessage(msg);
            this.scrollToBottom();
            if (msg.senderId === this.activePartnerId) this.markRead(this.activePartnerId);
        } else {
            // Not viewing this thread — bump unread badge and refresh the list if chat is open.
            this.refreshBadge();
            if (!document.getElementById('chatView').classList.contains('hidden')) this.loadConversations();
        }
    },

    async load() {
        await this.start();
        await this.loadConversations();

        const role = localStorage.getItem('userRole');
        // Client typically has a single partner (their trainer) — open it directly.
        if (role === 'Client' && this.conversations.length === 1) {
            const c = this.conversations[0];
            this.openConversation(c.partnerId, c.partnerName, c.partnerImageUrl);
        } else if (this.activePartnerId) {
            // keep current thread open on re-entry
            const c = this.conversations.find(x => x.partnerId === this.activePartnerId);
            if (c) this.openConversation(c.partnerId, c.partnerName, c.partnerImageUrl);
        } else {
            this.closeThread();
        }
    },

    async loadConversations() {
        try {
            this.conversations = await API.get('/chat/conversations') || [];
        } catch (err) {
            this.conversations = [];
            console.error(err);
        }
        this.renderConversations();
        this.updateBadgeFromConversations();
    },

    renderConversations() {
        const list = document.getElementById('chatConversations');
        const empty = document.getElementById('chatNoConversations');
        if (!list) return;

        if (this.conversations.length === 0) {
            list.innerHTML = '';
            empty.classList.remove('hidden');
            return;
        }
        empty.classList.add('hidden');

        list.innerHTML = this.conversations.map(c => {
            const active = c.partnerId === this.activePartnerId ? ' active' : '';
            const unread = c.unreadCount > 0
                ? `<span class="chat-unread">${c.unreadCount}</span>` : '';
            const preview = c.lastMessage
                ? this.escape(c.lastMessage)
                : `<span class="muted">${I18n.t('chat.noMessages')}</span>`;
            const time = c.lastMessageAt ? this.formatTime(c.lastMessageAt) : '';
            return `
                <button class="chat-conv${active}" data-partner-id="${this.escape(c.partnerId)}">
                    ${Profile.avatarHtml(c.partnerImageUrl, 'avatar-mini')}
                    <span class="chat-conv-body">
                        <span class="chat-conv-top">
                            <span class="chat-conv-name">${this.escape(c.partnerName || '')}</span>
                            <span class="chat-conv-time">${time}</span>
                        </span>
                        <span class="chat-conv-preview">${preview}</span>
                    </span>
                    ${unread}
                </button>`;
        }).join('');

        list.querySelectorAll('.chat-conv').forEach(btn => {
            btn.addEventListener('click', () => {
                const id = btn.dataset.partnerId;
                const c = this.conversations.find(x => x.partnerId === id);
                this.openConversation(id, c?.partnerName, c?.partnerImageUrl);
            });
        });
    },

    async openConversation(partnerId, name, imageUrl) {
        this.activePartnerId = partnerId;
        this.activePartnerName = name;

        document.getElementById('chatEmptyState').classList.add('hidden');
        document.getElementById('chatThread').classList.remove('hidden');
        document.querySelector('.chat-layout').classList.add('thread-open');
        document.getElementById('chatPartnerName').textContent = name || '';

        const box = document.getElementById('chatMessages');
        box.innerHTML = `<p class="muted small">${I18n.t('app.loading') || 'Učitavanje...'}</p>`;

        this.renderConversations();

        try {
            const messages = await API.get(`/chat/with/${encodeURIComponent(partnerId)}`) || [];
            box.innerHTML = '';
            if (messages.length === 0) {
                box.innerHTML = `<p class="muted small chat-thread-empty">${I18n.t('chat.noMessages')}</p>`;
            } else {
                messages.forEach(m => this.appendMessage(m));
            }
            this.scrollToBottom();
            await this.markRead(partnerId);
        } catch (err) {
            box.innerHTML = `<p class="error">${this.escape(err.message)}</p>`;
        }

        const input = document.getElementById('chatInput');
        if (input) { input.value = ''; this.autoGrow(input); input.focus(); }
    },

    closeThread() {
        this.activePartnerId = null;
        document.getElementById('chatThread').classList.add('hidden');
        document.getElementById('chatEmptyState').classList.remove('hidden');
        const layout = document.querySelector('.chat-layout');
        if (layout) layout.classList.remove('thread-open');
        this.renderConversations();
    },

    appendMessage(m) {
        const box = document.getElementById('chatMessages');
        const placeholder = box.querySelector('.chat-thread-empty');
        if (placeholder) placeholder.remove();
        if (box.querySelector(`[data-msg-id="${m.id}"]`)) return; // dedupe

        const mine = m.senderId === this.meId;
        const div = document.createElement('div');
        div.className = 'chat-msg' + (mine ? ' mine' : '');
        div.dataset.msgId = m.id;
        div.innerHTML = `
            <span class="chat-bubble">${this.escape(m.body)}</span>
            <span class="chat-msg-time">${this.formatTime(m.sentAt)}</span>`;
        box.appendChild(div);
    },

    async send() {
        const input = document.getElementById('chatInput');
        if (!input || !this.activePartnerId) return;
        const body = input.value.trim();
        if (!body) return;

        const btn = document.getElementById('chatSendBtn');
        btn.disabled = true;
        try {
            const msg = await API.post(`/chat/with/${encodeURIComponent(this.activePartnerId)}`, { body });
            // Echo immediately (hub will also push, but dedupe guards against doubles).
            this.appendMessage(msg);
            this.scrollToBottom();
            input.value = '';
            this.autoGrow(input);
            this.loadConversations();
        } catch (err) {
            alert(err.message);
        } finally {
            btn.disabled = false;
            input.focus();
        }
    },

    async markRead(partnerId) {
        try {
            await API.post(`/chat/with/${encodeURIComponent(partnerId)}/read`);
            const c = this.conversations.find(x => x.partnerId === partnerId);
            if (c) { c.unreadCount = 0; this.renderConversations(); }
            this.updateBadgeFromConversations();
        } catch (err) { /* ignore */ }
    },

    async refreshBadge() {
        try {
            const convs = await API.get('/chat/conversations') || [];
            this.conversations = convs;
            this.updateBadgeFromConversations();
        } catch (err) { /* ignore */ }
    },

    updateBadgeFromConversations() {
        const total = this.conversations.reduce((s, c) => s + (c.unreadCount || 0), 0);
        ['clientChatBadge', 'trainerChatBadge'].forEach(id => {
            const el = document.getElementById(id);
            if (!el) return;
            if (total > 0) {
                el.textContent = total > 99 ? '99+' : total;
                el.classList.remove('hidden');
            } else {
                el.classList.add('hidden');
            }
        });
    },

    autoGrow(el) {
        el.style.height = 'auto';
        el.style.height = Math.min(el.scrollHeight, 120) + 'px';
    },

    scrollToBottom() {
        const box = document.getElementById('chatMessages');
        if (box) box.scrollTop = box.scrollHeight;
    },

    formatTime(iso) {
        const d = new Date(iso);
        if (isNaN(d)) return '';
        const now = new Date();
        const sameDay = d.toDateString() === now.toDateString();
        const time = d.toLocaleTimeString(I18n.lang, { hour: '2-digit', minute: '2-digit' });
        if (sameDay) return time;
        return d.toLocaleDateString(I18n.lang, { day: '2-digit', month: '2-digit' }) + ' ' + time;
    },

    escape(s) {
        const div = document.createElement('div');
        div.textContent = s ?? '';
        return div.innerHTML;
    }
};
