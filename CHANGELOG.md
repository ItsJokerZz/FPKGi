# FPKGi - Nightly Changelog
<details>
<summary>[ v0.87-nightly Build: 4 ] from Feb 25th, 2025</summary>
<br>
    
**Fixes:**
- [Issue #8](https://github.com/ItsJokerZz/FPKGi/issues/8), where the config wouldnt save / would be reset to defaults when closing unless toggling the menu.
- [Issue #9](https://github.com/ItsJokerZz/FPKGi/issues/9), which prevented users from downloading content due to recent changes to handle page content.
</details>

<details>
<summary>[ v0.86-nightly Build: 309 ] from Feb 23rd, 2025</summary>
<br>

### **Fixes:**
- Empty or null app versions now show as "?.??" instead of an empty string.
- Fixed filtering, sorting, and ascending toggle; content now displays and saves correctly.
- Addressed UI freezing and blocking of app closure, enabling smooth sequential actions.
- Background music no longer restarts when toggling the menu and saves correctly.
- Content counter now updates correctly after content is updated, even when the app remains open.
- Fixed issue where cover images failed to load; now correctly displays the default cover.
- Config on load will no longer cause issues, as it properly adds any missing values to the defaults.

### **Improvements:**
- Resolved [issue #4](https://github.com/ItsJokerZz/FPKGi/issues/4). Downloading the app within itself will no longer crash and remove <br>
    itself; now it will open/download and launch LM's HB-Store to update, if needed.
- Download now include a 20MB limit to avoid false downloading, preventing issues.
- Long titles in the details UI will now scroll for full visibility instead of being cut off.

### **Additions:**
- Added check for updates on launch that will install and launch HB-Store, if not already present.
- When viewing details for the app under the "Homebrew" or "ALL" page, the values will update..
- Introduced a new default page for displaying all content, which is set for new users inital laucnh
- Dedicated pages added for themes, emulators, PS1/PS2, PSP games, and all content in one.

### **Optimizations & More:**
- Fixed issue where background images and other content failed to load with local URLs, as per <br>
    [ModdedWarfare's YT video](https://youtu.be/EYrvdpPGjTI?si=iWP-igln-WdBODDI&t=651), you must add "http(s)://" for local connections, or it won’t work.
- Reduced delays, freezing, and black screens, particularly on first launch, improving overall stability.
- Adjusted default values for generated content JSONs to reflect the content type more clearly.
</details>

<details>
<summary>[ v0.81-nightly Build: 24 ] from Feb 13th, 2025</summary>
<br>
    
**Fixes:**
- **Initial Setup**
  - Resolved an issue where the app couldn't create the necessary directories and files.
  - Fixed the package count to ensure it's updated after creating inital demo content.
</details>

<details>
<summary>[ v0.80-nightly Build: 193 ] from Jan 9th, 2025</summary>
<br>
    
**Fixes:**
- **Background Music:** Resolved issue with music not playing, toggling, or saving correctly when closing the menu.
- **Populate via Web:** Ensured settings are retained after closing the menu.
- **Search Filtering:**
  - **Improved Filtering:** Filter now persists across pages.
  - **Reset Functionality:** Filter resets properly, restoring unfiltered content.
- **Downloading:**
  - Fixed issues with invalid content blocking actions and downloads.

**Improvements:**
- **Downloading:**
  - Increased update interval for better performance and accuracy.
  - Improved download speed accuracy using smoothing.
  - Enhanced overall download estimates for UI display.

**Features:**
- **Downloading:** Added elapsed download time counter to the UI.
</details>
