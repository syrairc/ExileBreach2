using System.Drawing;
using ExileCore2.Shared.Attributes;
using ExileCore2.Shared.Interfaces;
using ExileCore2.Shared.Nodes;

namespace ExileBreach2;

public class ExileBreach2Settings : ISettings
{
    public ToggleNode Enable { get; set; } = new(false);

    [Menu("Draw in world")]
    public ToggleNode DrawInWorld { get; set; } = new(true);

    [Menu("Draw on map")]
    public ToggleNode DrawOnMap { get; set; } = new(true);

    [Menu("Line thickness")]
    public RangeNode<float> Thickness { get; set; } = new(2f, 1f, 8f);

    [Menu("Unstable breaches")]
    public ToggleNode DrawBreaches { get; set; } = new(true);

    [Menu("Expanding breach")]
    public ColorNode ExpandingColor { get; set; } = new(Color.MediumPurple);

    [Menu("Stabilised breach")]
    public ColorNode StabilisedColor { get; set; } = new(Color.Gold);

    [Menu("Collapsing breach")]
    public ColorNode CollapsingColor { get; set; } = new(Color.OrangeRed);

    [Menu("Unopened breaches")]
    public ToggleNode DrawUnopened { get; set; } = new(true);

    [Menu("Unopened breach")]
    public ColorNode UnopenedColor { get; set; } = new(Color.DimGray);

    [Menu("Hive walls")]
    public ToggleNode DrawWalls { get; set; } = new(true);

    [Menu("Fortress gates")]
    public ToggleNode DrawGates { get; set; } = new(true);

    [Menu("Open wall")]
    public ColorNode OpenColor { get; set; } = new(Color.LimeGreen);

    [Menu("Closed wall")]
    public ColorNode ClosedColor { get; set; } = new(Color.Firebrick);

    [Menu("Hive readout")]
    public ToggleNode ShowHiveReadout { get; set; } = new(true);

    [Menu("Readout X")]
    public RangeNode<int> ReadoutX { get; set; } = new(400, 0, 4000);

    [Menu("Readout Y")]
    public RangeNode<int> ReadoutY { get; set; } = new(120, 0, 2000);
}
