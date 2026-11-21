using System;
using WTP.Plant;

namespace WTP.Gameplay.Detektif
{
    /// <summary>Data inspeksi satu unit WTP di sebuah kasus.</summary>
    [Serializable]
    public class UnitReport
    {
        public UnitType unit;
        public Measurement[] measurements;

        public bool IsAbnormal
        {
            get
            {
                foreach (Measurement m in measurements)
                    if (m.abnormal) return true;
                return false;
            }
        }
    }
}
