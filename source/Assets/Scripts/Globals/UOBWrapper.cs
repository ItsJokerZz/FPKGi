using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
        internal static class Temperature
        {
            private static bool hasTempErrorOccurred;
            private static readonly Dictionary<UOB.Temperature, Tuple<float?, float?>> lastTemperatureInfo =
                new Dictionary<UOB.Temperature, Tuple<float?, float?>>
            {
                { UOB.Temperature.CPU, Tuple.Create<float?, float?>(null, null) },
                { UOB.Temperature.SOC, Tuple.Create<float?, float?>(null, null) }
            };

            public static void PrintError()
            {
                if (hasTempErrorOccurred) return;

                Print(true, PrintType.Error, "FAILED TO DETECT TEMP! (THIS SHOULDN'T HAPPEN! REPORT TO \"itsjokerzz / ItsJokerZz#3022\" on Discord)\n" +
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

                textObject.text = $"{sensorString}: {celsiusTemp} °C / {fahrenheitTemp ?? 0f} °F";
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
            public static int GetLogType(PrintType type)
            {
                switch (type)
                {
                    case PrintType.Default: return 0;
                    case PrintType.Warning: return 2;
                    case PrintType.Error: return 3;
                    default: return (int)type;
                }
            }

            public static void LogMessage(string message, PrintType type)
            {
                switch (type)
                {
                    case PrintType.Warning: Debug.LogWarning(message); break;
                    case PrintType.Error: Debug.LogError(message); break;
                    default: Debug.Log(message); break;
                }
            }
        }
    
    }

    public static bool DisablePrintWarnings { get; set; }

    public enum PrintType { Default, Warning, Error }

    // make use logtype and update uob plugin
    public static void Print(string message, PrintType type = PrintType.Default)
    {
        if (string.IsNullOrEmpty(message) ||
            (type == PrintType.Warning && DisablePrintWarnings)) return;

        if (Application.platform == RuntimePlatform.PS4)
            UOB.PrintToConsole(message, Logging.GetLogType(type));
        else Logging.LogMessage(message, type);
    }

    public static void Print(bool saveLog = false, PrintType type = PrintType.Default,
        string message = null, string filePath = "/user/data/UnityOrbisBridge.log")
    {
        if (string.IsNullOrEmpty(message) ||
            (type == PrintType.Warning && DisablePrintWarnings)) return;

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

    public static void UpdateDiskInfo(Text textObject, UOB.DiskInfo info)
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

        string text = UOB.GetDiskInfoAsFormattedText(info, diskInfo);

        if (textObject != null)
            textObject.text = text;
        else
            Print("UpdateDiskInfo(Text, UOB.DiskInfo) returned due to \"textObject\" being \"null\".");
    }

    public static async Task<string> DownloadAsBytes(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        url = Uri.EscapeUriString(url.Trim());
        Uri uri;
        if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
            !Regex.IsMatch(url, @"^(http(s)?):\/\/[^\s\/$.?#].[^\s]*$", RegexOptions.IgnoreCase))
        {
            Print($"Invalid URL format: {url}", PrintType.Default);
            return null;
        }

        if (Application.platform == RuntimePlatform.PS4)
        {
            UOB.BreakFromSandbox();

            int size;
            IntPtr ptr = UOB.DownloadAsBytes(url, out size);
            byte[] bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);

            return Encoding.UTF8.GetString(bytes).Replace("\r", "").Replace("\n", "");
        }

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.isNetworkError || request.isHttpError)
            {
                Print($"Failed to download from {url}:\n" +
                      $"Error: {request.error}", PrintType.Error);
                return null;
            }

            if (request.downloadHandler.data == null || request.downloadHandler.data.Length == 0)
            {
                Print("Download returned empty response", PrintType.Error);
                return null;
            }

            return Encoding.UTF8.GetString(request.downloadHandler.data).Replace("\r", "").Replace("\n", "");
        }
    }

    public static bool SetImageFromURL(string url, ref RawImage image)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        url = Uri.EscapeUriString(url.Trim());
        Uri uri;
        if (!Uri.TryCreate(url, UriKind.Absolute, out uri) ||
            !Regex.IsMatch(url, @"^(http(s)?):\/\/[^\s\/$.?#].[^\s]*$", RegexOptions.IgnoreCase))
        {
            Print($"Invalid URL format: {url}", PrintType.Default);
            return false;
        }

        byte[] imageBytes = null;

        if (Application.platform == RuntimePlatform.PS4)
        {
            int size;
            IntPtr dataPtr = UOB.DownloadAsBytes(url, out size);

            if (dataPtr == IntPtr.Zero || size <= 1256)
            {
                Print("Failed to download image or invalid pointer/size.", PrintType.Error);
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
                request.SendWebRequest();

                while (!request.isDone) { }

                if (request.isNetworkError || request.isHttpError)
                {
                    image.gameObject.SetActive(false);
                    Print($"Failed to download image: {request.error}", PrintType.Error);
                    return false;
                }

                imageBytes = request.downloadHandler.data;
            }
            catch (Exception ex)
            {
                Print($"Failed to download image from {url}:\nError Type: {ex.GetType().Name}\nMessage: {ex.Message}", PrintType.Error);
                image.gameObject.SetActive(false);
                return false;
            }
        }

        if (imageBytes == null || imageBytes.Length == 0)
        {
            Print("Downloaded image bytes are invalid.", PrintType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        if (imageBytes.Length < 2 ||
            !(imageBytes[0] == 0xFF
            && imageBytes[1] == 0xD8) &&  // JPEG/JPG
            !(imageBytes.Length >= 4
            && imageBytes[0] == 0x89 &&
            imageBytes[1] == 0x50
            && imageBytes[2] == 0x4E
            && imageBytes[3] == 0x47) &&  // PNG
            !(imageBytes[0] == 0x42
            && imageBytes[1] == 0x4D))  // BMP
        {
            Print("Downloaded image is not a valid image.", PrintType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        Texture2D texture = new Texture2D(2, 2);

        if (!texture.LoadImage(imageBytes) || texture.width == 0 || texture.height == 0 ||
            texture == Texture2D.whiteTexture || texture == Texture2D.blackTexture)
        {
            Print("Failed to load image from bytes or is invalid.", PrintType.Error);
            image.gameObject.SetActive(false);
            return false;
        }

        image.texture = texture;
        image.gameObject.SetActive(true);
        return true;
    }

}
