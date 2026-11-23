using WTP.UI;

namespace WTP.Core
{
    /// <summary>
    /// Satu tempat untuk nama-nama scene, supaya tidak ada string nama scene yang tersebar
    /// di banyak script. Scene harus terdaftar di File > Build Profiles > Scene List.
    /// Pindah scene memakai efek layar menggelap lalu terang lagi (ScreenFader).
    /// </summary>
    public static class SceneLoader
    {
        public const string MainMenu = "MainMenu";
        public const string ARPlay = "ARPlay";

        public static void Load(string sceneName) => ScreenFader.LoadScene(sceneName);
    }
}
