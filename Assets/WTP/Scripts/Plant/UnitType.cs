namespace WTP.Plant
{
    /// <summary>Tujuh unit WTP sesuai urutan aliran air. Angkanya = indeks level di Bagian 1.</summary>
    public enum UnitType
    {
        Intake = 0,
        Koagulasi = 1,
        Flokulasi = 2,
        Sedimentasi = 3,
        Filtrasi = 4,
        Desinfeksi = 5,
        Reservoir = 6,
    }
}
