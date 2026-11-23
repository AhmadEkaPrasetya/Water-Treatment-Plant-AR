namespace WTP.Audio
{
    /// <summary>
    /// Daftar efek suara. Nama file di Assets/WTP/Audio/SFX = nama di sini (huruf kecil),
    /// dibuat oleh Tools/Audio/generate_audio.py.
    /// </summary>
    public enum Sfx
    {
        Click,      // tombol ditekan (otomatis untuk semua Button)
        Tick,       // huruf dialog muncul
        Correct,    // jawaban / langkah benar
        Wrong,      // jawaban / langkah salah
        Star,       // satu bintang muncul di layar hasil
        Badge,      // badge baru
        Win,        // level / kasus / shift berhasil
        Fail,       // gagal
        Place,      // WTP ditaruh di meja
        Splash,     // "blup": sampah diambil, lumpur dibuang
        Pour,       // menuang tawas / air mengalir
        Alarm,      // event Operator Shift
        Warning,    // peringatan kecil (kepuasan warga rendah)
        Swish,      // panel muncul
    }
}
