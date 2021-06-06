using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Newtonsoft.Json;
using File = Godot.File;

public class ConfigurationManager : Node
{
    private static ConfigurationManager instance;

    private readonly List<NamedInputGroup> inputGroups;

    /// <summary>
    ///   Loads the configuration parameters from JSON files
    /// </summary>
    private ConfigurationManager()
    {
        instance = this;

        inputGroups = LoadListRegistry<NamedInputGroup>("res://configuration/input_options.json");

        GD.Print("ConfigurationManager loading ended");

        CheckForInvalidValues();

        GD.Print("ConfigurationManager are good");
    }

    public static ConfigurationManager Instance => instance;

    public IEnumerable<NamedInputGroup> InputGroups => inputGroups;

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged)
        {
            ApplyTranslations();
        }
    }

    /// <summary>
    ///   Applies translations to all registry loaded types. Called whenever the locale is changed
    /// </summary>
    public void ApplyTranslations()
    {
        ApplyRegistryObjectTranslations(inputGroups);
    }

    private static void CheckRegistryType<T>(Dictionary<string, T> registry)
        where T : class, IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.Value.InternalName = entry.Key;
            entry.Value.Check(entry.Key);
        }
    }

    private static void CheckRegistryType<T>(IEnumerable<T> registry)
        where T : class, IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.Check(string.Empty);

            if (string.IsNullOrEmpty(entry.InternalName))
                throw new Exception("registry list type should set internal name in Check");
        }
    }

    private static void ApplyRegistryObjectTranslations<T>(Dictionary<string, T> registry)
        where T : class, IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.Value.ApplyTranslations();
        }
    }

    private static void ApplyRegistryObjectTranslations<T>(IEnumerable<T> registry)
        where T : class, IRegistryType
    {
        foreach (var entry in registry)
        {
            entry.ApplyTranslations();
        }
    }

    private static string ReadJSONFile(string path)
    {
        using var file = new File();
        file.Open(path, File.ModeFlags.Read);
        var result = file.GetAsText();

        // This might be completely unnecessary
        file.Close();

        return result;
    }

    private Dictionary<string, T> LoadRegistry<T>(string path, JsonConverter[] extraConverters = null)
    {
        extraConverters ??= Array.Empty<JsonConverter>();

        var result = JsonConvert.DeserializeObject<Dictionary<string, T>>(ReadJSONFile(path), extraConverters);

        if (result == null)
            throw new InvalidDataException("Could not load a registry from file: " + path);

        GD.Print($"Loaded registry for {typeof(T)} with {result.Count} items");
        return result;
    }

    private List<T> LoadListRegistry<T>(string path, JsonConverter[] extraConverters = null)
    {
        extraConverters ??= Array.Empty<JsonConverter>();

        var result = JsonConvert.DeserializeObject<List<T>>(ReadJSONFile(path), extraConverters);

        if (result == null)
            throw new InvalidDataException("Could not load a registry from file: " + path);

        GD.Print($"Loaded registry for {typeof(T)} with {result.Count} items");
        return result;
    }

    private T LoadDirectObject<T>(string path, JsonConverter[] extraConverters = null)
        where T : class
    {
        extraConverters ??= Array.Empty<JsonConverter>();

        var result = JsonConvert.DeserializeObject<T>(ReadJSONFile(path), extraConverters);

        if (result == null)
            throw new InvalidDataException("Could not load a registry from file: " + path);

        return result;
    }

    private void CheckForInvalidValues()
    {
        CheckRegistryType(inputGroups);
    }

}
