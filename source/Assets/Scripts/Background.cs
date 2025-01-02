using Newtonsoft.Json;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityOrbisBridge;
using static JsonData;
using static UOBWrapper;
using static Utilities;
using static Variables;

public class Background : MonoBehaviour
{
    [SerializeField]
    private RawImage
        background,
        coverImage;

    [SerializeField]
    private Text[]
      mainTexts = { };

    [Header("Content Data")]
    public RectTransform
        textTransform;

    public GameObject prefab;

    [SerializeField]
    private JsonData Content;

    public Text freeSpace;

    private const float Spacing = 36.50f;
    private const float Offset = -20.00f;

    private AudioSource audioSource;

    #region Unity Methods
    private void OnApplicationQuit()
        => SaveConfiguration();

    private void Start()
    {
        mainTexts = new Text[BackgroundTextObjects.Length];

        for (int i = 0; i < mainTexts.Length; i++)
            mainTexts[i] = UI.FindTextComponent(BackgroundTextObjects[i]);

        Variables.mainTexts = mainTexts;

        var state = nightly ? "nightly" : "release";
        var ver = $"v{version:0.00}-{state} [build {build:000}]";
        UI.ChangeText(mainTexts, 0, ver);

        Variables.background = background; // might not be needed?

        CreatePrefabs();
        InitializeContent();
        SetVariables();
        HandleConfiguration();
        LoadCustomBackground();

        IO.EnsureDirectoryExists(directoryPath);
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "ContentJSONs"));
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "Downloads"));
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "Backgrounds"));
    }

    private void Update()
    {
        for (int i = 0; i < mainTexts.Length; i++) if (mainTexts.Length != i)
                mainTexts[i] = UI.FindTextComponent(BackgroundTextObjects[i]);

        UpdateDiskInfo(freeSpace, UOB.DiskInfo.Free);
        UpdateTemperature(mainTexts[1],
            new Color32(119, 221, 119, 255),
            new Color32(255, 237, 0, 255),
            new Color32(156, 82, 82, 255),
            UOB.Temperature.CPU, 55f, 70f);
    }
    #endregion

    #region Setup Methods
    private void SetVariables()
    {
        Variables.coverImage = coverImage;

        if (UnityEngine.Application.platform == RuntimePlatform.PS4)
            InitializeForPS4();
        else if (UnityEngine.Application.platform == RuntimePlatform.WindowsEditor)
        {
            directoryPath = "D:\\Projects\\Unity\\PS4\\FPKGi\\DATA\\";

            QualitySettings.vSyncCount = 0;
        }
    }

    private void InitializeForPS4()
    {
        QualitySettings.vSyncCount = 1;
        UnityEngine.Application.targetFrameRate = 60;

        // language = Marshal.PtrToStringAnsi(UOB.GetSystemLanguage());

        languageID = UOB.GetSystemLanguageID();

        UOB.BreakFromSandbox();

        // UOB.MountRootDirectories();
    }
    #endregion

    #region Prefab Management
    private void CreatePrefabs()
    {
        float totalHeight = ContentHandler.itemsPerPage * Spacing;
        Vector2 startPosition = new Vector2(0, totalHeight / 2);

        for (int i = 0; i < ContentHandler.itemsPerPage; i++)
        {
            Vector2 position = startPosition - new Vector2(0, i * Spacing - Offset);
            GameObject newPrefab = Instantiate(prefab, textTransform);
            RectTransform rectTransform = newPrefab.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = position;
            newPrefab.name = $"PKG{i + 1}";
        }
    }

    private void InitializeContent()
    {
        Variables.Content = Content;

        Transform pkgsTransform = GameObject.Find("PKGs")?.transform;

        if (pkgsTransform == null) return;

        Transform textTransform = pkgsTransform.Find("Text");

        if (textTransform == null) return;

        Content.PKGs.Clear();

        for (int i = 1; i <= ContentHandler.itemsPerPage; i++)
            AddPkgToContent(textTransform, i);
    }

    private void AddPkgToContent(Transform textTransform, int index)
    {
        Transform pkgTransform = textTransform.Find($"PKG{index}");

        if (pkgTransform != null)
        {
            PKG newPkg = new PKG
            {
                TitleID = pkgTransform.Find("TitleID")?.GetComponent<Text>(),
                Region = pkgTransform.Find("Region")?.GetComponent<Text>(),
                Downloaded = pkgTransform.Find("Downloaded")?.GetComponent<Text>(),
                Title = pkgTransform.Find("Title")?.GetComponent<Text>(),
                Size = pkgTransform.Find("Size")?.GetComponent<Text>()
            };
            Content.PKGs.Add(newPkg);
        }
    }
    #endregion

    #region Configuration Management
    public static void SaveConfiguration()
    {
        if (downloadPath.StartsWith("/data/") && !downloadPath.StartsWith("/user/"))
            downloadPath = Path.Combine("/user", downloadPath.TrimStart('/')).Replace("\\", "/");

        if (!downloadPath.EndsWith("/")) downloadPath += "/";

        string _sortCriteria = null;
        string _contentFilter = null;

        switch (sortCriteria)
        {
            case 0: _sortCriteria = "size"; break;
            case 1: _sortCriteria = "region"; break;
            case 2: _sortCriteria = "name"; break;
            case 3: _sortCriteria = "titleID"; break;
        }

        switch (contentFilter)
        {
            case 0: _contentFilter = "games"; break;
            case 1: _contentFilter = "apps"; break;
            case 2: _contentFilter = "updates"; break;
            case 3: _contentFilter = "dlc"; break;
            case 4: _contentFilter = "demos"; break;
            case 5: _contentFilter = "homebrew"; break;
        }

        var configJSON = new
        {
            FILTERING = new
            {
                CONTENT = _contentFilter,
                SORT = new
                {
                    type = _sortCriteria,
                    ascending
                },
                REGIONS = filteredRegions,
            },

            PREFERENCES = new
            {
                DOWNLOADS = new
                {
                    directDownload,
                    downloadPath,
                    installAfter,
                    deleteAfter,
                },

                APPLICATION = new
                {
                    background_uri,
                    backgroundMusic,
                    populateViaWeb,
                },

                CONTENT_URLS = new
                {
                    games = Variables.ContentURLs["games"],
                    apps = Variables.ContentURLs["apps"],
                    updates = Variables.ContentURLs["updates"],
                    DLC = Variables.ContentURLs["dlc"],
                    demos = Variables.ContentURLs["demos"],
                    homebrew = Variables.ContentURLs["homebrew"]
                }
            }
        };

        Variables.ContentURLs["games"] = configJSON.PREFERENCES.CONTENT_URLS.games ?? Variables.ContentURLs["games"];
        Variables.ContentURLs["apps"] = configJSON.PREFERENCES.CONTENT_URLS.apps ?? Variables.ContentURLs["apps"];
        Variables.ContentURLs["updates"] = configJSON.PREFERENCES.CONTENT_URLS.updates ?? Variables.ContentURLs["updates"];
        Variables.ContentURLs["dlc"] = configJSON.PREFERENCES.CONTENT_URLS.DLC ?? Variables.ContentURLs["dlc"];
        Variables.ContentURLs["demos"] = configJSON.PREFERENCES.CONTENT_URLS.demos ?? Variables.ContentURLs["demos"];
        Variables.ContentURLs["homebrew"] = configJSON.PREFERENCES.CONTENT_URLS.homebrew ?? Variables.ContentURLs["homebrew"];

        foreach (var key in Variables.ContentURLs.Keys.ToList())
        {
            if (string.IsNullOrEmpty(Variables.ContentURLs[key]))
                Variables.ContentURLs[key] = null;
        }

        string jsonString = JsonConvert.SerializeObject(configJSON, Formatting.Indented);
        string configPath = Path.Combine(directoryPath, "config.json");

        File.WriteAllText(configPath, jsonString);
    }

    private void HandleConfiguration()
    {
        string configPath = Path.Combine(directoryPath, "config.json");

        if (!File.Exists(configPath))
        {
            Print("CONFIG DOESN'T EXIST! CREATING...", PrintType.Warning);
            SaveConfiguration();
            JSON.ParseJSON(ContentType.Config);
            return;
        }

        string jsonContent = File.ReadAllText(configPath);
        var config = JsonConvert.DeserializeObject<Config>(jsonContent);

        switch (config.filtering.content.ToLower())
        {
            case "games": contentFilter = 0; break;
            case "apps": contentFilter = 1; break;
            case "updates": contentFilter = 2; break;
            case "dlc": contentFilter = 3; break;
            case "demos": contentFilter = 4; break;
            case "homebrew": contentFilter = 5; break;
        }

        switch (config.filtering.sort.type.ToLower())
        {
            case "size": sortCriteria = 0; break;
            case "region": sortCriteria = 1; break;
            case "name": sortCriteria = 2; break;
            case "titleID": sortCriteria = 3; break;
        }

        ascending = config.filtering.sort.ascending;
        filteredRegions = config.filtering.regions.Distinct().ToArray();

        directDownload = config.preferences.downloads.directDownload;
        downloadPath = config.preferences.downloads.downloadPath;
        installAfter = config.preferences.downloads.installAfter;
        deleteAfter = config.preferences.downloads.deleteAfter;

        background_uri = config.preferences.application.background_uri;
        backgroundMusic = config.preferences.application.backgroundMusic;
        populateViaWeb = config.preferences.application.populateViaWeb;

        Variables.ContentURLs["games"] = config.preferences.content_urls.games ?? Variables.ContentURLs["games"];
        Variables.ContentURLs["apps"] = config.preferences.content_urls.apps ?? Variables.ContentURLs["apps"];
        Variables.ContentURLs["updates"] = config.preferences.content_urls.updates ?? Variables.ContentURLs["updates"];
        Variables.ContentURLs["dlc"] = config.preferences.content_urls.dlc ?? Variables.ContentURLs["dlc"];
        Variables.ContentURLs["demos"] = config.preferences.content_urls.demos ?? Variables.ContentURLs["demos"];
        Variables.ContentURLs["homebrew"] = config.preferences.content_urls.homebrew ?? Variables.ContentURLs["homebrew"];

        JSON.ParseJSON((ContentType)contentFilter);
    }

    public void LoadCustomBackground()
    {
        if (URL.IsValidImage(background_uri))
            SetImageFromURL(background_uri, ref background);
        else IO.LoadImage(background_uri, ref background);
    }
    #endregion
}
