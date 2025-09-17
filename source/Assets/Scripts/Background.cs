using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityOrbisBridge;
using static JsonData;
using static UOBWrapper;
using static Utilities;
using static Variables;
using Application =
    UnityEngine.Application;

public class Background : MonoBehaviour
{
    [SerializeField]
    private RawImage background, coverImage;

    [Header("Content Data")]
    public RectTransform textTransform;

    public GameObject prefab;

    [SerializeField]
    private JsonData Content;

    private const float Spacing = 32f;
    private const float Offset = -12f;

    public static bool initializedApp = false;
    public static bool updateChecked = false;
    public static bool fullyInitialized = false;

    [SerializeField]
    public Transform controlContainer;

    [SerializeField]
    public GameObject touch, cross,
        square, triangle, circle;

    public static void SaveConfiguration()
    {
        if (!isConsole)
        {
            directoryPath = "D:\\Projects\\Unity\\PS4\\FPKGi\\DATA\\";
            downloadPath = "D:\\Projects\\Unity\\PS4\\FPKGi\\DATA\\Downloads\\";
        }

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
                    deleteOnCancel
                },
                APPLICATION = new
                {
                    background_uri,
                    backgroundMusic,
                    populateViaWeb,
                    enableUpdates
                },
                CONTENT_URLS = new
                {
                    PS1 = Variables.ContentURLs["ps1"],
                    PS2 = Variables.ContentURLs["ps2"],
                    PSP = Variables.ContentURLs["psp"],
                    PS5 = Variables.ContentURLs["ps5"],
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
        Variables.ContentURLs["ps5"] = configJSON.PREFERENCES.CONTENT_URLS.PS5 ?? Variables.ContentURLs["ps5"];
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

    public static void HandleConfiguration()
    {
        string configPath = Path.Combine(directoryPath, "config.json");

        if (!File.Exists(configPath))
        {
            Print(LogType.Log, "Creating JSON file: config.json");

            SaveConfiguration();
            return;
        }

        #region Resolves issues present in version v0.81 and prior.
        string homebrewPath = IO.GetFilePath(ContentType.Homebrew);
        if (File.Exists(homebrewPath))
        {
            string homebrewJson = File.ReadAllText(homebrewPath);
            var content = JsonConvert.DeserializeObject<Games>(homebrewJson);
            if (content != null && content.DATA != null)
            {
                var fpkgiEntry = content.DATA.FirstOrDefault(entry => entry.Value.title_id == "FPKGI13337");
                if (fpkgiEntry.Value != null)
                {
                    string oldKey = fpkgiEntry.Key;
                    var gameContent = fpkgiEntry.Value;
                    gameContent.title_id = "PKGI13337";

                    content.DATA.Remove(oldKey);
                    content.DATA[oldKey] = gameContent;

                    string updatedJson = JsonConvert.SerializeObject(content, Formatting.Indented);
                    File.WriteAllText(homebrewPath, updatedJson);
                }
            }
        }
        #endregion

        string jsonContent = File.ReadAllText(configPath);
        var config = JsonConvert.DeserializeObject<Config>(jsonContent);
        if (config == null)
            config = new Config();

        if (config.filtering == null)
            config.filtering = new ContentFilter();
        if (config.filtering.sort == null)
            config.filtering.sort = new Sort();
        if (config.filtering.regions == null)
            config.filtering.regions = new List<string>();

        string contentFilterStr = "all";
        if (!string.IsNullOrEmpty(config.filtering.content))
            contentFilterStr = config.filtering.content.ToLower();

        if (contentFilterStr == "ps1") contentFilter = (int)ContentType.PS1;
        else if (contentFilterStr == "ps2") contentFilter = (int)ContentType.PS2;
        else if (contentFilterStr == "psp") contentFilter = (int)ContentType.PSP;
        else if (contentFilterStr == "ps5") contentFilter = (int)ContentType.PS5;
        else if (contentFilterStr == "games") contentFilter = (int)ContentType.Games;
        else if (contentFilterStr == "apps") contentFilter = (int)ContentType.Apps;
        else if (contentFilterStr == "updates") contentFilter = (int)ContentType.Updates;
        else if (contentFilterStr == "dlc") contentFilter = (int)ContentType.DLC;
        else if (contentFilterStr == "demos") contentFilter = (int)ContentType.Demos;
        else if (contentFilterStr == "homebrew") contentFilter = (int)ContentType.Homebrew;
        else if (contentFilterStr == "emulators") contentFilter = (int)ContentType.Emulators;
        else if (contentFilterStr == "themes") contentFilter = (int)ContentType.Themes;
        else contentFilter = (int)ContentType.ALL;

        if (config.preferences == null)
            config.preferences = new Preferences();
        if (config.preferences.content_urls == null)
            config.preferences.content_urls = new ContentURLs();

        foreach (var key in Variables.ContentURLs.Keys.ToList())
        {
            var prop = config.preferences.content_urls.GetType()
                .GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            string value = prop != null ? prop.GetValue(config.preferences.content_urls) as string : null;

            if (!string.IsNullOrEmpty(value))
            {
                string formatted = URL.ProperFormatUrl(URL.DecryptBase64(value));
                Variables.ContentURLs[key] = URL.IsValidURI(formatted) ? formatted : null;
            }
            else if (Variables.ContentURLs[key] == null)
                Variables.ContentURLs[key] = null;
        }

        string sortType = "size";
        if (config.filtering.sort != null && !string.IsNullOrEmpty(config.filtering.sort.type))
            sortType = config.filtering.sort.type.ToLower();

        if (sortType == "size") sortCriteria = 0;
        else if (sortType == "region") sortCriteria = 1;
        else if (sortType == "name") sortCriteria = 2;
        else if (sortType == "titleid") sortCriteria = 3;
        else sortCriteria = 0;

        ascending = (config.filtering.sort != null) ? config.filtering.sort.ascending : true;
        filteredRegions = (config.filtering.regions != null) ? config.filtering.regions.Distinct().ToArray() : new string[0];

        directDownload = (config.preferences.downloads != null) ? config.preferences.downloads.directDownload : true;
        downloadPath = (config.preferences.downloads != null && !string.IsNullOrEmpty(config.preferences.downloads.downloadPath)) ?
                       config.preferences.downloads.downloadPath : "/data/FPKGi/Downloads";
        installAfter = (config.preferences.downloads != null) ? config.preferences.downloads.installAfter : true;
        deleteAfter = (config.preferences.downloads != null) ? config.preferences.downloads.deleteAfter : true;
        deleteOnCancel = (config.preferences.downloads != null) ? config.preferences.downloads.deleteOnCancel : false;

        background_uri = (config.preferences.application != null) ? config.preferences.application.background_uri : null;
        backgroundMusic = (config.preferences.application != null) ? config.preferences.application.backgroundMusic : true;
        enableUpdates = (config.preferences.application != null) ? config.preferences.application.enableUpdates : true;
        populateViaWeb = loadedOffline == false && (config.preferences.application != null && config.preferences.application.populateViaWeb);

        FindObjectOfType<Background>()?.LoadCustomBackground();

        SaveConfiguration();
    }

    public void LoadCustomBackground()
    {
        if (!File.Exists(background_uri))
        {
            if (URL.IsValidImage(background_uri))
            {
                var bg = URL.ProperFormatUrl(background_uri);
                if (URL.IsValidURI(bg))
                    SetImageFromURL(bg, ref background);
            }
        }
        else IO.LoadImage(background_uri, ref background);
    }

    private void OnApplicationQuit() => SaveConfiguration();

    public void InitializePkgContent()
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

    private void Awake()
    {
        Variables.background = background;
        Variables.coverImage = coverImage;

        Text versionText = UI.FindInactiveObjectsByPath("Canvas/Main/Version")?.GetComponent<Text>();

        UI.ChangeText(versionText, $"v{UI.FormatVersion(version)}");

        UI.ShowUIState(null);

        if (isConsole)
        {
            etaHEN = UOB.IsPlayStation5();
            GoldHEN = !etaHEN;

            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = etaHEN == true ? 120 : 60;
        }
        else
        {
            directoryPath = Path.GetFullPath(Application.dataPath + @"\..\DATA\");
            downloadPath = Path.GetFullPath(Application.dataPath + @"\..\DATA\Downloads\"); 

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 240;
        }

        if (directoryPath.Contains("/data/") && !directoryPath.StartsWith("/user/"))
            directoryPath = Path.Combine("/user", directoryPath.TrimStart('/')).Replace("\\", "/");

        if (downloadPath.Contains("/data/") && !downloadPath.StartsWith("/user/"))
            downloadPath = Path.Combine("/user", downloadPath.TrimStart('/')).Replace("\\", "/");

        if (!downloadPath.EndsWith("/")) downloadPath += "/";
        if (!directoryPath.EndsWith("/")) directoryPath += "/";

        directoryPath = Path.GetFullPath(directoryPath)
            .Replace("\\", Path.DirectorySeparatorChar.ToString())
            .Replace("/", Path.DirectorySeparatorChar.ToString());

        downloadPath = Path.GetFullPath(downloadPath)
            .Replace("\\", Path.DirectorySeparatorChar.ToString())
            .Replace("/", Path.DirectorySeparatorChar.ToString());

        initializedApp = true;
    }

    public IEnumerator UpdateDisplayInfo()
    {
        float? temperature = null;
        string freeSpace = string.Empty;

        Print(LogType.Assert, "Displaying content and system information...");

        UI.FindInactiveObjectsByPath("Canvas/Main/ContentSort")?.SetActive(true);
        UI.FindInactiveObjectsByPath("Canvas/Main/PkgCount")?.SetActive(true);
        UI.FindInactiveObjectsByPath("Canvas/Main/Temperature")?.SetActive(true);
        UI.FindInactiveObjectsByPath("Canvas/Main/FreeSpace")?.SetActive(true);

        InitializePkgContent();

        while (true)
        {
            var freeSpaceText = UI.FindInactiveObjectsByPath("Canvas/Main/FreeSpace")?.GetComponent<Text>();
            var temperatureText = UI.FindInactiveObjectsByPath("Canvas/Main/Temperature")?.GetComponent<Text>();

            if (isConsole)
            {
                temperature = UOB.GetTemperature(UOB.Temperature.CPU);

                if (GoldHEN == true)
                    freeSpace = UOB.GetDiskInfo(UOB.DiskInfo.Free);
            }

            bool freeSpaceChanged = freeSpaceText != null && freeSpaceText.text != freeSpace;
            bool temperatureChanged = temperatureText != null && temperatureText.text != temperature.ToString();

            if ((freeSpaceText != null && freeSpaceChanged) || (temperatureText != null && temperatureChanged))
            {
                if (isConsole)
                {
                    if (GoldHEN == true)
                        UpdateDiskInfo(freeSpaceText, UOB.DiskInfo.Free);
                    else freeSpaceText.text = "Not Yet Supported";

                    UpdateTemperature(temperatureText,
                            new Color32(119, 221, 119, 255),
                            new Color32(255, 237, 0, 255),
                            new Color32(156, 82, 82, 255),
                            UOB.Temperature.CPU, 55f, 70f);
                }
            }

            if (!isConsole)
            {
                if (freeSpaceText != null)
                    freeSpaceText.text = "Not Available";
                if (temperatureText != null)
                    temperatureText.text = "Not Available";
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public static async Task<bool> CheckForAppUpdates()
    {
        string latestHash = string.Empty, currentHash = string.Empty, fileVersion = string.Empty;

        if (loadedOffline == false)
        {
            if (!updateChecked)
            {
                Print(LogType.Assert, "Fetching the latest MD5 hash to compare...");

                latestHash = await DownloadAsBytes(updateHashUrl);

                var parts = UI.FormatVersion(version).Split('.');
                fileVersion = $"V{int.Parse(parts[0]):D2}{int.Parse(parts[1]):D2}";

                Print(fileVersion);

                string path;
                if (isConsole)
                    path = "/user/app/PKGI13337/app.pkg";
                else path = $"D:\\Projects\\Unity\\PS4\\FPKGi\\BUILD\\ED1633-PKGI13337_00-0000000000000000-A0100-{fileVersion}.pkg";

                currentHash = IO.ComputeFileMD5(path);
                if (updateAvailable == null)
                    updateAvailable = !IO.CompareMD5Hashes(currentHash, latestHash);

                if (latestVersion == null)
                {
                    Print(LogType.Assert, "Checking for the latest version available...");

                    string result = await DownloadAsBytes(updateVersionUrl);

                    if (result.Contains("No valid version found in any release."))
                        latestVersion = version;
                    else
                    {
                        double found;
                        if (double.TryParse(result, System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out found))
                            latestVersion = (float)found;

                        Print(LogType.Assert, $"Current MD5: {currentHash}");
                        Print(LogType.Assert, $"Latest MD5: {latestHash}");
                        Print(LogType.Assert, $"Latest Version: {UI.FormatVersion(latestVersion)}");
                        Print(LogType.Assert, $"Current Version: {UI.FormatVersion(version)}");
                        Print(LogType.Assert, $"Build Number: {buildNumber}");
                    }
                }

                updateChecked = true;
            }
            else updateChecked = true;

            if (version < latestVersion && updateAvailable == true)
            {
                Print(LogType.Warning, $"MD5 hash and version mismatch, update required!");

                UI.ShowUIState(UI.FindInactiveObjectsByPath("Canvas/Update"));
                Text currentVersionText = UI.FindInactiveObjectsByPath("Canvas/Update/Text/Versions/Current")?.GetComponent<Text>();
                Text latestVersionText = UI.FindInactiveObjectsByPath("Canvas/Update/Text/Versions/Latest")?.GetComponent<Text>();
                Text latestMD5Text = UI.FindInactiveObjectsByPath("Canvas/Update/Text/Versions/Latest/MD5")?.GetComponent<Text>();
                Text currentMD5Text = UI.FindInactiveObjectsByPath("Canvas/Update/Text/Versions/Current/MD5")?.GetComponent<Text>();
                Text latestSizeText = UI.FindInactiveObjectsByPath("Canvas/Update/Text/Versions/Latest/Size")?.GetComponent<Text>();
                currentVersionText.text = $"Current Version: {UI.FormatVersion(version)}";
                latestVersionText.text = $"Latest Version: {UI.FormatVersion(latestVersion)}";
                currentMD5Text.text = $"MD5: {currentHash}";
                latestMD5Text.text = $"MD5: {latestHash}";

                string latestSize = await DownloadAsBytes(updateSizeUrl);
                latestSizeText.text = $"Size: {IO.FormatByteString(latestSize)}";
            }
        }

        return true;
    }

    private IEnumerator Start()
    {
        while (!initializedApp) yield return null;

        if (isConsole)
        {
            UOB.BreakFromSandbox();

            float startTime = Time.time;
            string henFolder = GoldHEN == true ? "/data/GoldHEN/" : "/data/etaHEN/";

            if (IO.DoesPathExist("/data/UnityOrbisBridge.log"))
                File.Delete("/data/UnityOrbisBridge.log");

            string consoleType = etaHEN == true ? "PS5" : "PS4";
            string fwVersion = Marshal.PtrToStringAnsi(UOB.GetFWVersion());
            Print(LogType.Assert, $"Running on {consoleType} ({fwVersion})");

            while (!IO.DoesPathExist(henFolder))
                yield return null;

            Print(LogType.Assert, $"Successfully broke from sandbox in {Time.time - startTime} seconds!");

            UOB.InitializeNativeDialogs();
        }

        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "Backgrounds"));
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "ContentJSONs"));
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "Downloads"));

        for (int i = 0; i < 11; i++)
        {
            var task = JSON.ParseJSON((ContentType)i);
            while (!task.IsCompleted) yield return null;
        }

        var downloadTask = DownloadAsBytes("https://github.com/ItsJokerZz/FPKGi/");

        float downloadStart = Time.time;

        yield return new WaitUntil(() => downloadTask.IsCompleted || (Time.time - downloadStart) >= 5);

        if (string.IsNullOrEmpty(downloadTask.Result))
        {
            loadedOffline = true;

            if (GoldHEN == true)
                UOB.TextNotify(222, "Please connect to the internet and/or use local connection content!");

            Print(LogType.Warning, "Loaded offline, toggling \"Populate Via Web\" to prevent hanging...");
        }
        else
            loadedOffline = false;

        fullyInitialized = true;

    }

}