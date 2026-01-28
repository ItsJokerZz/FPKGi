# FPKGi - Nightly Changelog

<details>
<summary>[ v1.10.0-release Build: 437 ] from Jan 27th, 2026</summary>

### Additions
- Adds a download queue feature for content management
- Adds support for additional storage devices in the UI
- Enables "Delete After Install" functionality for PS5
- Shows matching box art depending on the system

### Improvements & Optimizations
- Notifies users of installations that need to be cancelled
- Saves menu scroll position when closing the menu
- Allows installing a package over an existing one
- Shows current used storage on the PS5 system
- Logs now display elapsed time for app initialization

### Fixes, Resolutions & More
- Config now applies correctly when opening the menu
- Crashes with both download methods have been resolved
- Installation now works even if the title already exists
- FPS drop on the ALL content page has been addressed
- PS5 firmware is now detected correctly in logs
- Content state updates properly after an update
- Default configuration path has been corrected
<br></details>

<details>
<summary>[ v1.01.0-release Build: 79 ] from Sept 11th, 2025</summary>

### Improvements & Optimizations
- Changed the write speed to bits per second for consistency.
- Made log PS4/5 and its firmware for future debugging.
- Updated logging and their types for better information.
- App now adds missing `config.json` values on save/load.

### Fixes, Resolutions & More
- Updated all URLs pointing to my old site to GitHub Gists.
- Made sure to remove whitespace & encode all used URLs.
- Added etaHEN whitelist jailbreak method to fix spam issue.
<br></details>

<details>
<summary>[ v1.01.1-release Build: 20 ] from Sept 11th, 2025</summary>

### Fixes, Resolutions & More
- Resolve issues creating config / content JSON(s) hard-locking and crashing app.
<br></details>

<details>
<summary>[ v1.01.0-release Build: 79 ] from Sept 11th, 2025</summary>

### Improvements & Optimizations
- Changed the write speed to bits per second for consistency.
- Made log PS4/5 and its firmware for future debugging.
- Updated logging and their types for better information.
- App now adds missing `config.json` values on save/load.

### Fixes, Resolutions & More
- Updated all URLs pointing to my old site to GitHub Gists.
- Made sure to remove whitespace & encode all used URLs.
- Added etaHEN whitelist jailbreak method to fix spam issue.
<br></details>

<details>
<summary>[ v1.00.0-release Build: 23 ] from May 7th, 2025</summary>
 
### Additions
- **“Delete After Install” for PS5**: Enabled the "Delete After Install" feature for PS5 downloads.  
- **ZIP Download Support**: Added support for downloading, extracting, and installing ZIP files.  
- **PS5 Dump Extraction**: Introduced PS5 dump extraction for itemzflow integration.  
- **ZIP Installation Order**: ZIP packages now install alphabetically, prioritizing numbers and symbols.  
- **Jailbreak Compatibility**: Improved compatibility using a whitelisted jailbreak on PS5.  
- **Double Circle Press**: Close with two presses of the circle button.  

### Improvements & Optimizations
- **Increased Items per Page**: Items per page now increased from 25 to 30, with UI update and new logo.  
- **Download Info Accuracy**: Switched to `ulong` for more precise download data.  
- **PS5 Content Restriction**: PS5 content can no longer be viewed on PS4.  

### Fixes, Resolutions & More
- **Fixed PS5 Direct Download Crashes**: Resolved crashes during direct downloads on PS5.  
- **Language Fix**: Resolved display issues for different languages.  
- **Non-UTF8 Title Install Fix**: Fixed unable to install non-UTF8 character (non-English) title files.  
- **Added Language Support**: Now supports Asian languages and Arabic for content titles as per [Miichael Crump's YT video](https://youtu.be/TF_PHNkPNIE?si=EBjrQ4jnzOb5oev4&t=678)
<br></details>

<details>
<summary>[ v0.87.7-nightly Build: 250 ] from Mar 19th, 2025</summary>
    
### Additions
- **PS5 Support** 
  - Remaining storage display is currently unavailable  
  - "Delete After Install" is disabled as of now  

- **Download Enhancements**
  - Support for base64-encoded URLs (for config/content)  
  - Redirect link support for both download methods and attachments  
  - Download resuming support for improved handling of interrupted downloads  
  - Ability to install completed packages if finished but not installed  
  - Support for resuming file downloads after cancellations or errors  

- **New Toggles & Options** 
  - Toggle to remove content on cancellation (added to the menu)  
  - Toggle to enable/disable app update checks at launch  
  - Ability to download completed packages from JSONs for later use  

### Improvements & Optimizations
- **Performance & Stability**  
  - Faster initial load times, especially for first-time users  
  - Check for internet connection on launch to prevent issues  

- **User Interface & Experience**  
  - Adjusted download UI logic and added a progress bar  
  - Slight shadow added to all UI text for better visibility  
  - Indicators added for download/install states (similar to the original)  
  - Titles now scroll in both the download UI and details UI  
  - Current save path displayed each time when editing (instead of showing empty)  

- **Content Management** 
  - Combined homebrew & emulator content now loads under homebrew  
  - "Populate Via Web" toggle now displays combined content under homebrew  

### Fixes, Resolutions & More
- Now using `ulong`s for file sizes instead of floats for better accuracy  
- Resolved multiple UI elements not displaying correctly due to culture settings  
- Filtered content count now properly reflects the actual available content
- More & Improved logging saved @ `/user/data/UnityOrbisBridge.log`
<br></details>

<details>
<summary>[ v0.87.3-nightly Build: 22 ] from Mar 3rd, 2025</summary>
    
### Fixed:
- Fixed issue where populating via web didn't load data correctly.
- Fixed background image displaying white when the file is missing.
- Fixed packages being deleted regardless of the toggle setting.
- Fixed inability to install content after the last update.
- Fixed JSON files not updating when using "Reload JSON Files."

### Resolved:
- Resolved GET request spamming during web population.
- Resolved issue with saving content containing invalid characters in its name.
- Resolved issue preventing content downloads when URLs contain spaces.
- Resolved app loading issues in regions where periods are used as commas.
- Resolved failure to create local JSON when loading from a URL fails.

### Improvements:
  - Improved overall UI interaction and responsiveness throughout the app.
  - Improved download progress display, including estimated remaining time and speed.
  - Improved content total display, consistent with previous versions.

  #### Implementations
  - Implemented caching for each page unless reloaded or "Populate via Web" is toggled.
<br></details>

<details>
<summary>[ v0.87-nightly Build: 4 ] from Feb 25th, 2025</summary>

### Fixes:
- [Issue #8](https://github.com/ItsJokerZz/FPKGi/issues/8), where config wouldn't save and reset to defaults unless toggling the menu.
- [Issue #9](https://github.com/ItsJokerZz/FPKGi/issues/9), which prevented users from downloading content due to recent changes to handle page content.
<br></details>

<details>
<summary>[ v0.86-nightly Build: 309 ] from Feb 23rd, 2025</summary>

### Fixes:
- Empty or null app versions now display as "?.??" instead of being blank.
- Fixed filtering, sorting, and ascending toggle; content now displays and saves correctly.
- Addressed UI freezing and app closure issues, ensuring smooth interactions.
- Background music no longer restarts when toggling the menu and saves correctly.
- Content counter updates properly after changes, even when the app remains open.
- Default cover now correctly appears when cover images fail to load.
- Config now properly includes missing values instead of causing issues on load.

### Improvements:
- Resolved [issue #4](https://github.com/ItsJokerZz/FPKGi/issues/4). Downloading the app within itself no longer crashes or removes it; instead, it opens/downloads and launches LM's HB-Store for updates if needed.
- Added a 20MB download limit to prevent false downloads.
- Long titles in the details UI now scroll instead of being cut off.

### Additions:
- Added check for updates on launch that installs and launches HB-Store if not already present.
- App details under "Homebrew" or "ALL" now update in real time.
- New default page displaying all content, set for new users on initial launch.
- Dedicated pages for themes, emulators, PS1/PS2, PSP games, and all content in one.

### Optimizations & More:
- Fixed background images and local content loading issues. As per [ModdedWarfare's YT video](https://youtu.be/EYrvdpPGjTI?si=iWP-igln-WdBODDI&t=651), local connections must use "http(s)://".
- Reduced delays, freezing, and black screens, improving stability.
- Adjusted default values for generated JSONs to better reflect content type.
<br></details>

<details>
<summary>[ v0.81-nightly Build: 24 ] from Feb 13th, 2025</summary>

### Fixes:
- **Initial Setup**
  - Fixed directory and file creation issues preventing setup.
  - Package count now updates correctly after initial demo content creation.
</details>

<details>
<summary>[ v0.80-nightly Build: 193 ] from Jan 9th, 2025</summary>

### Fixes:
- **Background Music:** Now plays, toggles, and saves correctly when closing the menu.
- **Populate via Web:** Settings persist after closing the menu.
- **Search Filtering:**
  - **Improved Filtering:** Filter now remains active across pages.
  - **Reset Functionality:** Properly resets to restore unfiltered content.
- **Downloading:**
  - Fixed issues where invalid content blocked actions and downloads.

### Improvements:
- **Downloading:**
  - Increased update interval for better performance and accuracy.
  - Improved download speed accuracy using smoothing.
  - Enhanced UI display of download estimates.

### Features:
- **Downloading:** Added elapsed download time counter to the UI.
<br></details>
