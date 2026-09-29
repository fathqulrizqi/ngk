self.addEventListener('push', function (event) {
    const options = {
        body: event.data.text(),          // Isi pesan dari push
        icon: 'icon.png',                 // Gambar ikon untuk notifikasi
        sound: 'default',                 // Suara default untuk notifikasi
        vibrate: [200, 100, 200],         // Pola getar, bisa dihilangkan jika tidak diperlukan
    };

    event.waitUntil(
        self.registration.showNotification('Notifikasi Push', options)
    );
});