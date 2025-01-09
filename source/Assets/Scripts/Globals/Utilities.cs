using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using static JsonData;
using static UOBWrapper;
using static Variables;

public class Utilities
{
    public class UI
    {
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
                int count = 0;

                foreach (var entry in chunk)
                {
                    parsedData[entry.Key] = entry.Value;

                    if (Content.PKGs.Count > count && Content.PKGs[count] != null) count++;
                }
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
    }

    public class JSON
    {
        private static int ParseGameData(string jsonContent = "")
        {
            var content = JsonConvert.DeserializeObject<Games>(jsonContent);

            parsedData.Clear();

            const int chunkSize = 25;
            var dataEntries = content.DATA.ToList();
            int totalCount = dataEntries.Count;

            for (int i = 0; i < totalCount; i += chunkSize)
            {
                var chunk = dataEntries.Skip(i).Take(chunkSize).ToList();

                if (!UI.UpdateUIFromChunk(chunk))
                    return 0;
            }
            return 1;
        }

        public static int ParseJSON(ContentType contentType = ContentType.Config)
        {
            string filePath = IO.GetFilePath(contentType);

            if (populateViaWeb)
            {
                try
                {
                    string url = null;

                    switch (contentType)
                    {
                        case ContentType.Apps:
                            url = Variables.ContentURLs["apps"];
                            break;
                        case ContentType.Demos:
                            url = Variables.ContentURLs["demos"];
                            break;
                        case ContentType.DLC:
                            url = Variables.ContentURLs["dlc"];
                            break;
                        case ContentType.Games:
                            url = Variables.ContentURLs["games"];
                            break;
                        case ContentType.Homebrew:
                            url = Variables.ContentURLs["homebrew"];
                            break;
                        case ContentType.Updates:
                            url = Variables.ContentURLs["updates"];
                            break;
                    }

                    if (string.IsNullOrEmpty(url))
                        throw new Exception("URL is null or empty.");

                    return ParseGameData(DownloadAsBytes(url));
                }
                catch (Exception ex)
                {
                    Print($"Web-based loading failed: {ex.Message}", PrintType.Error);
                    Print("Falling back to local file loading.", PrintType.Warning);
                }
            }

            if (!File.Exists(filePath))
            {
                try
                {
                    var pkgUrl = DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/download/?echo=1");


                    if (contentType == ContentType.Homebrew)
                    {
                        var homebrewJson = new
                        {
                            DATA = new Dictionary<string, object>
                    {
                        {
                            pkgUrl, new
                            {
                                title_id = "FPKGI13337",
                                region = "ALL",
                                name = "F[PKGi]",
                                version = DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/latestVersion/") ?? null,
                                release = "12-25-2024",
                                size = DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/download/size/") ?? "75000000",
                                min_fw = "4.50",
                                cover_url = "https://www.itsjokerzz.site/projects/FPKGi/Icon.png"
                            }
                        }
                    }
                        };

                        string jsonString = JsonConvert.SerializeObject(homebrewJson, Formatting.Indented);
                        File.WriteAllText(filePath, jsonString);
                    }
                    else if (contentType != ContentType.Config)
                    {
                        var defaultJson = new
                        {
                            DATA = new Dictionary<string, object> { {
                            "https://www.web.site/content.pkg", new
                            {
                                title_id = "CUSA00000",
                                region = "ALL",
                                name = "Demo",
                                version = "1.00",
                                release = "01-01-9999",
                                size = 13333333337,
                                min_fw = "12.00",
                                cover_url = (string)null
                            }
                        }
                    }
                        };

                        string jsonString = JsonConvert.SerializeObject(defaultJson, Formatting.Indented);
                        File.WriteAllText(filePath, jsonString);
                    }

                    ParseJSON((ContentType)contentFilter);
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
                string jsonContent = File.ReadAllText(filePath);

                if (contentType != ContentType.Config)
                    return ParseGameData(jsonContent);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Object reference not set to an instance of an object"))
                    return 0;

                Print($"Failed to read or parse JSON file: {ex.Message}", PrintType.Error);
                Print($"Stack Trace:\n{ex.StackTrace}", PrintType.Error);
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
                    return $"{Math.Round(gigabytes, 2):F2} GB";

                double megabytes = bytes / (1024.0 * 1024.0);
                if (megabytes >= 1)
                    return $"{Math.Round(megabytes, 2):F2} MB";

                double kilobytes = bytes / 1024.0;
                if (kilobytes >= 1)
                    return $"{Math.Round(kilobytes, 2):F2} KB";

                return $"{bytes} B";
            }
            return "0 B";
        }

        public static string FormatByteString(float bytes)
        {
            if (bytes >= 1e9) return $"{(bytes / 1e9):0.##} GB";
            if (bytes >= 1e6) return $"{(bytes / 1e6):0.##} MB";
            if (bytes >= 1e3) return $"{(bytes / 1e3):0.##} KB";

            return $"{bytes} B";
        }

        public static string GetFilePath(ContentType contentType)
        {
            switch (contentType)
            {
                case ContentType.Config:
                    return Path.Combine(directoryPath, "config.json");
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

                default: return string.Empty;
            }
        }

        public static bool IsValidImageExtension(string extension)
        {
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
        }

        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        public static void LoadImage(string path, ref RawImage image)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    Debug.LogWarning("Path is null or empty.");
                    return;
                }

                string directoryPath = Path.GetDirectoryName(path);
                string localFileName = Path.GetFileName(path);

                if (directoryPath != null && Directory.Exists(directoryPath))
                {
                    string matchedFile = Directory
                        .GetFiles(directoryPath)
                        .FirstOrDefault(
                            f => string.Equals(Path.GetFileName(f), localFileName, StringComparison.OrdinalIgnoreCase)
                        );

                    if (matchedFile == null)
                    {
                        Debug.LogError($"No file found matching '{localFileName}' in directory '{directoryPath}'.");
                    }

                    if (background == null)
                    {
                        Debug.LogError("Background object is null. Make sure it is assigned correctly.");
                    }

                    if (matchedFile != null && background != null)
                    {
                        string fileExtension = Path.GetExtension(matchedFile).ToLower();

                        if (IO.IsValidImageExtension(fileExtension))
                        {
                            Texture2D texture = new Texture2D(2, 2);
                            byte[] imageData = File.ReadAllBytes(matchedFile);

                            if (texture.LoadImage(imageData))
                            {
                                Debug.Log("Image loaded successfully.");

                                image.texture = texture;

                                image.gameObject.SetActive(true);
                            }
                            else
                            {
                                Debug.LogError("Failed to load image into texture.");
                            }

                            background_uri = matchedFile;
                        }
                        else
                        {
                            Debug.LogError("Invalid image extension.");
                        }
                    }
                }
                else
                {
                    Debug.LogError("Directory does not exist or directoryPath is null.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception occurred: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    public class URL
    {
        public static bool IsValid(string url) =>
            Regex.IsMatch(url, @"^(http(s)?:\/\/)?(www\.)?[a-zA-Z0-9\-]+(\.[a-zA-Z]{2,})+");

        public static bool IsValidImageType(string url)
        {
            return IO.IsValidImageExtension(Path.GetExtension(url));
        }

        public static bool IsValidImage(string url) => !string.IsNullOrEmpty(url) && IsValid(url) && IsValidImageType(url);
    }

    public class Menu
    {
        public static bool IsValidMenuItemIndex(int index)
        {
            return index >= 0 && index < menuTexts.Length;
        }

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
