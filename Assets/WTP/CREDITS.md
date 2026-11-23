# Kredit & Sumber Aset

Aturan tugas: maksimal 60% aset gratis/pihak ketiga, minimal 40% buatan sendiri.

## Aset pihak ketiga (gratis)

| Aset | Sumber | Lisensi | Dipakai untuk |
| --- | --- | --- | --- |
| Template Mobile AR (MobileARTemplateAssets, XR Interaction Toolkit samples) | Unity Technologies | Unity Companion License | kamera AR, visual bidang terdeteksi, simulasi XR di Editor |
| Font LiberationSans SDF | TextMesh Pro (Unity) | SIL Open Font License | semua teks UI |
| Sprite UI bawaan (UISprite, Knob) | Unity built-in resources | Unity Editor | latar tombol, slider |

## Aset buatan sendiri (tim)

| Aset | Lokasi | Cara dibuat |
| --- | --- | --- |
| 7 model unit WTP, kit pipa, kota | `Assets/WTP/Models` | script Blender `Tools/Blender/build_wtp_units.py` |
| Model & potret Tirta, Pak Wacana, Pak Bakar Lahan | `Assets/WTP/Models/Characters`, `Assets/WTP/UI/Portraits` | script Blender yang sama |
| Shader air keruh → jernih | `Assets/WTP/Shaders/WTPWater.shader` | ditulis tangan (HLSL URP) |
| Material (beton, logam, air, jendela, dll.) | `Assets/WTP/Materials` | menu WTP > Setup Materials |
| Efek partikel (flok, endapan, kuman, abu, kilau) + tekstur titik | `Assets/WTP/Scripts/Plant/PlantEffects.cs`, `Assets/WTP/Materials/ParticleDot.png` | kode + menu WTP > Setup Audio & Effects |
| 14 efek suara + 3 musik latar | `Assets/WTP/Audio` | disintesis dengan `Tools/Audio/generate_audio.py` (numpy) |
| Sprite bintang | `Assets/WTP/UI/Star.png` | dibuat lewat kode |
| Semua layar UI, transisi layar & panel | `Assets/WTP/Scenes`, `Assets/WTP/Scripts/UI` | menu WTP > Build ... |
| Konten: dialog, 21 soal, 3 level Rakit, 6 kasus Detektif, 5 event Shift | `Assets/WTP/Data` | ditulis tim |
| Semua script game | `Assets/WTP/Scripts` | ditulis tim |
