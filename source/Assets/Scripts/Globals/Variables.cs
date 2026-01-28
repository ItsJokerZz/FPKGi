using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Variables
{
    public static float version = 1.100f;
    public static int buildNumber = 0437;

    public static bool? GoldHEN = null;
    public static bool? etaHEN = null;

    public static bool? loadedOffline = null;
    public static bool? updateAvailable = null;
    public static float? latestVersion = null;

    public static string updateVersionUrl =
        "https://gist.githubusercontent.com/ItsJokerZz/05061df82bb31d500e7d0f89bfa0a221/raw/FPKGi_latest.version?cache-bust=1";

    public static string updateReleaseUrl =
        "https://gist.githubusercontent.com/ItsJokerZz/f1d4236d04bbbe0fbf47f1e12b5ab611/raw/FPKGi_latest.release?cache-bust=1";

    public static string updateSizeUrl =
        "https://gist.githubusercontent.com/ItsJokerZz/6e979e91d020bb574c66e74793b4201f/raw/FPKGi_latest.size?cache-bust=1";

    public static string updateHashUrl =
        "https://raw.githubusercontent.com/ItsJokerZz/FPKGi/refs/heads/release/HASH.md5?cache-bust=1";

    public static string updateDownloadUrl => GoldHEN == true
            ? "https://pkg-zone.com/download/ps4/PKGI13337/latest"
            : "https://pkg-zone.com/download/ps5/PKGI13337/latest";

    public static bool isConsole = Application.platform != RuntimePlatform.WindowsEditor;

    public static readonly string iconPath = "/user/appmeta/PKGI13337/icon0.png";

    #region Global Variabales
    public static Color blueish = new Color32(72, 142, 255, 255);

    public static Color redish = new Color32(156, 82, 82, 255);

    public static Color yellowish = new Color32(250, 250, 75, 255);

    public static RawImage background,
        coverImage;

    public static Text[] menuTexts;

    public static string[] MenuTextObjects =
        {
            "Canvas/Menu/Text/FilteringOptions/SortBy/Size",
            "Canvas/Menu/Text/FilteringOptions/SortBy/Region",
            "Canvas/Menu/Text/FilteringOptions/SortBy/Name",
            "Canvas/Menu/Text/FilteringOptions/SortBy/TitleID",
            "Canvas/Menu/Text/FilteringOptions/Content/Selection",
            "Canvas/Menu/Text/FilteringOptions/Regions/USA",
            "Canvas/Menu/Text/FilteringOptions/Regions/Europe",
            "Canvas/Menu/Text/FilteringOptions/Regions/Japan",
            "Canvas/Menu/Text/FilteringOptions/Regions/Asia",
            "Canvas/Menu/Text/UserPreferences/DirectDownload",
            "Canvas/Menu/Text/UserPreferences/InstallOnceDone",
            "Canvas/Menu/Text/UserPreferences/DeleteAfterInstall",
            "Canvas/Menu/Text/UserPreferences/DeleteOnCancel",
            "Canvas/Menu/Text/UserPreferences/PopulateViaWeb",
            "Canvas/Menu/Text/UserPreferences/EnableAppUpdates",
            "Canvas/Menu/Text/UserPreferences/BackgroundMusic",
            "Canvas/Menu/Text/UserPreferences/ChangeBackground",
            "Canvas/Menu/Text/UserPreferences/ChangeSavePath",
            "Canvas/Menu/Text/UserPreferences/ReloadJsonFiles"
        },
        sortByOptions = { "Size", "Region", "Name", "Title ID" },
        contentOptions =
        {
            "PS1",
            "PS2",
            "PSP",
            "PS5",
            "Games",
            "Apps",
            "Updates",
            "DLCs",
            "Demos",
            "Homebrew",
            "Emulators",
            "Themes",
            "ALL"
        };

    public static JsonData Content;

    #endregion

    #region User Configuration
    public static string directoryPath = "/user/data/FPKGi/",
        downloadPath = $"{directoryPath}Downloads/",
        background_uri = null;

    public static bool ascending = true,
        directDownload = true,
        installAfter = true,
        deleteAfter = false,
        deleteOnCancel = false,
        populateViaWeb = false,
        backgroundMusic = true,
        enableUpdates = true;

    public static Dictionary<string, string> ContentURLs = new Dictionary<string, string>
    {
        { "ps1", null },
        { "ps2", null },
        { "psp", null },
        { "ps5", null },
        { "games", null },
        { "apps", null },
        { "updates", null },
        { "dlc", null },
        { "demos", null },
        { "homebrew", null },
        { "emulators", null },
        { "themes", null },
    };

    public static int sortCriteria = 2, contentFilter = (int)ContentType.ALL;

    public static string[] filteredRegions = { "Asia", "Europe", "Japan", "USA" };

    public struct PreviousSettings
    {
        public bool ascending;
        public int sortCriteria;
        public int contentFilter;
        public string[] filteredRegions;
        public string searchFilter;
        public string background_uri;
        public Texture previousBg;
        public bool directDownload;
        public bool populateViaWeb;
        public bool installAfter;
        public bool deleteAfter;
        public bool deleteOnCancel;
        public bool enableUpdates;
        public bool backgroundMusic;
    }

    public static PreviousSettings pS = new PreviousSettings();
    #endregion
}
