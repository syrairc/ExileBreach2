using System;
using System.Runtime.InteropServices;
using GameOffsets2.Native;

namespace ExileBreach2;

public static class BreachOffsets
{
    public const int ServerDataMechanicHandlers = 0x23E8;

    public const int ExpandingBrequelRulesetId = 92;
    public const int EstablishedBrequelRulesetId = 93;
    public const int BrequelBarrierRulesetId = 94;
}

public enum BreachPhase
{
    Unopened = 0,
    Expanding = 1,
    Stabilised = 2,
    Collapsing = 3,
    Done = 4,
}

public enum HivePhase
{
    Idle = 0,
    Running = 1,
}

[StructLayout(LayoutKind.Explicit, Size = 0x50)]
public struct RulesetHeader
{
    [FieldOffset(0x30)] public int Id;
    [FieldOffset(0x48)] public long Owner;
}

[StructLayout(LayoutKind.Explicit, Size = 0x1B0)]
public struct ExpandingBrequelHandler
{
    [FieldOffset(0x000)] public RulesetHeader Ruleset;
    [FieldOffset(0x190)] public StdVector Slots;
    [FieldOffset(0x1A8)] public float MaxRadius;
}

[StructLayout(LayoutKind.Explicit, Size = 0x50)]
public struct BreachSlot
{
    public const float RadiusSaturation = 100f;
    public const float RadiusExponent = 0.6f;
    public const float RadiusReference = 300f;

    [FieldOffset(0x00)] public float Current;
    [FieldOffset(0x04)] public float Target;
    [FieldOffset(0x08)] public float Duration;
    [FieldOffset(0x0C)] public float Elapsed;
    [FieldOffset(0x10)] public float Countdown;
    [FieldOffset(0x18)] public int RatePct;
    [FieldOffset(0x20)] public byte Initialised;
    [FieldOffset(0x24)] public int GridX;
    [FieldOffset(0x28)] public int GridY;
    [FieldOffset(0x38)] public BreachPhase Phase;
    [FieldOffset(0x40)] public long OwnerHandler;

    public readonly float RawRadius =>
        Duration <= 0f ? Current : Current + (Target - Current) * Math.Clamp(Elapsed / Duration, 0f, 1f);

    public readonly float GridRadius
    {
        get
        {
            var raw = RawRadius;
            return raw <= 0f ? 0f : MathF.Pow(MathF.Min(raw / RadiusSaturation, 1f), RadiusExponent) * RadiusReference;
        }
    }
}

[StructLayout(LayoutKind.Explicit, Size = 0x1E0)]
public struct EstablishedBrequelHandler
{
    [FieldOffset(0x000)] public RulesetHeader Ruleset;
    [FieldOffset(0x1BC)] public int AilithSegments;
    [FieldOffset(0x1C0)] public int AilithSegmentsMax;
    [FieldOffset(0x1C8)] public byte Wave;
    [FieldOffset(0x1C9)] public byte WaveTotal;
    [FieldOffset(0x1CC)] public HivePhase Phase;
}
