using Godot;

/// <summary>
///   This is the first autoloaded class. Used to perform some actions that should happen
///   as the first things in the game
/// </summary>
public class StartupActions : Node
{
    private StartupActions()
    {
        var userDir = OS.GetUserDataDir().Replace('\\', '/');

        GD.Print("user:// directory is: ", userDir);

        // Load settings here, to make sure locales etc. are applied to the main loaded and autoloaded scenes
        if (Settings.Instance == null)
            GD.PrintErr("Failed to initialize settings.");
    }
}
