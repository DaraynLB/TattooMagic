using Verse;

namespace TattooMagic
{
    // Composed (not inherited) tier/progression-counter state for a passive
    // tattoo effect. An owning HediffComp holds one of these as a plain field
    // and calls ExposeData() from its own CompExposeData() override.
    //
    // scopeKey is passed into TryRegisterQualifyingEvent explicitly rather
    // than stored on this class, since the owning comp always has its own
    // parent.def.defName on hand and this avoids needing a separate
    // lifecycle step to keep a stored scopeKey in sync across save/load.
    public class TattooTierProgress
    {
        public int tier = 1;

        public int progressionCounter;

        public void ExposeData()
        {
            Scribe_Values.Look(ref tier, "tier", 1);
            Scribe_Values.Look(ref progressionCounter, "progressionCounter", 0);
        }

        // Increments progressionCounter and, if not already at tier 2, checks
        // it against the accessor-resolved threshold. Flips tier from 1 to 2
        // in place the first time the threshold is reached and returns true
        // only on that call; returns false on every other call, including
        // all calls once tier 2 has already been reached (the counter keeps
        // incrementing harmlessly past that point).
        public bool TryRegisterQualifyingEvent(string scopeKey, string thresholdKey, int defaultThreshold)
        {
            progressionCounter++;

            if (tier >= 2)
                return false;

            float threshold = TattooEffectValues.Get(scopeKey, thresholdKey, defaultThreshold);
            if (progressionCounter < threshold)
                return false;

            tier = 2;
            return true;
        }
    }
}
