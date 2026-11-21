using System.Collections.Generic;
using WTP.Plant;

namespace WTP.Gameplay.Rakit
{
    /// <summary>
    /// Simulasi "Alirkan Air" di Bagian 2: air sungai lewat slot 1..7 sesuai urutan kartu.
    /// Class biasa (bukan MonoBehaviour), jadi hanya berisi aturan, tanpa tampilan.
    ///
    /// Aturan: sebuah unit hanya bekerja kalau semua unit SEBELUMNYA (menurut proses WTP yang benar)
    /// sudah dilewati air. Kalau belum, unit itu gagal dan pemain diberi tahu akibatnya,
    /// mis. Filtrasi sebelum Sedimentasi -> filter tersumbat lumpur.
    /// </summary>
    public static class FlowSimulation
    {
        public const int SlotCount = 7;

        /// <summary>Hasil air setelah melewati satu slot.</summary>
        public struct Step
        {
            public bool ok;            // kartu di slot ini bekerja dengan benar
            public float turbidity;    // kekeruhan air setelah slot ini (NTU)
            public string message;     // penjelasan untuk pemain
        }

        /// <summary>
        /// Jalankan simulasi. cards = isi slot 1..7 (tidak boleh null).
        /// problem = penjelasan kesalahan pertama (null kalau WTP benar).
        /// </summary>
        public static Step[] Run(IReadOnlyList<CardData> cards, out string problem)
        {
            var steps = new Step[cards.Count];
            var passed = new bool[SlotCount];   // unit mana yang sudah dilewati air (indeks = UnitType)
            float turbidity = WaterQuality.RiverTurbidity;
            problem = null;

            for (int i = 0; i < cards.Count; i++)
            {
                CardData card = cards[i];
                var step = new Step();

                if (card.kind != CardKind.Unit)
                {
                    // Gapura, patung, air mancur...: air lewat tanpa diolah.
                    step.ok = false;
                    step.message = string.IsNullOrEmpty(card.simulationNote)
                        ? $"{card.displayName} tidak mengolah air sama sekali."
                        : card.simulationNote;
                }
                else
                {
                    int missing = FirstMissingBefore(card.unit, passed);
                    if (missing < 0)
                    {
                        // Hanya unit yang benar-benar bekerja yang dihitung "sudah dilewati".
                        // Filter yang tersumbat tidak menyaring, jadi Desinfeksi sesudahnya ikut gagal.
                        passed[(int)card.unit] = true;
                        step.ok = true;
                        turbidity = System.Math.Min(turbidity, PlantController.TurbidityAfter(card.unit));
                        step.message = WorkingMessage(card.unit);
                    }
                    else
                    {
                        step.ok = false;
                        step.message = WrongOrderMessage(card.unit, (UnitType)missing);
                    }
                }

                step.turbidity = turbidity;
                steps[i] = step;
                if (!step.ok && problem == null) problem = step.message;
            }

            // Jaga-jaga: semua slot bekerja tapi ada unit yang tidak dipasang.
            if (problem == null)
                for (int u = 0; u < SlotCount; u++)
                    if (!passed[u])
                    {
                        problem = $"WTP ini tidak punya unit {(UnitType)u}!";
                        break;
                    }
            return steps;
        }

        /// <summary>Unit pertama (urutan WTP) sebelum 'unit' yang belum dilewati air, atau -1.</summary>
        static int FirstMissingBefore(UnitType unit, bool[] passed)
        {
            for (int u = 0; u < (int)unit; u++)
                if (!passed[u]) return u;
            return -1;
        }

        static string WorkingMessage(UnitType unit)
        {
            switch (unit)
            {
                case UnitType.Intake: return "Intake menyaring sampah besar dan memompa air sungai masuk.";
                case UnitType.Koagulasi: return "Tawas dicampur cepat, kotoran halus mulai menempel satu sama lain.";
                case UnitType.Flokulasi: return "Diaduk pelan, kotoran menggumpal jadi flok besar.";
                case UnitType.Sedimentasi: return "Flok yang berat mengendap jadi lumpur. Air mulai jernih.";
                case UnitType.Filtrasi: return "Pasir dan kerikil menyaring sisa partikel. Air jernih!";
                case UnitType.Desinfeksi: return "Klorin membunuh kuman. Air aman diminum.";
                default: return "Reservoir menampung air bersih lalu mengirimnya ke kota.";
            }
        }

        static string WrongOrderMessage(UnitType unit, UnitType missing)
        {
            if (missing == UnitType.Intake)
                return $"Sampah sungai ikut masuk ke {unit} dan menyumbat pompa! Intake harus paling depan.";

            switch (unit)
            {
                case UnitType.Flokulasi:
                    return "Air diaduk, tapi tidak ada flok yang terbentuk karena tawas belum dicampur di Koagulasi.";
                case UnitType.Sedimentasi:
                    return $"Kotoran halus tidak mau mengendap karena belum digumpalkan. {missing} harus dilewati dulu.";
                case UnitType.Filtrasi:
                    return "Filter langsung tersumbat lumpur! Kotoran harus digumpalkan dan diendapkan dulu sebelum disaring.";
                case UnitType.Desinfeksi:
                    return "Klorin habis bereaksi dengan kotoran dan kuman bersembunyi di balik partikel. Air harus jernih dulu sebelum diberi klorin.";
                case UnitType.Reservoir:
                    return $"Air yang belum selesai diolah (belum lewat {missing}) sudah ditampung dan dikirim ke warga!";
                default:
                    return $"{unit} dipasang sebelum {missing}. Urutannya belum tepat.";
            }
        }
    }
}
