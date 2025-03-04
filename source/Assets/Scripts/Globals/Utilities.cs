using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static JsonData;
using static UOBWrapper;
using static Variables;
using Random = System.Random;

public class Utilities
{
    public class UI
    {
        public static GameObject FindInactiveObjectsByPath(string path)
        {
            Transform[] objs = Resources.FindObjectsOfTypeAll<Transform>();

            for (int i = 0; i < objs.Length; i++)
            {
                if (objs[i].hideFlags == HideFlags.None)
                {
                    Transform current = objs[i];
                    string fullPath = current.name;

                    while (current.parent != null)
                    {
                        current = current.parent;
                        fullPath = current.name + "/" + fullPath;
                    }

                    if (fullPath == path)
                        return objs[i].gameObject;
                }
            }

            return null;
        }

        public static Text FindTextComponent(string path)
        {
            GameObject obj = GameObject.Find(path);
            return obj?.GetComponent<Text>();
        }

        public static void ChangeText(Text[] texts, int num, string text)
        {
            if (texts != null && num >= 0 && num < texts.Length
                && texts[num] != null) texts[num].text = text;
        }

        public static void ChangeText(Text textObject, string text)
        {
            if (textObject != null && text != null) textObject.text = text;
        }

        public static bool UpdateUIFromChunk(IEnumerable<KeyValuePair<string, GameContent>> chunk)
        {
            try
            {
                foreach (var entry in chunk)
                    parsedData[entry.Key] = entry.Value;
            }
            catch (Exception ex)
            {
                Print($"Failed to parse a chunk of JSON content: {ex.Message}", PrintType.Error);
                return false;
            }

            return true;
        }

        public static void UpdateScrollbar(Scrollbar scrollbar)
        {
            if (ContentHandler.filteredCount <= 0)
            {
                scrollbar.value = 0;
                scrollbar.size = 1;

                return;
            }

            ContentHandler.contentScroll = Mathf.Clamp(ContentHandler.contentScroll, 0, ContentHandler.filteredCount - 1);

            int totalVisibleItems = ContentHandler.itemsPerPage;
            int totalPages = Mathf.CeilToInt((float)ContentHandler.filteredCount / totalVisibleItems);

            ContentHandler.currentPage = Mathf.Clamp(ContentHandler.currentPage, 0, totalPages - 1);

            scrollbar.value = (float)ContentHandler.contentScroll / (ContentHandler.filteredCount - 1);

            float minSize = 0.1f;
            float maxSize = 0.7f;
            float sizeFactor = (float)totalVisibleItems / ContentHandler.filteredCount;

            scrollbar.size = Mathf.Clamp(sizeFactor, minSize, maxSize);

            int clampPkgCount = Mathf.Clamp(ContentHandler.filteredCount + 1 - ContentHandler.removedCount, 0, ContentHandler.filteredCount + 1 - ContentHandler.removedCount);

            if (clampPkgCount <= 0)
                scrollbar.gameObject.SetActive(false);
        }

        public static string FormatVersion(float? version)
        {
            string formattedVersion;

            string versionStr = version?.ToString("0.000", CultureInfo.InvariantCulture);

            if (versionStr.EndsWith("0"))
                formattedVersion = version?.ToString("0.00", CultureInfo.InvariantCulture);
            else
            {
                string[] parts = versionStr.Split('.');
                if (parts.Length > 1 && parts[1].Length > 2)
                    formattedVersion = $"{parts[0]}.{parts[1].Substring(0, 2)}.{parts[1].Substring(2)}";
                else
                    formattedVersion = version?.ToString("0.00", CultureInfo.InvariantCulture);
            }

            if (CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator == ",")
                formattedVersion = formattedVersion.Replace('.', ',');

            return formattedVersion;
        }

    }

    public class JSON
    {
        public static string FindKeyByValue(Dictionary<string, GameContent> dict, string titleId) =>
            dict.FirstOrDefault(pair => pair.Value.title_id == titleId).Key;

        private static int ParseGameData(string jsonContent = "", bool preserveExisting = false)
        {
            try
            {
                var content = JsonConvert.DeserializeObject<Games>(jsonContent);

                if (!preserveExisting)
                    parsedData.Clear();

                const int chunkSize = 100;
                var dataEntries = content.DATA.ToList();

                for (int i = 0; i < dataEntries.Count; i += chunkSize)
                {
                    var chunk = dataEntries.Skip(i).Take(chunkSize);
                    foreach (var entry in chunk)
                    {
                        parsedData[entry.Key] = entry.Value;
                    }
                }

                return 1;
            }
            catch (Exception ex)
            {
                Print($"Failed to parse game data: {ex.Message}", PrintType.Error);
                return 0;
            }
        }

        public static async Task<int> ParseJSON(ContentType contentType = ContentType.Config)
        {
            if (contentType == ContentType.ALL)
            {
                parsedData.Clear();
                var allContentTypes = Enum.GetValues(typeof(ContentType))
                    .Cast<ContentType>()
                    .Where(type => type != ContentType.Config && type != ContentType.ALL);

                var processedTitleIds = new HashSet<string>();

                foreach (var type in allContentTypes)
                {
                    string filePath = IO.GetFilePath(type);
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            string jsonContent = File.ReadAllText(filePath);
                            var content = JsonConvert.DeserializeObject<Games>(jsonContent);

                            if (content?.DATA != null)
                            {
                                foreach (var entry in content.DATA)
                                {
                                    if (!processedTitleIds.Contains(entry.Value.title_id))
                                    {
                                        parsedData[entry.Key] = entry.Value;
                                        processedTitleIds.Add(entry.Value.title_id);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Print($"Error loading {type}: {ex.Message}", PrintType.Error);
                        }
                    }
                    else
                        await ParseJSON(type);
                }
                return parsedData.Count > 0 ? 1 : 0;
            }

            string url = null;
            string singleFilePath = IO.GetFilePath(contentType);
            bool preserveExisting = contentType !=
                ContentType.Config && contentFilter == (int)ContentType.ALL;
            string webContent = string.Empty;

            if (populateViaWeb)
            {
                try
                {
                    switch (contentType)
                    {
                        case ContentType.PS1:
                            url = Variables.ContentURLs["ps1"];
                            break;
                        case ContentType.PS2:
                            url = Variables.ContentURLs["ps2"];
                            break;
                        case ContentType.PSP:
                            url = Variables.ContentURLs["psp"];
                            break;
                        case ContentType.Games:
                            url = Variables.ContentURLs["games"];
                            break;
                        case ContentType.Apps:
                            url = Variables.ContentURLs["apps"];
                            break;
                        case ContentType.Updates:
                            url = Variables.ContentURLs["updates"];
                            break;
                        case ContentType.Demos:
                            url = Variables.ContentURLs["demos"];
                            break;
                        case ContentType.DLC:
                            url = Variables.ContentURLs["dlc"];
                            break;
                        case ContentType.Homebrew:
                            url = Variables.ContentURLs["homebrew"];
                            break;
                        case ContentType.Emulators:
                            url = Variables.ContentURLs["emulators"];
                            break;
                        case ContentType.Themes:
                            url = Variables.ContentURLs["themes"];
                            break;
                    }

                    if (URL.IsValidURI(url))
                    {
                        Print(true, PrintType.Warning, "Attempting to download JSON from: " + url);
                        webContent = await DownloadAsBytes(url);

                        if (string.IsNullOrEmpty(webContent))
                            Print($"Web-based loading failed: no data available for parsing.", PrintType.Error);
                    }
                }
                catch (Exception ex)
                {
                    Print($"Web-based loading failed: {ex.Message}", PrintType.Error);
                    Print("Falling back to local file loading.", PrintType.Warning);
                }
            }
            if (!File.Exists(singleFilePath) && contentType != ContentType.ALL)
            {
                try
                {
                    var pkgUrl = await DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/download/?echo=1");

                    if (contentType == ContentType.Homebrew)
                    {
                        var homebrewJson = new
                        {
                            DATA = new Dictionary<string, object>
                            {
                                {
                                    pkgUrl, new
                                    {
                                        title_id = "PKGI13337",
                                        region = "ALL",
                                        name = "F[PKGi]",
                                        version = UI.FormatVersion(version),
                                        release = "12-25-2024",
                                        size = "75000000",
                                        min_fw = "4.50",
                                        cover_url = "https://www.itsjokerzz.site/projects/FPKGi/Icon.png"
                                    }
                                }
                            }
                        };

                        string jsonString = JsonConvert.SerializeObject(homebrewJson, Formatting.Indented);
                        File.WriteAllText(singleFilePath, jsonString);
                    }
                    else if (contentType != ContentType.Config)
                    {
                        var random = new Random();
                        var id = $"{random.Next(0, 10)}{random.Next(0, 10)}{random.Next(0, 10)}{random.Next(0, 10)}{random.Next(0, 10)}";

                        var regions = new[] { "USA", "EUR", "ASIA", "JAP", "UNK" };
                        var region = regions[random.Next(regions.Length)];

                        var version = $"{random.Next(0, 10)}.{random.Next(0, 10)}{random.Next(0, 10)}";

                        var startDate = new DateTime(2000, 1, 1);
                        var endDate = new DateTime(2050, 12, 31);
                        var range = (endDate - startDate).Days;
                        var randomDate = startDate.AddDays(random.Next(range)).ToString("MM-dd-yyyy");

                        var size = 500000000L + (long)(random.NextDouble() * (50000000000L - 500000000L));
                        var firmware = $"{random.Next(1, 14)}.{random.Next(0, 100):D2}";

                        string defaultName = string.Empty;
                        switch (contentType)
                        {
                            case ContentType.PS1:
                                defaultName = "PlayStation Classic";
                                break;
                            case ContentType.PS2:
                                defaultName = "PlayStation 2 Classic";
                                break;
                            case ContentType.PSP:
                                defaultName = "PSP Remaster";
                                break;
                            case ContentType.Games:
                                defaultName = "Game";
                                break;
                            case ContentType.Apps:
                                defaultName = "Application";
                                break;
                            case ContentType.Updates:
                                defaultName = "Update";
                                break;
                            case ContentType.DLC:
                                defaultName = "DLC";
                                break;
                            case ContentType.Demos:
                                defaultName = "Demo";
                                break;
                            case ContentType.Emulators:
                                defaultName = "Emulator";
                                break;
                            case ContentType.Themes:
                                defaultName = "Theme";
                                break;
                        }

                        var fileName = defaultName;

                        switch (fileName)
                        {
                            case "PSP Remaster": fileName = "psp-remaster"; break;
                            case "PlayStation Classic": fileName = "ps1-classic"; break;
                            case "PlayStation 2 Classic": fileName = "ps2-classic"; break;
                        }

                        var downloadLink = $"https://www.web.site/{fileName.ToLower()}.pkg";

                        var defaultJson = new
                        {
                            DATA = new Dictionary<string, object>
                            {
                                {
                                    downloadLink, new
                                    {
                                        title_id = $"CUSA{id}",
                                        region,
                                        name = defaultName,
                                        version,
                                        release = randomDate,
                                        size,
                                        min_fw = firmware,
                                        cover_url = (string)null
                                    }
                                }
                            }
                        };

                        string jsonString = JsonConvert.SerializeObject(defaultJson, Formatting.Indented);
                        File.WriteAllText(singleFilePath, jsonString);
                    }

                    await ParseJSON((ContentType)contentFilter);
                    return 1;
                }
                catch (Exception ex)
                {
                    Print($"Failed to generate default JSON: {ex.Message}", PrintType.Error);
                    return 0;
                }
            }

            try
            {
                string jsonContent = File.ReadAllText(singleFilePath);
                bool isValidWebContent = populateViaWeb && URL.IsValidURI(url);
                if (contentType != ContentType.Config)
                    if (string.IsNullOrEmpty(webContent))
                        return ParseGameData(jsonContent, preserveExisting);

                return isValidWebContent
            ? ParseGameData(webContent, preserveExisting)
            : ParseGameData(jsonContent, preserveExisting);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Object reference not set to an instance of an object"))
                    return 0;
            }


            return 1;
        }

    }

    public class IO
    {
        public static string FormatByteString(string byteString)
        {
            long bytes;

            if (long.TryParse(byteString, out bytes))
            {
                double gigabytes = bytes / (1024.0 * 1024.0 * 1024.0);
                if (gigabytes >= 1)
                    return $"{gigabytes:0.##} GB";

                double megabytes = bytes / (1024.0 * 1024.0);
                if (megabytes >= 1)
                    return $"{megabytes:0.##} MB";

                double kilobytes = bytes / 1024.0;
                if (kilobytes >= 1)
                    return $"{kilobytes:0.##} KB";

                return $"{bytes} B";
            }
            return "0 B";
        }

        public static string FormatByteString(float bytes)
        {
            double byteValue = bytes;
            if (byteValue >= 1e9)
                return $"{byteValue / 1e9:0.##} GB";
            if (byteValue >= 1e6)
                return $"{byteValue / 1e6:0.##} MB";
            if (byteValue >= 1e3)
                return $"{byteValue / 1e3:0.##} KB";

            return $"{byteValue} B";
        }

        public static string GetFilePath(ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.Config:
                    return Path.Combine(directoryPath, "config.json");
                case ContentType.PS1:
                    return Path.Combine(directoryPath, "ContentJSONs", "PS1.json");
                case ContentType.PS2:
                    return Path.Combine(directoryPath, "ContentJSONs", "PS2.json");
                case ContentType.PSP:
                    return Path.Combine(directoryPath, "ContentJSONs", "PSP.json");
                case ContentType.Games:
                    return Path.Combine(directoryPath, "ContentJSONs", "GAMES.json");
                case ContentType.Apps:
                    return Path.Combine(directoryPath, "ContentJSONs", "APPS.json");
                case ContentType.Updates:
                    return Path.Combine(directoryPath, "ContentJSONs", "UPDATES.json");
                case ContentType.DLC:
                    return Path.Combine(directoryPath, "ContentJSONs", "DLC.json");
                case ContentType.Demos:
                    return Path.Combine(directoryPath, "ContentJSONs", "DEMOS.json");
                case ContentType.Homebrew:
                    return Path.Combine(directoryPath, "ContentJSONs", "HOMEBREW.json");
                case ContentType.Emulators:
                    return Path.Combine(directoryPath, "ContentJSONs", "EMULATORS.json");
                case ContentType.Themes:
                    return Path.Combine(directoryPath, "ContentJSONs", "THEMES.json");

                default: return string.Empty;
            }
        }

        public static bool IsValidImageExtension(string extension) =>
            extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);

        public static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            path = Path.GetFullPath(path);
            if (Directory.Exists(path)) return;

            Stack<string> toCreate = new Stack<string>();

            while (!Directory.Exists(path))
            {
                toCreate.Push(path);
                path = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(path)) break;
            }

            while (toCreate.Count > 0)
                Directory.CreateDirectory(toCreate.Pop());
        }

        public static void LoadImage(string path, ref RawImage image)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return;

                string directoryPath = Path.GetDirectoryName(path);
                string localFileName = Path.GetFileName(path);

                if (directoryPath != null && Directory.Exists(directoryPath))
                {
                    string matchedFile = Directory
                        .GetFiles(directoryPath)
                        .FirstOrDefault(
                            f => string.Equals(Path.GetFileName(f), localFileName, StringComparison.OrdinalIgnoreCase)
                        );

                    if (matchedFile == null || background == null)
                        return;

                    if (matchedFile != null && background != null)
                    {
                        string fileExtension = Path.GetExtension(matchedFile).ToLower();

                        if (IO.IsValidImageExtension(fileExtension))
                        {
                            Texture2D texture = new Texture2D(2, 2);
                            byte[] imageData = File.ReadAllBytes(matchedFile);

                            if (texture.LoadImage(imageData))
                            {
                                image.texture = texture;
                                image.gameObject.SetActive(true);
                            }

                            background_uri = matchedFile;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Print($"Failed to load image: {ex.Message}", PrintType.Error);
            }
        }

        public static bool IsValidPackageFile(string filePath)  // shoutout LM
        {
            byte[] ExpectedMagic = { 0x7F, (byte)'C', (byte)'N', (byte)'T' };

            if (!File.Exists(filePath))
                return false;

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] header = new byte[ExpectedMagic.Length];
                    if (stream.Read(header, 0, header.Length) != header.Length)
                        return false;

                    if (!ExpectedMagic.SequenceEqual(header))
                        return false;
                }
            }
            catch (IOException)
            {
                return false;
            }

            return true;
        }

    }

    public class URL
    {
        public static bool IsValidURI(string url)
        {
            if (File.Exists(url)) return true;
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrEmpty(url)) return false;

            url = Uri.EscapeUriString(url.Trim());

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
            !Regex.IsMatch(url, @"^(http(s)?):\/\/[^\s\/$.?#].[^\s]*$", RegexOptions.IgnoreCase))
            {
                if (!File.Exists(url))
                {
                    Print($"Invalid URL format: {url}", PrintType.Error);
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidImageType(string url)
            => IO.IsValidImageExtension(Path.GetExtension(url));

        public static bool IsValidImage(string url) => !string.IsNullOrEmpty(url) && IsValidURI(url) && IsValidImageType(url);

    }

    public class Menu
    {
        public static bool IsValidMenuItemIndex(int index)
            => index >= 0 && index < menuTexts.Length;

        public static void HighlightMenuItem(int index)
        {
            ResetMenuItemsToDefault();

            if (IsValidMenuItemIndex(index))
                SetMenuItemColor(index, blueish);
        }

        public static void ResetMenuItemsToDefault()
        {
            for (int i = 0; i < menuTexts.Length; i++)
                SetMenuItemColor(i, Color.white);
        }

        public static void SetMenuItemColor(int index, Color color)
        {
            if (IsValidMenuItemIndex(index))
                menuTexts[index].color = color;
        }

    }

    public class Store
    {
        public enum UpdateReturns
        {
            FoundUpdate = 0,
            ErrorOccured = 1,
            NoUpdate = 2,
            NotInstalled = 3
        }

        [DllImport("store_api")]
        public static extern UpdateReturns sceStoreApiCheckUpdate(string TitleId);

        [DllImport("store_api")]
        private static extern bool sceStoreApiLaunchStore(string TitleId);

        public static bool CheckForUpdates(string TitleId = "PKGI13337")
            => sceStoreApiCheckUpdate(TitleId) == UpdateReturns.FoundUpdate;

        public static void UpdateApplication(string titleId = "PKGI13337")
            => sceStoreApiLaunchStore(titleId);
    }
}
