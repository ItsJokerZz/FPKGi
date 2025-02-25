using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static JsonData;

public class Variables
{
    public static bool nightly = true;
    public static float version = 0.87f;
    public static int build = 4;

    public static bool? updateAvailable = null;
    public static float? latestVersion = null;

    #region Global Variabales
    public static RawImage
        background, coverImage;

    public static Font selectedFont;

    public static GameObject
        menuCanvas, menuControls;

    public static Text[]
        mainTexts, menuTexts;

    public static JsonData Content;

    public static List<KeyValuePair<int,
        GameContent>> GameContentAsList;

    public static bool isPopulatedViaWeb = false;

    public static string[]
        BackgroundTextObjects =
        {
            "Version",
            "Temperature",
            "ContentSort",
            "Controls/Main/Circle/Text",
            "Controls/Main/Triangle/Text",
            "Controls/Main/Square/Text",
            "Controls/Main/X/Text",
            "Controls/Menu/Circle/Text",
            "Controls/Menu/Triangle/Text",
            "Controls/Menu/X/Text"
        },

        MenuTextObjects =
        {
            "FilteringOptions/SortBy/Size",
            "FilteringOptions/SortBy/Region",
            "FilteringOptions/SortBy/Name",
            "FilteringOptions/SortBy/TitleID",

            "FilteringOptions/Content/Selection",

            "FilteringOptions/Regions/USA",
            "FilteringOptions/Regions/Europe",
            "FilteringOptions/Regions/Japan",
            "FilteringOptions/Regions/Asia",

            "UserPreferences/ReloadConfig",
            "UserPreferences/ChangeSavePath",
            "UserPreferences/DirectDownload",
            "UserPreferences/InstallOnceDone",
            "UserPreferences/DeleteAfterInstall",
            "UserPreferences/PopulateViaWeb",
            "UserPreferences/BackgroundMusic",
            "UserPreferences/ChangeBackground",
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
