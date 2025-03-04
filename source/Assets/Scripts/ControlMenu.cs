using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        updateCanvas,
        closeCanvas,

        mainControls,
        menuControls,
        detailControls,
        downloadControls,
        closeControls;

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
        menuLoaded = false,
        downloadCanvasWasActive = false,
        isDownloading = false;

    public static bool reloadTriggered = false;

    private Coroutine scrollCoroutine;
    private Coroutine downloadCoroutine;

    #endregion

    #region Coroutine Handling
    private IEnumerator InitializeCoroutine()
    {
        while (!initialized) yield return null; // not really needed

        audioSource = GetComponent<AudioSource>();

        if (backgroundMusic) audioSource.Play();

        while (!menuCanvas.activeSelf) yield return null; // shouldnt be needed either tbh

        Variables.menuCanvas = menuCanvas; // global def. can remove (used to view in editor)
        Variables.menuControls = menuControls; // global def. can remove (used to view in editor)

        UpdateSettingsOptions();
    }

    private void InitializeMenuItems()
    {
        menuTexts = new Text[MenuTextObjects.Length];

        for (int i = 0; i < menuTexts.Length; i++)
        {
            menuTexts[i] = UI.FindInactiveObjectsByPath(MenuTextObjects[i])?.GetComponent<Text>();

            if (menuTexts[i] == null)
                Print(true, PrintType.Error, $"Text component not found for path: {MenuTextObjects[i]}");
        }
    }

    private void ResetDownloadState()
    {
        downloadCanvasWasActive = false;

        if (UnityEngine.Application.platform == RuntimePlatform.PS4)
            UOB.ResetDownloadVars();

        if (downloadCoroutine != null)
        {
            StopCoroutine(downloadCoroutine);
            downloadCoroutine = null;
        }

        isDownloading = false;

        ShowUIState(null, mainControls);

        if (downloadCanvas != null)
        {
            Transform textTransform = downloadCanvas.transform.Find("Text");
            if (textTransform != null)
            {
                Text title = textTransform.Find("Title")?.GetComponent<Text>();
                Text info = textTransform.Find("Info")?.GetComponent<Text>();

                if (title != null) UI.ChangeText(title, string.Empty);
                if (info != null) UI.ChangeText(info, string.Empty);
            }
        }
    }

    private IEnumerator UpdateDownloadProgress()
    {
        bool hasCompleted = false;
        bool errorOccurred = false;
        string progressValue = string.Empty;
        string fileSizeBytes = "0";
        string downloadBytes = "0";
        string progress = "???";
        string speed = FormatSpeed(0);
        float lastUpdateTime = Time.time;
        float downloadStartTime = Time.time;
        float updateInterval = 0.5f;
        float minUpdateInterval = 0.2f;
        float maxUpdateInterval = 1.5f;
        float smoothedDownloadSpeed = 0f;
        float ewmaAlpha = 0.25f;

        if (UnityEngine.Application.platform == RuntimePlatform.PS4)
            UOB.ResetDownloadVars();

        isDownloading = true;

        while (isDownloading && !hasCompleted)
        {
            if (UnityEngine.Application.platform == RuntimePlatform.PS4)
            {
                if (UOB.HasDownloadErrorOccured())
                {
                    errorOccurred = true;
                    hasCompleted = true;
                }

                if (UOB.HasDownloadCompleted())
                    hasCompleted = true;
                
                speed = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("speed"));
                fileSizeBytes = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("filesize"));
                downloadBytes = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("downloaded"));
                progress = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("progress"));
                progressValue = progress == int.MinValue.ToString() ? "0" : progress;
            }

            if (Time.time - lastUpdateTime >= updateInterval)
            {
                float downloadedBytes = 0f;
                float totalFileSize = 0f;
                float elapsedDownloadTime = Time.time - downloadStartTime;
                float elapsedDownloadedBytes = float.TryParse(downloadBytes, out downloadedBytes) ? downloadedBytes : 0;
                float totalFileSizeBytes = float.TryParse(fileSizeBytes, out totalFileSize) ? totalFileSize : 0;

                UI.ChangeText(downloadCanvas.transform.Find("Text/Title")?.GetComponent<Text>(), currentContentItem.Value.name);

                float parsedSpeed;
                if (float.TryParse(speed, out parsedSpeed))
                {
                    smoothedDownloadSpeed = smoothedDownloadSpeed == 0f ? parsedSpeed
                        : smoothedDownloadSpeed * (1 - ewmaAlpha) + parsedSpeed * ewmaAlpha;
                }
                else
                    smoothedDownloadSpeed = 0f;

                updateInterval = smoothedDownloadSpeed > 1024
                    ? Mathf.Max(minUpdateInterval, updateInterval * 0.95f) : Mathf.Min(maxUpdateInterval, updateInterval * 1.05f);

                float remainingBytes = totalFileSizeBytes - elapsedDownloadedBytes;
                float estimatedTime = smoothedDownloadSpeed > 0 ? remainingBytes / smoothedDownloadSpeed : float.MaxValue;
                string estimatedTimeFormatted = estimatedTime < float.MaxValue ? FormatTime(estimatedTime) : "Calculating...";

                string downloadSpeedText = FormatSpeed(smoothedDownloadSpeed);
                string elapsedTimeFormatted = FormatTime(elapsedDownloadTime);
                string downloadedFormatted = IO.FormatByteString(elapsedDownloadedBytes);
                string fileSizeFormatted = IO.FormatByteString(totalFileSizeBytes);

                UI.ChangeText(downloadCanvas.transform.Find("Text/Info")?.GetComponent<Text>(),
                    $"Download Speed: {downloadSpeedText}\n" +
                    $"Remaining Time: {estimatedTimeFormatted}\n\n\n" +
                    $"Elapsed Download Time: {elapsedTimeFormatted}\n" +
                    $"Downloaded: {downloadedFormatted} " +
                    $"/ {fileSizeFormatted} ({progressValue}%)");

                if (!cancelCanvas.activeSelf) ShowUIState(downloadCanvas, downloadControls);

                lastUpdateTime = Time.time;
            }

            yield return null;
        }
       
        ResetDownloadState();
       
        string sanitizedFilename = currentContentItem.Value.name;
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            sanitizedFilename = sanitizedFilename.Replace(invalidChar.ToString(), string.Empty);

        if (sanitizedFilename.Length > 255) sanitizedFilename = sanitizedFilename.Substring(0, 255);
        string packagePath = $"{downloadPath}{sanitizedFilename} [{currentContentItem.Value.title_id}].pkg";

        bool isValidPkg = IO.IsValidPackageFile(packagePath);

        if (!isValidPkg)
        {
            try {
                File.Delete(packagePath);
                Print(true, PrintType.Default, $"Invalid package file header, not installing, and deleting...");

            }
            catch { /* do nothing */ }
        }
            
        if (hasCompleted && !errorOccurred && (UnityEngine.Application.platform == 
            RuntimePlatform.PS4 && installAfter) && isValidPkg)
        {
            Print(true, PrintType.Default, $"Installing package [{packagePath}] from " +
                $"{downloadPath} & {(deleteAfter ? "deleting file after." : "keeping file.")}");

            UOB.InstallLocalPackage(packagePath, currentContentItem.Value.name, deleteAfter);
        }

        yield return null;
    }

    private string FormatTime(float totalSeconds)
    {
        if (float.IsNaN(totalSeconds) || totalSeconds == float.MaxValue)
            return "Calculating...";

        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
        return time.ToString(@"hh\:mm\:ss");
    }

    private string FormatSpeed(float bytesPerSecond)
    {
        int unitIndex = 0;

        string[] units = { "b/s", "Kb/s", "Mb/s" };
        while (bytesPerSecond >= 1024 && unitIndex < units.Length - 1)
        {
            bytesPerSecond /= 1024;
            unitIndex++;
        }

        int decimalPlaces = unitIndex == 0 ? 0 : unitIndex == 1 ? 1 : 2;

        return $"{bytesPerSecond.ToString($"F{decimalPlaces}")} {units[unitIndex]}";
    }

    private IEnumerator ScrollText(Text textComponent, string content, float scrollDuration = 12f, float pauseDuration = 0f)
    {
        if (textComponent == null) yield break;

        textComponent.text = content;
        float contentWidth = textComponent.preferredWidth;
        float viewportWidth = textComponent.rectTransform.rect.width;

        if (contentWidth <= viewportWidth)
            yield break;

        string paddedContent = content + "          " + content;
        int totalChars = paddedContent.Length;

        while (true)
        {
            yield return new WaitForSeconds(pauseDuration);

            float startTime = Time.time;
            float endTime = startTime + scrollDuration;

            while (Time.time < endTime)
            {
                float progress = (Time.time - startTime) / scrollDuration;
                int charOffset = Mathf.FloorToInt(progress * content.Length);
                textComponent.text = paddedContent.Substring(charOffset, content.Length);
                yield return null;
            }

            textComponent.text = content;
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

            if (!closeCanvas.activeSelf && !downloadCanvas.activeSelf
                && !cancelCanvas.activeSelf && !updateCanvas.activeSelf)
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
                        ShowUIState(null, mainControls);
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

                        ShowUIState(detailsCanvas, detailControls);

                        if (detailsCanvas == null) return;

                        Transform textTransform = detailsCanvas.transform.Find("Text");

                        if (textTransform == null) return;

                        Text name = textTransform.Find("Name")?.GetComponent<Text>();
                        Text title_id = textTransform.Find("TitleID")?.GetComponent<Text>();
                        Text version = textTransform.Find("AppVersion")?.GetComponent<Text>();
                        Text min_fw = textTransform.Find("MinFWReq")?.GetComponent<Text>();
                        Text release = textTransform.Find("Release")?.GetComponent<Text>();

                        UI.ChangeText(name, currentContentItem.Value.name);
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
                    if (!closeCanvas.activeSelf && !detailsCanvas.activeSelf
                        && !downloadCanvas.activeSelf && !cancelCanvas.activeSelf)
                    {
                        if (Input.GetButtonDown("L1")) ScrollOption(-1);
                        if (Input.GetButtonDown("R1")) ScrollOption(1);

                        if (Input.GetButtonDown("L1") || Input.GetButtonDown("R1"))
                        {
                            cooldownTimer = inputCooldown;

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

                        if (Input.GetButtonDown("L1") || Input.GetButtonDown("R1"))
                        {
                            cooldownTimer = inputCooldown;
                            SaveConfiguration();
                        }

                        if (Input.GetButtonDown("Touchpad")) // make UOB HELPER FUNC FOR THIS (KB INPUT HANDLING)
                        {
                            if (UnityEngine.Application.platform != RuntimePlatform.PS4) return;
                            IntPtr kbInput = UOB.GetKeyboardInput(SearchText, "");
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
                if (menuLoaded)
                    ExecuteMenuItemAction();
                else
                {
                    if (cancelCanvas.activeSelf)
                    {
                        if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                        {
                            UOB.CancelDownload();
                            if (downloadCoroutine != null)
                            {
                                StopCoroutine(downloadCoroutine);
                                downloadCoroutine = null;
                            }
                        }

                        ShowUIState(null, mainControls);
                        downloadCanvasWasActive = false;
                        isDownloading = false;
                        return;
                    }

                    if (updateCanvas.activeSelf)
                    {
                        if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                        {
                            UOB.EnterSandbox();
                            Store.UpdateApplication();
                        }

                        ShowUIState(null, mainControls);
                        return;
                    }

                    if (closeControls.activeSelf && closeCanvas.activeSelf)
                    {
                        SaveConfiguration();

                        if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                        {
                            UOB.CancelDownload();

                            if (downloadCoroutine != null)
                            {
                                StopCoroutine(downloadCoroutine);
                                downloadCoroutine = null;
                            }
                        }

                        isDownloading = false;
                        downloadCanvasWasActive = false;

                        if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                            UOB.ExitApplication();

#if UNITY_EDITOR_WIN
                        UnityEditor.EditorApplication.isPlaying = false;
#endif
                        return;
                    }

                    if (!downloadCanvasWasActive)
                    {
                        if (downloadCanvas.activeSelf) return;

                        if (currentPkg.TitleID.text == "PKGI13337")
                        {
                            ShowUIState(null, mainControls);
                            await CheckForAppUpdates();
                            return;
                        }

                        var downloadlink = Uri.EscapeUriString(Uri.UnescapeDataString(currentContentItem.Key));
                        
                        Print(true, PrintType.Warning, "Attempting to download PKG from: " + downloadlink); // move to UOB

                        string sanitizedFilename = currentContentItem.Value.name;
                        foreach (char invalidChar in Path.GetInvalidFileNameChars())
                            sanitizedFilename = sanitizedFilename.Replace(invalidChar.ToString(), string.Empty);

                        if (sanitizedFilename.Length > 255)
                            sanitizedFilename = sanitizedFilename.Substring(0, 255);

                        if (directDownload)
                        {
                            if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                                UOB.CancelDownload();

                            if (downloadCoroutine != null)
                            {
                                StopCoroutine(downloadCoroutine);
                                downloadCoroutine = null;
                            }

                            downloadCanvas.SetActive(false);
                            downloadControls.SetActive(false);

                            downloadCanvasWasActive = false;
                            isDownloading = false;

                            if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                                UOB.DownloadPkgFile(downloadlink, downloadPath,
                                    $"{sanitizedFilename} [{currentContentItem.Value.title_id}]", true, "NULL");

                            downloadCoroutine = StartCoroutine(UpdateDownloadProgress());

                            downloadCanvasWasActive = true;
                        }
                        else
                        {
                            if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                                UOB.DownloadAndInstallPKG(downloadlink, sanitizedFilename, currentContentItem.Value.cover_url);
                        }
                    }
                }
            }

            if (Input.GetButtonDown("Circle"))
            {
                cooldownTimer = inputCooldown;

                if (downloadCanvasWasActive && cancelCanvas.activeSelf)
                {
                    ShowUIState(downloadCanvas, downloadControls);
                    return;
                }

                if (updateCanvas.activeSelf)
                {
                    ShowUIState(null, mainControls);
                    return;
                }

                if (menuCanvas.activeSelf)
                    ToggleMenu(true);
                else if (detailsCanvas.activeSelf)
                {
                    ShowUIState(null, mainControls);

                    if (scrollCoroutine != null)
                    {
                        StopCoroutine(scrollCoroutine);
                        scrollCoroutine = null;
                    }
                }
                else if (closeCanvas.activeSelf || cancelCanvas.activeSelf)
                {
                    if (isDownloading)
                        ShowUIState(downloadCanvas, downloadControls);
                    else
                        ShowUIState(null, mainControls);
                }
                else if (isDownloading)
                    ShowUIState(cancelCanvas, closeControls);
                else
                    ShowUIState(closeCanvas, closeControls);
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

        string currentText = mainControls.activeSelf ? null : menuTexts[textArrayInt].text;

        if (menuLoaded && textArrayInt == 11)
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
                    toggleBackToLocal = true;
                    HandleConfiguration();
                    UpdateSettingsOptions();
                    ToggleMenu();
                    break;

                case 10: // make UOB HELPER FUNC FOR THIS (KB INPUT HANDLING)
                    if (UnityEngine.Application.platform != RuntimePlatform.PS4) return;
                    IntPtr kbInput = UOB.GetKeyboardInput(setDownloadPath, "");
                    string kbOutput = Marshal.PtrToStringAnsi(kbInput);

                    if (string.IsNullOrEmpty(kbOutput) ||
                      kbOutput == "NULL") return;
                    else downloadPath = kbOutput;
                    reloadTriggered = true;
                    break;

                case 11:
                    ScrollOption(1);
                    break;

                case 12:
                    ToggleOption(12, "Install Once Done", ref installAfter);
                    break;
                case 13:
                    ToggleOption(13, "Delete After Install", ref deleteAfter);
                    break;
                case 14:
                    ToggleOption(14, "Populate Via Web", ref populateViaWeb);
                    toggleBackToLocal = true;
                    break;
                case 15:
                    ToggleOption(15, "Background Music", ref backgroundMusic);

                    if (!backgroundMusic)
                        audioSource.Pause();
                    else
                        audioSource.Play();
                    break;

                case 16:
                    if (UnityEngine.Application.platform != RuntimePlatform.PS4)
                        return;

                    IntPtr _kbInput = UOB.GetKeyboardInput("Enter an image path: (local path or URL)",
                      string.IsNullOrEmpty(background_uri) || background_uri == null ? Path.Combine(directoryPath,
                        $"Backgrounds" + (UnityEngine.Application.platform == RuntimePlatform.PS4 ? "/" : "\\")) : background_uri
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
        InitializeMenuItems();

        Menu.HighlightMenuItem(selectedIndex);

        UI.UpdateScrollbar(scrollbar);

        UI.ChangeText(menuTexts, 4, $"> {contentOptions[contentFilter]}");

        if (menuTexts[4] != null && menuTexts[4].text.Contains("^"))
            UI.ChangeText(menuTexts, 4, $"v {sortByOptions[sortCriteria]}");
        else if (menuTexts[4] != null && menuTexts[4].text.Contains("v"))
            UI.ChangeText(menuTexts, 4, $"^ {sortByOptions[sortCriteria]}");

        if (filteredRegions.Contains("USA")) ToggleRegionOption(5, "USA");
        if (filteredRegions.Contains("Europe")) ToggleRegionOption(6, "Europe");
        if (filteredRegions.Contains("Japan")) ToggleRegionOption(7, "Japan");
        if (filteredRegions.Contains("Asia")) ToggleRegionOption(8, "Asia");

        UI.ChangeText(menuTexts, 11, directDownload ? $"> Direct Download" : $"> Back. Download");
        UI.ChangeText(menuTexts, 12, installAfter ? $"x Install Once Done" : $"o Install Once Done");
        UI.ChangeText(menuTexts, 13, deleteAfter ? $"x Delete After Install" : $"o Delete After Install");
        UI.ChangeText(menuTexts, 14, populateViaWeb ? $"x Populate Via Web" : $"o Populate Via Web");
        UI.ChangeText(menuTexts, 15, backgroundMusic ? $"x Background Music" : $"o Background Music");
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
            pS.populateViaWeb = populateViaWeb;
            pS.backgroundMusic = backgroundMusic;

            Print($"Current Settings:\n" +
                $"Ascending: {pS.ascending}\n" +
                $"Sort Criteria: {pS.sortCriteria}\n" +
                $"Content Filter: {pS.contentFilter}\n" +
                $"Filtered Regions: {string.Join(", ", pS.filteredRegions)}\n" +
                $"Background Path: {pS.background_uri}\n" +
                $"Direct Download: {pS.directDownload}\n" +
                $"Install After: {pS.installAfter}\n" +
                $"Delete After: {pS.deleteAfter}\n" +
                $"Populate Via Web: {pS.populateViaWeb}\n" +
                $"Background Music: {pS.backgroundMusic}\n");
        }

        cooldownTimer = inputCooldown;
        menuLoaded = !menuLoaded;

        if (menuLoaded)
            ShowUIState(menuCanvas, menuControls);
        else
        {
            ShowUIState(null, mainControls);

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
                populateViaWeb = pS.populateViaWeb;
                backgroundMusic = pS.backgroundMusic;

                Print($"Reset Settings:\n" +
                  $"Ascending: {ascending}\n" +
                  $"Sort Criteria: {sortCriteria}\n" +
                  $"Content Filter: {contentFilter}\n" +
                  $"Filtered Regions: {string.Join(", ", filteredRegions)}\n" +
                  $"Background Path: {background_uri}\n" +
                  $"Direct Download: {directDownload}\n" +
                  $"Install After: {installAfter}\n" +
                  $"Delete After: {deleteAfter}\n" +
                  $"Populate Via Web: {populateViaWeb}\n" +
                  $"Background Music: {backgroundMusic}\n");

                UI.ChangeText(menuTexts, 4, $"> {contentOptions[contentFilter]}");

                if (menuTexts[4] != null && menuTexts[4].text.Contains("^"))
                    UI.ChangeText(menuTexts, 4, $"v {sortByOptions[sortCriteria]}");
                else if (menuTexts[4] != null && menuTexts[4].text.Contains("v"))
                    UI.ChangeText(menuTexts, 4, $"^ {sortByOptions[sortCriteria]}");

                var regionMappings = new Dictionary<string, int>
                {
                    { "USA", 5 }, { "Europe", 6 }, { "Japan", 7 }, { "Asia", 8 }
                };

                foreach (var region in regionMappings.Keys)
                    UI.ChangeText(menuTexts, regionMappings[region],
                      filteredRegions.Contains(region) ? $"x {region}" : $"o {region}");

                background.gameObject.SetActive(URL.IsValidURI(background_uri) || URL.IsValidImage(background_uri));

                UI.ChangeText(menuTexts, 11, directDownload ? "> Direct Download" : "> Back. Download");
                UI.ChangeText(menuTexts, 12, installAfter ? "x Install Once Done" : "o Install Once Done");
                UI.ChangeText(menuTexts, 13, deleteAfter ? "x Delete After Install" : "o Delete After Install");
                UI.ChangeText(menuTexts, 14, populateViaWeb ? "x Populate Via Web" : "o Populate Via Web");
                UI.ChangeText(menuTexts, 15, backgroundMusic ? "x Background Music" : "o Background Music");

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
        const string rightTriangle = "> ";
        string currentText = menuTexts != null ?
          menuTexts[4].text.TrimStart(rightTriangle[0],
            ' ') : contentOptions[contentFilter];

        int currentIndex = Array.IndexOf(contentOptions, currentText);

        if (currentIndex == -1) currentIndex = contentFilter;

        int newIndex = scrollRight ? (currentIndex + 1) % contentOptions.Length :
          (currentIndex - 1 + contentOptions.Length) % contentOptions.Length;

        contentFilter = newIndex;
        if (menuTexts == null) return;

        UI.ChangeText(menuTexts, 4, $"{rightTriangle}{contentOptions[newIndex]}");
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

    private void ShowUIState(GameObject canvas, GameObject controls)
    {
        menuCanvas.SetActive(false);
        detailsCanvas.SetActive(false);
        downloadCanvas.SetActive(false);
        cancelCanvas.SetActive(false);
        updateCanvas.SetActive(false);
        closeCanvas.SetActive(false);

        mainControls.SetActive(false);
        menuControls.SetActive(false);
        detailControls.SetActive(false);
        downloadControls.SetActive(false);
        closeControls.SetActive(false);

        if (canvas != null) canvas.SetActive(true);
        if (controls != null) controls.SetActive(true);
    }

    #endregion

    private IEnumerator Start()
    {
        StartCoroutine(InitializeCoroutine());
        string previousText = string.Empty;

        while (true)
        {
            HandleUserInput();

            if (cooldownTimer > 0)
                cooldownTimer = Mathf.Clamp(cooldownTimer - Time.deltaTime, 0, inputCooldown);

            var textComponent = UI.FindInactiveObjectsByPath("Canvas/Main/Text/ContentSort")?.GetComponent<Text>();
            if (textComponent || initialized)
            {
                string currentText = $"{contentOptions[contentFilter]}";
                if (currentText != previousText)
                {
                    textComponent.text = currentText;
                    previousText = currentText;

                    reloadTriggered = true;
                }
            }

            UIManagement.HighlightCurrentPkg();

            yield return null;
        }
    }

}