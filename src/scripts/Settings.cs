using System;
using System.Globalization;
using System.Linq;
using Godot;
using Newtonsoft.Json;

/// <summary>
///   Class that handles storing and applying player changeable game settings.
/// </summary>
public class Settings
{
    private static readonly string DefaultLanguageValue = TranslationServer.GetLocale();
    private static readonly CultureInfo DefaultCultureValue = CultureInfo.CurrentCulture;
    private static readonly InputDataList DefaultControls = GetCurrentlyAppliedControls();

    /// <summary>
    ///   Singleton used for holding the live copy of game settings.
    /// </summary>
    private static readonly Settings SingletonInstance = InitializeGlobalSettings();

    static Settings()
    {
    }

    private Settings()
    {
        // This is mainly just to make sure the property is read here before anyone can change TranslationServer locale
        if (DefaultLanguage.Length < 1)
            GD.PrintErr("Default locale is empty");
    }

    public static Settings Instance => SingletonInstance;

    public static string DefaultLanguage => DefaultLanguageValue;

    public static CultureInfo DefaultCulture => DefaultCultureValue;

    public SettingValue<string> SelectedLanguage { get; set; } = new SettingValue<string>(null);

    /// <summary>
    ///   The current controls of the game.
    ///   It stores the godot actions like g_move_left and
    ///   their associated <see cref="SpecifiedInputKey">SpecifiedInputKey</see>
    /// </summary>
    public SettingValue<InputDataList> CurrentControls { get; set; } =
        new SettingValue<InputDataList>(GetDefaultControls());

    public static bool operator ==(Settings lhs, Settings rhs)
    {
        return Equals(lhs, rhs);
    }

    public static bool operator !=(Settings lhs, Settings rhs)
    {
        return !(lhs == rhs);
    }

    /// <summary>
    ///   Returns the default controls which never change, unless there is a new release.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     This relies on the static member holding the default controls to be initialized before the code has a chance
    ///     to modify the controls.
    ///   </para>
    /// </remarks>
    /// <returns>The default controls</returns>
    public static InputDataList GetDefaultControls()
    {
        return (InputDataList)DefaultControls.Clone();
    }

    /// <summary>
    ///   Returns the currently applied controls. Gathers the data from the godot InputMap.
    ///   Required to get the default controls.
    /// </summary>
    /// <returns>The current inputs</returns>
    public static InputDataList GetCurrentlyAppliedControls()
    {
        return new InputDataList(InputMap.GetActions().OfType<string>()
            .ToDictionary(p => p,
                p => InputMap.GetActionList(p).OfType<InputEventWithModifiers>().Select(
                    x => new SpecifiedInputKey(x)).ToList()));
    }

    /// <summary>
    ///   Tries to return a C# culture info from Godot language name
    /// </summary>
    /// <param name="language">The language name to try to understand</param>
    /// <returns>The culture info</returns>
    public static CultureInfo GetCultureInfo(string language)
    {
        // Perform hard coded translations first
        var translated = TranslateLocaleToCSharp(language);
        if (translated != null)
            language = translated;

        try
        {
            return new CultureInfo(language);
        }
        catch (CultureNotFoundException)
        {
            // Some locales might have "_extra" at the end that C# doesn't understand, because it uses a dash

            if (!language.Contains("_"))
                throw;

            // So we first try converting "_" to "-" and go with that
            language = language.Replace('_', '-');

            try
            {
                return new CultureInfo(language);
            }
            catch (CultureNotFoundException)
            {
                language = language.Split("-")[0];

                GD.Print("Failed to get CultureInfo with whole language name, tried stripping extra, new: ",
                    language);
                return new CultureInfo(language);
            }
        }
    }

    /// <summary>
    ///   Translates a Godot locale to C# locale name
    /// </summary>
    /// <param name="godotLocale">Godot locale</param>
    /// <returns>C# locale name, or null if there is not a premade mapping</returns>
    public static string TranslateLocaleToCSharp(string godotLocale)
    {
        // ReSharper disable StringLiteralTypo
        switch (godotLocale)
        {
            case "eo":
                return "en";
            case "sr_Latn":
                return "sr-Latn-RS";
            case "sr_Cyrl":
                return "sr-Cyrl-RS";
        }

        // ReSharper restore StringLiteralTypo
        return null;
    }

    /// <summary>
    ///   Overrides Native name if an override is set
    /// </summary>
    /// <param name="godotLocale">Godot locale</param>
    /// <returns>Native name, or null if there is not a premade mapping</returns>
    public static string GetLanguageNativeNameOverride(string godotLocale)
    {
        switch (godotLocale)
        {
            case "eo":
                return "Esperanto";
        }

        return null;
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (GetType() != obj.GetType())
        {
            return false;
        }

        return Equals((Settings)obj);
    }

    public bool Equals(Settings obj)
    {
        // Compare all properties in the two objects for equality.
        var type = GetType();

        foreach (var property in type.GetProperties())
        {
            // Returns if any of the properties don't match.
            object thisValue = property.GetValue(this);
            object objValue = property.GetValue(obj);

            if (thisValue != objValue && thisValue?.Equals(objValue) != true)
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        int hashCode = 17;

        var type = GetType();

        foreach (var property in type.GetProperties())
            hashCode ^= property.GetHashCode();

        return hashCode;
    }

    /// <summary>
    ///   Returns a cloned deep copy of the settings object.
    /// </summary>
    public Settings Clone()
    {
        Settings settings = new Settings();
        settings.CopySettings(this);

        return settings;
    }

    /// <summary>
    ///   Loads values from an existing settings object.
    /// </summary>
    public void LoadFromObject(Settings settings)
    {
        CopySettings(settings);
    }

    /// <summary>
    ///   Loads default values.
    /// </summary>
    public void LoadDefaults()
    {
        Settings settings = new Settings();
        CopySettings(settings);
    }

    /// <summary>
    ///   Saves the current settings by writing them to the settings file
    /// </summary>
    /// <returns>True on success, false if the file can't be written.</returns>
    public bool Save()
    {
        using var file = new File();
        var error = file.Open(Constants.CONFIGURATION_FILE, File.ModeFlags.Write);

        if (error != Error.Ok)
        {
            GD.PrintErr("Couldn't open settings file for writing.");
            return false;
        }

        file.StoreString(JsonConvert.SerializeObject(this));

        file.Close();

        return true;
    }

    /// <summary>
    ///   Applies all current settings to any applicable engine systems.
    /// </summary>
    /// <param name="delayedApply">
    ///   If true things that can't be immediately on game startup be applied, are applied later
    /// </param>
    public void ApplyAll(bool delayedApply = false)
    {
        if (delayedApply)
        {
            GD.Print("Doing delayed apply for some settings");

            // If this is not delay applied, this also causes some errors in godot editor output when running
            Invoke.Instance.Queue(ApplyInputSettings);
        }
        else
        {
            ApplyInputSettings();
        }

        ApplyLanguageSettings();
    }

    /// <summary>
    ///   Applies the current controls to the InputMap.
    /// </summary>
    public void ApplyInputSettings()
    {
        CurrentControls.Value.ApplyToGodotInputMap();
    }

    /// <summary>
    ///   Applies current language settings to any applicable engine systems.
    /// </summary>
    public void ApplyLanguageSettings()
    {
        string language = SelectedLanguage.Value;
        CultureInfo cultureInfo;

        // Process locale info in case it isn't exactly right
        if (string.IsNullOrEmpty(language))
        {
            language = DefaultLanguage;
            cultureInfo = DefaultCulture;
        }
        else
        {
            cultureInfo = GetCultureInfo(language);
        }

        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        // Set locale for the game. Called after C# locale change so that string
        // formatting uses could also get updated properly.
        TranslationServer.SetLocale(language);
    }

    /// <summary>
    ///   Loads, initializes and returns the global settings object.
    /// </summary>
    private static Settings InitializeGlobalSettings()
    {
        try
        {
            Settings settings = LoadSettings();

            if (settings == null)
            {
                GD.PrintErr("Loading settings from file failed, using default settings");
                settings = new Settings();
            }

            settings.ApplyAll(true);

            return settings;
        }
        catch (Exception e)
        {
            // Godot doesn't seem to catch this nicely so we print the errors ourselves
            GD.PrintErr("Error initializing global settings: ", e);
            throw;
        }
    }

    /// <summary>
    ///   Creates and returns a settings object loaded from the configuration settings file, or defaults if that fails.
    /// </summary>
    private static Settings LoadSettings()
    {
        using var file = new File();
        var error = file.Open(Constants.CONFIGURATION_FILE, File.ModeFlags.Read);

        if (error != Error.Ok)
        {
            GD.Print("Failed to open settings configuration file, file is missing or unreadable. "
                + "Using default settings instead.");

            var settings = new Settings();
            settings.Save();

            return settings;
        }

        var text = file.GetAsText();

        file.Close();

        try
        {
            return JsonConvert.DeserializeObject<Settings>(text);
        }
        catch
        {
            GD.Print("Failed to deserialize settings file data, data may be improperly formatted. "
                + "Using default settings instead.");

            var settings = new Settings();
            settings.Save();

            return settings;
        }
    }

    /// <summary>
    ///   Debug helper for dumping what C# considers valid locales
    /// </summary>
    private static void DumpValidCSharpLocales()
    {
        GD.Print("Locales (C#):");

        foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures & ~CultureTypes.NeutralCultures))
        {
            GD.Print(culture.DisplayName + " - " + culture.Name);
        }

        GD.Print(string.Empty);
    }

    /// <summary>
    ///   Copies all properties from another settings object to the current one.
    /// </summary>
    private void CopySettings(Settings settings)
    {
        var type = GetType();

        foreach (var property in type.GetProperties())
        {
            if (!property.CanWrite)
                continue;

            // Since the properties we want to copy are SettingValue generics we use the IAssignableSetting
            // interface and AssignFrom method to convert the property to the correct concrete class.
            var setting = (IAssignableSetting)property.GetValue(this);

            setting.AssignFrom(property.GetValue(settings));
        }
    }
}
