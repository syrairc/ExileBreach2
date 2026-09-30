using System;
using System.Collections.Generic;
using System.Drawing;
using ExileCore2;
using ExileCore2.PoEMemory.Components;
using ExileCore2.PoEMemory.MemoryObjects;
using ExileCore2.Shared.Enums;
using ExileCore2.Shared.Helpers;
using GameOffsets2.Native;
using Vector2 = System.Numerics.Vector2;

namespace ExileBreach2;

public class ExileBreach2Core : BaseSettingsPlugin<ExileBreach2Settings>
{
    private const string IconsFile = "Icons.png";
    private const int MaxHandlerCount = 64;
    private const long MaxSlotVectorBytes = 0x1000;
    private const int TransitionableOpen = 1;
    private const int TransitionableClosed = 2;
    private const float UnopenedGridRadius = 4f;
    private const float UnopenedIconSize = 24f;
    private const float WallGridRadius = 5f;
    private const float GateBarLengthScale = 0.5f;
    private const float GateBarThickness = 6f;
    private const int RingSegments = 64;
    private const int MarkerSegments = 12;

    private readonly List<Entity> _walls = new();
    private readonly List<Entity> _gates = new();
    private uint? _entitiesVersion;
    private StdVector _handlerVector;
    private long _expandingHandler;
    private long _establishedHandler;

    public override bool Initialise()
    {
        Graphics.InitImage(IconsFile);
        return true;
    }

    public override void AreaChange(AreaInstance area)
    {
        _walls.Clear();
        _gates.Clear();
        _entitiesVersion = null;
        _handlerVector = default;
        _expandingHandler = 0;
        _establishedHandler = 0;
    }

    public override void Render()
    {
        RefreshHandlers();
        if (Settings.DrawWalls.Value || Settings.DrawGates.Value)
            RefreshEntities();
        DrawBreaches();
        DrawWalls();
        DrawGates();
        DrawHiveReadout();
    }

    private void RefreshHandlers()
    {
        var serverData = GameController.IngameState?.Data?.ServerData?.Address ?? 0;
        var vector = serverData == 0
            ? default
            : GameController.Memory.Read<StdVector>(serverData + BreachOffsets.ServerDataMechanicHandlers);
        if (vector.First == _handlerVector.First && vector.Last == _handlerVector.Last)
            return;

        _handlerVector = vector;
        _expandingHandler = 0;
        _establishedHandler = 0;

        var count = vector.ElementCount<long>();
        if (vector.First == 0 || count <= 0 || count > MaxHandlerCount)
            return;

        foreach (var handler in GameController.Memory.ReadStdVector<long>(vector))
        {
            switch (GameController.Memory.Read<RulesetHeader>(handler).Id)
            {
                case BreachOffsets.ExpandingBrequelRulesetId:
                    _expandingHandler = handler;
                    break;
                case BreachOffsets.EstablishedBrequelRulesetId:
                    _establishedHandler = handler;
                    break;
            }
        }
    }

    private void RefreshEntities()
    {
        var entityList = GameController.EntityListWrapper;
        if (entityList.EntitiesVersion == _entitiesVersion)
            return;

        _entitiesVersion = entityList.EntitiesVersion;
        _walls.Clear();
        _gates.Clear();

        foreach (var entity in entityList.Entities)
        {
            var path = entity.Path;
            if (path == null)
                continue;

            if (path.EndsWith("BrequelBlocker", StringComparison.Ordinal))
                _walls.Add(entity);
            else if (path.Contains("BreachTower/Objects/", StringComparison.Ordinal)
                     && (path.EndsWith("ArmBlockage", StringComparison.Ordinal)
                         || path.EndsWith("ArmBlockageCore", StringComparison.Ordinal)))
                _gates.Add(entity);
        }
    }

    private void DrawBreaches()
    {
        if (_expandingHandler == 0 || (!Settings.DrawBreaches.Value && !Settings.DrawUnopened.Value))
            return;

        var memory = GameController.Memory;
        var slots = memory.Read<ExpandingBrequelHandler>(_expandingHandler).Slots;
        if (slots.First == 0 || slots.Last <= slots.First || slots.Last - slots.First > MaxSlotVectorBytes)
            return;

        foreach (var address in memory.ReadStdVector<long>(slots))
        {
            var slot = memory.Read<BreachSlot>(address);
            if (slot.Initialised == 0 || slot.Phase == BreachPhase.Done || (slot.GridX == 0 && slot.GridY == 0))
                continue;

            var grid = new Vector2(slot.GridX, slot.GridY);
            var color = PhaseColor(slot.Phase);
            var radius = slot.GridRadius;
            if (radius > 0f)
            {
                if (Settings.DrawBreaches.Value)
                    Ring(grid, radius, color, RingSegments, true, true);
                continue;
            }

            if (!Settings.DrawUnopened.Value)
                continue;

            Ring(grid, UnopenedGridRadius, color, MarkerSegments, true, false);
            if (Settings.DrawOnMap.Value)
                Graphics.DrawImageOnMap(IconsFile, SpriteHelper.GetUV(MapIconsIndex.BreachHandFaceLeft), color,
                    grid, grid, UnopenedIconSize, VisibleSubMap.Any);
        }
    }

    private void DrawWalls()
    {
        if (!Settings.DrawWalls.Value)
            return;

        foreach (var wall in _walls)
        {
            var blockage = wall.GetComponent<TriggerableBlockage>();
            var positioned = wall.GetComponent<Positioned>();
            if (blockage == null || positioned == null)
                continue;

            var color = blockage.IsOpened ? Settings.OpenColor.Value : Settings.ClosedColor.Value;
            Ring(positioned.GridPos, WallGridRadius, color, MarkerSegments, true, true);
        }
    }

    private void DrawGates()
    {
        if (!Settings.DrawGates.Value || !Settings.DrawOnMap.Value)
            return;

        foreach (var gate in _gates)
        {
            var positioned = gate.GetComponent<Positioned>();
            if (positioned == null)
                continue;

            switch (gate.GetComponent<Transitionable>()?.Flag1 ?? 0)
            {
                case TransitionableOpen:
                    Ring(positioned.GridPos, WallGridRadius, Settings.OpenColor.Value, MarkerSegments, false, true);
                    break;
                case TransitionableClosed when !TryDrawGateBar(gate):
                    Ring(positioned.GridPos, WallGridRadius, Settings.ClosedColor.Value, MarkerSegments, false, true);
                    break;
            }
        }
    }

    private bool TryDrawGateBar(Entity gate)
    {
        var blockage = gate.GetComponent<TriggerableBlockage>();
        if (blockage == null || blockage.Max.X <= blockage.Min.X || blockage.Max.Y <= blockage.Min.Y)
            return false;

        var size = blockage.Max - blockage.Min;
        var centre = (blockage.Min + blockage.Max) * 0.5f;
        var half = MathF.Max(size.X, size.Y) * 0.5f * GateBarLengthScale;
        var along = size.X >= size.Y ? new Vector2(half, 0f) : new Vector2(0f, half);
        Graphics.DrawLineOnMap(centre - along, centre + along, GateBarThickness, Settings.ClosedColor.Value,
            VisibleSubMap.Any);
        return true;
    }

    private void DrawHiveReadout()
    {
        if (!Settings.ShowHiveReadout.Value || _establishedHandler == 0)
            return;

        var hive = GameController.Memory.Read<EstablishedBrequelHandler>(_establishedHandler);
        if (hive.Phase != HivePhase.Running)
            return;

        Graphics.DrawText($"Wave {hive.Wave}/{hive.WaveTotal}   Ailith {hive.AilithSegments}/{hive.AilithSegmentsMax}",
            new Vector2(Settings.ReadoutX.Value, Settings.ReadoutY.Value));
    }

    private void Ring(Vector2 grid, float radius, Color color, int segments, bool inWorld, bool onMap)
    {
        if (inWorld && Settings.DrawInWorld.Value)
            Graphics.DrawCircleInWorld(GameController.IngameState.Data.ToWorldWithTerrainHeight(grid),
                radius / PoeMapExtension.WorldToGridConversion, color, Settings.Thickness.Value, segments, true);

        if (onMap && Settings.DrawOnMap.Value)
            Graphics.DrawCircleOnMap(grid, true, radius, color, Settings.Thickness.Value, segments, VisibleSubMap.Any);
    }

    private Color PhaseColor(BreachPhase phase) => phase switch
    {
        BreachPhase.Expanding => Settings.ExpandingColor.Value,
        BreachPhase.Stabilised => Settings.StabilisedColor.Value,
        BreachPhase.Collapsing => Settings.CollapsingColor.Value,
        _ => Settings.UnopenedColor.Value,
    };
}
