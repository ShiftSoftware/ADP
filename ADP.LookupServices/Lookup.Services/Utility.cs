using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ShiftSoftware.ADP.Lookup.Services;

public class Utility
{
    public static string GetLocalizedText(Dictionary<string, string> multiLingualText, string language)
    {
        string result = null;

        if (multiLingualText?.Count == 0)
            return null;

        string shorLanguage = string.Empty;

        if (string.IsNullOrWhiteSpace(language))
            result = multiLingualText?.FirstOrDefault(x => x.Key.ToLower() == "en").Value;
        else
        {
            if (language.Length <= 2)
                shorLanguage = language.ToLower();
            else
                shorLanguage = language.Substring(0, 2).ToLower();

            if (!multiLingualText.TryGetValue(shorLanguage, out result))
                result = multiLingualText?.FirstOrDefault(x => x.Key.ToLower() == "en").Value;
        }

        return result;
    }

    public static string GetLocalizedText(string multiLingualText, string language)
    {
        if (string.IsNullOrWhiteSpace(multiLingualText))
            return null;

        var defaultVale = new Dictionary<string, string>();
        defaultVale.Add("en", multiLingualText);

        try
        {
            return GetLocalizedText(
            JsonSerializer.Deserialize<Dictionary<string, string>>(multiLingualText) ?? defaultVale,
            language);
        }
        catch (Exception)
        {
            return GetLocalizedText(defaultVale, language);
        }
    }

    public static string GenerateBlobStorageFullUrl(string blobBaseUrl, string containerName,string filePath)
    {
        if (string.IsNullOrEmpty(blobBaseUrl) || string.IsNullOrEmpty(containerName))
            throw new InvalidOperationException("Base URL or container is not set.");

        // Create the base Uri
        Uri baseUri = new Uri(blobBaseUrl);

        // Combine the base Uri with the container and file partial URL
        Uri containerUri = new Uri(baseUri, containerName + "/");

        if (filePath.StartsWith("/") || filePath.StartsWith("\\"))
            filePath = filePath.Substring(1);

        Uri fullUri = new Uri(containerUri, filePath);

        // Encode the full URL
        return Uri.EscapeUriString(fullUri.ToString());
    }
}