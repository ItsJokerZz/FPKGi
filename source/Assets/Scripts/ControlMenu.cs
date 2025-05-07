using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityOrbisBridge;
using static Background;
using static ContentHandler;
using static JsonData;
using static UOBWrapper;
using static Utilities;
using static Variables;

public class ControlMenu : MonoBehaviour
{
    #region Fields
    [SerializeField]
    private GameObject
        menuCanvas,
        detailsCanvas,
        downloadCanvas,
        cancelCanvas,
        updateCanvas;

    [SerializeField]
    private float
        inputCooldown = 0.20f,
        cooldownTimer = 0.00f;

    [SerializeField]
    public Scrollbar scrollbar;

    #endregion

    #region Variables
    private AudioSource audioSource;

    private bool
        menuLoaded = false, isDownloading = false;

    public static bool reloadTriggered = false;

    private float lastCirclePressTime = -1;
    private bool isDoublePress = false;

    private Coroutine scrollCoroutine;
    private Coroutine downloadCoroutine;

    public string sanitizedFilename;

    #endregion

    #region Coroutine Handling
    private IEnumerator InitializeCoroutine()
    {
        while (!initializedApp)
            yield return null;

        audioSource = GetComponent<AudioSource>();
        menuTexts = new Text[MenuTextObjects.Length];

        for (int i = 0; i < menuTexts.Length; i++)
            menuTexts[i] =
                UI.FindInactiveObjectsByPath(MenuTextObjects[i])?.GetComponent<Text>();

        while (loadedOffline == null || !fullyInitialized)
            yield return null;

        HandleConfiguration();
        UpdateSettingsOptions();

        if (backgroundMusic)
            audioSource.Play();

        Background background =
            FindObjectOfType<Background>();
        background?.InitializePkgContent();
        UIManagement.HighlightCurrentPkg();

        if (enableUpdates)
            yield return CheckForAppUpdates();

        yield return null;
    }

    private void ResetDownloadState()
    {
        if (isConsole)
            UOB.ResetDownloadVars();

        if (downloadCoroutine != null)
        {
            StopCoroutine(downloadCoroutine);
            downloadCoroutine = null;
        }

        isDownloading = false;

        UI.ShowUIState(null);

        if (downloadCanvas != null)
        {
            UI.ChangeText(UI.FindInactiveObjectsByPath("Canvas/Download/Text/Title")?.GetComponent<Text>(), string.Empty);
            UI.ChangeText(UI.FindInactiveObjectsByPath("Canvas/Download/Text/Elapsed")?.GetComponent<Text>(), string.Empty);
            UI.ChangeText(UI.FindInactiveObjectsByPath("Canvas/Download/Text/WriteSpeed")?.GetComponent<Text>(), string.Empty);
            UI.ChangeText(UI.FindInactiveObjectsByPath("Canvas/Download/Text/Percentage")?.GetComponent<Text>(), string.Empty);
            UI.ChangeText(UI.FindInactiveObjectsByPath("Canvas/Download/Text/RemainingTime")?.GetComponent<Text>(), string.Empty);
            UI.FindInactiveObjectsByPath("Canvas/Download/ProgressBar")?.SetActive(false);
        }
    }

    private IEnumerator UpdateDownloadProgress()
    {
        bool hasDownloadCompleted = false,
             downloadErrorOccurred = false;

        string progressPercentage = "0",
               totalFileSize = "0",
               downloadedBytes = "0",
               networkSpeed = "0";

        float lastUpdateTimestamp = Time.time,
              downloadStartTimestamp = Time.time,
              lastWriteTimestamp = Time.time,

              smoothedNetworkSpeed = 0f,
              smoothedWriteSpeed = 0f,

              updateInterval = 0.5f,
              minUpdateInterval = 0.2f,
              maxUpdateInterval = 1.5f,
              ewmaAlpha = 0.25f,

              previousDownloadedBytes = 0f;

        if (isConsole)
            UOB.ResetDownloadVars();

        isDownloading = true;

        var name = UI.FindInactiveObjectsByPath("Canvas/Download/Text/Title")?.GetComponent<Text>();

        UI.ChangeText(name, currentContentItem.Value.name);

        UI.SetFontByText(ref name);
        if (name.font == FindObjectOfType<ContentHandler>()?.Arabic)
            name.fontSize = 28;
        else name.fontSize = 36;

        UI.ShowUIState(downloadCanvas);

        while (isDownloading && !hasDownloadCompleted)
        {
            if (isConsole)
            {
                if (UOB.HasDownloadErrorOccured())
                {
                    downloadErrorOccurred = true;
                    hasDownloadCompleted = true;
                }

                if (UOB.HasDownloadCompleted())
                    hasDownloadCompleted = true;

                float parsedSpeed;
                float.TryParse(Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("speed")),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out parsedSpeed);

                networkSpeed = parsedSpeed.ToString("0.##", CultureInfo.InvariantCulture);

                totalFileSize = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("filesize"));
                downloadedBytes = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("downloaded"));

                progressPercentage = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("progress"));
                progressPercentage = progressPercentage == int.MinValue.ToString() ? "0" : progressPercentage;
            }

            if (Time.time - lastUpdateTimestamp >= updateInterval)
            {
                float elapsedDownloadTime = Time.time - downloadStartTimestamp;

                ulong downloadedBytesValue;
                ulong.TryParse(downloadedBytes, out downloadedBytesValue);

                ulong totalFileSizeValue;
                ulong.TryParse(totalFileSize, out totalFileSizeValue);

                ulong networkSpeedValue;
                ulong.TryParse(networkSpeed, out networkSpeedValue);

                smoothedNetworkSpeed = smoothedNetworkSpeed == 0
                    ? networkSpeedValue : (ulong)(ewmaAlpha * networkSpeedValue
                    + (1 - ewmaAlpha) * smoothedNetworkSpeed);

                updateInterval = smoothedNetworkSpeed > 1024
                    ? Mathf.Max(minUpdateInterval, updateInterval * 0.95f)
                    : Mathf.Min(maxUpdateInterval, updateInterval * 1.05f);

                float timeDifference = Mathf.Max(0.1f, Time.time - lastWriteTimestamp);
                ulong writeSpeed = (ulong)((downloadedBytesValue - previousDownloadedBytes) / timeDifference);

                smoothedWriteSpeed = smoothedWriteSpeed == 0
                    ? writeSpeed : (ulong)(ewmaAlpha * writeSpeed + (1 - ewmaAlpha) * smoothedWriteSpeed);

                previousDownloadedBytes = downloadedBytesValue;

                lastWriteTimestamp = Time.time;

                ulong remainingBytes = totalFileSizeValue - downloadedBytesValue;
                float effectiveSpeed = Math.Min(smoothedNetworkSpeed, smoothedWriteSpeed);

                string formattedEstimatedTimeRemaining
                    = FormatTime(effectiveSpeed > 0 ? remainingBytes / effectiveSpeed : 0);

                float progressAsFloat;
                float.TryParse(progressPercentage, out progressAsFloat);

                progressAsFloat /= 100f;

                UI.ChangeText(downloadCanvas.transform.Find("Text/Elapsed")?.GetComponent<Text>(),
                    $"Elapsed: {FormatTime(elapsedDownloadTime)}");

                UI.ChangeText(downloadCanvas.transform.Find("Text/WriteSpeed")?.GetComponent<Text>(),
                    $"Write Speed: {FormatSpeed(smoothedWriteSpeed, false)}");

                UI.ChangeText(downloadCanvas.transform.Find("Text/Percentage")?.GetComponent<Text>(),
                    $"{progressPercentage}%");

                UI.ChangeText(downloadCanvas.transform.Find("Text/DownloadSpeed")?.GetComponent<Text>(),
                    $"{FormatSpeed(smoothedNetworkSpeed, true)}" +
                    $" - {IO.FormatByteString(downloadedBytesValue)}" +
                    $" of {IO.FormatByteString(totalFileSizeValue)}");

                UI.ChangeText(downloadCanvas.transform.Find("Text/RemainingTime")?.GetComponent<Text>(),
                    $"Remaining: {formattedEstimatedTimeRemaining}");

                var progressBar = UI.FindInactiveObjectsByPath("Canvas/Download/ProgressBar/Progress")?.GetComponent<RectTransform>();

                progressBar.pivot = new Vector2(0f, 0.5f);
                progressBar.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Lerp(10.55f, 735.64f, progressAsFloat) - 10.56f);

                progressBar.anchoredPosition = new Vector2(10.56f, progressBar.anchoredPosition.y);
                UI.FindInactiveObjectsByPath("Canvas/Download/ProgressBar")?.SetActive(true);

                lastUpdateTimestamp = Time.time;
            }

            yield return null;
        }

        ResetDownloadState();

        string downloadedFile = $"{downloadPath}[{currentContentItem.Value.title_id}] {sanitizedFilename}";

        if (IO.IsValidZipArchiveFile(downloadedFile + ".pkg"))
        {
            string archiveFile = downloadedFile + ".zip";
            File.Move(downloadedFile + ".pkg", archiveFile);
            IO.EnsureDirectoryExists(downloadedFile);
            UOB.ExtractZipFile(archiveFile, downloadedFile);

            if (File.Exists(archiveFile))
                File.Delete(archiveFile);

            var pkgFiles = Directory.GetFiles(downloadedFile, "*.pkg", SearchOption.AllDirectories)
                .OrderBy(f =>
                {
                    string directory = Path.GetDirectoryName(f);
                    string dirName = Path.GetFileName(directory);
                    var dirMatch = Regex.Match(dirName, @"\d+");
                    return dirMatch.Success ? int.Parse(dirMatch.Value) : int.MaxValue;
                })
                .OrderBy(f =>
                {
                    string fileName = Path.GetFileNameWithoutExtension(f);
                    var fileMatch = Regex.Match(fileName, @"\d+");
                    return fileMatch.Success ? int.Parse(fileMatch.Value) : int.MaxValue;
                })
                .ThenBy(f => f).ToList();

            foreach (string pkgFile in pkgFiles)
            {
                bool isValidPackage = IO.IsValidPackageFile(pkgFile);

                if (!isValidPackage)
                {
                    try
                    {
                        File.Delete(pkgFile);
                        Print(true, PrintType.Default, "Invalid file header, not installing, and deleting...");
                    }
                    catch { /* do nothing */ }
                }

                if ((hasDownloadCompleted && !downloadErrorOccurred && isValidPackage) && (isConsole && installAfter))
                {
                    Print(true, PrintType.Default, $"Installing package {pkgFile}");
                    UOB.InstallLocalPackage(pkgFile, currentContentItem.Value.name, deleteAfter);

                    if (GoldHEN == true && deleteAfter)
                        Directory.Delete(downloadedFile);
                }
            }
        }
        else
        {
            bool isValidPackage = IO.IsValidPackageFile(downloadedFile + ".pkg");
            if (!isValidPackage)
            {
                try
                {
                    File.Delete(downloadedFile + ".pkg");
                    Print(true, PrintType.Default, "Invalid file header, not installing, and deleting...");
                }
                catch { /* do nothing */ }
            }

            if (hasDownloadCompleted && !downloadErrorOccurred && (isConsole && installAfter) && isValidPackage)
            {
                Print(true, PrintType.Default, $"Installing package [{downloadedFile + ".pkg"}] " +
                    $"from {downloadPath} & {(deleteAfter ? "deleting file after." : "keeping file.")}");

                UOB.InstallLocalPackage(downloadedFile + ".pkg", currentContentItem.Value.name, deleteAfter);
            }
        }

        yield return null;
    }

    public string FormatTime(float totalSeconds)
    {
        if (float.IsNaN(totalSeconds) || totalSeconds == float.MinValue || totalSeconds == float.MaxValue) return "Calculating...";
        return TimeSpan.FromSeconds(totalSeconds).ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    public string FormatSpeed(float bytesPerSecond, bool displayInBits)
    {
        int unitIndex = 0;
        float speed = bytesPerSecond;
        string[] units;

        if (!displayInBits)
            units = new[] { "B/s", "KB/s", "MB/s", "GB/s" };
        else
        {
            speed *= 8;
            units = new[] { "b/s", "Kb/s", "Mb/s", "Gb/s" };
        }

        while (speed >= 1024 && unitIndex < units.Length - 1)
        {
            speed /= 1024;
            unitIndex++;
        }

        int decimalPlaces = unitIndex == 0 ? 0 : unitIndex == 1 ? 1 : 2;

        return $"{speed.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture)} {units[unitIndex]}";
    }

    private IEnumerator ScrollText(Text textComponent, string content)
    {
        float scrollSpeed = 50f;
        float scrollPosition = 0f;

        if (textComponent == null) yield break;

        textComponent.text = content;

        float contentWidth = textComponent.preferredWidth;
        float viewportWidth = textComponent.rectTransform.rect.width;

        if (contentWidth <= viewportWidth)
            yield break;

        string paddedContent = content + "          " + content;

        while (true)
        {
            scrollPosition += scrollSpeed * Time.deltaTime;

            if (scrollPosition >= contentWidth)
                scrollPosition -= contentWidth;

            int charOffset = Mathf.FloorToInt(scrollPosition / (contentWidth / paddedContent.Length));
            int substringLength = Mathf.Min(content.Length, paddedContent.Length - charOffset);

            textComponent.text = paddedContent.Substring(charOffset, substringLength);

            yield return null;
        }
    }

    #endregion

    #region User Input Handling
    private async void HandleUserInput()
    {
        if (cooldownTimer <= 0)
        {
            float horizontalInput = Input.GetAxis("Dpad-X") + Input.GetAxis("KB-X");
            float verticalInput =
              Input.GetAxis("LStick-Y") + Input.GetAxis("Dpad-Y") + Input.GetAxis("Mouse-Y");

            if (!downloadCanvas.activeSelf && !cancelCanvas.activeSelf && !updateCanvas.activeSelf)
            {
                if (Input.GetButtonDown("Triangle"))
                    ToggleMenu(false);

                if (Input.GetButtonDown("Square"))
                {
                    if (downloadCanvas.activeSelf) return;

                    cooldownTimer = inputCooldown;

                    if (menuLoaded) return;

                    if (string.IsNullOrEmpty(currentPkg.TitleID.text)) return;

                    if (detailsCanvas.activeSelf)
                    {
                        UI.ShowUIState(null);
                        return;
                    }
                    else
                    {
                        string url = currentContentItem.Value.cover_url;

                        if (string.IsNullOrEmpty(url) || url == null)
                            coverImage.gameObject.SetActive(false);
                        else
                        {
                            if (URL.IsValidImage(url))
                                SetImageFromURL(url, ref coverImage);
                        }

                        if (currentPkg.TitleID.text == "PKGI13337")
                        {
                            if (Variables.version != latestVersion)
                            {
                                string oldKey = JSON.FindKeyByValue(parsedData, "PKGI13337");
                                if (oldKey != null)
                                {
                                    var pkgiEntry = parsedData[oldKey];

                                    currentContentItem.Value.version = latestVersion.HasValue ? UI.FormatVersion(latestVersion.Value) : UI.FormatVersion(Variables.version);
                                    currentContentItem.Value.release = await DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/getRelease/") ?? "12-25-2024";
                                    currentContentItem.Value.size = await DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/download/size/") ?? "75000000";

                                    GameContent content = pkgiEntry;

                                    string newKey = await DownloadAsBytes("https://www.itsjokerzz.site/projects/FPKGi/download/?echo=1");

                                    parsedData.Remove(oldKey);
                                    parsedData[newKey] = content;

                                    var output = new
                                    {
                                        DATA = new Dictionary<string, GameContent>
                                        {
                                            { newKey, content }
                                        }
                                    };

                                    string jsonContent = JsonConvert.SerializeObject(output, Formatting.Indented);
                                    File.WriteAllText(IO.GetFilePath(ContentType.Homebrew), jsonContent);
                                }
                            }
                        }

                        UI.ShowUIState(detailsCanvas);

                        if (detailsCanvas == null) return;

                        Transform textTransform = detailsCanvas.transform.Find("Text");

                        if (textTransform == null) return;

                        Text name = textTransform.Find("Name")?.GetComponent<Text>();
                        Text title_id = textTransform.Find("TitleID")?.GetComponent<Text>();
                        Text version = textTransform.Find("AppVersion")?.GetComponent<Text>();
                        Text min_fw = textTransform.Find("MinFWReq")?.GetComponent<Text>();
                        Text release = textTransform.Find("Release")?.GetComponent<Text>();

                        UI.ChangeText(name, currentContentItem.Value.name);
                        UI.SetFontByText(ref name);

                        if (name.font == FindObjectOfType<ContentHandler>()?.Arabic)
                            name.fontSize = 32;
                        else name.fontSize = 42;

                        UI.ChangeText(title_id, $"Title ID: {currentContentItem.Value.title_id} " +
                                $"[{currentContentItem.Value.region}]");

                        UI.ChangeText(version, $"Package Version: {currentContentItem.Value.version}");
                        UI.ChangeText(min_fw, $"Required Firmware: {currentContentItem.Value.min_fw}");
                        UI.ChangeText(release, $"Release Date: {currentContentItem.Value.release}");

                        if (scrollCoroutine != null)
                        {
                            StopCoroutine(scrollCoroutine);
                            scrollCoroutine = null;
                        }

                        scrollCoroutine = StartCoroutine(ScrollText(name, currentContentItem.Value.name));
                    }
                }

                if (menuCanvas.activeSelf && !downloadCanvas.activeSelf)
                {
                    if (verticalInput != 0) NavigateMenu(verticalInput);
                    if (horizontalInput != 0 && selectedIndex == 4
                        || selectedIndex == 11) ScrollOption(horizontalInput);
                }
                else
                {
                    if (!detailsCanvas.activeSelf && !downloadCanvas.activeSelf && !cancelCanvas.activeSelf)
                    {
                        if (Input.GetButtonDown("L1")) ScrollOption(-1);
                        if (Input.GetButtonDown("R1")) ScrollOption(1);

                        if (Input.GetButtonDown("L1") || Input.GetButtonDown("R1"))
                        {
                            cooldownTimer = inputCooldown;
                            SaveConfiguration();
                        }

                        if (Input.GetButtonDown("Touchpad")) // make UOB HELPER FUNC FOR THIS (KB INPUT HANDLING)
                        {
                            if (!isConsole) return;

                            IntPtr kbInput = UOB.GetKeyboardInput("Search Content (By name or title ID)", "");
                            string kbOutput = Marshal.PtrToStringAnsi(kbInput);

                            if (string.IsNullOrEmpty(kbOutput) || kbOutput == "NULL")
                                searchFilter = string.Empty;
                            else
                                searchFilter = kbOutput;
                        }
                    }

                    if (!detailsCanvas.activeSelf && !downloadCanvas.activeSelf)
                    {
                        int scrollAmount = itemsPerPage;

                        if (Input.GetAxis("L2") != 0)
                        {
                            cooldownTimer = inputCooldown;
                            contentScroll -= scrollAmount;

                            if (contentScroll < 0) contentScroll = filteredCount > 0 ?
                                Mathf.Max(0, filteredCount - (filteredCount % itemsPerPage == 0
                                ? itemsPerPage : filteredCount % itemsPerPage)) : 0;
                        }

                        if (Input.GetAxis("R2") != 0)
                        {
                            cooldownTimer = inputCooldown;

                            contentScroll += scrollAmount;
                            if (contentScroll >= filteredCount)
                                contentScroll = 0;
                        }

                        if (verticalInput != 0)
                        {
                            cooldownTimer = inputCooldown;

                            float clampedValue = Mathf.Clamp(verticalInput, 0f, 1f);
                            bool scrollDown = clampedValue == 0,
                              scrollUp = clampedValue > 0f;

                            if (Content.PKGs != null && Content.PKGs.Count > 0)
                            {
                                foreach (var pkg in Content.PKGs)
                                {
                                    if (pkg != null && pkg.TitleID.enabled)
                                    {
                                        pkg.TitleID.color = Color.white;
                                        pkg.Region.color = Color.white;
                                        pkg.Title.color = Color.white;
                                        pkg.Size.color = Color.white;
                                    }
                                }

                                var previousPkg = Content.PKGs[contentScroll % itemsPerPage];
                                if (previousPkg != null && previousPkg.TitleID.enabled)
                                    previousPkg.TitleID.color = Color.white;
                            }

                            if (scrollDown)
                            {
                                contentScroll++;

                                if (contentScroll >= filteredCount) contentScroll = 0;
                            }
                            else if (scrollUp)
                            {
                                contentScroll--;

                                if (contentScroll < 0) contentScroll
                                        = filteredCount > 0 ? filteredCount - 1 : 0;
                            }
                        }
                    }
                }
            }

            if (Input.GetButtonDown("X"))
            {
                sanitizedFilename =
                   IO.SanitizeFilename(currentContentItem.Value.name);

                if (UI.IsNonEnglish(sanitizedFilename))
                    sanitizedFilename = $"content-{contentScroll+1}";

                string packagePath =
                    $"{downloadPath}[{currentContentItem.Value.title_id}] {sanitizedFilename}.pkg";

                if (menuLoaded)
                    ExecuteMenuItemAction();
                else
                {
                    if (cancelCanvas.activeSelf)
                    {
                        if (isConsole)
                        {
                            UOB.CancelDownload();

                            if (deleteOnCancel && IO.DoesPathExist($"{packagePath}.resume"))
                                File.Delete($"{packagePath}.resume");
                        }

                        if (downloadCoroutine != null)
                        {
                            StopCoroutine(downloadCoroutine);
                            downloadCoroutine = null;
                        }

                        UI.ShowUIState(null);

                        isDownloading = false;

                        return;
                    }

                    if (updateCanvas.activeSelf)
                    {
                        if (isConsole)
                            UOB.UpdateViaHomebrewStore("PKGI13337");

                        UI.ShowUIState(null);

                        return;
                    }

                    if (!downloadCanvas.activeSelf)
                    {
                        if (currentPkg.TitleID.text == "PKGI13337")
                        {
                            await CheckForAppUpdates();

                            return;
                        }

                        var downloadlink = URL.DecryptBase64(Uri.EscapeUriString(Uri.UnescapeDataString(currentContentItem.Key)));

                        if (isConsole)
                            Print(true, PrintType.Warning, $"Attempting to download PKG from: {downloadlink}"); // move to UOB

                        if (directDownload)
                        {
                            if (currentPkg.Downloaded.text == "+")
                            {
                                Print(true, PrintType.Default, $"Installing package [{packagePath}] from {downloadPath} & {(deleteAfter ? "deleting file after." : "keeping file.")}");
                                UOB.InstallLocalPackage(packagePath, currentContentItem.Value.name, deleteAfter);
                            }
                            else
                            {
                                if (isConsole)
                                    UOB.DownloadPkgFile(downloadlink, downloadPath, $"[{currentContentItem.Value.title_id}] {sanitizedFilename}", true, "NULL");
                                Text name = downloadCanvas.transform.Find("Text/Title")?.GetComponent<Text>();

                                downloadCoroutine = StartCoroutine(UpdateDownloadProgress());

                                if (scrollCoroutine != null)
                                {
                                    StopCoroutine(scrollCoroutine);
                                    scrollCoroutine = null;
                                }

                                scrollCoroutine = StartCoroutine(ScrollText(name, currentContentItem.Value.name));
                            }
                        }
                        else
                        {
                            if (isConsole)
                                UOB.InstallWebPackage(downloadlink, sanitizedFilename, currentContentItem.Value.cover_url);
                        }
                    }
                }
            }

            if (Input.GetButtonDown("Circle"))
            {
                cooldownTimer = inputCooldown;
                float currentTime = Time.unscaledTime;

                if (currentTime - lastCirclePressTime <= 0.4f)
                {
                    isDoublePress = true;
                    SaveConfiguration();

                    if (isConsole)
                        UOB.ExitApplication();

#if UNITY_EDITOR_WIN
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                }
                else
                {
                    isDoublePress = false;
                }

                lastCirclePressTime = currentTime;

                if (!isDoublePress)
                {
                    if (cancelCanvas.activeSelf)
                    {
                        UI.ShowUIState(downloadCanvas);
                        return;
                    }

                    if (updateCanvas.activeSelf)
                    {
                        UI.ShowUIState(null);
                        return;
                    }

                    if (menuCanvas.activeSelf)
                        ToggleMenu(true);
                    else if (detailsCanvas.activeSelf)
                    {
                        UI.ShowUIState(null);

                        if (scrollCoroutine != null)
                        {
                            StopCoroutine(scrollCoroutine);
                            scrollCoroutine = null;
                        }
                    }
                    else if (cancelCanvas.activeSelf)
                    {
                        if (isDownloading)
                            UI.ShowUIState(downloadCanvas);
                        else
                            UI.ShowUIState(null);
                    }
                    else if (isDownloading)
                        UI.ShowUIState(cancelCanvas);
                }
            }

        }

    }

    private void NavigateMenu(float verticalInput)
    {
        cooldownTimer = inputCooldown;

        Menu.ResetMenuItemsToDefault();

        selectedIndex =
          (selectedIndex + (verticalInput > 0 ? -1 : 1) +
            menuTexts.Length) % menuTexts.Length;

        Menu.HighlightMenuItem(selectedIndex);
    }

    private void ScrollOption(float horizontalInput, int textArrayInt = -1)
    {
        if (textArrayInt == -1) textArrayInt = selectedIndex;
        string currentText = menuTexts[textArrayInt].text;

        if (menuLoaded && textArrayInt == 9)
        {
            string rightTriangle = "> ";
            string directDownloadText = $"{rightTriangle}Direct Download";
            string backDownloadText = $"{rightTriangle}Back. Download";

            if (horizontalInput != 0)
            {
                cooldownTimer = inputCooldown;

                string newText = currentText == directDownloadText ?
                  backDownloadText : directDownloadText;

                UI.ChangeText(menuTexts, textArrayInt, newText);

                if (newText == directDownloadText)
                    directDownload = true;
                else directDownload = false;
            }
        }
        else
        {
            if (!menuLoaded || textArrayInt == 4)
            {
                cooldownTimer = inputCooldown;

                if (horizontalInput < 0)
                    ScrollOptionLeft();
                else if (horizontalInput > 0)
                    ScrollOptionRight();
            }
        }
    }

    private void ExecuteMenuItemAction()
    {
        cooldownTimer = inputCooldown;

        var region = string.Empty;
        switch (selectedIndex)
        {
            case 5:
                region = "USA";
                break;
            case 6:
                region = "Europe";
                break;
            case 7:
                region = "Japan";
                break;
            case 8:
                region = "Asia";
                break;
        }

        if (menuLoaded)
            switch (selectedIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    SetSortOption(sortByOptions[(int)(SortBy)selectedIndex], selectedIndex);
                    reloadTriggered = true;
                    break;

                case 4:
                    ScrollOption(1);
                    break;

                case 5:
                case 6:
                case 7:
                case 8:
                    ToggleRegionOption(selectedIndex, region);
                    reloadTriggered = true;
                    break;

                case 9:
                    ScrollOption(1);
                    break;

                case 10: // make UOB HELPER FUNC FOR THIS (KB INPUT HANDLING)
                    ToggleOption(10, "Install Once Done", ref installAfter);
                    break;

                case 11:
                    if (GoldHEN == true || !isConsole)
                        ToggleOption(11, "Delete After Install", ref deleteAfter);
                    break;

                case 12:
                    ToggleOption(12, "Delete On Cancel", ref deleteOnCancel);
                    break;

                case 13:
                    ToggleOption(13, "Populate Via Web", ref populateViaWeb);
                    toggleBackToLocal = true;
                    reloadTriggered = true;
                    break;

                case 14:
                    ToggleOption(14, "Enable App Updates", ref enableUpdates);
                    break;

                case 15:
                    ToggleOption(15, "Background Music", ref backgroundMusic);

                    if (!backgroundMusic)
                        audioSource.Pause();
                    else
                        audioSource.Play();
                    break;

                case 16:
                    if (!isConsole)
                        return;

                    IntPtr _kbInput = UOB.GetKeyboardInput("Enter an image path: (local path or URL)",
                      string.IsNullOrEmpty(background_uri) || background_uri == null ? Path.Combine(directoryPath,
                        $"Backgrounds" + (isConsole ? "/" : "\\")) : background_uri
                    );

                    string _kbOutput = Marshal.PtrToStringAnsi(_kbInput);

                    if (string.IsNullOrEmpty(_kbOutput) || _kbOutput == "NULL")
                    {
                        background_uri = null;
                        background.gameObject.SetActive(false);
                        return;
                    }

                    if (!File.Exists(_kbOutput))
                    {
                        if (URL.IsValidImage(_kbOutput))
                            SetImageFromURL(_kbOutput, ref background);
                    }
                    else IO.LoadImage(_kbOutput, ref background);

                    break;


                case 17: // make UOB HELPER FUNC FOR THIS (KB INPUT HANDLING)
                    if (!isConsole) return;
                    IntPtr kbInput = UOB.GetKeyboardInput("Set Download Location...", downloadPath);
                    string kbOutput = Marshal.PtrToStringAnsi(kbInput);

                    if (string.IsNullOrEmpty(kbOutput) ||
                      kbOutput == "NULL") return;
                    else downloadPath = kbOutput;
                    reloadTriggered = true;
                    break;

                case 18:
                    toggleBackToLocal = true;
                    HandleConfiguration();
                    UpdateSettingsOptions();
                    ToggleMenu();
                    break;

            }
    }

    private void SetSortOption(string optionName, int index)
    {
        if (menuTexts[index] != null && menuTexts[index].text.Contains("^"))
        {
            UI.ChangeText(menuTexts, index, $"v {optionName}");

            ascending = false;
        }
        else if (menuTexts[index] != null && menuTexts[index].text.Contains("v"))
        {
            UI.ChangeText(menuTexts, index, $"^ {optionName}");

            ascending = true;
        }
        else
        {
            for (int i = 0; i <= 3; i++)
            {
                if (menuTexts[i] != null)
                    UI.ChangeText(menuTexts, i, sortByOptions[i]);
            }

            if (ascending)
            {
                if (menuTexts[index] != null)
                    UI.ChangeText(menuTexts, index, $"^ {optionName}");
            }
            else
            {
                if (menuTexts[index] != null)
                    UI.ChangeText(menuTexts, index, $"v {optionName}");
            }

            sortCriteria = index;
        }
    }

    #endregion

    #region UI Interaction
    public void UpdateSettingsOptions()
    {
        UI.ChangeText(menuTexts, 4, $"> {contentOptions[contentFilter]}");

        if (menuTexts[4] != null && menuTexts[4].text.Contains("^"))
            UI.ChangeText(menuTexts, 4, $"v {sortByOptions[sortCriteria]}");
        else if (menuTexts[4] != null && menuTexts[4].text.Contains("v"))
            UI.ChangeText(menuTexts, 4, $"^ {sortByOptions[sortCriteria]}");

        for (int i = 5; i < 9; i++)
        {
            string regionName = menuTexts[i].text.Substring(2);
            bool isRegionSelected = Array.Exists(filteredRegions, element => element == regionName);
            UI.ChangeText(menuTexts, i, isRegionSelected ? $"x {regionName}" : $"o {regionName}");
        }

        Text directDownloadText = UI.FindInactiveObjectsByPath("Canvas/Menu/Text/UserPreferences/DirectDownload")?.GetComponent<Text>();
        UI.ChangeText(directDownloadText, directDownload ? "> Direct Download" : "> Back. Download");

        Text installAfterText = UI.FindInactiveObjectsByPath("Canvas/Menu/Text/UserPreferences/InstallOnceDone")?.GetComponent<Text>();
        UI.ChangeText(installAfterText, installAfter ? "x Install Once Done" : "o Install Once Done");

        Text deleteAfterText = UI.FindInactiveObjectsByPath("Canvas/Menu/Text/UserPreferences/DeleteAfterInstall")?.GetComponent<Text>();
        UI.ChangeText(deleteAfterText, etaHEN == true ? "x Delete After Install" : (deleteAfter ? "x Delete After Install" : "o Delete After Install"));

        Text deleteOnCancelText = UI.FindInactiveObjectsByPath("Canvas/Menu/Text/UserPreferences/DeleteOnCancel")?.GetComponent<Text>();
        UI.ChangeText(deleteOnCancelText, deleteOnCancel ? "x Delete On Cancel" : "o Delete On Cancel");

        Text populateViaWebText = UI.FindInactiveObjectsByPath("Canvas/Menu/Text/UserPreferences/PopulateViaWeb")?.GetComponent<Text>();
        UI.ChangeText(populateViaWebText, populateViaWeb ? "x Populate Via Web" : "o Populate Via Web");

        Text enableAppUpdatesText = UI.FindInactiveObjectsByPath("Canvas/Menu/Text/UserPreferences/EnableAppUpdates")?.GetComponent<Text>();
        UI.ChangeText(enableAppUpdatesText, enableUpdates ? "x Enable App Updates" : "o Enable App Updates");

        Text backgroundMusicText = UI.FindInactiveObjectsByPath("Canvas/Menu/Text/UserPreferences/BackgroundMusic")?.GetComponent<Text>();
        UI.ChangeText(backgroundMusicText, backgroundMusic ? "x Background Music" : "o Background Music");
    }

    public void ToggleMenu(bool resetSettings = false)
    {
        if (downloadCanvas.activeSelf) return;

        if (!menuLoaded)
        {
            pS.ascending = ascending;
            pS.sortCriteria = sortCriteria;
            pS.contentFilter = contentFilter;
            pS.filteredRegions = filteredRegions;

            if (pS.previousBg != null)
                pS.background_uri = background_uri;

            if (pS.previousBg != null)
                pS.previousBg = background.texture;

            pS.directDownload = directDownload;
            pS.installAfter = installAfter;
            pS.deleteAfter = deleteAfter;
            pS.deleteOnCancel = deleteOnCancel;
            pS.populateViaWeb = populateViaWeb;
            pS.enableUpdates = enableUpdates;
            pS.backgroundMusic = backgroundMusic;
        }

        cooldownTimer = inputCooldown;
        menuLoaded = !menuLoaded;

        if (menuLoaded)
            UI.ShowUIState(menuCanvas);
        else
        {
            UI.ShowUIState(null);

            if (resetSettings)
            {
                ascending = pS.ascending;
                sortCriteria = pS.sortCriteria;
                contentFilter = pS.contentFilter;
                filteredRegions = pS.filteredRegions;

                if (pS.previousBg != null)
                    background_uri = pS.background_uri;

                if (pS.previousBg != null)
                    background.texture = pS.previousBg;

                directDownload = pS.directDownload;
                installAfter = pS.installAfter;
                deleteAfter = pS.deleteAfter;
                deleteOnCancel = pS.deleteOnCancel;
                populateViaWeb = pS.populateViaWeb;
                enableUpdates = pS.enableUpdates;
                backgroundMusic = pS.backgroundMusic;

                UpdateSettingsOptions();

                background.gameObject.SetActive(URL.IsValidURI(background_uri) || URL.IsValidImage(background_uri));

                if (!backgroundMusic)
                    audioSource.Stop();
                else
                    audioSource.Play();
            }
            else
                SaveConfiguration();
        }
    }

    private void ScrollOptionLeft() => ScrollOptionToOption(false);

    private void ScrollOptionRight() => ScrollOptionToOption(true);

    private void ScrollOptionToOption(bool scrollRight)
    {
        Text selectionText = UI.FindInactiveObjectsByPath("Canvas/Menu/Text/FilteringOptions/Content/Selection")?.GetComponent<Text>();

        const string rightTriangle = "> ";
        string currentText = selectionText.text.TrimStart(rightTriangle[0], ' ');

        int currentIndex = Array.IndexOf(contentOptions, currentText);

        if (currentIndex == -1) currentIndex = contentFilter;

        int newIndex;
        do
        {
            newIndex = scrollRight ? (currentIndex + 1) % contentOptions.Length :
                (currentIndex - 1 + contentOptions.Length) % contentOptions.Length;
            currentIndex = newIndex;
        } while (GoldHEN == true && contentOptions[newIndex] == "PS5");

        contentFilter = newIndex;

        UI.ChangeText(selectionText, $"{rightTriangle}{contentOptions[newIndex]}");
    }

    public static void AddRegion(string region)
    {
        if (!Array.Exists(filteredRegions, element => element == region))
        {
            Array.Resize(ref filteredRegions, filteredRegions.Length + 1);
            filteredRegions[filteredRegions.Length - 1] = region;
        }
    }

    public static void RemoveRegion(string region)
    {
        int index = Array.IndexOf(filteredRegions, region);

        if (index != -1)
        {
            for (int i = index; i < filteredRegions.Length - 1; i++)
                filteredRegions[i] = filteredRegions[i + 1];

            Array.Resize(ref filteredRegions, filteredRegions.Length - 1);
        }
    }

    private void ToggleRegionOption(int index, string regionName)
    {
        bool isRegionSelected = menuTexts[index].text.Contains($"x {regionName}");

        ToggleOption(index, regionName, ref isRegionSelected);

        if (isRegionSelected) AddRegion(regionName);
        else RemoveRegion(regionName);
    }

    private void ToggleOption(int index, string text, ref bool toggle)
    {
        string currentState = menuTexts[index].text;
        string checkedState = $"x {text}";
        string uncheckedState = $"o {text}";

        string newState = currentState == checkedState ? uncheckedState : checkedState;

        UI.ChangeText(menuTexts, index, newState);

        toggle = newState == checkedState;
    }

    #endregion

    private IEnumerator Start()
    {
        string previousText = string.Empty;

        StartCoroutine(InitializeCoroutine());

        while (!fullyInitialized)
            yield return null;

        while (true)
        {
            string currentText = $"{contentOptions[contentFilter]}";

            HandleUserInput();

            if (cooldownTimer > 0)
                cooldownTimer = Mathf.Clamp(cooldownTimer - Time.deltaTime, 0, inputCooldown);

            var textComponent =
                UI.FindInactiveObjectsByPath("Canvas/Main/ContentSort")?.GetComponent<Text>();

            if (currentText != previousText)
            {
                textComponent.text = $"{currentText} Content";
                previousText = currentText;
                reloadTriggered = true;
            }

            UIManagement.HighlightCurrentPkg();

            yield return null;
        }
    }

}