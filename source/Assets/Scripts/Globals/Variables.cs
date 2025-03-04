using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Variables
{
    public static bool? updateAvailable = null;
    public static float? latestVersion = null;

    public static float version = 0.873f;
    public static bool nightly = true;
    public static bool canary = false;
    public static int buildNumber = 44;

    #region Global Variabales
    public static RawImage
        background, coverImage;

    public static Font selectedFont;

    public static GameObject
        menuCanvas, menuControls;

    public static Text[]
        mainTexts, menuTexts;

    public static JsonData Content;

    public static string[]
        BackgroundTextObjects =
        {
            "Canvas/Main/Text/Version",
            "Canvas/Main/Text/Temperature",
            "Canvas/Main/Text/ContentSort",
            "Canvas/Main/Images/Controls/Main/Circle/Text",
            "Canvas/Main/Images/Controls/Main/Triangle/Text",
            "Canvas/Main/Images/Controls/Main/Square/Text",
            "Canvas/Main/Images/Controls/Main/X/Text",
            "Canvas/Main/Images/Controls/Menu/Circle/Text",
            "Canvas/Main/Images/Controls/Menu/Triangle/Text",
            "Canvas/Main/Images/Controls/Menu/X/Text"
        },

        MenuTextObjects =
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

            "Canvas/Menu/Text/UserPreferences/ReloadConfig",
            "Canvas/Menu/Text/UserPreferences/ChangeSavePath",
            "Canvas/Menu/Text/UserPreferences/DirectDownload",
            "Canvas/Menu/Text/UserPreferences/InstallOnceDone",
            "Canvas/Menu/Text/UserPreferences/DeleteAfterInstall",
            "Canvas/Menu/Text/UserPreferences/PopulateViaWeb",
            "Canvas/Menu/Text/UserPreferences/BackgroundMusic",
            "Canvas/Menu/Text/UserPreferences/ChangeBackground",
        },

        sortByOptions = { "Size", "Region",
                       "Name", "Title ID" },

        contentOptions = {  "PS1", "PS2", "PSP", "Games", "Apps", "Updates",
                "DLCs", "Demos", "Homebrew", "Emulators", "Themes", "ALL" };

    public static string
        SearchText = "Search Content (By name or title ID)",
        setDownloadPath = "Set Download Location...";
    #endregion

    #region Configuration
    public static Color blueish
        = new Color32(72, 142, 255, 255);

    public static string
        language = "en-US", // not really needed (atm, atleast)
        directoryPath = "/data/FPKGi/",
        downloadPath = $"{directoryPath}Downloads/",
        background_uri = null;

    public static bool
        ascending = true,
        directDownload = true,
        backgroundMusic = true,
        enableUpdates = true,

        installAfter = true,
        deleteAfter = false,
        populateViaWeb = false;

    public static Dictionary<string, string>
        ContentURLs = new Dictionary<string, string>
        {
            { "ps1", null },
            { "ps2", null },
            { "psp", null },
            { "games", null },
            { "apps", null },
            { "updates", null },
            { "dlc", null },
            { "demos", null },
            { "homebrew", null },
            { "emulators", null },
            { "themes", null },
    };

    public static int
        languageID = 1, sortCriteria = 2,
        contentFilter = (int)ContentType.ALL;

    public static string[]
        filteredRegions = { "Asia",
        "Europe", "Japan", "USA" };

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
        public bool backgroundMusic;
    }

    public static PreviousSettings
        pS = new PreviousSettings();
    #endregion
}
