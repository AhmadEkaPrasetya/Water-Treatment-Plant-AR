using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace WTP.AR
{
    /// <summary>
    /// Gestur dua jari pada WTP yang sudah ditaruh:
    /// - cubit (jarak jari berubah) -> perbesar / perkecil
    /// - putar (sudut garis antar jari berubah) -> putar WTP di sumbu Y
    /// Dipasang di root prefab WTP_Plant.
    /// </summary>
    public class PlantGestures : MonoBehaviour
    {
        [Tooltip("Batas skala relatif terhadap ukuran awal prefab")]
        [SerializeField] float minScale = 0.3f;
        [SerializeField] float maxScale = 3f;

        Vector3 baseScale;
        bool gestureActive;
        float prevDistance;
        float prevAngle;

        void Awake() => baseScale = transform.localScale;
        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            var touches = Touch.activeTouches;
            if (touches.Count < 2)
            {
                gestureActive = false;
                return;
            }

            Vector2 a = touches[0].screenPosition;
            Vector2 b = touches[1].screenPosition;
            float distance = Vector2.Distance(a, b);
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

            // Frame pertama dua jari: simpan posisi awal saja.
            if (!gestureActive)
            {
                gestureActive = true;
                prevDistance = distance;
                prevAngle = angle;
                return;
            }

            // Cubit: skala dikali perbandingan jarak sekarang / jarak sebelumnya.
            if (prevDistance > 1f)
            {
                float relative = transform.localScale.x / baseScale.x * (distance / prevDistance);
                transform.localScale = baseScale * Mathf.Clamp(relative, minScale, maxScale);
            }

            // Putar: jari berputar berlawanan jarum jam (sudut naik) -> WTP ikut berlawanan jarum jam
            // dilihat dari atas, yaitu rotasi Y negatif di Unity.
            transform.Rotate(0f, -Mathf.DeltaAngle(prevAngle, angle), 0f, Space.World);

            prevDistance = distance;
            prevAngle = angle;
        }
    }
}
