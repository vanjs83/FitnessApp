importScripts('https://www.gstatic.com/firebasejs/10.13.2/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.13.2/firebase-messaging-compat.js');

firebase.initializeApp({
    apiKey: "AIzaSyCDwPhjlnP4huCfOX5gkMVrr1pV824JopI",
    authDomain: "fitnessapp-6655e.firebaseapp.com",
    projectId: "fitnessapp-6655e",
    storageBucket: "fitnessapp-6655e.firebasestorage.app",
    messagingSenderId: "707739351150",
    appId: "1:707739351150:web:2b83dd0fc067944826ce7f"
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage((payload) => {
    const title = (payload.notification && payload.notification.title) || 'FitnessApp';
    const body = (payload.notification && payload.notification.body) || '';
    self.registration.showNotification(title, {
        body,
        icon: '/favicon.ico',
        badge: '/favicon.ico',
        data: payload.data || {}
    });
});

self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    event.waitUntil(clients.openWindow('/'));
});
