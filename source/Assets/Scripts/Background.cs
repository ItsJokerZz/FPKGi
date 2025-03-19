using Newtonsoft.Json;
using System.Collections;
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
using Application = UnityEngine.Application;

public class Background : MonoBehaviour
{
    [SerializeField]
    private RawImage background, coverImage;

    [Header("Content Data")]
    public RectTransform textTransform;

    public GameObject prefab;

    [SerializeField]
    private JsonData Content;

    public Text freeSpace;

    private const float Spacing = 36.50f;
    private const float Offset = -20.00f;

    public static bool initializedApp = false;
    public static bool updateChecked = false;

    public static void SaveConfiguration()
    {
        if (!isConsole)
        {
            directoryPath = "D:\\Projects\\Unity\\PS4\\FPKGi - PS5\\DATA\\";
            downloadPath = "D:\\Projects\\Unity\\PS4\\FPKGi - PS5\\DATA\\Downloads\\";
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

        var _deleteAfter = etaHEN == true || deleteAfter;

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
                    deleteAfter = _deleteAfter,
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

    public static void HandleConfiguration()
    {
        string configPath = Path.Combine(directoryPath, "config.json");
        if (!File.Exists(configPath))
        {
            Print(true, PrintType.Warning, "CONFIG DOESN'T EXIST! CREATING...");
            SaveConfiguration();
            return;
        }

        #region Resolves issues present in version v0.81 and prior
        string homebrewPath = IO.GetFilePath(ContentType.Homebrew);
        if (File.Exists(homebrewPath))
        {
            string homebrewJson = File.ReadAllText(homebrewPath);
            var content = JsonConvert.DeserializeObject<Games>(homebrewJson);
            var fpkgiEntry = content.DATA.FirstOrDefault(entry => entry.Value.title_id == "FPKGI13337");
            if (fpkgiEntry.Value != null)
            {
                var oldKey = fpkgiEntry.Key;
                var gameContent = fpkgiEntry.Value;
                gameContent.title_id = "PKGI13337";

                content.DATA.Remove(oldKey);
                content.DATA[oldKey] = gameContent;

                string updatedJson = JsonConvert.SerializeObject(content, Formatting.Indented);
                File.WriteAllText(homebrewPath, updatedJson);
            }
        }
        #endregion

        string jsonContent = File.ReadAllText(configPath);
        var config = JsonConvert.DeserializeObject<Config>(jsonContent);

        if (config.preferences == null)
        {
            Print(true, PrintType.Error, "Config preferences are null.");
            return;
        }

        if (config.preferences.content_urls == null)
        {
            Print(true, PrintType.Error, "Content URLs in preferences are null.");
            return;
        }

        foreach (var key in Variables.ContentURLs.Keys.ToList())
        {
            var configValue = config.preferences.content_urls.GetType()
                .GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(config.preferences.content_urls) as string;

            if (!string.IsNullOrEmpty(configValue))
                Variables.ContentURLs[key] = URL.DecryptBase64(configValue);
        }

        if (config.filtering == null)
        {
            Print(true, PrintType.Error, "Filtering in config is null.");
            return;
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
        deleteAfter = etaHEN == true || (config.preferences.downloads?.deleteAfter ?? deleteAfter);
        deleteOnCancel = config.preferences.downloads?.deleteOnCancel ?? deleteOnCancel;

        background_uri = config.preferences.application?.background_uri ?? background_uri;
        backgroundMusic = config.preferences.application?.backgroundMusic ?? backgroundMusic;
        enableUpdates = config.preferences.application?.enableUpdates ?? enableUpdates;

        if (loadedOffline == true)
            populateViaWeb = false;
        else
            populateViaWeb = config.preferences.application?.populateViaWeb ?? populateViaWeb;

        FindObjectOfType<Background>()?.LoadCustomBackground();

        SaveConfiguration();
    }

    public void LoadCustomBackground()
    {
        if (!File.Exists(background_uri))
        {
            if (URL.IsValidImage(background_uri))
                SetImageFromURL(background_uri, ref background);
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

        Text versionText = UI.FindInactiveObjectsByPath("Canvas/Main/Text/Version")?.GetComponent<Text>();
        var state = nightly && !canary ? "nightly" : (canary ? "canary" : "release");
        UI.ChangeText(versionText, $"v{UI.FormatVersion(version)}-{state} [build {buildNumber:000}]");

        if (isConsole)
        {
            etaHEN = UOB.IsPlayStation5();
            GoldHEN = !etaHEN;

            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = etaHEN == true ? 120 : 60;
        }
        else
        {
            directoryPath = "D:\\Projects\\Unity\\PS4\\FPKGi - PS5\\DATA\\";
            downloadPath = "D:\\Projects\\Unity\\PS4\\FPKGi - PS5\\DATA\\Downloads\\";

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

    private IEnumerator UpdateDisplayInfo()
    {
        float? temperature = null;
        string freeSpace = string.Empty;

        Print(true, PrintType.Default, "Displaying content and system information...");

        UI.FindInactiveObjectsByPath("Canvas/Main/Text/ContentSort")?.SetActive(true);
        UI.FindInactiveObjectsByPath("Canvas/Main/Text/PkgCount")?.SetActive(true);
        UI.FindInactiveObjectsByPath("Canvas/Main/Text/Temperature")?.SetActive(true);
        UI.FindInactiveObjectsByPath("Canvas/Main/Text/FreeSpace")?.SetActive(true);

        InitializePkgContent();

        while (true)
        {
            var freeSpaceText = UI.FindInactiveObjectsByPath("Canvas/Main/Text/FreeSpace")?.GetComponent<Text>();
            var temperatureText = UI.FindInactiveObjectsByPath("Canvas/Main/Text/Temperature")?.GetComponent<Text>();

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
        Print(true, PrintType.Default, "Checking for app updates...");

        if (!updateChecked)
        {
            Print(true, PrintType.Default, "Fetching latest MD5 hash to compare...");

            string latestHash =
                await DownloadAsBytes("https://raw.githubusercontent.com/ItsJokerZz/FPKGi/nightly/HASH.md5");

            string path;
            if (isConsole)
                path = "/user/app/PKGI13337/app.pkg";
            else path = "D:\\Projects\\Unity\\PS4\\FPKGi - PS5\\BUILD\\ED1633-PKGI13337_00-0000000000000000-A0100-V0100.pkg";

            string currentHash = IO.ComputeFileMD5(path);
            if (updateAvailable == null)
                updateAvailable = !canary && !IO.CompareMD5Hashes(currentHash, latestHash);

            Print(true, PrintType.Default, $"Latest MD5: {latestHash}");
            Print(true, PrintType.Default, $"Current MD5: {currentHash}");

            if (latestVersion == null)
            {
                Print(true, PrintType.Default, "Checking for the latest version available...");

                string result = await DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/latestVersion/");

                if (result.Contains("No valid version found in any release."))
                    latestVersion = version;
                else
                {
                    double found;
                    if (double.TryParse(result, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out found))
                        latestVersion = (float)found;

                    Print(true, PrintType.Default, $"Latest Version: {UI.FormatVersion(latestVersion)}");
                    Print(true, PrintType.Default, $"Current Version: {UI.FormatVersion(version)}");
                }
            }

            updateChecked = true;
        }
        else updateChecked = true;

      //  if (version < latestVersion && updateAvailable == true)
      //  {
            Print(true, PrintType.Warning, $"MD5 hash and version mismatch, update required!");

            UI.ShowUIState(UI.FindInactiveObjectsByPath("Canvas/Update"), UI.FindInactiveObjectsByPath("Canvas/Main/Images/Controls/Close"));
            Text textComponent = UI.FindInactiveObjectsByPath("Canvas/Update/Text/Versions")?.GetComponent<Text>();
            textComponent.text = $"Latest Version: {UI.FormatVersion(latestVersion)}\nCurrent Version: {UI.FormatVersion(version)}";
     //   }

        return true;
    }

    private IEnumerator Start()
    {
        while (!initializedApp) yield return null;

        if (isConsole)
        {
            UOB.BreakFromSandbox();
            UOB.InitializeNativeDialogs();

            float startTime = Time.time;

            while (!UOB.IsFreeOfSandbox())
                yield return null;

            if (IO.DoesPathExist("/user/data/UnityOrbisBridge.log"))
                File.Delete("/user/data/UnityOrbisBridge.log");

            Print(true, PrintType.Default, $"Successfully broke from sandbox in {Time.time - startTime} seconds!");
        }

        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "Backgrounds"));
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "ContentJSONs"));
        IO.EnsureDirectoryExists(Path.Combine(directoryPath, "Downloads"));

        for (int i = 0; i < 11; i++)
        {
            var task = JSON.ParseJSON((ContentType)i);
            while (!task.IsCompleted) yield return null;
        }

        var downloadTask =
            DownloadAsBytes("https://github.com/ItsJokerZz/FPKGi/");

        float downloadStart = Time.time;

        yield return new WaitUntil(() =>
        downloadTask.IsCompleted ||
        (Time.time - downloadStart) >= 5);

        if (string.IsNullOrEmpty(downloadTask.Result))
        {
            loadedOffline = true;

            if (GoldHEN == true)
                UOB.TextNotify(222, "Please connect to the internet and/or use local connection content!");

            Print(true, PrintType.Warning, "Loaded offline, toggling \"Populate Via Web\" to prevent hanging...");
        }
        else
            loadedOffline = false;

        StartCoroutine(UpdateDisplayInfo());
    }

}