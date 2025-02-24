# FPKGi <img alt="GitHub Tag" src="https://img.shields.io/github/v/tag/itsjokerzz/fpkgi"> - A Server-Based PS4 Content Installer

> [!NOTE]  
> **FPKGi (Fake PKG Installer)** is a tool for installing modified `.pkg` files on the PS4, inspired by the original [PKGi](https://www.github.com/bucanero/pkgi-ps3) for PS3. It allows you to manage and install your own content using `.json` files, either locally over your network or from the web, and download packages from your private server. Fully compatible with "fake" `.pkg` content, this open-source, community-driven project is designed for educational and personal use. It supports game preservation in the PS4 homebrew scene by providing a seamless way to browse, download, and install content efficiently. A recommended setup is offloading to a NAS, running a web server with Node.js or Python, and using direct URLs for downloads. Use responsibly with your own content and servers and make sure to be downloading FPKGi from our GitHub official repository, via [pkg-zone.com](https://pkg-zone.com/details/FPKGI13337), or through the Homebrew Store. **Avoid third-party sources, we do not take any responsibility if there's any damage to your console due to malicious FPKG(s) downloaded from any public third-party hosted sources or unknown sources.**

## Setup Instructions
#### Download the Latest Version:
   - Get the latest compiled package from the [Releases](https://github.com/ItsJokerZz/FPKGi/releases) or visit [pkg-zone.com](https://pkg-zone.com/details/FPKGI13337).

#### Install the Package:
   - Choose your preferred method to install the package on your console.
   - Alternatively, you can install directly from LightningMods' [Homebrew Store](https://github.com/LightningMods/PS4-Store).

### Populate Content
Launch the application to automatically create the necessary directories and `.json` files in the `/data/FPKGi/` folder.

#### Populate Content Locally
Edit the `.json` files generated at `/data/FPKGi/ContentJSONs/` to add your content. <br>
**You can also generate / populate, and save the necessary `.json` file [here](https://www.itsjokerzz.site/projects/FPKGi/gen/) on my site.**

> [!NOTE]
> Use bytes for `"size"` and for specifying content's region: `"USA"`, `"JAP"`, `"EUR"`, `"ASIA"`, or `null`.

### Example `.json` structure:
```json
{
    "DATA": {
        "https://www.example.com/directLinkToContent.pkg": {
            "region": "USA",
            "name": "Content Title",
            "version": "1.00",
            "release": "11-15-2014",
            "size": 1000000000,
            "min_fw": null,
            "cover_url": "https://www.example.com/cover.png"
        }
    }
}
```

> [!IMPORTANT]  
> URLs must be direct links to `.pkg` files. Indirect links may cause issues, and the `"size"` attribute is **REQUIRED!**<br>
> Please ensure the size is as accurate as possible to prevent issues with downloading! The following fields can be <br> left as `null`
where applicable: `"version"`, `"region"`, `"release"`, `"min_fw"`, and `"cover_url"` if you decide.

#### Populate Content Via Web
To enable web population, edit the `config.json` file located at `/data/FPKGi` and enable it in the menu.

### Locate and edit the following section:
```json
    "CONTENT_URLS": {
      "PS1": null,
      "PS2": null,
      "PSP": null,
      "games": null,
      "apps": null,
      "updates": null,
      "DLC": null,
      "demos": null,
      "homebrew": null,
      "emulators": null,
      "themes": null
    }
```

#### Replace `null` with URLs pointing to `.json` files containing your content:
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
      <li><a href="https://www.github.com/ItsJokerZz/store-api" target="_blank">HB Store's API</a></li>
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
> **THIS IS NOT A FIX OR RECOMMENDED BUT THIS CAN HELP AS A TEMPORARILY <br>
> WORKAROUND UNTIL REAOLVED. PLEASE BE ADVISED AND REVERT THIS AFTER!**

1. **Enable Debug Menu**:  
   - Open the **GoldHEN** menu.  
   - Navigate to **Debug Settings** and enable **Full Menu**.  

2. **Configure NP Environment**:  
   - Go to the PS4's settings.  
   - Navigate to `Debug Settings > PlayStation Network`.  
   - Set **NP Environment** to `invalid`.

3. **Restart the Console**.  

This solution is sourced from [r/ps4homebrew](https://www.reddit.com/r/ps4homebrew/comments/1ctvvg2/comment/l4exrpy/), where several users, including myself, have found it to be effective.

**I am planning on looking into an actual fix rather than a work-around for this issue. If you have any ideas, <br> please make a pull-request 
of [UnityOrbisBridge](https://github.com/ItsJokerZz/UnityOrbisBridge) plugin and I'll gladly look into it and merge into the branch.**

## Special Thanks
- **Original App Creator**: [Bucanero](https://www.github.com/bucanero)
- **Original PKGi Music**: [nobodo](https://www.github.com/nobodo)

  ### Unity Sources:
    - [RetroGamer74](https://www.github.com/RetroGamer74) & [Lapy055](https://www.github.com/Lapy055)

A huge thank you to the following members of the [OOSDK Discord](https://www.discord.com/invite/GQr8ydn) for their support:
- **[TheMagicalBlob](https://github.com/TheMagicalBlob)**, [LightningMods](https://github.com/LightningMods), [Al-Azif](https://github.com/Al-Azif), Da Puppeh, Kernel Panic, lainofthewired, and others.

For more credits, please check out [UnityOrbisBridge](https://www.github.com/ItsJokerZz/UnityOrbisBridge). If I've missed anyone, feel free to reach out. <br><br>
For assistance, please leave an issue and/or join my community [Discord server](https://discord.com/invite/RjG4Whf) and I'll help you out.

## License
This project is licensed under the GNU General Public License v2.0 - see the [LICENSE](LICENSE) file for details.
