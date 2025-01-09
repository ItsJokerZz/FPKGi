# FPKGi - A Server Populated PS4 Content Downloader

> [!NOTE]
> FPKGi stands for Fake PKG and references modified .pkg files for backported PS4 games. The project is a clone of the original [PKGi](https://www.github.com/bucanero/pkgi-ps3).
> This lets you populate the app with your own content via `.json` files locally or from the web, and download from your own servers.
> It supports any `.pkg `content, provided you have the necessary licenses. If you're using older FW with backported games, you're all set!
> This tool is for educational and personal use only. Its open-sourced and community-driven, committed to game preservation for the PS4 homebrew community,
> giving everybody the quick and easy way to view and download and quickly install their offloaded content.

## Setup Instructions
Download the latest pre-compiled [`.pkg`](https://github.com/ItsJokerZz/FPKGi/releases) or build it yourself as per, [how to build](#how-to-build), and install it on your console.

### Populate Content
Launch the application to automatically create the necessary directories and `.json` files in the `/data/FPKGi/` folder.

#### Populate Content Locally
Edit the `.json` files generated at `/data/FPKGi/ContentJSONs/` to add your content. <br>
**You can also generate / populate, and save the necessary `.json` file [here](https://www.itsjokerzz.site/projects/FPKGi/gen/) on my site.**

> [!NOTE]
> Use bytes for `"size"` and `"release"` must be in the format: `"MM-DD-YYYY"`.<br>
> Region codes for specifying content regions: `"USA"`, `"JAP"`, `"EUR"`, `"ASIA"`, or `"UNK"`. <br>

Example `.json` structure:
```json
{
    "DATA": {
        "https://www.example.com/directLinkToContent.pkg": {
            "region": "USA",
            "name": "Content Title",
            "version": "1.00",
            "release": "11-15-2014",
            "size": "1000000000",
            "min_fw": null,
            "cover_url": "https://www.example.com/cover.png"
        }
    }
}
```

> [!IMPORTANT]  
> URLs must be direct links to `.pkg` files. Indirect links may cause issues and `"size"` is **REQUIRED!**<br>
> `"region"`, `"release"`,`"min_fw"`, and the `"cover_url"` are all able to be left `null` where needed.

#### Populate Content Via Web
To enable web population, edit the `config.json` file located at `/data/FPKGi`.

Locate and edit the following section:
```json
"CONTENT_URLS": {
   "games": null,
   "apps": null,
   "updates": null,
   "DLC": null,
   "demos": null,
   "homebrew": null
}
```

Replace `null` with URLs pointing to `.json` files containing your content:
```json
"CONTENT_URLS": {
   "games": "https://www.example.com/GAMES.json"
}
```

Unspecified fields will default to loading content from local `.json` files.

## Controls & Settings
### Navigation
- **Move Through Items**: Use <kbd>(LS)tick</kbd>/<kbd>(RS)tick</kbd> or use the dpad to navigate.
- **Select/Download**: Press **![X](https://www.github.com/bucanero/pkgi-ps3/raw/master/data/CROSS.png)** to select or download content.
- **Page & Category Navigation**:
  - <kbd>L1</kbd>/<kbd>R1</kbd>: Changes pages.
  - <kbd>L2</kbd>/<kbd>R2</kbd>: Changes category.
- **View Details**: Press **![square](https://www.github.com/bucanero/pkgi-ps3/raw/master/data/SQUARE.png)** to view detailed information about the selected content.

### Settings Menu
- **Press ![triangle](https://www.github.com/bucanero/pkgi-ps3/raw/master/data/TRIANGLE.png) to open settings**.
- **Save/Cancel**:
  - Press **![triangle](https://www.github.com/bucanero/pkgi-ps3/raw/master/data/TRIANGLE.png)** to save current settings changes.
  - Press **![circle](https://www.github.com/bucanero/pkgi-ps3/raw/master/data/CIRCLE.png)** to toggle menu & not save setting.
 
<br>
  
- **Press the touchpad to search or filter through content by title ID or name.**
  
## Features
### Core Features
- **Search**: Quickly find content using keywords.
- **Sorting**: Organize content by size, name, region, or title ID.
- **Filtering**: Filter content by type for faster navigation.
- **View Options**: Toggle between ascending and descending order.

### Customization
- **Background Music**: Toggle the original PKGi background music by [nobodo](https://www.github.com/nobodo).
- **Custom Backgrounds**:
Add images via URL or locally (supports `.png`, `.bmp`, `.jpg`, and `.jpeg`).
  - URL Example:
    ```json
    "background_uri": "https://www.example.com/image.png"
    ```
  - Local Example:
    ```json
    "background_uri": "/data/FPKGi/Backgrounds/custom.png"
    ```
  - Reset to default:
    ```json
    "background_uri": null
    ```

### Download Management
- **Background Downloads**: Supports simultaneous downloads with automatic installation and rest-mode support.
- **Foreground Downloads**: Single download support, with a queue feature planned in future updates.<br>

    - You can edit the path within the app's settings, or manually through the '.config.json' like so:
    ```json
    "downloadPath": "/mnt/usb0/"
    ```

    - When using, you must provide '/user/' before your path, unless it's in '/mnt/', for example:
    ```json
    "downloadPath": "/user/data/folder/"
    ```
    
    - To reset, simply set `null` or type the default path:
    ```json
    "downloadPath": "/user/data/FPKGi/Downloads/"
    ```
    
## How to Build

<details>
  <summary><strong>Prerequisites</strong></summary>
  <ul>
    <li><strong>Unity Hub</strong> & <strong>Unity 2017.2.0p1</strong> (or a compatible version)</li>
    <li><strong>PS4 SDK 4.50+</strong> with Unity integration</li>
    <li><a href="https://github.com/CyB1K/PS4-Fake-PKG-Tools-3.87" target="_blank">PS4 Fake PKG Tools 3.87</a></li>
    <li><a href="https://www.dotnet.microsoft.com/en-us/download/dotnet-framework/net46" target="_blank">.NET 4.6 Developer Pack</a></li>
  </ul>

  <details>
    <summary><strong>Included Precompiled Dependencies</strong></summary>
    <ul>
      <li><a href="https://www.github.com/SaladLab/Json.Net.Unity3D" target="_blank">Json.Net.Unity3D</a></li>
      <li><a href="https://www.github.com/ItsJokerZz/UnityOrbisBridge" target="_blank">UnityOrbisBridge</a></li>
      <li><a href="https://www.github.com/ItsJokerZz/UOBWrapper" target="_blank">UOBWrapper</a></li>
    </ul>
  </details>
</details>

### Steps to Build

1. **Ensure Prerequisites are Set Up:**
   - Install **Unity Hub** and **Unity 2017.2.0p1** (or a compatible version).
   - Set up the **PS4 SDK 4.50+** with the matching Unity integration.
   - Download and move the [PS4 Fake PKG Tools 3.87](https://github.com/CyB1K/PS4-Fake-PKG-Tools-3.87) to the SDK.
   - Download and install the [.NET 4.6 Developer Pack](https://www.dotnet.microsoft.com/en-us/download/dotnet-framework/net46).

2. **Open the Project:**
   - Launch **Unity Hub**, add your project, and open it.

3. **Configure Build Settings:**
   - In Unity, go to `File -> Build Settings`.
   - Click the `Player Settings` button.
   - Expand the `Other Settings` section.
     - Find `PS4 SDK Override` and set the correct SDK path.

     **Note:** Necessary files are provided in the root of the source!

   - Expand the `Publishing Settings` section.
     - Set the paths for the following:
       - `Share File Param` under the `Package` section.
      
    - Under the `Title Voice Recognition Details` section.
       - `pronunciation.xml` and `pronunciation.sig`

**For more detailed guidance with images, check out [RetroGamer74's guide](https://github.com/RetroGamer74/HowToBuildWithUnityPS4FakePKG/blob/master/README.md).**

## Troubleshooting Error Code `CE-36441-8`
If you encounter this error, follow these steps to resolve it:

> [!WARNING]  
> **THIS WILL BLOCK ACCESS to Sony CDN & API, and other related services.**

1. **Enable Debug Menu**:  
   - Open the **GoldHEN** menu.  
   - Navigate to **Debug Settings** and enable **Full Menu**.  

2. **Configure NP Environment**:  
   - Go to the PS4's settings.  
   - Navigate to `Debug Settings > PlayStation Network`.  
   - Set **NP Environment** to `sp-int`.

3. **Restart the Console**.  

This solution is sourced from [r/ps4homebrew](https://www.reddit.com/r/ps4homebrew/comments/1ctvvg2/comment/l4exrpy/), where several users, including myself, have found it to be effective.

**I am planning on looking into an actual fix rather than a work-around for this issue. If you have any ideas, <br> please make a pull-request 
of [UnityOrbisBridge](https://github.com/ItsJokerZz/UnityOrbisBridge) plugin and I'll gladly look into it and merge into the branch.**

## Special Thanks
A huge thank you to the following members of the [OOSDK Discord](https://www.discord.com/invite/GQr8ydn) for their support:
- **[TheMagicalBlob](https://github.com/TheMagicalBlob)**, [LightningMods](https://github.com/LightningMods), [Al-Azif](https://github.com/Al-Azif), Da Puppeh, Kernel Panic, lainofthewired, and others.

## License
This project is licensed under the GNU General Public License v2.0 - see the [LICENSE](LICENSE) file for details.
