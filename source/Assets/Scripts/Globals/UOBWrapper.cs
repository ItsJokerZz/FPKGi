using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityOrbisBridge;
using static UOBWrapper.Private;

public static class UOBWrapper
{
    internal static class Private
    {
        internal static readonly Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");

        internal static class Temperature
        {
            private static bool hasTempErrorOccurred;
            private static readonly Dictionary<UOB.Temperature, Tuple<float?, float?>>
                lastTemperatureInfo = new Dictionary<UOB.Temperature, Tuple<float?, float?>>
                {
                    { UOB.Temperature.CPU, Tuple.Create<float?, float?>(null, null) },
                    { UOB.Temperature.SOC, Tuple.Create<float?, float?>(null, null) }
                };

            public static void PrintError()
            {
                if (hasTempErrorOccurred) return;

                Print(LogType.Error, "FAILED TO DETECT TEMP! (THIS SHOULDN'T HAPPEN! REPORT TO \"itsjokerzz / ItsJokerZz#3022\" on Discord)\n" +
                    "You may also submit an issue with the GitHub repo, so I can try to troubleshoot, but this is undocumented.");

                hasTempErrorOccurred = true;
            }

            private static Color DetermineColor(float temperature, Color? coldColor, Color? normalColor, Color? hotColor, float min, float max)
            {
                if (temperature < min) return coldColor.GetValueOrDefault(Color.white);
                if (temperature >= min && temperature < max) return normalColor.GetValueOrDefault(Color.white);
                return hotColor.GetValueOrDefault(Color.white);
            }

            private static void UpdateTextObject(Text textObject, string sensorString, float celsiusTemp, float? fahrenheitTemp, Color? coldColor, Color? normalColor, Color? hotColor, float min, float max)
            {
                if (coldColor.HasValue || normalColor.HasValue || hotColor.HasValue)
                    textObject.color = DetermineColor(celsiusTemp, coldColor, normalColor, hotColor, min, max);

                textObject.text = $"{sensorString}: {celsiusTemp.ToString(CultureInfo.InvariantCulture)}" +
                    $" °C / {fahrenheitTemp.Value.ToString(CultureInfo.InvariantCulture)} °F";
            }

            public static void Update(Text textObject, UOB.Temperature temperature, Color? coldColor = null, Color? normalColor = null, Color? hotColor = null, float min = 55f, float max = 70f)
            {
                if (Application.platform != RuntimePlatform.PS4) return;

                string sensorString = temperature == UOB.Temperature.CPU ? "CPU" : "SoC";
                float? celsiusTemp = UOB.GetTemperature(temperature, false);
                float? fahrenheitTemp = UOB.GetTemperature(temperature, true);

                if (!celsiusTemp.HasValue)
                {
                    PrintError();

                    textObject.text = $"{sensorString}: N/A °C / N/A °F";
                }
                else
                {
                    if (lastTemperatureInfo[temperature].Item1 == celsiusTemp
                        && lastTemperatureInfo[temperature].Item2 == fahrenheitTemp) return;

                    lastTemperatureInfo[temperature] = Tuple.Create(celsiusTemp, fahrenheitTemp);

                    UpdateTextObject(textObject, sensorString, celsiusTemp.Value,
                        fahrenheitTemp, coldColor, normalColor, hotColor, min, max);

                }
            }
        }

        internal static class Logging
        {
            public static int GetLogType(LogType type)
            {
                switch (type)
                {
                    case LogType.Assert: return 0; // DEBUG
                    case LogType.Log: return 1;
                    case LogType.Warning: return 2;
                    case LogType.Error: return 3;
                    case LogType.Exception: return 4; // CRITIAL
                    default: return (int)type;
                }
            }

            public static void LogMessage(string message, LogType type)
            {
                switch (type)
                {
                    case LogType.Warning: Debug.LogWarning(message); break;
                    case LogType.Error: Debug.LogError(message); break;
                    default: Debug.Log(message); break;
                }
            }
        }

        internal static string ProperFormatUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            url = url.Trim();

            string scheme = string.Empty;
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                scheme = "http://";
                url = url.Substring(7);
            }
            else if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                scheme = "https://";
                url = url.Substring(8);
            }

            var sb = new StringBuilder();

            for (int i = 0; i < url.Length; i++)
            {
                char c = url[i];

                if (c == '%' && i + 2 < url.Length && Uri.IsHexDigit(url[i + 1]) && Uri.IsHexDigit(url[i + 2]))
                {
                    sb.Append(url.Substring(i, 3));
                    i += 2;
                    continue;
                }

                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.' || c == '~' ||
                    c == '/' || c == ':' || c == '?' || c == '#' || c == '@' || c == '!' ||
                    c == '$' || c == '&' || c == '\'' || c == '*' || c == '+' || c == ',' ||
                    c == ';' || c == '=')
                    sb.Append(c);
                else if (c == ' ')
                    sb.Append("%20");
                else
                {
                    foreach (var b in Encoding.UTF8.GetBytes(new[] { c }))
                        sb.Append('%').Append(b.ToString("X2"));
                }
            }

            return scheme + sb.ToString();
        }

    }

    public static bool DisablePrintWarnings { get; set; }

    public enum PrintType { Default, Warning, Error }

    public static void Print(string message, LogType type = LogType.Log)
    {
        if (string.IsNullOrEmpty(message) ||
            (type == LogType.Warning && DisablePrintWarnings)) return;

        if (Application.platform == RuntimePlatform.PS4)
            UOB.PrintToConsole(message, Logging.GetLogType(type));
        else Logging.LogMessage(message, type);
    }

    public static void Print(LogType type = LogType.Log,
        string message = null, bool saveLog = true, string filePath = "/data/UnityOrbisBridge.log")
    {
        if (string.IsNullOrEmpty(message) ||
            (type == LogType.Warning && DisablePrintWarnings)) return;

        if (Application.platform == RuntimePlatform.PS4 && UOB.IsFreeOfSandbox())
        {
            if (saveLog)
                UOB.PrintAndLog(message, Logging.GetLogType(type), filePath);
            else UOB.PrintToConsole(message, Logging.GetLogType(type));
        }
        else Logging.LogMessage(message, type);
    }

    public static void UpdateTemperature(Text textObject, UOB.Temperature temperature = UOB.Temperature.CPU)
        => Temperature.Update(textObject, temperature);

    public static void UpdateTemperature(Text textObject, Color cold, Color normal, Color hot,
        UOB.Temperature temperature = UOB.Temperature.CPU)
        => Temperature.Update(textObject, temperature, cold, normal, hot);

    public static void UpdateTemperature(Text textObject, Color cold, Color normal, Color hot,
        UOB.Temperature temperature = UOB.Temperature.CPU, float min = 55f, float max = 70f)
        => Temperature.Update(textObject, temperature, cold, normal, hot, min, max);

    public static void UpdateDiskInfo(Text textObject, UOB.DiskInfo info, string mountPoint = "/user")
    {
        if (Application.platform != RuntimePlatform.PS4 || !UOB.IsFreeOfSandbox()) return; // move the IsFreeOfSandbox() check to the API itself

        var diskInfo = new Dictionary<UOB.DiskInfo, string>
        {
            { UOB.DiskInfo.Used, UOB.GetDiskInfo(UOB.DiskInfo.Used) },
            { UOB.DiskInfo.Free, UOB.GetDiskInfo(UOB.DiskInfo.Free) },
            { UOB.DiskInfo.Total, UOB.GetDiskInfo(UOB.DiskInfo.Total) },
            { UOB.DiskInfo.Percent, UOB.GetDiskInfo(UOB.DiskInfo.Percent) }
        };

        string currentInfo = diskInfo[info];

        if (currentInfo == UOB.lastDiskInfo[info]) return;

        UOB.lastDiskInfo[info] = currentInfo;

        string text = UOB.GetDiskInfoAsFormattedText(info, mountPoint);

        if (textObject != null)
            textObject.text = text;
        else
            Print("UpdateDiskInfo(Text, UOB.DiskInfo, string) returned due to \"textObject\" being \"null\".");
    }

    public static async Task<string> DownloadAsBytes(string url)
    {
        url = ProperFormatUrl(url);
        if (string.IsNullOrWhiteSpace(url))
        {
            Print($"Invalid URL format: {url}", LogType.Error);
            return null;
        }

        if (Application.platform == RuntimePlatform.PS4)
        {
            int size;
            IntPtr ptr = UOB.DownloadAsBytes(url, out size);

            if (ptr == IntPtr.Zero || size == 0)
            {
                Print("Download returned empty response", LogType.Error);
                return null;
            }

            byte[] bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);
            return Latin1.GetString(bytes);
        }

        int maxRedirects = 5;
        for (int i = 0; i < maxRedirects; i++)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.isNetworkError || request.isHttpError)
                {
                    Print($"Failed to download from {url}:\nError: {request.error}", LogType.Error);
                    return null;
                }

                if (request.responseCode >= 300 && request.responseCode < 400)
                {
                    string newUrl = request.GetResponseHeader("Location");
                    if (!string.IsNullOrEmpty(newUrl))
                    {
                        url = newUrl.StartsWith("http") ? newUrl : new Uri(new Uri(url), newUrl).ToString();
                        continue;
                    }
                }

                byte[] data = request.downloadHandler.data;
                if (data == null || data.Length == 0)
                {
                    Print("Download returned empty response", LogType.Error);
                    return null;
                }

                return Latin1.GetString(data);
            }
        }

        Print($"Too many redirects: {url}", LogType.Error);
        return null;
    }

    /*
        public static async Task<string> DownloadRangeBytes(string url, uint offset, uint size)
        {
            url = ProperFormatUrl(url);
            if (string.IsNullOrWhiteSpace(url))
            {
                Print($"Invalid URL format: {url}", LogType.Error);
                return null;
            }

            if (Application.platform == RuntimePlatform.PS4)
            {
                try
                {
                    int outSize;
                    IntPtr ptr = UOB.DownloadAsBytesRange(url, offset, size, out outSize);
                    if (ptr == IntPtr.Zero || outSize == 0)
                        return null;
                    int len = outSize;
                    if (size > 0 && outSize >= (int)size)
                        len = (int)size;
                    var managed = new byte[len];
                    Marshal.Copy(ptr, managed, 0, len);
                    return Latin1.GetString(managed);
                }
                catch (Exception ex)
                {
                    Print($"Failed to download range via plugin: {ex.Message}", LogType.Error);
                    return null;
                }
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Range", "bytes=" + offset + "-" + (offset + size - 1));
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (request.isNetworkError || request.isHttpError)
                {
                    Print($"Failed to download range from {url}:\nError: {request.error}", LogType.Error);
                    return null;
                }

                var bytes = request.downloadHandler.data;
                if (bytes == null || bytes.Length == 0) return null;
                return Latin1.GetString(bytes);
            }
        }
     */

    public static bool SetImageFromURL(string url, ref RawImage image)
    {
        url = ProperFormatUrl(url);
        if (string.IsNullOrWhiteSpace(url))
        {
            Print($"Invalid URL format: {url}", LogType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        byte[] imageBytes = null;

        if (Application.platform == RuntimePlatform.PS4)
        {
            int size;
            IntPtr dataPtr = UOB.DownloadAsBytes(url, out size);

            if (dataPtr == IntPtr.Zero || size <= 1256)
            {
                Print("Failed to download image or invalid pointer/size.", LogType.Error);
                image.gameObject.SetActive(false);
                return false;
            }

            imageBytes = new byte[size];
            Marshal.Copy(dataPtr, imageBytes, 0, size);
        }
        else
        {
            try
            {
                UnityWebRequest request = UnityWebRequest.Get(url);
                var operation = request.SendWebRequest();

                while (!operation.isDone) { }

                if (request.isNetworkError || request.isHttpError)
                {
                    Print($"Failed to download image: {request.error}", LogType.Error);
                    image.gameObject.SetActive(false);
                    return false;
                }

                imageBytes = request.downloadHandler.data;
            }
            catch (Exception ex)
            {
                Print($"Failed to download image from {url}:\nError: {ex.GetType().Name}\nMessage: {ex.Message}", LogType.Error);
                image.gameObject.SetActive(false);
                return false;
            }
        }

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Print("Downloaded image bytes are invalid.", LogType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        if (imageBytes.Length < 2 ||
            !(imageBytes[0] == 0xFF
            && imageBytes[1] == 0xD8) && // JPG
            !(imageBytes.Length >= 4
            && imageBytes[0] == 0x89 &&
            imageBytes[1] == 0x50
            && imageBytes[2] == 0x4E
            && imageBytes[3] == 0x47) && // PNG
            !(imageBytes[0] == 0x42
            && imageBytes[1] == 0x4D))   // BMP
        {
            Print("Downloaded image is not a valid image.", LogType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        Texture2D texture = new Texture2D(2, 2);
        if (!texture.LoadImage(imageBytes) || texture.width == 0 || texture.height == 0)
        {
            Print("Failed to load image from bytes or is invalid.", LogType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        image.texture = texture;
        image.gameObject.SetActive(true);
        return true;
    }

    public static bool SetImageFromBytes(byte[] imageBytes, ref RawImage image)
    {
        if (imageBytes == null || imageBytes.Length == 0)
        {
            Print("Downloaded image bytes are invalid.", LogType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        if (imageBytes.Length < 2 ||
            !(imageBytes[0] == 0xFF
            && imageBytes[1] == 0xD8) && // JPG
            !(imageBytes.Length >= 4
            && imageBytes[0] == 0x89 &&
            imageBytes[1] == 0x50
            && imageBytes[2] == 0x4E
            && imageBytes[3] == 0x47) && // PNG
            !(imageBytes[0] == 0x42
            && imageBytes[1] == 0x4D))   // BMP
        {
            Print("Downloaded image is not a valid image.", LogType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        Texture2D texture = new Texture2D(2, 2);
        if (!texture.LoadImage(imageBytes) || texture.width == 0 || texture.height == 0)
        {
            Print("Failed to load image from bytes or is invalid.", LogType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        image.texture = texture;
        image.gameObject.SetActive(true);
        return true;
    }

}