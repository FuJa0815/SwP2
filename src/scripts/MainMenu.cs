using Godot;

/// <summary>
///   Class managing the main menu and everything in it
/// </summary>
public class MainMenu : Node
{
    [Export]
    public NodePath GuiAnimationsPath;

    private AnimationPlayer guiAnimations;

    public override void _Ready()
    {
        guiAnimations = GetNode<AnimationPlayer>(GuiAnimationsPath);
    }

    private void NewGamePressed()
    {
        guiAnimations.Play("MenuSlideLeft");
    }

    private void LoadGamePressed()
    {
        guiAnimations.Play("MenuSlideLeft");
    }

    private void OptionsPressed()
    {
        guiAnimations.Play("MenuSlideLeft");
    }

    private void QuitPressed()
    {
        GetTree().Quit();
    }
}
