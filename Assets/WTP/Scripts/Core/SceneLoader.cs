using UnityEngine.SceneManagement;

namespace WTP.Core
{
    /// <summary>
    /// Satu tempat untuk nama-nama scene, supaya tidak ada string nama scene yang tersebar
    /// di banyak script. Scene harus terdaftar di File > Build Profiles > Scene List.
    /// (Efek transisi/fade ditambahkan nanti di sini.)
    /// </summary>
    public static class SceneLoader
    {
        public const string MainMenu = "MainMenu";
        public const string ARPlay = "ARPlay";

        public static void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    }
}
