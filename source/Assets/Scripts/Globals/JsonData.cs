using System;
using System.Collections.Generic;
using UnityEngine.UI;

public enum ContentType
{
    Config = -1,
    PS1,
    PS2,
    PSP,
    Games,
    Apps,
    Updates,
    DLC,
    Demos,
    Homebrew,
    Emulators,
    Themes,
    ALL
}

public enum SortBy
{
    Size,
    Region,
    Name,
    TitleID
}

[Serializable]
public class JsonData
{
    public class Config
    {
        public ContentFilter filtering { get; set; }
        public Preferences preferences { get; set; }
    }

    public class ContentFilter
    {
        public string content { get; set; }
        public Sort sort { get; set; }
        public List<string> regions { get; set; }
    }

    public class Sort
    {
        public string type { get; set; }
        public bool ascending { get; set; }
    }

    public class Preferences
    {
        public Downloads downloads { get; set; }
        public Application application { get; set; }
        public ContentURLs content_urls { get; set; }
    }

    public class Downloads
    {
        public bool directDownload { get; set; }
        public string downloadPath { get; set; }
        public bool installAfter { get; set; }
        public bool deleteAfter { get; set; }
    }

    public class Application
    {
        public string background_uri { get; set; }
        public bool backgroundMusic { get; set; }
        public bool populateViaWeb { get; set; }
    }

    public class ContentURLs
    {
        public string ps1 { get; set; }
        public string ps2 { get; set; }
        public string psp { get; set; }
        public string games { get; set; }
        public string apps { get; set; }
        public string updates { get; set; }
        public string dlc { get; set; }
        public string demos { get; set; }
        public string homebrew { get; set; }
        public string emulators { get; set; }
        public string themes { get; set; }
    }

    public class GameContent
    {
        public string title_id { get; set; }
        public string region { get; set; }
        public string name { get; set; }
        public string version { get; set; }
        public string release { get; set; }
        public string size { get; set; }
        public string min_fw { get; set; }
        public string cover_url { get; set; }
    }

    public class Games
    {
        public Dictionary<string, GameContent> DATA { get; set; }
    }

    public static Dictionary<string, GameContent>
        parsedData = new Dictionary<string, GameContent>();

    public List<PKG> PKGs = new List<PKG>();

    [Serializable]
    public class PKG
    {
        public Text TitleID, Region, Title, Downloaded, Size;
    }

    public static Config
    config = new Config();
}
