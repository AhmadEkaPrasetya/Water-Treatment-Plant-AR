namespace WTP.Core
{
    /// <summary>Status game saat ini (lihat flowchart di dokumen Tugas 5).</summary>
    public enum GameState
    {
        Menu,      // di menu utama
        Scanning,  // memindai bidang / belum menaruh WTP
        Playing,   // WTP sudah ditaruh, pemain sedang bermain
        Paused,    // dijeda
        Result,    // layar hasil level (skor & bintang)
    }
}
