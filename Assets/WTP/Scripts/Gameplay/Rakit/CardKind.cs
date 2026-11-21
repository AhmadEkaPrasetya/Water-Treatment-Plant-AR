namespace WTP.Gameplay.Rakit
{
    /// <summary>Jenis kartu di Bagian 2 (Rakit WTP).</summary>
    public enum CardKind
    {
        Unit,   // unit WTP asli (Intake ... Reservoir)
        Trap,   // kartu jebakan usulan Pak Wacana (Gapura, Patung, Panggung): mahal, tidak mengolah air
        Decoy,  // pengecoh yang terdengar seperti unit air (Air Mancur, Saringan Kain), tapi bukan unit WTP
    }
}
