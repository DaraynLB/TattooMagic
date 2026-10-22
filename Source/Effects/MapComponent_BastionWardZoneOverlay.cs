using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    // Purely visual, purely additive — draws a solid, translucent gold fill
    // plus a bold edge line over every active Bastion Ward zone's actual
    // cells (research.md R4's own "a mote failing to render for any reason
    // cannot desync from or block the actual mechanic" design: this reads
    // GameComponent_BastionWardZones.activeZones live, every frame, and owns
    // no state of its own — nothing here can drift from what TryAbsorb/the
    // heal pass actually protect). RimWorld auto-discovers and instantiates
    // every non-abstract MapComponent subclass per map (mirrors
    // GameComponent_BastionWardZones' own auto-discovery, feature 014's
    // established precedent) — needs no manual registration anywhere.
    //
    // GenDraw.DrawFieldEdges alone (the first version of this file) only
    // draws a thin boundary line, not a fill — found too subtle to read as
    // "this ground is warded" at a glance (player feedback). A second
    // attempt added a per-cell fill via SolidColorMaterials.
    // SimpleSolidColorMaterial (ShaderDatabase.SolidColor) — that rendered
    // as solid magenta in-game (Unity's shader-variant-missing fallback
    // color), confirmed via decompiling the installed Assembly-CSharp.dll
    // (ilspycmd) that vanilla's own GenDraw.DrawFieldEdges/DrawDiagonalStripes
    // never use SolidColorMaterials for this at all — both instead build
    // their material via MaterialPool.MatFrom(<a real texture>,
    // ShaderDatabase.Transparent, tintColor, renderQueue) and draw at
    // AltitudeLayer.MetaOverlays, not MapDataOverlay. This version mirrors
    // that exact, proven pattern (BaseContent.WhiteTex as the tint base,
    // same as several other vanilla translucent-quad overlays use).
    //
    // [StaticConstructorOnStartup]: RimWorld's startup check warns about any
    // type holding a static Material field without it (issue #4) — the
    // attribute guarantees static init runs on the main thread, where all
    // asset loading must happen.
    [StaticConstructorOnStartup]
    public class MapComponent_BastionWardZoneOverlay : MapComponent
    {
        private const int RenderQueue = 2900; // matches GenDraw.DrawFieldEdges' own default

        private static readonly Color EdgeColor = new Color(1f, 0.85f, 0.25f, 0.9f);
        private static readonly Color FillColor = new Color(1f, 0.75f, 0.15f, 0.25f);

        private static Material fillMaterial;

        private static Material FillMaterial => fillMaterial ?? (fillMaterial =
            MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.Transparent, FillColor, RenderQueue));

        public MapComponent_BastionWardZoneOverlay(Map map) : base(map)
        {
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            List<BastionWardZone> zones = GameComponent_BastionWardZones.Get()?.activeZones;
            if (zones == null || zones.Count == 0)
                return;

            for (int i = 0; i < zones.Count; i++)
            {
                BastionWardZone zone = zones[i];
                if (zone.map != map)
                    continue;

                List<IntVec3> cells = zone.FieldCells;
                for (int j = 0; j < cells.Count; j++)
                {
                    Vector3 drawPos = cells[j].ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                    Graphics.DrawMesh(MeshPool.plane10, drawPos, Quaternion.identity, FillMaterial, 0);
                }

                GenDraw.DrawFieldEdges(cells, EdgeColor);
            }
        }
    }
}
