﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
        closeCanvas,
        cancelCanvas,
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

    private bool menuLoaded = false,
      downloadCanvasWasActive = false,
      isDownloading = false;
    #endregion

    #region Unity Methods
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (backgroundMusic) audioSource.Play();

        currentPkg = Content.PKGs[contentScroll];

        StartCoroutine(InitializeMenuCoroutine());

        HighlightCurrentPkg();
    }

    private void Update()
    {
        HandleUserInput();

        if (cooldownTimer > 0) cooldownTimer =
          Mathf.Clamp(cooldownTimer - Time.deltaTime, 0, inputCooldown);

        UI.ChangeText(mainTexts, 2, $"{contentOptions[contentFilter]}");
    }
    #endregion

    #region Coroutine Handling
    [DllImport("UnityOrbisBridge", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ResetDownloadVars();


    private IEnumerator InitializeMenuCoroutine()
    {
        while (!menuCanvas.activeSelf) yield return null;

        Variables.menuCanvas = menuCanvas; // global def. can remove (used to view in editor)
        Variables.menuControls = menuControls; // global def. can remove (used to view in editor)

        InitializeMenuItems();

        Menu.HighlightMenuItem(selectedIndex);

        UI.UpdateScrollbar(scrollbar);

        UI.ChangeText(menuTexts, 4, $"> {contentOptions[contentFilter]}");
        SetSortOption(sortByOptions[sortCriteria], sortCriteria);

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

    private void InitializeMenuItems()
    {
        menuTexts = new Text[MenuTextObjects.Length];

        for (int i = 0; i < menuTexts.Length; i++)
            menuTexts[i] = UI.FindTextComponent(MenuTextObjects[i]);
    }

    private IEnumerator UpdateDownloadProgress()
    {
        isDownloading = true;

        Transform textTransform = downloadCanvas.transform.Find("Text");

        Text title = textTransform.Find("Title")?.GetComponent<Text>();
        Text info = textTransform.Find("Info")?.GetComponent<Text>();

        string lastDownloadBytes = "";
        string lastFileSizeBytes = "";

        float downloadStartTime = Time.time;
        float lastUpdateTime = Time.time;
        float lastDownloadedBytes = 0;
        float smoothedDownloadSpeed;
        float updateInterval = 1.0f;

        List<float> downloadSpeeds
            = new List<float>();

        int maxSpeedHistory = 5;

        if (!parsedData.ContainsKey(parsedData.Keys.ElementAt(contentScroll)))
        {
            Print("Invalid parsed data for the current package.", PrintType.Warning);

            goto finish;
        }

        while (isDownloading)
        {
            if (UOB.GetDownloadError() == -1) goto finish; // might not be needed, may be able to tweak UOB

            string downloadBytes = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("downloaded"));
            string fileSizeBytes = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("filesize"));
            string progress = Marshal.PtrToStringAnsi(UOB.GetDownloadInfo("progress"));

            if (string.IsNullOrEmpty(downloadBytes) || string.IsNullOrEmpty(fileSizeBytes) || string.IsNullOrEmpty(progress))
            {
                Print("One of the download values is empty or invalid. Retrying...", PrintType.Warning);

                yield return null;

                continue;
            }

            if (downloadBytes != lastDownloadBytes || fileSizeBytes != lastFileSizeBytes)
            {
                float elapsedTime = Time.time - lastUpdateTime;

                if (elapsedTime >= updateInterval)
                {
                    long downloadedBytes, totalFileSizeBytes;

                    if (!long.TryParse(downloadBytes, out downloadedBytes) || !long.TryParse(fileSizeBytes, out totalFileSizeBytes))
                    {
                        Print("Invalid download or file size data. Stopping download.", PrintType.Warning);

                        goto finish;
                    }

                    UI.ChangeText(title, GameContentAsList[contentScroll].Value.name);

                    float downloadSpeed = (downloadedBytes - lastDownloadedBytes) / elapsedTime;

                    downloadSpeeds.Add(downloadSpeed);

                    if (downloadSpeeds.Count > maxSpeedHistory)
                        downloadSpeeds.RemoveAt(0);

                    smoothedDownloadSpeed = downloadSpeeds.Average();

                    float remainingBytes = totalFileSizeBytes - downloadedBytes;
                    float estimatedTime = remainingBytes / smoothedDownloadSpeed;

                    if (estimatedTime < 0) estimatedTime = 0;

                    float elapsedDownloadTime = Time.time - downloadStartTime;

                    string elapsedTimeFormatted = FormatTime(elapsedDownloadTime);
                    string estimatedTimeFormatted = FormatTime(estimatedTime);

                    string downloadSpeedText = IO.FormatByteString(smoothedDownloadSpeed) + "/s";
                    string downloadedFormatted = IO.FormatByteString(downloadedBytes);
                    string fileSizeFormatted = IO.FormatByteString(fileSizeBytes);

                    UI.ChangeText(info,
                        $"Download Speed: {downloadSpeedText}\n" +
                        $"Remaining Time: {estimatedTimeFormatted}\n\n\n" +
                        $"Elapsed Download Time: {elapsedTimeFormatted}\n" +
                        $"Downloaded: {downloadedFormatted} / {fileSizeFormatted} ({progress}%)");

                    if (!cancelCanvas.activeSelf)
                    {
                        mainControls.SetActive(false);
                        closeControls.SetActive(false);
                        detailControls.SetActive(false);
                        downloadControls.SetActive(true);
                        downloadCanvas.SetActive(true);
                    }

                    lastDownloadedBytes = downloadedBytes;
                    lastUpdateTime = Time.time;
                }

                lastDownloadBytes = downloadBytes;
                lastFileSizeBytes = fileSizeBytes;

                float downloadProgress;
                if (float.TryParse(progress, out downloadProgress) && downloadProgress >= 98f)
                    isDownloading = false;
            }

            if (!isDownloading) break;

            yield return null;
        }

        Task.Delay(5000).Wait();
        // yield return new WaitForSeconds(5f);

        if (directDownload && installAfter)
        {
            if (UOB.GetDownloadError() == 200)
            {
                string packagePath = $"{downloadPath}{GameContentAsList[contentScroll].Value.name} " +
                    $"[{GameContentAsList[contentScroll].Value.title_id}].pkg";

                Print($"Installing... \"{packagePath}\"");
                UOB.InstallLocalPackage(packagePath, GameContentAsList[contentScroll].Value.name, deleteAfter);
            }
        }

        goto finish;

    finish: 
        ResetDownloadVars();

        isDownloading = false;

        detailControls.SetActive(false);
        downloadControls.SetActive(false);
        downloadCanvas.SetActive(false);

        UI.ChangeText(title, string.Empty);
        UI.ChangeText(info, string.Empty);

        downloadCanvasWasActive = false;

        mainControls.SetActive(true);
    }


    private string FormatTime(float totalSeconds)
    {
        if (totalSeconds < 0) return "N/A";

        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
        StringBuilder formattedTime = new StringBuilder();

        if (time.Days > 0)
            formattedTime.Append($"{time.Days}d ");

        if (time.Hours > 0)
            formattedTime.Append($"{time.Hours}h ");

        if (time.Minutes > 0)
            formattedTime.Append($"{time.Minutes}m ");

        formattedTime.Append($"{time.Seconds}s");

        return formattedTime.ToString().Trim();
    }
    #endregion

    #region User Input Handling
    private void HandleUserInput()
    {
        if (cooldownTimer <= 0)
        {
            float horizontalInput = Input.GetAxis("Dpad-X") + Input.GetAxis("KB-X");
            float verticalInput =
              Input.GetAxis("LStick-Y") + Input.GetAxis("Dpad-Y") + Input.GetAxis("Mouse-Y");

            if (!closeCanvas.activeSelf && !downloadCanvas.activeSelf && !cancelCanvas.activeSelf)
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
                        mainControls.SetActive(true);
                        detailsCanvas.SetActive(false);
                        detailControls.SetActive(false);

                        return;
                    }
                    else
                    {
                        var url = GameContentAsList[contentScroll].Value.cover_url;

                        if (string.IsNullOrEmpty(url) || url == null)
                            coverImage.gameObject.SetActive(false);
                        else
                        {
                            if (URL.IsValidImage(url))
                                SetImageFromURL(url, ref coverImage);
                        }

                        mainControls.SetActive(false);
                        detailsCanvas.SetActive(true);
                        detailControls.SetActive(true);

                        if (detailsCanvas == null) return;

                        Transform textTransform = detailsCanvas.transform.Find("Text");

                        if (textTransform == null) return;

                        Text name = textTransform.Find("Name")?.GetComponent<Text>();
                        Text title_id = textTransform.Find("TitleID")?.GetComponent<Text>();
                        Text version = textTransform.Find("AppVersion")?.GetComponent<Text>();
                        Text min_fw = textTransform.Find("MinFWReq")?.GetComponent<Text>();
                        Text release = textTransform.Find("Release")?.GetComponent<Text>();

                        UI.ChangeText(name, GameContentAsList[contentScroll].Value.name);
                        UI.ChangeText(title_id, $"Title ID: {GameContentAsList[contentScroll].Value.title_id} [{GameContentAsList[contentScroll].Value.region}]");
                        UI.ChangeText(version, $"Package Version: {GameContentAsList[contentScroll].Value.version}");
                        UI.ChangeText(min_fw, $"Required Firmware: {GameContentAsList[contentScroll].Value.min_fw}");
                        UI.ChangeText(release, $"Release Date: {GameContentAsList[contentScroll].Value.release}");
                    }
                }

                if (menuCanvas.activeSelf)
                {
                    if (!downloadCanvas.activeSelf)
                    {
                        if (verticalInput != 0)
                            NavigateMenu(verticalInput);
                        if (horizontalInput != 0 &&
                          selectedIndex == 4 || selectedIndex == 11)
                            ScrollOption(horizontalInput);
                    }
                }
                else
                {
                    if (!closeCanvas.activeSelf && !detailsCanvas.activeSelf && !downloadCanvas.activeSelf && !cancelCanvas.activeSelf)
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

                            contentScroll = 0;

                            UIManagement.ClearDisplayedItems();

                            HighlightCurrentPkg();
                        }

                        if (Input.GetButtonDown("L1") || Input.GetButtonDown("R1")) SaveConfiguration();

                        if (Input.GetButtonDown("Touchpad")) // make UOB HELPER FUNC FOR THIS (KB INPUT HANDLING)
                        {
                            if (UnityEngine.Application.platform != RuntimePlatform.PS4) return;
                            IntPtr kbInput = UOB.GetKeyboardInput(SearchText, "");
                            string kbOutput = Marshal.PtrToStringAnsi(kbInput);

                            if (string.IsNullOrEmpty(kbOutput) || kbOutput == "NULL")
                                filter = string.Empty;
                            else filter = kbOutput;

                            UIManagement.ClearDisplayedItems();

                            HighlightCurrentPkg();
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

                            if (filteredCount > 0) HighlightCurrentPkg();
                        }

                        if (Input.GetAxis("R2") != 0)
                        {
                            cooldownTimer = inputCooldown;

                            contentScroll += scrollAmount;
                            if (contentScroll >= filteredCount)
                                contentScroll = 0;

                            if (filteredCount > 0)
                                HighlightCurrentPkg();
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

                                if (contentScroll < 0) contentScroll = filteredCount > 0 ? filteredCount - 1 : 0;
                            }

                            if (filteredCount > 0)
                                HighlightCurrentPkg();
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
                    if (!downloadCanvasWasActive)
                    {
                        if (closeControls.activeSelf)
                        {
                            SaveConfiguration();

                            if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                            {
                                UOB.EnterSandbox();
                                UOB.ExitApplication();
                            }
#if UNITY_EDITOR_WIN
                            UnityEditor.EditorApplication.isPlaying = false;
#endif
                        }
                        else if (!closeControls.activeSelf)
                        {
                            if (downloadCanvas.activeSelf || string.IsNullOrEmpty(currentPkg.TitleID.text)) return;

                            var pkgData = GameContentAsList[contentScroll].Value;

                            var downloadInfo = parsedData
                              .FirstOrDefault(kv =>
                                kv.Value.title_id == pkgData.title_id &&
                                kv.Value.region == pkgData.region &&
                                kv.Value.name == pkgData.name &&
                                kv.Value.version == pkgData.version &&
                                kv.Value.release == pkgData.release &&
                                kv.Value.size == pkgData.size &&
                                kv.Value.min_fw == pkgData.min_fw &&
                                kv.Value.cover_url == pkgData.cover_url);

                            if (directDownload)
                            {
                                if (detailsCanvas.activeSelf)
                                    detailsCanvas.SetActive(false);

                                if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                                {
                                    // if (isDownloading) UOB.CancelDownload();

                                    // make UOB wrapper function for this (mainly to eliminate the name arg)
                                    UOB.DownloadPKG(downloadInfo.Key, downloadPath, $"{downloadInfo.Value.name} [{downloadInfo.Value.title_id}]", true, "NULL");

                                    StopCoroutine(UpdateDownloadProgress());
                                    StartCoroutine(UpdateDownloadProgress());
                                }
                                else if (!downloadCanvas.activeSelf)
                                {
                                    downloadCanvas.SetActive(true);
                                    downloadControls.SetActive(true);

                                    mainControls.SetActive(false);
                                    detailControls.SetActive(false);
                                }

                                downloadCanvasWasActive = true;
                            }
                            else
                            {
                                if (UnityEngine.Application.platform == RuntimePlatform.PS4)
                                    UOB.DownloadAndInstallPKG(downloadInfo.Key, downloadInfo.Value.name, downloadInfo.Value.cover_url);
                            }
                        }
                    }
                    else
                    {
                        if (cancelCanvas.activeSelf)
                        {
                            if (UnityEngine.Application.platform
                                == RuntimePlatform.PS4) UOB.CancelDownload();

                            cancelCanvas.SetActive(false);
                            closeControls.SetActive(false);
                            downloadCanvas.SetActive(false);

                            mainControls.SetActive(true);

                            downloadCanvasWasActive = false;
                            isDownloading = false;

                            // reset download * marks (ONCE ADDED)
                        }
                    }
                }
            }

            if (Input.GetButtonDown("Circle"))
            {
                cooldownTimer = inputCooldown;

                if (downloadCanvasWasActive && cancelCanvas.activeSelf)
                {
                    cancelCanvas.SetActive(false);
                    downloadCanvas.SetActive(true);

                    closeControls.SetActive(false);
                    downloadControls.SetActive(true);

                    return;
                }

                if (menuCanvas.activeSelf)
                    ToggleMenu(true);
                else if (detailsCanvas.activeSelf)
                {
                    detailControls.SetActive(false);
                    detailsCanvas.SetActive(false);

                    closeCanvas.SetActive(false);
                    closeControls.SetActive(false);

                    downloadControls.SetActive(false);
                    downloadCanvas.SetActive(false);

                    mainControls.SetActive(true);

                }
                else if (closeCanvas.activeSelf || cancelCanvas.activeSelf)
                {
                    cancelCanvas.SetActive(false);
                    closeControls.SetActive(false);
                    closeCanvas.SetActive(false);

                    mainControls.SetActive(true);
                }
                else if (downloadCanvas.activeSelf)
                {
                    downloadCanvas.SetActive(false);
                    closeCanvas.SetActive(false);
                    mainControls.SetActive(false);

                    cancelCanvas.SetActive(true);
                    closeControls.SetActive(true);

                    downloadControls.SetActive(false);
                }
                else
                {
                    mainControls.SetActive(false);

                    detailControls.SetActive(false);
                    detailsCanvas.SetActive(false);

                    if (downloadCanvas.activeSelf)
                        downloadCanvas.SetActive(false);

                    closeControls.SetActive(true);
                    closeCanvas.SetActive(true);
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
        if (populateViaWeb) isPopulatedViaWeb = false;

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

                if (JSON.ParseJSON((ContentType)contentFilter) == 0)
                    UIManagement.ClearDisplayedItems();

                HighlightCurrentPkg();
            }
        }
    }

    private void ExecuteMenuItemAction()
    {
        cooldownTimer = inputCooldown;

        if (menuLoaded)
            switch (selectedIndex)
            {
                case 0:
                    SetSortOption(sortByOptions[(int)SortBy.Size], 0);
                    break;
                case 1:
                    SetSortOption(sortByOptions[(int)SortBy.Region], 1);
                    break;
                case 2:
                    SetSortOption(sortByOptions[(int)SortBy.Name], 2);
                    break;
                case 3:
                    SetSortOption(sortByOptions[(int)SortBy.TitleID], 3);
                    break;
                case 4:
                    ScrollOption(1);
                    break;
                case 5:
                    ToggleRegionOption(5, "USA");
                    break;
                case 6:
                    ToggleRegionOption(6, "Europe");
                    break;
                case 7:
                    ToggleRegionOption(7, "Japan");
                    break;
                case 8:
                    ToggleRegionOption(8, "Asia");
                    break;

                case 9:
                    JSON.ParseJSON(ContentType.Config);
                    JSON.ParseJSON((ContentType)contentFilter);

                    UIManagement.ClearDisplayedItems();

                    HighlightCurrentPkg();

                    ToggleMenu(true);
                    break;

                case 10: // make UOB HELPER FUNC FOR THIS (KB INPUT HANDLING)
                    if (UnityEngine.Application.platform != RuntimePlatform.PS4) return;
                    IntPtr kbInput = UOB.GetKeyboardInput(setDownloadPath, "");
                    string kbOutput = Marshal.PtrToStringAnsi(kbInput);

                    if (string.IsNullOrEmpty(kbOutput) ||
                      kbOutput == "NULL") return;
                    else downloadPath = kbOutput;
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
                    JSON.ParseJSON((ContentType)contentFilter);

                    UIManagement.ClearDisplayedItems();

                    HighlightCurrentPkg();
                    break;

                case 15:
                    ToggleOption(15, "Background Music", ref backgroundMusic);


                    if (!backgroundMusic)
                    {
                        audioSource.time = 0;
                        audioSource.Stop();
                    }
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

                    if (URL.IsValidImage(_kbOutput))
                        SetImageFromURL(_kbOutput, ref background);
                    else IO.LoadImage(_kbOutput, ref background);
                    break;

            }
    }

    private void SetSortOption(string optionName, int index, bool toggle = true)
    {
        if (menuTexts[index] != null && menuTexts[index].text.Contains("^"))
        {
            UI.ChangeText(menuTexts, index, $"v {optionName}");

            if (toggle) ascending = false;
        }
        else if (menuTexts[index] != null && menuTexts[index].text.Contains("v"))
        {
            UI.ChangeText(menuTexts, index, $"^ {optionName}");

            if (toggle) ascending = true;
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

        HighlightCurrentPkg();
    }
    #endregion

    #region Menu Interaction
    private void ToggleMenu(bool resetSettings = false)
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

            var _filteredRegions =
              string.Join(", ", pS.filteredRegions);

            Print($"Current Settings:\n" +
                $"Ascending: {pS.ascending}\n" +
                $"Sort Criteria: {pS.sortCriteria}\n" +
                $"Content Filter: {pS.contentFilter}\n" +
                $"Filtered Regions: {_filteredRegions}\n" +
                $"Background Path: {pS.background_uri}\n" +
                $"Direct Download: {pS.directDownload}\n" +
                $"Install After: {pS.installAfter}\n" +
                $"Delete After: {pS.deleteAfter}\n" +
                $"Populate Via Web: {pS.populateViaWeb}\n" +
                $"Background Music: {pS.backgroundMusic}\n");
        }

        cooldownTimer = inputCooldown;
        detailsCanvas.SetActive(false);

        menuLoaded = !menuLoaded;

        if (resetSettings && !menuLoaded)
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

            if (JSON.ParseJSON((ContentType)contentFilter) == 0)
                UIManagement.ClearDisplayedItems();

            HighlightCurrentPkg();

            UI.ChangeText(menuTexts, 4, $"> {contentOptions[contentFilter]}");

            SetSortOption(sortByOptions[sortCriteria], sortCriteria, false);

            var regionMappings = new Dictionary<string,
              int> {
          {
            "USA",
            5
          },
          {
            "Europe",
            6
          },
          {
            "Japan",
            7
          },
          {
            "Asia",
            8
          }
        };

            foreach (var region in regionMappings.Keys)
                UI.ChangeText(menuTexts, regionMappings[region],
                  filteredRegions.Contains(region) ? $"x {region}" : $"o {region}");

            background.gameObject.SetActive(!string.IsNullOrEmpty(background_uri));

            UI.ChangeText(menuTexts, 11, directDownload ? "> Direct Download" : "> Back. Download");
            UI.ChangeText(menuTexts, 12, installAfter ? "x Install Once Done" : "o Install Once Done");
            UI.ChangeText(menuTexts, 13, deleteAfter ? "x Delete After Install" : "o Delete After Install");
            UI.ChangeText(menuTexts, 14, populateViaWeb ? "x Populate Via Web" : "o Populate Via Web");
            UI.ChangeText(menuTexts, 15, backgroundMusic ? "x Background Music" : "o Background Music");

            if (!backgroundMusic)
            {
                audioSource.time = 0;
                audioSource.Stop();
            }
            else
                audioSource.Play();
        }
        else if (!resetSettings && !menuLoaded) SaveConfiguration();

        mainControls.SetActive(!menuLoaded);
        detailControls.SetActive(false);
        menuControls.SetActive(menuLoaded);
        menuCanvas.SetActive(menuLoaded);

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

        HighlightCurrentPkg();
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
}
