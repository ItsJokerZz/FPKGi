using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static JsonData;
using static UOBWrapper;
using static Variables;
using Random = System.Random;

public class Utilities : MonoBehaviour
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

        public static void AdjustSpacingForCentering(Transform container, HorizontalLayoutGroup layoutGroup)
        {
            int childCount = 0;
            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i).gameObject;
                if (child != null && (child.name.ToLower().Contains("touch")
                    || child.name.ToLower().Contains("cross")
                     || child.name.ToLower().Contains("circle")
                     || child.name.ToLower().Contains("square")
                     || child.name.ToLower().Contains("triangle")))
                    childCount++;
            }

            // Calculate the negative spacing based on the number of items
            float spacingValue = 0f;

            if (childCount == 1)
            {
                spacingValue = -10; // Small negative spacing for 1 item
            }
            else if (childCount == 2)
            {
                spacingValue = -925; // Slightly larger negative spacing for 2 items
            }
            else if (childCount == 3)
            {
                spacingValue = -795; // Larger negative spacing for 3 items
            }
            else
            {
                spacingValue = -450; // Even larger negative spacing for more than 3 items
            }

            // Apply the negative spacing value directly
            layoutGroup.spacing = spacingValue;

            // Force layout rebuild to ensure the updated spacing is applied
            LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
        }

        public static void ResizePrefab(Transform container, GameObject prefab, string textStr)
        {
            if (prefab == null || container == null)
                return;

            GameObject instance = GameObject.Instantiate(prefab, container);
            if (instance == null)
                return;

            Transform imageTransform = instance.transform.Find("Image");
            Transform textTransform = instance.transform.Find("Text");

            if (imageTransform == null || textTransform == null)
                return;

            RectTransform imageRect = imageTransform.GetComponent<RectTransform>();
            RectTransform textRect = textTransform.GetComponent<RectTransform>();

            if (imageRect == null || textRect == null)
                return;

            Text textComponent = textTransform.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = textStr;
                textComponent.cachedTextGenerator.Invalidate();
                textComponent.cachedTextGeneratorForLayout.Invalidate();

                float preferredWidth = textComponent.preferredWidth;
                textRect.sizeDelta = new Vector2(preferredWidth, 48);
            }

            Vector2 imagePosition = imageRect.anchoredPosition;
            float imageWidth = imageRect.rect.width;

            textRect.anchoredPosition = new Vector2(imagePosition.x + imageWidth, imagePosition.y);

            imageRect.anchorMin = new Vector2(0, 0.5f); // Anchor to the left of the parent container
            imageRect.anchorMax = new Vector2(0, 0.5f); // Anchor to the left of the parent container
            imageRect.pivot = new Vector2(0, 0.5f); // Set pivot to the left center

            textRect.anchorMin = new Vector2(0, 0.5f); // Anchor to the left of the parent container
            textRect.anchorMax = new Vector2(0, 0.5f); // Anchor to the left of the parent container
            textRect.pivot = new Vector2(0, 0.5f); // Set pivot to the left center

            RectTransform prefabRect = instance.GetComponent<RectTransform>();
            if (prefabRect != null)
            {
                float combinedWidth = imageWidth + textRect.rect.width;
                prefabRect.sizeDelta = new Vector2(combinedWidth, prefabRect.sizeDelta.y); // Set width to match image + text
            }

            HorizontalLayoutGroup layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
            if (layoutGroup != null)
                AdjustSpacingForCentering(container, layoutGroup);
        }

        public static void RemoveAllChildren(Transform container)
        {
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
                GameObject.DestroyImmediate(container.GetChild(i).gameObject);
        }

        public static void ShowUIState(GameObject canvas)
        {
            FindInactiveObjectsByPath("Canvas/Menu")?.SetActive(false);
            FindInactiveObjectsByPath("Canvas/Details")?.SetActive(false);
            FindInactiveObjectsByPath("Canvas/Download")?.SetActive(false);
            FindInactiveObjectsByPath("Canvas/Cancel")?.SetActive(false);
            FindInactiveObjectsByPath("Canvas/Update")?.SetActive(false);
            FindInactiveObjectsByPath("Canvas/Close")?.SetActive(false);

            Background bg = FindInactiveObjectsByPath("Scripts")?.GetComponent<Background>();
            RemoveAllChildren(FindInactiveObjectsByPath("Canvas/Main/Controls")?.GetComponent<Transform>());

            if (canvas == null)
            {
                ResizePrefab(bg.controlContainer, bg.touch, "Search");
                ResizePrefab(bg.controlContainer, bg.cross, "Download");
                ResizePrefab(bg.controlContainer, bg.square, "Details");
                ResizePrefab(bg.controlContainer, bg.triangle, "Menu");
                ResizePrefab(bg.controlContainer, bg.circle, "Exit");
            }
            else
            {
                switch (canvas.name)
                {
                    case "Menu":
                        ResizePrefab(bg.controlContainer, bg.cross, "Select");
                        ResizePrefab(bg.controlContainer, bg.circle, "Cancel");
                        ResizePrefab(bg.controlContainer, bg.triangle, "Close");
                        break;

                    case "Details":
                        ResizePrefab(bg.controlContainer, bg.cross, "Download");
                        ResizePrefab(bg.controlContainer, bg.circle, "Close");
                        ResizePrefab(bg.controlContainer, bg.triangle, "Menu");
                        break;

                    case "Download":
                        ResizePrefab(bg.controlContainer, bg.circle, "Cancel Download");
                        break;

                    case "Cancel":
                    case "Update":
                    case "Close":
                        ResizePrefab(bg.controlContainer, bg.cross, "Confirm");
                        ResizePrefab(bg.controlContainer, bg.circle, "Cancel");
                        break;
                }
            }

            canvas?.SetActive(true);
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
                Print(true, PrintType.Error, $"Failed to parse a chunk of JSON content: {ex.Message}");
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
            if (!version.HasValue)
                return "?.??.?";

            string versionStr = version.Value.ToString("0.000", CultureInfo.InvariantCulture);
            string[] parts = versionStr.Split('.');
            string major = parts[0];
            string decimalPart = parts[1];

            if (decimalPart.Length < 3)
                decimalPart = decimalPart.PadRight(3, '0');

            string minor = decimalPart.Substring(0, 2);
            string patch = decimalPart.Substring(2, 1);

            return $"{major}.{minor}.{patch}";
        }

        public static bool IsNonEnglish(string input)
        {
            foreach (char c in input)
            {
                if (c > 127)
                    return true;
            }

            return false;
        }

        public static void SetFontByText(ref Text text)
        {
            var contentHandler =
                FindObjectOfType<ContentHandler>();

            int arabic = 0, asain = 0, korean = 0;

            foreach (char c in text.text)
            {
                if ((c >= 0x0600 && c <= 0x06FF) ||
                    (c >= 0x0750 && c <= 0x077F) ||
                    (c >= 0x08A0 && c <= 0x08FF))
                    arabic++;

                // Asain (Simplified & Traditional Chinese, Taiwanese, & Japanse)
                else if ((c >= 0x4E00 && c <= 0x9FFF) ||
                    (c >= 0x3400 && c <= 0x4DBF) ||
                    (c >= 0x3100 && c <= 0x312F) ||
                    (c >= 0x2F00 && c <= 0x2FDF) ||
                    (c >= 0x3040 && c <= 0x309F) ||
                    (c >= 0x30A0 && c <= 0x30FF) ||
                    (c >= 0x20000 && c <= 0x2A6DF))
                    asain++;

                else if ((c >= 0xAC00 && c <= 0xD7AF) || 
                    (c >= 0x1100 && c <= 0x11FF) ||
                    (c >= 0x3130 && c <= 0x318F))
                    korean++;
            }

            if (arabic > 0)
                text.font = contentHandler.Arabic;
            else if (asain > 0)
                text.font = contentHandler.Asian;
            else if (korean > 0)
                text.font = contentHandler.Korean;
            else text.font = contentHandler.Multi;
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
                if (!preserveExisting)
                    parsedData.Clear();

                const int chunkSize = 100;

                var dataEntries = JsonConvert.DeserializeObject<Games>(jsonContent).DATA.ToList();

                for (int i = 0; i < dataEntries.Count; i += chunkSize)
                {
                    var chunk = dataEntries.Skip(i).Take(chunkSize);
                    foreach (var entry in chunk)
                        parsedData[entry.Key] = entry.Value;
                }

                return 1;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public static async Task<int> ParseJSON(ContentType contentType)
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
                    if (IO.DoesPathExist(filePath))
                    {
                        try
                        {
                            string jsonContent = File.ReadAllText(filePath);
                            var content = JsonConvert.DeserializeObject<Games>(jsonContent);

                            if (content != null && content.DATA != null)
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
                            Print("Error loading " + type.ToString() + ": " + ex.Message, PrintType.Error);
                        }
                    }
                    else
                        await ParseJSON(type);
                }
                return (parsedData.Count > 0) ? 1 : 0;
            }

            string url = null;
            string singleFilePath = IO.GetFilePath(contentType);
            bool preserveExisting = (contentType != ContentType.Config && contentFilter == (int)ContentType.ALL);
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
                        case ContentType.PS5:
                            url = Variables.ContentURLs["ps5"];
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
                        default:
                            break;
                    }

                    if (URL.IsValidURI(url))
                    {
                        Print(true, PrintType.Warning, "Attempting to download JSON from: " + url);
                        webContent = await DownloadAsBytes(url);

                        if (string.IsNullOrEmpty(webContent))
                        {
                            Print(true, PrintType.Error, "Web-based loading failed: no data available for parsing.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Print(true, PrintType.Error, "Web-based loading failed: " + ex.Message);
                    Print(true, PrintType.Warning, "Falling back to local file loading.");
                }
            }

            if (!IO.DoesPathExist(singleFilePath) && contentType != ContentType.ALL)
            {
                try
                {
                    if (contentType == ContentType.Homebrew)
                    {
                        string pkgUrl = await DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/download/?echo=1");
                        string pkgSize = await DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/download/size/");

                        var homebrewJson = new
                        {
                            DATA = new Dictionary<string, object>
                            {
                                {
                                    pkgUrl, new
                                    {
                                        title_id = "PKGI13337",
                                        region = "ALL",
                                        name = "FPKGi",
                                        version = UI.FormatVersion(version),
                                        release = "12-25-2024",
                                        size = pkgSize ?? "82182144",
                                        min_fw = "4.50+ / PS5",
                                        cover_url = "https://pkg-zone.com/images/PKGI13337/cover.png"
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
                        string id = random.Next(0, 10).ToString() + random.Next(0, 10).ToString() +
                                    random.Next(0, 10).ToString() + random.Next(0, 10).ToString() +
                                    random.Next(0, 10).ToString();
                        var regions = new[] { "USA", "EUR", "ASIA", "JAP", null };
                        string region = regions[random.Next(regions.Length)];
                        string version = random.Next(0, 10).ToString() + "." + random.Next(0, 10).ToString() +
                                         random.Next(0, 10).ToString();
                        DateTime startDate = new DateTime(2000, 1, 1);
                        DateTime endDate = new DateTime(2050, 12, 31);
                        int range = (endDate - startDate).Days;
                        string randomDate = startDate.AddDays(random.Next(range)).ToString("MM-dd-yyyy");
                        long size = 500000000L + (long)(random.NextDouble() * (50000000000L - 500000000L));
                        string firmware = random.Next(1, 14).ToString() + "." + random.Next(0, 100).ToString("D2");

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
                            case ContentType.PS5:
                                defaultName = "PS5 Dump";
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

                        string fileName;
                        switch (defaultName)
                        {
                            case "PSP Remaster":
                                fileName = "psp-remaster";
                                break;
                            case "PlayStation Classic":
                                fileName = "ps1-classic";
                                break;
                            case "PlayStation 2 Classic":
                                fileName = "ps2-classic";
                                break;
                            default:
                                fileName = defaultName.ToLower();
                                break;
                        }

                        string downloadLink = "https://www.web.site/" + fileName + ".pkg";

                        var defaultJson = new
                        {
                            DATA = new Dictionary<string, object>
                    {
                        {
                            downloadLink, new
                            {
                                title_id = "CUSA" + id,
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
                    Print("Failed to generate default JSON: " + ex.Message, PrintType.Error);
                    return 0;
                }
            }

            try
            {
                string jsonContent = File.ReadAllText(singleFilePath);
                bool isValidWebContent = (populateViaWeb && URL.IsValidURI(url) && !string.IsNullOrEmpty(webContent));
                int parseResult = 0;

                if (contentType != ContentType.Config && contentType != ContentType.ALL && isValidWebContent)
                    parseResult = ParseGameData(webContent, preserveExisting);

                if (parseResult == 0)
                    parseResult = ParseGameData(jsonContent, preserveExisting);

                return parseResult;
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
            ulong bytes;

            if (ulong.TryParse(byteString, out bytes))
                return FormatByteString(bytes);

            return "0 B";
        }

        public static string FormatByteString(ulong bytes)
        {
            if (bytes >= 1000000000)
                return (bytes / 1000000000.0).ToString("0.##", CultureInfo.InvariantCulture) + " GB";

            if (bytes >= 1000000)
                return (bytes / 1000000.0).ToString("0.##", CultureInfo.InvariantCulture) + " MB";

            if (bytes >= 1000)
                return (bytes / 1000.0).ToString("0.##", CultureInfo.InvariantCulture) + " KB";

            return bytes.ToString(CultureInfo.InvariantCulture) + " B";
        }

        public static bool DoesPathExist(string path)
            => File.Exists(path) || Directory.Exists(path);

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
                case ContentType.PS5:
                    return Path.Combine(directoryPath, "ContentJSONs", "PS5.json");
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

        public static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            path = Path.GetFullPath(path);
            string directoryPath = Path.HasExtension(path) ? Path.GetDirectoryName(path) : path;

            if (DoesPathExist(directoryPath)) return;

            Stack<string> toCreate = new Stack<string>();

            while (!string.IsNullOrEmpty(directoryPath) && !DoesPathExist(directoryPath))
            {
                toCreate.Push(directoryPath);
                directoryPath = Path.GetDirectoryName(directoryPath);
            }

            while (toCreate.Count > 0)
                Directory.CreateDirectory(toCreate.Pop());
        }

        public static bool IsValidPackageFile(string filePath)  // shoutout LM
        {
            byte[] ExpectedMagic = { 0x7F, (byte)'C', (byte)'N', (byte)'T' };

            if (!DoesPathExist(filePath))
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

        public static bool IsValidImageExtension(string extension)
            => extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);

        public static bool IsValidZipArchiveFile(string filePath)
        {

            if (!File.Exists(filePath))
                return false;

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (stream.Length < 4)
                        return false;

                    byte[] header = new byte[4];

                    stream.Read(header, 0, header.Length);

                    if (header.SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }))
                        return true;
                }
            }
            catch (IOException)
            {
                return false;
            }

            return false;
        }

        public static void LoadImage(string path, ref RawImage image)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return;

                string directoryPath = Path.GetDirectoryName(path);
                string localFileName = Path.GetFileName(path);

                if (directoryPath != null && DoesPathExist(directoryPath))
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
                Print(true, PrintType.Error, $"Failed to load image: {ex.Message}");
            }
        }

        public static string ComputeFileMD5(string filePath)
        {
            if (!DoesPathExist(filePath)) return string.Empty;

            using (MD5 md5 = MD5.Create())
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    StringBuilder sb = new StringBuilder();

                    foreach (byte b in hash)
                        sb.Append(b.ToString("x2"));

                    return sb.ToString();
                }
            }
        }

        public static bool CompareMD5Hashes(string hash1, string hash2)
            => string.Equals(hash1, hash2, StringComparison.OrdinalIgnoreCase);

        public static string SanitizeFilename(string filename)
        {
            var sanitized = new string(filename
                .Where(c => !Path.GetInvalidFileNameChars().Contains(c))
                .ToArray());

            return sanitized.Length > 255 ? sanitized.Substring(0, 255) : sanitized;
        }

    }

    public class URL
    {
        public static string DecryptBase64(string encodedString)
        {
            if (IsValidURI(encodedString)) return encodedString;

            byte[] decodedBytes = Convert.FromBase64String(encodedString);
            string decodedString = Encoding.UTF8.GetString(decodedBytes);

            return decodedString;
        }

        public static bool IsValidURI(string url)
        {
            if (IO.DoesPathExist(url)) return true;
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrEmpty(url)) return false;

            url = Uri.EscapeUriString(url.Trim());

            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
            !Regex.IsMatch(url, @"^(http(s)?):\/\/[^\s\/$.?#].[^\s]*$", RegexOptions.IgnoreCase))
            {
                if (!IO.DoesPathExist(url))
                    return false;
            }

            return true;
        }

        public static bool IsValidImageType(string url)
            => IO.IsValidImageExtension(Path.GetExtension(url));

        public static bool IsValidImage(string url)
            => !string.IsNullOrEmpty(url) && IsValidURI(url) && IsValidImageType(url);

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

}
