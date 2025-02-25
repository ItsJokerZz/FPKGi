using Newtonsoft.Json;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityOrbisBridge;
using static JsonData;
using static UOBWrapper;
using static Utilities;
using static Variables;
using System.Reflection;

public class Background : MonoBehaviour
{
    [SerializeField]
    private RawImage
        background, coverImage;

    [SerializeField]
    private Text[]
        mainTexts = { };

    public Text freeSpace;

    [Header("Content Data")]
    public RectTransform textTransform;

    public GameObject prefab;

    [SerializeField] private JsonData Content;

    private const float Spacing = 37.50f;
    private const float Offset = -20.00f;

    private void OnApplicationQuit()
        => SaveConfiguration();

    private void InitializeContent()
    {
        Variables.Content = Content;
        Transform pkgsTransform = GameObject.Find("PKGs")?.transform;

        if (pkgsTransform != null)
        {
            Transform textTransform = pkgsTransform.Find("Text");
            if (textTransform != null)
            {
                Content.PKGs.Clear();
                Vector2 startPosition = new Vector2(0, ContentHandler.itemsPerPage * Spacing / 2);

                for (int i = 0; i < ContentHandler.itemsPerPage; i++)
                {
                    Vector2 position = startPosition - new Vector2(0, i * Spacing - Offset);
                    GameObject newPrefab = Instantiate(prefab, textTransform);
                    newPrefab.GetComponent<RectTransform>().anchoredPosition = position;
                    newPrefab.name = $"PKG{i + 1}";

                    Transform pkgTransform = textTransform.Find($"PKG{i + 1}");
                    if (pkgTransform != null)
                    {
                        Content.PKGs.Add(new PKG
                        {
                            TitleID = pkgTransform.Find("TitleID")?.GetComponent<Text>(),
                            Region = pkgTransform.Find("Region")?.GetComponent<Text>(),
                            Downloaded = pkgTransform.Find("Downloaded")?.GetComponent<Text>(),
                            Title = pkgTransform.Find("Title")?.GetComponent<Text>(),
                            Size = pkgTransform.Find("Size")?.GetComponent<Text>()
                        });
                    }
                }
            }
        }
    }

    public static void SaveConfiguration()
    {
        if (downloadPath.StartsWith("/data/") && !downloadPath.StartsWith("/user/"))
            downloadPath = Path.Combine("/user", downloadPath.TrimStart('/')).Replace("\\", "/");

        if (!downloadPath.EndsWith("/")) downloadPath += "/";

        string _sortCriteria = null;
        switch (sortCriteria)
        {
            case 0:
                _sortCriteria = "size";
                break;
            case 1:
                _sortCriteria = "region";
                break;
            case 2:
                _sortCriteria = "name";
                break;
            case 3:
                _sortCriteria = "titleID";
                break;
        }

        string _contentFilter = null;
        switch (contentFilter)
        {
            case (int)ContentType.PS1:
                _contentFilter = "ps1";
                break;
            case (int)ContentType.PS2:
                _contentFilter = "ps2";
                break;
            case (int)ContentType.PSP:
                _contentFilter = "psp";
                break;
            case (int)ContentType.Games:
                _contentFilter = "games";
                break;
            case (int)ContentType.Apps:
                _contentFilter = "apps";
                break;
            case (int)ContentType.Updates:
                _contentFilter = "updates";
                break;
            case (int)ContentType.DLC:
                _contentFilter = "dlc";
                break;
            case (int)ContentType.Demos:
                _contentFilter = "demos";
                break;
            case (int)ContentType.Homebrew:
                _contentFilter = "homebrew";
                break;
            case (int)ContentType.Emulators:
                _contentFilter = "emulators";
                break;
            case (int)ContentType.Themes:
                _contentFilter = "themes";
                break;
            case (int)ContentType.ALL:
                _contentFilter = "all";
                break;
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
                REGIONS = filteredRegions?.Distinct().ToArray() ?? new string[0],
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
                    PS1 = Variables.ContentURLs["ps1"],
                    PS2 = Variables.ContentURLs["ps2"],
                    PSP = Variables.ContentURLs["psp"],
                    games = Variables.ContentURLs["games"],
                    apps = Variables.ContentURLs["apps"],
                    updates = Variables.ContentURLs["updates"],
                    DLC = Variables.ContentURLs["dlc"],
                    demos = Variables.ContentURLs["demos"],
                    homebrew = Variables.ContentURLs["homebrew"],
                    emulators = Variables.ContentURLs["emulators"],
                    themes = Variables.ContentURLs["themes"]
                }
            }
        };

        Variables.ContentURLs["ps1"] = configJSON.PREFERENCES.CONTENT_URLS.PS1 ?? Variables.ContentURLs["ps1"];
        Variables.ContentURLs["ps2"] = configJSON.PREFERENCES.CONTENT_URLS.PS2 ?? Variables.ContentURLs["ps2"];
        Variables.ContentURLs["psp"] = configJSON.PREFERENCES.CONTENT_URLS.PSP ?? Variables.ContentURLs["psp"];
        Variables.ContentURLs["games"] = configJSON.PREFERENCES.CONTENT_URLS.games ?? Variables.ContentURLs["games"];
        Variables.ContentURLs["apps"] = configJSON.PREFERENCES.CONTENT_URLS.apps ?? Variables.ContentURLs["apps"];
        Variables.ContentURLs["updates"] = configJSON.PREFERENCES.CONTENT_URLS.updates ?? Variables.ContentURLs["updates"];
        Variables.ContentURLs["dlc"] = configJSON.PREFERENCES.CONTENT_URLS.DLC ?? Variables.ContentURLs["dlc"];
        Variables.ContentURLs["demos"] = configJSON.PREFERENCES.CONTENT_URLS.demos ?? Variables.ContentURLs["demos"];
        Variables.ContentURLs["homebrew"] = configJSON.PREFERENCES.CONTENT_URLS.homebrew ?? Variables.ContentURLs["homebrew"];
        Variables.ContentURLs["emulators"] = configJSON.PREFERENCES.CONTENT_URLS.emulators ?? Variables.ContentURLs["emulators"];
        Variables.ContentURLs["themes"] = configJSON.PREFERENCES.CONTENT_URLS.themes ?? Variables.ContentURLs["themes"];

        foreach (var key in Variables.ContentURLs.Keys.ToList())
        {
            if (string.IsNullOrEmpty(Variables.ContentURLs[key]))
                Variables.ContentURLs[key] = null;
        }

        string jsonString = JsonConvert.SerializeObject(configJSON, Formatting.Indented);
        string configPath = Path.Combine(directoryPath, "config.json");

        File.WriteAllText(configPath, jsonString);
    }

    public async void HandleConfiguration()
    {
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "ContentJSONs"));
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "Downloads"));
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "Backgrounds"));

        string configPath = Path.Combine(directoryPath, "config.json");

        if (!File.Exists(configPath))
        {
            Print("CONFIG DOESN'T EXIST! CREATING...", PrintType.Warning);
            await JSON.ParseJSON(ContentType.Config);
            SaveConfiguration();
            return;
        }

        string jsonContent = File.ReadAllText(configPath);
        var config = JsonConvert.DeserializeObject<Config>(jsonContent);

        if (config.preferences.content_urls != null)
        {
            foreach (var key in Variables.ContentURLs.Keys.ToList())
            {
                var configValue = config.preferences.content_urls.GetType()
                    .GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?
                    .GetValue(config.preferences.content_urls) as string;
                
                if (!string.IsNullOrEmpty(configValue))
                {
                    Variables.ContentURLs[key] = configValue;
                }
            }
        }

        string contentFilterStr = config.filtering.content?.ToLower();
        if (!string.IsNullOrEmpty(contentFilterStr))
        {
            switch (contentFilterStr)
            {
                case "ps1":
                    contentFilter = (int)ContentType.PS1;
                    break;
                case "ps2":
                    contentFilter = (int)ContentType.PS2;
                    break;
                case "psp":
                    contentFilter = (int)ContentType.PSP;
                    break;
                case "games":
                    contentFilter = (int)ContentType.Games;
                    break;
                case "apps":
                    contentFilter = (int)ContentType.Apps;
                    break;
                case "updates":
                    contentFilter = (int)ContentType.Updates;
                    break;
                case "dlc":
                    contentFilter = (int)ContentType.DLC;
                    break;
                case "demos":
                    contentFilter = (int)ContentType.Demos;
                    break;
                case "homebrew":
                    contentFilter = (int)ContentType.Homebrew;
                    break;
                case "emulators":
                    contentFilter = (int)ContentType.Emulators;
                    break;
                case "themes":
                    contentFilter = (int)ContentType.Themes;
                    break;
                case "all":
                    contentFilter = (int)ContentType.ALL;
                    break;
            }
        }

        string sortType = config.filtering.sort?.type?.ToLower();
        if (!string.IsNullOrEmpty(sortType))
        {
            switch (sortType)
            {
                case "size":
                    sortCriteria = 0;
                    break;
                case "region":
                    sortCriteria = 1;
                    break;
                case "name":
                    sortCriteria = 2;
                    break;
                case "titleid":
                    sortCriteria = 3;
                    break;
            }
        }

        ascending = config.filtering.sort?.ascending ?? ascending;
        filteredRegions = config.filtering.regions?.Distinct().ToArray() ?? filteredRegions;
        
        directDownload = config.preferences.downloads?.directDownload ?? directDownload;
        downloadPath = config.preferences.downloads?.downloadPath ?? downloadPath;
        installAfter = config.preferences.downloads?.installAfter ?? installAfter;
        deleteAfter = config.preferences.downloads?.deleteAfter ?? deleteAfter;

        background_uri = config.preferences.application?.background_uri ?? background_uri;
        backgroundMusic = config.preferences.application?.backgroundMusic ?? backgroundMusic;
        populateViaWeb = config.preferences.application?.populateViaWeb ?? populateViaWeb;

        SaveConfiguration();

        await JSON.ParseJSON((ContentType)contentFilter);
        ContentHandler.UpdateContent(0);

        if (URL.IsValidImage(background_uri))
            SetImageFromURL(background_uri, ref background);
        else
            IO.LoadImage(background_uri, ref background);
    }

    public static async void CheckForUpdates()
    {
        if (updateAvailable == null && latestVersion == null)
        {
            float found;

            updateAvailable = UnityEngine.Application.platform != RuntimePlatform.PS4 || Store.CheckForUpdates();
            string result = await DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/latestVersion/");

            if (float.TryParse(result, out found))
                latestVersion = found;
        }

        if (version < latestVersion || updateAvailable == true)
        {
            UI.FindInactiveObjectsByPath("Canvas/Details")?.SetActive(false);
            GameObject.Find("Canvas/Main/Images/Controls/Details")?.SetActive(false);
            GameObject.Find("Canvas/Main/Images/Controls/Main")?.SetActive(false);
            GameObject.Find("Canvas/Main/Images/Controls/Close")?.SetActive(true);
            UI.FindInactiveObjectsByPath("Canvas/Update")?.SetActive(true);

            var textComponent = UI.FindInactiveObjectsByPath("Canvas/Update/Text/Versions")?.GetComponent<Text>();
            if (textComponent != null)
                textComponent.text = $"Latest Version: {latestVersion}\nCurrent Version: {version}";
        }
    }

    private IEnumerator UpdateDisplayInfo()
    {
        while (UnityEngine.Application.platform == RuntimePlatform.PS4)
        {
            UOB.BreakFromSandbox();

            for (int i = 0; i < mainTexts.Length; i++)
                if (mainTexts.Length != i)
                    mainTexts[i] = UI.FindTextComponent(BackgroundTextObjects[i]);

            UpdateDiskInfo(freeSpace, UOB.DiskInfo.Free);
            UpdateTemperature(mainTexts[1],
                new Color32(119, 221, 119, 255),
                new Color32(255, 237, 0, 255),
                new Color32(156, 82, 82, 255),
                UOB.Temperature.CPU, 55f, 70f);

            yield return new WaitForSeconds(1f);
        }
    }

    private void InitializeApplication()
    {
        var state = nightly ? "nightly" : "release";

        mainTexts = new Text[BackgroundTextObjects.Length];
        for (int i = 0; i < mainTexts.Length; i++)
            mainTexts[i] = UI.FindTextComponent(BackgroundTextObjects[i]);

        Variables.mainTexts = mainTexts;
        Variables.background = background;
        Variables.coverImage = coverImage;

        if (UnityEngine.Application.platform == RuntimePlatform.PS4)
        {
            QualitySettings.vSyncCount = 1;
            UnityEngine.Application.targetFrameRate = 60;
            // language = Marshal.PtrToStringAnsi(UOB.GetSystemLanguage());
            languageID = UOB.GetSystemLanguageID();
            UOB.InitializeNativeDialogs();
        }
        else if (UnityEngine.Application.platform == RuntimePlatform.WindowsEditor)
        {
            QualitySettings.vSyncCount = 0;
            directoryPath = "D:\\Projects\\Unity\\PS4\\FPKGi\\DATA\\";
        }

        UI.ChangeText(mainTexts, 0, $"v{version:0.00}-{state} [build {build:000}]");

        InitializeContent();
        HandleConfiguration();

        CheckForUpdates();

        StartCoroutine(UpdateDisplayInfo());
    }

    private void Start()
       => InitializeApplication();

}
