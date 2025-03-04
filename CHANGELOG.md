# FPKGi - Nightly Changelog

<details>
<summary>[ v0.87.3-nightly Build: 44 ] from Mar 4th, 2025</summary>
    
### **Fixed:**
- Issue where populating via web didn't load data correctly.
- Background image displaying white when the file is missing.
- Packages being deleted regardless of the toggle setting.
- Inability to install content after the last update.
- JSON files not updating when using "Reload JSON Files."

### **Resolved:**
- GET request spamming during web population.
- Issue with saving content containing invalid characters in its name.
- Issue preventing content downloads when URLs contain spaces.
- App loading issues in regions where periods are used as commas.
- Failure to create local JSON when loading from a URL fails.

### **Improvements & More:**
  #### **Improvements**
  - Overall UI interaction and responsiveness throughout the app.
  - Download progress display, including estimated remaining time and speed.
  - Content total display and is now consistent with previous versions.

  #### **Implementations**
  - Caching for each page unless reloaded or "Populate via Web" is toggled.
  - Checks for invalid downloads, to prevent false install and UI updates.
  - More logging saved to `/data/UnityOrbisBridge.log` and printed to UART.
<br></details>

<details>
<summary>[ v0.87-nightly Build: 4 ] from Feb 25th, 2025</summary>

### **Fixes:**
- [Issue #8](https://github.com/ItsJokerZz/FPKGi/issues/8), where config wouldn't save and reset to defaults unless toggling the menu.
- [Issue #9](https://github.com/ItsJokerZz/FPKGi/issues/9), which prevented users from downloading content due to recent changes to handle page content.
<br></details>

<details>
<summary>[ v0.86-nightly Build: 309 ] from Feb 23rd, 2025</summary>
<br>

### **Fixes:**
- Empty or null app versions now display as "?.??" instead of being blank.
- Fixed filtering, sorting, and ascending toggle; content now displays and saves correctly.
- Addressed UI freezing and app closure issues, ensuring smooth interactions.
- Background music no longer restarts when toggling the menu and saves correctly.
- Content counter updates properly after changes, even when the app remains open.
- Default cover now correctly appears when cover images fail to load.
- Config now properly includes missing values instead of causing issues on load.

### **Improvements:**
- Resolved [issue #4](https://github.com/ItsJokerZz/FPKGi/issues/4). Downloading the app within itself no longer crashes or removes it; instead, it opens/downloads and launches LM's HB-Store for updates if needed.
- Added a 20MB download limit to prevent false downloads.
- Long titles in the details UI now scroll instead of being cut off.

### **Additions:**
- Added check for updates on launch that installs and launches HB-Store if not already present.
- App details under "Homebrew" or "ALL" now update in real time.
- New default page displaying all content, set for new users on initial launch.
- Dedicated pages for themes, emulators, PS1/PS2, PSP games, and all content in one.

### **Optimizations & More:**
- Fixed background images and local content loading issues. As per [ModdedWarfare's YT video](https://youtu.be/EYrvdpPGjTI?si=iWP-igln-WdBODDI&t=651), local connections must use "http(s)://".
- Reduced delays, freezing, and black screens, improving stability.
- Adjusted default values for generated JSONs to better reflect content type.
<br></details>

<details>
<summary>[ v0.81-nightly Build: 24 ] from Feb 13th, 2025</summary>

### **Fixes:**
- **Initial Setup**
  - Fixed directory and file creation issues preventing setup.
  - Package count now updates correctly after initial demo content creation.
</details>

<details>
<summary>[ v0.80-nightly Build: 193 ] from Jan 9th, 2025</summary>

### **Fixes:**
- **Background Music:** Now plays, toggles, and saves correctly when closing the menu.
- **Populate via Web:** Settings persist after closing the menu.
- **Search Filtering:**
  - **Improved Filtering:** Filter now remains active across pages.
  - **Reset Functionality:** Properly resets to restore unfiltered content.
- **Downloading:**
  - Fixed issues where invalid content blocked actions and downloads.

### **Improvements:**
- **Downloading:**
  - Increased update interval for better performance and accuracy.
  - Improved download speed accuracy using smoothing.
  - Enhanced UI display of download estimates.

### **Features:**
- **Downloading:** Added elapsed download time counter to the UI.
<br></details>