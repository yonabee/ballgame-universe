using System;
using Godot;

public partial class GUI : Control
{
    public static Label SeedText;
    public static Label Height;
    public static Label Objects;
    public static Label Status;
    public static Label Location;
    public static Label Noise1;
    public static Label Noise2;
    public static Label Noise3;
    public static Label Time;
    public static ProgressBar Progress;

    public void Initialize()
    {
        SeedText ??= GetNode<Label>("./Info1/SeedText");
        Height ??= GetNode<Label>("./Info2/HeightText");
        Objects ??= GetNode<Label>("./Info2/ObjectsText");
        Time ??= GetNode<Label>("./Info2/TimeText");
        Status ??= GetNode<Label>("./Info2/StatusText");
        Location ??= GetNode<Label>("./Info2/PositionText");
        Noise1 ??= GetNode<Label>("./Info2/NoiseText1");
        Noise2 ??= GetNode<Label>("./Info2/NoiseText2");
        Noise3 ??= GetNode<Label>("./Info2/NoiseText3");
        Progress ??= GetNode<ProgressBar>("./ProgressBar");
        SeedText.Text = Universe.Seed.ToUpper();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("info_toggle"))
        {
            if (SeedText.Visible && !Objects.Visible)
            {
                Objects.Visible = true;
                Time.Visible = true;
                Status.Visible = true;
                Location.Visible = true;
                Height.Visible = true;
                Noise1.Visible = true;
                Noise2.Visible = true;
                Noise3.Visible = true;
            }
            else if (!SeedText.Visible)
            {
                SeedText.Visible = true;
            }
            else
            {
                SeedText.Visible = false;
                Objects.Visible = false;
                Time.Visible = false;
                Status.Visible = false;
                Location.Visible = false;
                Height.Visible = false;
                Noise1.Visible = false;
                Noise2.Visible = false;
                Noise3.Visible = false;
            }
        }
    }
}
