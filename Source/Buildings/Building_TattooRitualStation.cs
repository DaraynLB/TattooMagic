using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TattooMagic
{
    public class Building_TattooRitualStation : Building
    {
        // The recipient stands one cell past the far edge of the station's
        // footprint, on the opposite side from the performer's interaction
        // cell (which sits one cell before the near edge). Position is the
        // footprint's anchor cell (its near/south-west corner when
        // unrotated), not its center, so this has to be footprint-size-
        // aware rather than a simple mirror through Position — mirroring
        // through Position was the bug: for anything bigger than 1x1 it
        // put the "opposite side" cell inside or overlapping the building
        // itself instead of past its far edge.
        public IntVec3 RecipientSpotCell => Position + new IntVec3(0, 0, def.size.z).RotatedBy(Rotation);

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
                yield return gizmo;

            yield return new Command_Action
            {
                defaultLabel = "Apply tattoo ritual",
                defaultDesc = "Choose a colonist and a tattoo to apply at this station.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TattooRitual", false),
                action = () => Find.WindowStack.Add(new Dialog_ChooseTattoo(this)),
            };

            yield return new Command_Action
            {
                defaultLabel = "Tattoo codex",
                defaultDesc = "Browse every tattoo this station can apply and what they do.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TattooCodex", false),
                action = () => Find.WindowStack.Add(new Dialog_TattooCodex()),
            };
        }
    }
}
