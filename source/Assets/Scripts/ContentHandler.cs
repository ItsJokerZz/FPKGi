using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityOrbisBridge;
using static JsonData;
using static Utilities;
using static Variables;

public class ContentHandler : MonoBehaviour
{
    public static string downloadLink = string.Empty;
    public static string searchFilter = string.Empty;
    public static int filteredCount = 0;
    public static int removedCount = 0;
    public static int selectedIndex = 0;
    public static int contentScroll = 0;
    public static int itemsPerPage = 30;
    public static int currentPage = 0;

    public Text pkgCount;

    public Font Multi, Arabic, Korean, Asian;

    public static PKG currentPkg;

    public static int allCachedCount = 0;

    public static bool? initialCountUpdated = null;
    public static bool toggleBackToLocal = false;

    public static Dictionary<ContentType, Dictionary<string, GameContent>>
        contentTypeCache = new Dictionary<ContentType, Dictionary<string, GameContent>>();

    public static Dictionary<string, GameContent>
        allContentCache = new Dictionary<string, GameContent>();
    public static Dictionary<string, GameContent>
        homebrewCombinedCache = new Dictionary<string, GameContent>();

    public static KeyValuePair<string, GameContent> currentContentItem;

    public static Dictionary<string, GameContent> GetCurrentCacheForFilter()
    {
        if ((ContentType)contentFilter != ContentType.Config)
        {
            if ((ContentType)contentFilter == ContentType.Homebrew)
                return GetHomebrewCombinedCache();
            else if ((ContentType)contentFilter == ContentType.ALL)
                return contentTypeCache[ContentType.ALL];
            else
                return contentTypeCache.ContainsKey((ContentType)contentFilter)
                    ? contentTypeCache[(ContentType)contentFilter]
                    : new Dictionary<string, GameContent>();
        }

        return new Dictionary<string, GameContent>();
    }

    public static Dictionary<string, GameContent> GetHomebrewCombinedCache()
    {

        var homebrewContent = contentTypeCache.ContainsKey(ContentType.Homebrew)
            ? contentTypeCache[ContentType.Homebrew] : new Dictionary<string, GameContent>();

        var emulatorContent = contentTypeCache.ContainsKey(ContentType.Emulators)
            ? contentTypeCache[ContentType.Emulators] : new Dictionary<string, GameContent>();

        homebrewCombinedCache = homebrewContent.Concat(emulatorContent).ToDictionary(x => x.Key, x => x.Value);

        ControlMenu.reloadTriggered = false;

        return homebrewCombinedCache;
    }

    public static class Filtering
    {
        public static void RemoveInvalidItems(ref Dictionary<string, GameContent> parsedData)
        {
            var keysToRemove = parsedData
                .Where(item => item.Key == null
                            || item.Value == null ||
                            string.IsNullOrEmpty(item.Value.title_id) ||
                            string.IsNullOrEmpty(item.Value.name) ||
                            string.IsNullOrEmpty(item.Value.size))
                .Select(item => item.Key)
                .ToList();

            var removed = 0;

            foreach (var key in keysToRemove)
            {
                parsedData.Remove(key);
                removed++;
            }

            if (removed > removedCount)
                removedCount = removed;
        }

        public static List<KeyValuePair<string, GameContent>> ApplyFilter(List<KeyValuePair<string, GameContent>> itemsList)
        {
            if (GoldHEN == true)
            {
                itemsList = itemsList.Where(item =>
                    !item.Key.Contains("PS5") &&
                    !item.Value.name.Contains("PS5"))
                    .ToList();
            }

            if (!string.IsNullOrEmpty(searchFilter))
            {
                string filterLower = searchFilter.ToLower().Trim();

                var filteredByName = itemsList
                    .Where(item => item.Value.name != null && item.Value.name.ToLower().StartsWith(filterLower))
                    .ToList();

                if (!filteredByName.Any())
                {
                    filteredByName = itemsList
                        .Where(item => item.Value.title_id != null && item.Value.title_id.ToLower().StartsWith(filterLower))
                        .ToList();
                }

                if (!filteredByName.Any())
                    return new List<KeyValuePair<string, GameContent>>();

                itemsList = filteredByName;
            }

            itemsList = FilterByRegion(itemsList);
            return itemsList;
        }

        public static List<KeyValuePair<string, GameContent>> FilterByRegion(List<KeyValuePair<string, GameContent>> itemsList)
        {
            if (filteredRegions.Count() == 0)
            {
                return itemsList.Where(item =>
                {
                    string region = item.Value.region?.ToUpper() ?? "";
                    return region == "UNK" || region == "???" || region == "ALL" ||
                           string.IsNullOrEmpty(region);
                }).ToList();
            }

            return itemsList.Where(item =>
            {
                string region = item.Value.region?.ToUpper() ?? "";

                return (filteredRegions.Contains("Asia") && region == "ASIA") ||
                       (filteredRegions.Contains("Europe") && region == "EUR") ||
                       (filteredRegions.Contains("Japan") && region == "JAP") ||
                       (filteredRegions.Contains("USA") && region == "USA") ||
                       region == "UNK" || region == "???" || region == "ALL" ||
                       string.IsNullOrEmpty(region);
            }).ToList();
        }

        public static IEnumerable<KeyValuePair<string, GameContent>> SortItems(IEnumerable<KeyValuePair<string, GameContent>> itemsList)
        {
            IOrderedEnumerable<KeyValuePair<string, GameContent>> ordered;
            long size;
            double numericSize;

            switch (sortCriteria)
            {
                case 0:
                    ordered = ascending ?
                        itemsList.OrderBy(item =>
                        {
                            if (long.TryParse(item.Value.size, out size))
                                return size;
                            string sizeStr = item.Value.size.ToUpper().Trim();
                            if (double.TryParse(sizeStr.Replace("KB", "").Replace("MB", "").Replace("GB", "").Trim(), out numericSize))
                            {
                                if (sizeStr.EndsWith("KB")) return (long)(numericSize * 1024);
                                if (sizeStr.EndsWith("MB")) return (long)(numericSize * 1024 * 1024);
                                if (sizeStr.EndsWith("GB")) return (long)(numericSize * 1024 * 1024 * 1024);
                            }
                            return long.MaxValue;
                        }) :
                        itemsList.OrderByDescending(item =>
                        {
                            if (long.TryParse(item.Value.size, out size))
                                return size;
                            string sizeStr = item.Value.size.ToUpper().Trim();
                            if (double.TryParse(sizeStr.Replace("KB", "").Replace("MB", "").Replace("GB", "").Trim(), out numericSize))
                            {
                                if (sizeStr.EndsWith("KB")) return (long)(numericSize * 1024);
                                if (sizeStr.EndsWith("MB")) return (long)(numericSize * 1024 * 1024);
                                if (sizeStr.EndsWith("GB")) return (long)(numericSize * 1024 * 1024 * 1024);
                            }
                            return long.MaxValue;
                        });
                    break;

                case 1:
                    ordered = ascending ?
                        itemsList.OrderBy(item =>
                        {
                            string region = item.Value.region?.ToUpperInvariant() ?? "???";
                            switch (region)
                            {
                                case "???": return 0;
                                case "ALL": return 1;
                                case "ASIA": return 2;
                                case "EUR": return 3;
                                case "JAP": return 4;
                                case "USA": return 5;
                                default: return 6;
                            }
                        }) :
                        itemsList.OrderByDescending(item =>
                        {
                            string region = item.Value.region?.ToUpperInvariant() ?? "???";
                            switch (region)
                            {
                                case "???": return 0;
                                case "ALL": return 1;
                                case "ASIA": return 2;
                                case "EUR": return 3;
                                case "JAP": return 4;
                                case "USA": return 5;
                                default: return 6;
                            }
                        });
                    break;

                case 2:
                    ordered = ascending ?
                        itemsList.OrderBy(item => item.Value.name ?? string.Empty) :
                        itemsList.OrderByDescending(item => item.Value.name ?? string.Empty);
                    break;

                case 3:
                    int number;
                    ordered = ascending ?
                        itemsList.OrderBy(item =>
                        {
                            string titleId = item.Value.title_id ?? string.Empty;
                            string numericPart = new string(titleId.Where(char.IsDigit).ToArray());
                            return int.TryParse(numericPart, out number) ? number : int.MaxValue;
                        }) :
                        itemsList.OrderByDescending(item =>
                        {
                            string titleId = item.Value.title_id ?? string.Empty;
                            string numericPart = new string(titleId.Where(char.IsDigit).ToArray());
                            return int.TryParse(numericPart, out number) ? number : int.MaxValue;
                        });
                    break;

                default:
                    return itemsList;
            }

            return ordered;
        }
    }

    public static class UIManagement
    {
        private static Dictionary<string, Font> fontCache = new Dictionary<string, Font>();
        private static Dictionary<string, int> cachedCounts = new Dictionary<string, int>();
        private static Dictionary<string, List<KeyValuePair<string, GameContent>>> cachedItemsList = new Dictionary<string, List<KeyValuePair<string, GameContent>>>();

        public static void ClearDisplayedItems()
        {
            foreach (var pkg in Content.PKGs)
            {
                if (pkg == null) continue;

                UI.ChangeText(pkg.TitleID, string.Empty);
                UI.ChangeText(pkg.Region, string.Empty);
                UI.ChangeText(pkg.Title, string.Empty);
                UI.ChangeText(pkg.Size, string.Empty);
            }
        }
        [DllImport("UnityOrbisBridge")] public static extern long GetRemoteFileSize(string url);

        public static void DisplayItems(IEnumerable<KeyValuePair<string, GameContent>> items)
        {
            var groupedItemsByKey = items.GroupBy(item => item.Key);
            int currentIndex = 0;
            var displayedTitles = new HashSet<string>();
            var duplicateTitles = new HashSet<string>();
            var validRegions = new HashSet<string> { "asia", "eur", "jap", "usa", "all" };
            var contentHandler = FindObjectOfType<ContentHandler>();
            var invalidFileNameChars = Path.GetInvalidFileNameChars();

            var arabicFont = contentHandler?.Arabic;
            var multiFont = contentHandler?.Multi;

            foreach (var itemGroup in groupedItemsByKey)
            {
                foreach (var item in itemGroup)
                {
                    if (currentIndex >= Content.PKGs.Count) break;

                    var package = Content.PKGs[currentIndex];
                    if (package == null || item.Value == null || string.IsNullOrEmpty(item.Value.title_id)
                        || string.IsNullOrEmpty(item.Value.name) || string.IsNullOrEmpty(item.Value.size))
                        continue;

                    string region = ProcessRegion(item.Value.region);
                    item.Value.region = region;

                    item.Value.min_fw = string.IsNullOrEmpty(item.Value.min_fw) ? "?.??" : item.Value.min_fw;
                    item.Value.release = string.IsNullOrEmpty(item.Value.release) ? "UNKNOWN" : item.Value.release;
                    item.Value.version = item.Value.version ?? "?.??";

                    bool isFirstOccurrence = !displayedTitles.Contains(item.Value.name);
                    if (!isFirstOccurrence)
                        duplicateTitles.Add(item.Value.name);
                    else
                        displayedTitles.Add(item.Value.name);

                    string titleToDisplay = duplicateTitles.Contains(item.Value.name)
                        ? $"{item.Value.name} [v{item.Value.version}]" : item.Value.name;

                    UpdatePackageUI(package, item.Value.title_id, region, titleToDisplay, item.Value.size);
                 // UpdatePackageUI(package, item.Value.title_id, region, titleToDisplay, GetRemoteFileSize(item.Key).ToString());

                    SetFontByTextCached(ref package.Title, titleToDisplay, arabicFont, multiFont);

                    currentIndex++;
                }

                UpdateDownloadStatus(Content.PKGs[currentIndex - 1], invalidFileNameChars);
            }
        }

        private static string ProcessRegion(string region)
        {
            if (string.IsNullOrEmpty(region))
                return "???";

            region = region.ToLower().Trim();

            if (region == "unk" || region == "???" || region.Length > 4)
                return "???";

            var validRegions = new HashSet<string> { "asia", "eur", "jap", "usa", "all" };
            return validRegions.Contains(region) ? region.ToUpper() : "???";
        }

        private static void UpdatePackageUI(PKG package, string titleId, string region, string title, string size)
        {
            UI.ChangeText(package.TitleID, titleId);
            UI.ChangeText(package.Region, region);
            UI.ChangeText(package.Title, title);
            UI.ChangeText(package.Size, IO.FormatByteString(size));
        }

        private static void SetFontByTextCached(ref Text text, string textContent, Font arabicFont, Font multiFont)
        {
            Font cachedFont;
            if (fontCache.TryGetValue(textContent, out cachedFont))
                text.font = cachedFont;
            else
            {
                Font detectedFont = DetectFontOptimized(textContent, arabicFont, multiFont);
                fontCache[textContent] = detectedFont;
                text.font = detectedFont;
            }

            text.fontSize = text.font == arabicFont ? 18 : 28;
        }

        private static Font DetectFontOptimized(string text, Font arabicFont, Font multiFont)
        {
            if (string.IsNullOrEmpty(text))
                return multiFont;

            int sampleSize = Math.Min(text.Length, 20);
            int step = Math.Max(1, text.Length / sampleSize);

            int arabic = 0, asian = 0, korean = 0;

            for (int i = 0; i < text.Length; i += step)
            {
                char c = text[i];

                if ((c >= 0x0600 && c <= 0x06FF) ||
                    (c >= 0x0750 && c <= 0x077F) ||
                    (c >= 0x08A0 && c <= 0x08FF))
                    arabic++;
                else if ((c >= 0x4E00 && c <= 0x9FFF) ||
                    (c >= 0x3400 && c <= 0x4DBF) ||
                    (c >= 0x3100 && c <= 0x312F) ||
                    (c >= 0x2F00 && c <= 0x2FDF) ||
                    (c >= 0x3040 && c <= 0x309F) ||
                    (c >= 0x30A0 && c <= 0x30FF) ||
                    (c >= 0x20000 && c <= 0x2A6DF))
                    asian++;
                else if ((c >= 0xAC00 && c <= 0xD7AF) ||
                    (c >= 0x1100 && c <= 0x11FF) ||
                    (c >= 0x3130 && c <= 0x318F))
                    korean++;
            }

            var contentHandler = FindObjectOfType<ContentHandler>();
            if (arabic > 0)
                return arabicFont;
            else if (asian > 0)
                return contentHandler?.Asian ?? multiFont;
            else if (korean > 0)
                return contentHandler?.Korean ?? multiFont;
            else
                return multiFont;
        }

        private static string SanitizeFilename(string filename, char[] invalidChars)
        {
            if (string.IsNullOrEmpty(filename))
                return string.Empty;

            var sb = new System.Text.StringBuilder(filename);
            foreach (char invalidChar in invalidChars)
                sb.Replace(invalidChar.ToString(), string.Empty);

            string result = sb.ToString();
            return result.Length > 255 ? result.Substring(0, 255) : result;
        }

        private static void UpdateDownloadStatus(PKG package, char[] invalidFileNameChars)
        {
            package.Downloaded.gameObject.SetActive(false);
            UI.ChangeText(package.Downloaded, "");
            package.Downloaded.color = Color.white;

            string sanitizedFilename = SanitizeFilename(package.Title.text, invalidFileNameChars);
            var packagePath = Path.Combine(downloadPath, $"[{package.TitleID.text}] {sanitizedFilename}.pkg");

            bool isFullyDownloaded = File.Exists(packagePath) && IO.IsValidPackageFile(packagePath);
            bool isPartiallyDownloaded = !isFullyDownloaded && File.Exists($"{packagePath}.resume");
            bool isInstalled = isConsole && UOB.CheckIfAppExists(package.TitleID.text);

            if (isInstalled)
            {
                package.Downloaded.gameObject.SetActive(true);
                UI.ChangeText(package.Downloaded, "x");
                package.Downloaded.color = blueish;
            }
            else if (isFullyDownloaded)
            {
                package.Downloaded.gameObject.SetActive(true);
                UI.ChangeText(package.Downloaded, "+");
                package.Downloaded.color = yellowish;
            }
            else if (isPartiallyDownloaded)
            {
                package.Downloaded.gameObject.SetActive(true);
                UI.ChangeText(package.Downloaded, "o");
                package.Downloaded.color = redish;
            }
        }

        public static void UpdateAllDownloadStatuses()
        {
            if (Content.PKGs == null || Content.PKGs.Count == 0) return;

            var invalidFileNameChars = Path.GetInvalidFileNameChars();

            foreach (var pkg in Content.PKGs)
            {
                if (pkg == null || string.IsNullOrEmpty(pkg.TitleID.text) || string.IsNullOrEmpty(pkg.Title.text))
                    continue;

                UpdateDownloadStatus(pkg, invalidFileNameChars);
            }
        }

        public static void UpdateHomebrewContent()
        {
            homebrewCombinedCache = GetHomebrewCombinedCache();

            parsedData = homebrewCombinedCache;

            var combinedItems = Filtering.FilterByRegion(parsedData.ToList());
            combinedItems = Filtering.ApplyFilter(combinedItems);

            UI.ChangeText(FindObjectOfType<ContentHandler>()?.pkgCount,
                $"Content: {combinedItems.Count} [{allCachedCount}]");
        }

        public static async Task UpdateRegularContent()
        {
            if (contentTypeCache.ContainsKey((ContentType)contentFilter)
                && contentTypeCache[(ContentType)contentFilter] != null)
            {
                parsedData = contentTypeCache[(ContentType)contentFilter];
            }
            else
            {
                if (await JSON.ParseJSON((ContentType)contentFilter) != 0)
                    contentTypeCache[(ContentType)contentFilter] =
                        new Dictionary<string, GameContent>(parsedData);
            }
        }

        public static async void UpdateContent(int page)
        {
            currentPage = Mathf.Max(page, 0);
            int startIndex = currentPage * itemsPerPage;

            foreach (var pkg in Content.PKGs)
            {
                if (pkg != null)
                {
                    pkg.Downloaded.gameObject.SetActive(false);
                    UI.ChangeText(pkg.Downloaded, "");
                    pkg.Downloaded.color = Color.white;
                }
            }

            if (contentFilter != (int)ContentType.Config)
            {
                if (contentFilter != (int)ContentType.ALL)
                {
                    if (contentFilter == (int)ContentType.Homebrew)
                        UpdateHomebrewContent();
                    else
                        await UpdateRegularContent();
                }
                else
                {
                    if (contentTypeCache.ContainsKey(ContentType.ALL))
                        parsedData = contentTypeCache[ContentType.ALL];
                    else
                        parsedData = new Dictionary<string, GameContent>();
                }
            }

            if ((ContentType)contentFilter != ContentType.Homebrew)
            {
                if (!contentTypeCache.ContainsKey((ContentType)contentFilter) ||
                    contentTypeCache[(ContentType)contentFilter] == null)
                {
                    if (await JSON.ParseJSON((ContentType)contentFilter) != 0)
                        contentTypeCache[(ContentType)contentFilter] = new Dictionary<string, GameContent>(parsedData);
                }
                else
                    parsedData = contentTypeCache[(ContentType)contentFilter];
            }

            // Cache filtered and sorted items for ALL page to avoid expensive operations
            string cacheKey = $"itemsList_{contentFilter}_{string.Join(",", filteredRegions)}_{searchFilter}_{sortCriteria}_{ascending}";
            IEnumerable<KeyValuePair<string, GameContent>> sortedItemsList;

            if ((ContentType)contentFilter == ContentType.ALL && cachedItemsList.ContainsKey(cacheKey))
            {
                sortedItemsList = cachedItemsList[cacheKey];
                filteredCount = sortedItemsList.Count();
            }
            else
            {
                var itemsList = parsedData
                    .Where(item => item.Value != null &&
                        !string.IsNullOrEmpty(item.Value.title_id) &&
                        !string.IsNullOrEmpty(item.Value.name) &&
                        !string.IsNullOrEmpty(item.Value.size)).ToList();

                if ((ContentType)contentFilter == ContentType.ALL ||
                    contentFilter == (int)ContentType.Homebrew)
                    itemsList = Filtering.FilterByRegion(itemsList);

                itemsList = Filtering.ApplyFilter(itemsList);

                filteredCount = itemsList.Count;
                sortedItemsList = Filtering.SortItems(itemsList);

                // Cache for ALL page
                if ((ContentType)contentFilter == ContentType.ALL)
                    cachedItemsList[cacheKey] = sortedItemsList.ToList();
            }
            if (startIndex >= sortedItemsList.Count())
            {
                currentPage = Mathf.Max(0, (sortedItemsList.Count() - 1) / itemsPerPage);
                startIndex = currentPage * itemsPerPage;
            }

            ClearDisplayedItems();
            var itemsToDisplay = sortedItemsList.Skip(startIndex).Take(itemsPerPage).ToList();
            DisplayItems(itemsToDisplay);

            for (int i = itemsToDisplay.Count; i < Content.PKGs.Count; i++)
            {
                if (Content.PKGs[i] != null)
                {
                    UI.ChangeText(Content.PKGs[i].TitleID, string.Empty);
                    UI.ChangeText(Content.PKGs[i].Region, string.Empty);
                    UI.ChangeText(Content.PKGs[i].Title, string.Empty);
                    UI.ChangeText(Content.PKGs[i].Size, string.Empty);
                }
            }

            UI.UpdateScrollbar();
        }

        public static async void UpdatePkgCount()
        {
            bool needUpdate =
                toggleBackToLocal
                || ControlMenu.reloadTriggered
                || Background.fullyInitialized
                || (Background.initializedApp
                && initialCountUpdated == null);

            if (!needUpdate)
                return;

            if (initialCountUpdated == null)
            {
                toggleBackToLocal = true;
                initialCountUpdated = true;
                ControlMenu.reloadTriggered = true;
            }

            if (toggleBackToLocal)
            {
                toggleBackToLocal = false;

                foreach (var key in contentTypeCache.Keys.ToList())
                    contentTypeCache[key]?.Clear();

                contentTypeCache.Clear();
                allContentCache.Clear();
                homebrewCombinedCache.Clear();
                fontCache.Clear();
                cachedCounts.Clear();
                cachedItemsList.Clear();

                ControlMenu.reloadTriggered = true;
            }

            if (ControlMenu.reloadTriggered)
            {
                ControlMenu.reloadTriggered = false;
                contentScroll = 0;
            }

            var contentTypes = Enum.GetValues(typeof(ContentType))
                                   .Cast<ContentType>()
                                   .Where(type => type != ContentType.Config && type != ContentType.ALL)
                                   .ToList();

            ContentType currentType = (ContentType)contentFilter;
            if (currentType != ContentType.ALL && currentType != ContentType.Config)
            {
                if (contentTypes.Remove(currentType))
                    contentTypes.Add(currentType);
            }

            if (!contentTypeCache.ContainsKey((ContentType)contentFilter) || contentTypeCache[(ContentType)contentFilter] == null)
            {
                foreach (var type in contentTypes)
                {
                    if (type == currentType)
                        continue;

                    if (contentTypeCache.ContainsKey(type) && contentTypeCache[type] != null)
                        continue;

                    parsedData.Clear();

                    if (await JSON.ParseJSON(type) != 0)
                        contentTypeCache[type] = new Dictionary<string, GameContent>(parsedData);
                }

                parsedData.Clear();

                if (await JSON.ParseJSON(currentType) != 0)
                    contentTypeCache[currentType] = new Dictionary<string, GameContent>(parsedData);
            }

            allContentCache.Clear();

            foreach (var type in contentTypes)
            {
                if (contentTypeCache.ContainsKey(type) && contentTypeCache[type] != null)
                {
                    foreach (var kv in contentTypeCache[type])
                    {
                        if (!allContentCache.ContainsKey(kv.Key))
                            allContentCache.Add(kv.Key, kv.Value);
                    }
                }
            }

            contentTypeCache[ContentType.ALL] = new Dictionary<string, GameContent>(allContentCache);

            UpdateContent(currentPage);

            Dictionary<string, GameContent> currentCache = GetCurrentCacheForFilter();
            var currentItems = currentCache
                .Where(item => item.Value != null &&
                               !string.IsNullOrEmpty(item.Value.title_id) &&
                               !string.IsNullOrEmpty(item.Value.name) &&
                               !string.IsNullOrEmpty(item.Value.size))
                .ToList();

            if ((ContentType)contentFilter == ContentType.ALL
                || (ContentType)contentFilter == ContentType.Homebrew)
                currentItems = Filtering.FilterByRegion(currentItems);

            currentItems = Filtering.ApplyFilter(currentItems);

            int currentFilteredCount = currentItems.Count;

            int fullCount = 0;
            if (contentTypeCache.ContainsKey(ContentType.ALL))
            {
                // Cache the count to avoid expensive LINQ operations
                if (!cachedCounts.ContainsKey("fullCount"))
                {
                    cachedCounts["fullCount"] = contentTypeCache[ContentType.ALL]
                        .Where(item => item.Value != null &&
                                       !string.IsNullOrEmpty(item.Value.title_id) &&
                                       !string.IsNullOrEmpty(item.Value.name) &&
                                       !string.IsNullOrEmpty(item.Value.size) &&
                                       !(GoldHEN == true && (item.Key.Contains("PS5") || item.Value.name.Contains("PS5"))))
                        .Count();
                }
                fullCount = cachedCounts["fullCount"];
            }

            int currentCount = contentTypeCache.ContainsKey(currentType)
                ? contentTypeCache[currentType]
                      .Where(item => item.Value != null &&
                                     !string.IsNullOrEmpty(item.Value.title_id) &&
                                     !string.IsNullOrEmpty(item.Value.name) &&
                                     !string.IsNullOrEmpty(item.Value.size) &&
                                     !(GoldHEN == true && (item.Key.Contains("PS5") || item.Value.name.Contains("PS5"))))
                      .Count() : 0;

            var contentHandler = FindObjectOfType<ContentHandler>();

            if ((ContentType)contentFilter == ContentType.ALL)
            {
                // Cache filtered count to avoid expensive LINQ operations
                string cacheKey = $"filteredAllCount_{string.Join(",", filteredRegions)}";
                int filteredAllCount;

                if (!cachedCounts.ContainsKey(cacheKey))
                {
                    cachedCounts[cacheKey] = Filtering.FilterByRegion(contentTypeCache[ContentType.ALL].ToList())
                        .Where(item => item.Value != null &&
                                       !string.IsNullOrEmpty(item.Value.title_id) &&
                                       !string.IsNullOrEmpty(item.Value.name) &&
                                       !string.IsNullOrEmpty(item.Value.size) &&
                                       !(GoldHEN == true && (item.Key.Contains("PS5") || item.Value.name.Contains("PS5"))))
                        .Count();
                }

                filteredAllCount = cachedCounts[cacheKey];
                int leftValue = Mathf.Clamp(filteredAllCount - removedCount, 0, filteredAllCount - removedCount);

                UI.ChangeText(contentHandler?.pkgCount, $"Content: {leftValue} [{fullCount}]");
            }
            else if ((ContentType)contentFilter != ContentType.Homebrew)
                UI.ChangeText(contentHandler?.pkgCount, $"Content: {currentCount} [{fullCount}]");

            allCachedCount = fullCount;
        }

        private static int lastContentScroll = -1;
        private static int lastContentFilter = -1;

        public static void HighlightCurrentPkg()
        {
            if (Content.PKGs == null || Content.PKGs.Count == 0) return;

            // Update install state for all displayed packages every frame
            UpdateAllDownloadStatuses();

            int pageIndex = contentScroll % itemsPerPage;
            currentPkg = Content.PKGs[pageIndex];

            if (currentPkg != null)
            {
                currentPkg.TitleID.color = blueish;
                currentPkg.Region.color = blueish;
                currentPkg.Title.color = blueish;
                currentPkg.Size.color = blueish;
            }

            foreach (var pkg in Content.PKGs)
            {
                if (pkg != currentPkg && pkg.TitleID.enabled)
                {
                    pkg.TitleID.color = Color.white;
                    pkg.Region.color = Color.white;
                    pkg.Title.color = Color.white;
                    pkg.Size.color = Color.white;
                }
            }

            // Only update content when scroll position or filter actually changes
            int currentPage = contentScroll / itemsPerPage;
            if (lastContentScroll != contentScroll || lastContentFilter != contentFilter)
            {
                UpdateContent(currentPage);
                lastContentScroll = contentScroll;
                lastContentFilter = contentFilter;
            }

            // Only update count when needed (not every frame)
            if (ControlMenu.reloadTriggered || toggleBackToLocal)
            {
                UpdatePkgCount();
            }

            Dictionary<string, GameContent> currentCache = (ContentType)contentFilter == ContentType.Homebrew
            ? GetHomebrewCombinedCache() : (ContentType)contentFilter == ContentType.ALL
                ? (contentTypeCache.ContainsKey(ContentType.ALL) ? contentTypeCache[ContentType.ALL] :
                new Dictionary<string, GameContent>()) : (ContentType)contentFilter == ContentType.Config
                    ? parsedData : (contentTypeCache.ContainsKey((ContentType)contentFilter) ?
                    contentTypeCache[(ContentType)contentFilter] : new Dictionary<string, GameContent>());


            if (currentCache == null)
                return;

            var itemsList = currentCache.Where(item => item.Value != null &&
                               !string.IsNullOrEmpty(item.Value.title_id) &&
                               !string.IsNullOrEmpty(item.Value.name) &&
                               !string.IsNullOrEmpty(item.Value.size)).ToList();

            if ((ContentType)contentFilter == ContentType.ALL ||
                (ContentType)contentFilter == ContentType.Homebrew)
                itemsList = Filtering.FilterByRegion(itemsList);

            itemsList = Filtering.ApplyFilter(itemsList);
            var sortedItemsList = Filtering.SortItems(itemsList).ToList();

            if (sortedItemsList.Count == 0)
                return;

            int startIndex = currentPage * itemsPerPage,
                selectedIndex = startIndex + pageIndex;

            if (selectedIndex < 0 || selectedIndex >= sortedItemsList.Count)
                return;

            currentContentItem = sortedItemsList[selectedIndex];
        }
    }

}