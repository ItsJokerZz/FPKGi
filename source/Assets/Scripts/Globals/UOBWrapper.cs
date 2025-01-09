using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
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

                Print("FAILED TO DETECT TEMP! (THIS SHOULDN'T HAPPEN! REPORT TO \"itsjokerzz / ItsJokerZz#3022\" on Discord)\n" +
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

    public static void Print(string message, PrintType type = PrintType.Default)
    {
        if (string.IsNullOrEmpty(message) ||
            (type == PrintType.Warning && DisablePrintWarnings)) return;

        if (Application.platform == RuntimePlatform.PS4)
            UOB.PrintToConsole(message, Logging.GetLogType(type));
        else Logging.LogMessage(message, type);
    }

    public static void Print(bool saveLog = false, PrintType type = PrintType.Default,
        string message = null, string filePath = "/data/UnityOrbisBridge.log")
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

    public static string DownloadAsBytes(string url)
    {
        if (Application.platform == RuntimePlatform.PS4)
        {
            int size;

            IntPtr ptr = UOB.DownloadAsBytes(url, out size);
            byte[] bytes = new byte[size];
            Marshal.Copy(ptr, bytes, 0, size);

            return Encoding.UTF8.GetString(bytes).Replace("\r", "").Replace("\n", "");
        }

        return null;
    }

    public static bool SetImageFromURL(string url, ref RawImage image)
    {
        if (Application.platform != RuntimePlatform.PS4) return false;

        if (!Regex.IsMatch(url, @"^(http(s)?:\/\/)?(www\.)?[a-zA-Z0-9\-]+(\.[a-zA-Z]{2,})+"))
        {
            Print($"Invalid URL format: {url}", PrintType.Default);
            return false;
        }

        int size;
        IntPtr dataPtr = UOB.DownloadAsBytes(url, out size);

        if (dataPtr == IntPtr.Zero || size <= 1256)
        {
            image.gameObject.SetActive(false);

            Print("Failed to download image or invalid pointer/size.", PrintType.Error);
            return false;
        }

        byte[] imageBytes = new byte[size];
        Marshal.Copy(dataPtr, imageBytes, 0, size);

        if (imageBytes.Length == 0)
        {
            image.gameObject.SetActive(false);

            Print("Downloaded image bytes are invalid.", PrintType.Error);
            return false;
        }

        Texture2D texture = new Texture2D(2, 2);

        if (texture.LoadImage(imageBytes))
        {
            image.texture = texture;
            image.gameObject.SetActive(true);
            return true;
        }
        else
        {
            image.gameObject.SetActive(false);

            Print("Failed to load image from bytes.", PrintType.Error);
            return false;
        }
    }
}
