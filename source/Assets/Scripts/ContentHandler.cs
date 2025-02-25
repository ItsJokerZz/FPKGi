using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static JsonData;
using static Utilities;
using static Variables;

public class ContentHandler : MonoBehaviour
{
    public static string filter = string.Empty;
    public static int filteredCount = 0;
    public static int removedCount = 0;
    public static int selectedIndex = 0;
    public static int contentScroll = 0;
    public static int itemsPerPage = 25;
    public static int currentPage = 0;

    public Text pkgCount;
    public Text currentInt;
    public static PKG currentPkg;

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
            if (!string.IsNullOrEmpty(filter))
            {
                string filterLower = filter.ToLower().Trim();

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
                    return region == "ALL" || region == "???" || region == "UNK" ||
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
                       region == "ALL" || region == "???" || region == "UNK" ||
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
                                case "ALL": return 0;
                                case "ASIA": return 1;
                                case "EUR": return 2;
                                case "JAP": return 3;
                                case "USA": return 4;
                                case "???":
                                default:
                                    return 4;
                            }
                        }) :
                        itemsList.OrderByDescending(item =>
                        {
                            string region = item.Value.region?.ToUpperInvariant() ?? "???";
                            switch (region)
                            {
                                case "ALL": return 0;
                                case "ASIA": return 1;
                                case "EUR": return 2;
                                case "JAP": return 3;
                                case "USA": return 4;
                                case "???":
                                default:
                                    return 4;
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

        public static void DisplayItems(IEnumerable<KeyValuePair<string, GameContent>> items)
        {
            var groupedItemsByKey = items.GroupBy(item => item.Key);
            int currentIndex = 0;
            var displayedTitles = new HashSet<string>();
            var duplicateTitles = new HashSet<string>();
            var validRegions = new HashSet<string> { "asia", "eur", "jap", "usa", "all" };

            foreach (var itemGroup in groupedItemsByKey)
            {
                foreach (var item in itemGroup)
                {
                    if (currentIndex >= Content.PKGs.Count) break;

                    var package = Content.PKGs[currentIndex];
                    if (package == null) continue;

                    if (item.Value == null || string.IsNullOrEmpty(item.Value.title_id)
                        || string.IsNullOrEmpty(item.Value.name) || string.IsNullOrEmpty(item.Value.size))
                        continue;

                    string region = item.Value.region?.ToLower().Trim();

                    if (string.IsNullOrEmpty(region) || !validRegions.Contains(region) ||
                        region.Length > 4 || region == "unk" || region == "???") item.Value.region = "???";
                    else
                        item.Value.region = region.ToUpper();

                    item.Value.region.Replace("???", "UNK");
                    item.Value.min_fw = string.IsNullOrEmpty(item.Value.min_fw) ? "?.??" : item.Value.min_fw;
                    item.Value.release = string.IsNullOrEmpty(item.Value.release) ? "UNKNOWN" : item.Value.release;

                    bool isFirstOccurrence = !displayedTitles.Contains(item.Value.name);

                    if (!isFirstOccurrence)
                        duplicateTitles.Add(item.Value.name);
                    else
                        displayedTitles.Add(item.Value.name);

                    currentIndex++;
                }
            }

            currentIndex = 0;
            foreach (var itemGroup in groupedItemsByKey)
            {
                foreach (var item in itemGroup)
                {
                    if (currentIndex >= Content.PKGs.Count) break;

                    var package = Content.PKGs[currentIndex];
                    if (package == null) continue;

                    if (item.Value == null || string.IsNullOrEmpty(item.Value.title_id)
                        || string.IsNullOrEmpty(item.Value.name)
                        || string.IsNullOrEmpty(item.Value.size))
                        continue;

                    string region = item.Value.region?.Trim().ToLower();
                    item.Value.region = (string.IsNullOrEmpty(region) ||
                        !validRegions.Contains(region) || region.Length > 4)
                        ? "???" : region.ToUpper();

                    item.Value.version = item.Value.version ?? "?.??";
                    item.Value.min_fw = item.Value.min_fw ?? "?.??";
                    item.Value.release = item.Value.release ?? "UNKNOWN";

                    string titleToDisplay = duplicateTitles.Contains(item.Value.name)
                        ? $"{item.Value.name} [v{item.Value.version}]" : item.Value.name;

                    UI.ChangeText(package.TitleID, item.Value.title_id);
                    UI.ChangeText(package.Region, item.Value.region.ToUpper());
                    UI.ChangeText(package.Title, titleToDisplay);
                    UI.ChangeText(package.Size, IO.FormatByteString(item.Value.size));

                    currentIndex++;
                }
            }
        }

        public static void UpdateScrollbar()
        {
            var controlMenu = FindObjectOfType<ControlMenu>();
            if (controlMenu?.scrollbar != null)
                UI.UpdateScrollbar(controlMenu.scrollbar);
        }
    }

    private static void UpdateGameContentList(IEnumerable<KeyValuePair<string, GameContent>> sortedItemsList)
    {
        GameContentAsList = sortedItemsList
            .Select((item, index) => new KeyValuePair<int, GameContent>(index, item.Value))
            .ToList();
    }

    public static async void UpdateContent(int page)
    {
        currentPage = Mathf.Max(page, 0);
        int startIndex = currentPage * itemsPerPage;
        
        Dictionary<string, GameContent> tempData = new Dictionary<string, GameContent>();

        if (contentFilter == (int)ContentType.ALL)
        {
            var savedData = new Dictionary<string, GameContent>(parsedData);
            
            foreach (ContentType type in Enum.GetValues(typeof(ContentType)))
            {
                if (type != ContentType.Config && type != ContentType.ALL)
                {
                    parsedData.Clear();
                    int result = await JSON.ParseJSON(type, false);
                    if (result != 0)
                    {
                        foreach (var kvp in parsedData)
                        {
                            string key = kvp.Key;
                            if (tempData.ContainsKey(key))
                                key = $"{key}_{type}";
                            tempData[key] = kvp.Value;
                        }
                    }
                }
            }
            
            parsedData = tempData;
        }
        else if (contentFilter == (int)ContentType.Homebrew)
        {
            parsedData.Clear();
            await JSON.ParseJSON(ContentType.Homebrew, false);
            foreach (var kvp in parsedData)
                tempData[kvp.Key] = kvp.Value;

            parsedData.Clear();
            await JSON.ParseJSON(ContentType.Emulators, false);
            foreach (var kvp in parsedData)
            {
                string key = kvp.Key;
                if (tempData.ContainsKey(key))
                    key = $"{key}_emulator";
                tempData[key] = kvp.Value;
            }
            
            parsedData = tempData;
        }
        else
        {
            parsedData.Clear();
            await JSON.ParseJSON((ContentType)contentFilter, false);
        }

        var itemsList = parsedData
            .Where(item => item.Value != null &&
                           !string.IsNullOrEmpty(item.Value.title_id) &&
                           !string.IsNullOrEmpty(item.Value.name) &&
                           !string.IsNullOrEmpty(item.Value.size))
            .ToList();

        if (contentFilter == (int)ContentType.ALL || contentFilter == (int)ContentType.Homebrew)
            itemsList = Filtering.FilterByRegion(itemsList);

        itemsList = Filtering.ApplyFilter(itemsList);

        int filteredOutCount = parsedData.Count - itemsList.Count;
        filteredCount = itemsList.Count;

        var sortedItemsList = Filtering.SortItems(itemsList);

        if (startIndex >= sortedItemsList.Count())
        {
            currentPage = Mathf.Max(0, (sortedItemsList.Count() - 1) / itemsPerPage);
            startIndex = currentPage * itemsPerPage;
        }

        UIManagement.ClearDisplayedItems();

        var itemsToDisplay = sortedItemsList.Skip(startIndex).Take(itemsPerPage).ToList();
        UIManagement.DisplayItems(itemsToDisplay);

        int displayedItemCount = itemsToDisplay.Count;
        for (int i = displayedItemCount; i < Content.PKGs.Count; i++)
        {
            var pkg = Content.PKGs[i];
            if (pkg != null)
            {
                UI.ChangeText(pkg.TitleID, string.Empty);
                UI.ChangeText(pkg.Region, string.Empty);
                UI.ChangeText(pkg.Title, string.Empty);
                UI.ChangeText(pkg.Size, string.Empty);
            }
        }

        UIManagement.UpdateScrollbar();
        UpdateGameContentList(sortedItemsList);
    }

    public async void UpdatePkgCount()
    {
        int totalPKGs = 0;

        var contentTypes = Enum.GetValues(typeof(ContentType))
            .Cast<ContentType>()
            .Where(type => type != ContentType.Config && type != ContentType.ALL)
            .ToList();

        foreach (var type in contentTypes)
        {
            parsedData.Clear();
            await JSON.ParseJSON(type);
            totalPKGs += parsedData.Count;
        }

        filteredCount = Mathf.Max(filteredCount, 0);
        Filtering.RemoveInvalidItems(ref parsedData);
        await JSON.ParseJSON((ContentType)contentFilter);

        int clampPkgCount = Mathf.Clamp(filteredCount - removedCount, 0, filteredCount - removedCount);
        var text = $"Content: {clampPkgCount} ({totalPKGs - removedCount})";
        if (pkgCount.text != text) UI.ChangeText(pkgCount, text);

        int currentPKG = 0;

        if (clampPkgCount > 0)
            currentPKG = Mathf.Clamp(contentScroll + 1, 0, clampPkgCount);

        UI.ChangeText(currentInt, $"{currentPKG} / {clampPkgCount}");

        currentInt.enabled = !(currentPKG == 0 && clampPkgCount == 0);
    }

    public static void HighlightCurrentPkg()
    {
        if (Content.PKGs != null && Content.PKGs.Count > 0)
        {
            currentPkg = Content.PKGs[contentScroll % itemsPerPage];
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
        }

        UpdateContent(contentScroll / itemsPerPage);

        ContentHandler contentHandler =
            FindObjectOfType<ContentHandler>();

        contentHandler?.UpdatePkgCount();
    }
}
