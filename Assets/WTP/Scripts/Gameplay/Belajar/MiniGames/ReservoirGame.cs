using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WTP.Audio;

namespace WTP.Gameplay.Belajar
{
    /// <summary>
    /// Mini-game Reservoir: puzzle pipa 4x3. Tap petak untuk memutar pipanya 90 derajat
    /// sampai air dari reservoir (kiri) tersambung ke kota (kanan).
    /// Pipa yang sudah dialiri air berwarna biru, jadi pemain tahu sampai mana sambungannya.
    /// Skor: selesai dalam 20 dtk = 100, turun sampai 50 di 60 dtk; waktu habis = 30.
    /// </summary>
    public class ReservoirGame : MiniGame
    {
        // Arah lubang pipa sebagai bit: Utara=1, Timur=2, Selatan=4, Barat=8.
        const int N = 1, E = 2, S = 4, W = 8;
        const int Cols = 4, Rows = 3;

        [Tooltip("12 petak, urut baris dari atas: (0,0) (1,0) (2,0) (3,0) (0,1) ...")]
        [SerializeField] Button[] tiles = new Button[Cols * Rows];
        [SerializeField] RectTransform entryMarker;  // panah masuk di kiri
        [SerializeField] RectTransform exitMarker;   // tanda kota di kanan
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text timerText;

        [Tooltip("Format: barisMasuk,barisKeluar:baris0/baris1/baris2. " +
                 "Simbol: - | lurus, L(U+T) r(T+S) 7(S+B) J(B+U) belokan, . = petak acak")]
        [SerializeField] string[] layouts =
        {
            "1,1:.r7./-JL-/....",
            "0,2:-7../.L7./..L-",
            "2,0:..r-/.rJ./-J..",
        };
        [SerializeField] float timeLimit = 60f;

        [SerializeField] Color dryColor = new Color(0.55f, 0.6f, 0.65f);
        [SerializeField] Color wetColor = new Color(0.2f, 0.6f, 1f);

        readonly int[] baseMask = new int[Cols * Rows];   // bentuk pipa sebelum diputar
        readonly int[] rotation = new int[Cols * Rows];   // 0..3 (kali 90 derajat searah jarum jam)
        int entryRow, exitRow;
        float elapsed;
        bool running;

        void Awake()
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                int index = i;
                tiles[i].onClick.AddListener(() => OnTile(index));
            }
        }

        protected override void OnBegin()
        {
            LoadLayout(layouts[Random.Range(0, layouts.Length)]);

            // Acak putaran semua petak; ulangi kalau kebetulan sudah tersambung.
            do
            {
                for (int i = 0; i < rotation.Length; i++) rotation[i] = Random.Range(0, 4);
            } while (Flow(new bool[Cols * Rows]));

            elapsed = 0f;
            running = true;
            statusText.text = "Tap pipa untuk memutarnya. Sambungkan reservoir ke kota!";
            PlaceMarker(entryMarker, entryRow);
            PlaceMarker(exitMarker, exitRow);
            Refresh();
        }

        void LoadLayout(string layout)
        {
            string[] parts = layout.Split(':');
            string[] ends = parts[0].Split(',');
            entryRow = int.Parse(ends[0]);
            exitRow = int.Parse(ends[1]);
            string[] rows = parts[1].Split('/');
            for (int y = 0; y < Rows; y++)
            for (int x = 0; x < Cols; x++)
                baseMask[y * Cols + x] = MaskOf(rows[y][x]);
        }

        static int MaskOf(char c)
        {
            switch (c)
            {
                case '-': return E | W;
                case '|': return N | S;
                case 'L': return N | E;
                case 'r': return E | S;
                case '7': return S | W;
                case 'J': return W | N;
                default: return Random.value < 0.5f ? (E | W) : (N | E);   // petak pengecoh
            }
        }

        // Putar bitmask 90 derajat searah jarum jam: U->T, T->S, S->B, B->U.
        static int Rotate(int mask, int times)
        {
            for (int i = 0; i < times; i++) mask = ((mask << 1) | (mask >> 3)) & 15;
            return mask;
        }

        int MaskAt(int x, int y) => Rotate(baseMask[y * Cols + x], rotation[y * Cols + x]);

        void OnTile(int index)
        {
            if (!running) return;
            rotation[index] = (rotation[index] + 1) % 4;
            Refresh();
        }

        /// <summary>
        /// Telusuri aliran air dari pintu masuk (sisi barat petak (0, entryRow)).
        /// wet[i] = true untuk petak yang dialiri. Return true kalau air sampai ke kota.
        /// </summary>
        bool Flow(bool[] wet)
        {
            if ((MaskAt(0, entryRow) & W) == 0) return false;

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(0, entryRow));
            wet[entryRow * Cols] = true;
            bool reached = false;

            int[] dirs = { N, E, S, W };
            Vector2Int[] steps = { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) };

            while (queue.Count > 0)
            {
                Vector2Int c = queue.Dequeue();
                int mask = MaskAt(c.x, c.y);
                if (c.x == Cols - 1 && c.y == exitRow && (mask & E) != 0) reached = true;

                for (int d = 0; d < 4; d++)
                {
                    if ((mask & dirs[d]) == 0) continue;
                    Vector2Int n = c + steps[d];
                    if (n.x < 0 || n.x >= Cols || n.y < 0 || n.y >= Rows) continue;
                    int opposite = dirs[(d + 2) % 4];
                    if ((MaskAt(n.x, n.y) & opposite) == 0) continue;   // pipa tetangga tidak menyambung
                    int ni = n.y * Cols + n.x;
                    if (wet[ni]) continue;
                    wet[ni] = true;
                    queue.Enqueue(n);
                }
            }
            return reached;
        }

        void Refresh()
        {
            var wet = new bool[Cols * Rows];
            bool connected = Flow(wet);

            for (int i = 0; i < tiles.Length; i++)
            {
                Transform pipe = tiles[i].transform.Find("Pipe");
                pipe.localRotation = Quaternion.Euler(0f, 0f, -90f * rotation[i]);
                // Lengan pipa sesuai bentuk dasar; putarannya diwakili rotasi "Pipe".
                SetArm(pipe, "N", baseMask[i] & N, wet[i]);
                SetArm(pipe, "E", baseMask[i] & E, wet[i]);
                SetArm(pipe, "S", baseMask[i] & S, wet[i]);
                SetArm(pipe, "W", baseMask[i] & W, wet[i]);
                pipe.Find("Center").GetComponent<Image>().color = wet[i] ? wetColor : dryColor;
            }

            if (connected) StartCoroutine(Win());
        }

        void SetArm(Transform pipe, string name, int open, bool wet)
        {
            Transform arm = pipe.Find(name);
            arm.gameObject.SetActive(open != 0);
            arm.GetComponent<Image>().color = wet ? wetColor : dryColor;
        }

        void PlaceMarker(RectTransform marker, int row)
        {
            // Sejajarkan penanda dengan baris petak yang bersangkutan.
            var tile = (RectTransform)tiles[row * Cols].transform;
            marker.anchoredPosition = new Vector2(marker.anchoredPosition.x, tile.anchoredPosition.y);
        }

        void Update()
        {
            if (!running) return;
            elapsed += Time.deltaTime;
            timerText.text = $"Waktu: {Mathf.CeilToInt(timeLimit - elapsed)} dtk";
            if (elapsed >= timeLimit)
            {
                running = false;
                statusText.text = "Waktu habis! Air belum sampai ke kota.";
                AudioManager.Play(Sfx.Wrong);
                StartCoroutine(FinishLater(30));
            }
        }

        IEnumerator Win()
        {
            running = false;
            statusText.text = "Tersambung! Air bersih mengalir ke Kota Tirtapura.";
            AudioManager.Play(Sfx.Pour);
            Unit.SetWaterLevel(1f);
            int score = Mathf.RoundToInt(Mathf.Lerp(100f, 50f, Mathf.InverseLerp(20f, timeLimit, elapsed)));
            yield return FinishLater(score);
        }

        IEnumerator FinishLater(int score)
        {
            yield return new WaitForSeconds(2.5f);
            Finish(score);
        }

        public override void Cancel()
        {
            StopAllCoroutines();
            running = false;
            base.Cancel();
        }
    }
}
