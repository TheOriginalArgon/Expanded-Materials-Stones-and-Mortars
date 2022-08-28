using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;

namespace ExpandedMaterialsStones
{
    public enum DryStage : byte
    {
        Ruined,
        Fresh,
        Dried
    }
    public class CompProperties_BrickDrying : CompProperties
    {
        public float daysToDryStart = 2f;

        public float daysToRuinStart = -1f;

        public string thingToTurnInto;

        public string thingToRuinInto;

        public int TicksToDryStart => Mathf.RoundToInt(daysToDryStart * 60000f);

        public int TicksToRuinStart => Mathf.RoundToInt(daysToRuinStart * 60000f);

        public CompProperties_BrickDrying()
        {
            compClass = typeof(CompBrickDrying);
        }

        public CompProperties_BrickDrying(float daysToDryStart)
        {
            this.daysToDryStart = daysToDryStart;
        }
    }

    class CompBrickDrying : ThingComp
    {
        private float dryProgressInt;

        private float lightRequired = 0.6f;
        public CompProperties_BrickDrying Props => (CompProperties_BrickDrying)props;

        public float DryProgress
        {
            get
            {
                return dryProgressInt;
            }
            set
            {
                dryProgressInt = value;
            }
        }

        public int TicksUntilDryOrRuinedAtCurrentConditions
        {
            get
            {
                float ambientTemp = parent.AmbientTemperature;
                ambientTemp = Mathf.RoundToInt(ambientTemp);
                return TicksUntilDryOrRuined(ambientTemp);
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        public DryStage Stage
        {
            get
            {
                if (DryProgress < (float)Props.TicksToDryStart && DryProgress > (float)Props.TicksToRuinStart)
                {
                    return DryStage.Fresh;
                }
                if (DryProgress < (float)Props.TicksToRuinStart)
                {
                    return DryStage.Ruined;
                }
                return DryStage.Dried;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref dryProgressInt, "dryProgress", 0f);
        }

        public override void CompTick()
        {
            Tick(1);
        }

        public override void CompTickRare()
        {
            Tick(250);
        }

        public float DryRate(float temp, Map map)
        {
            if ((map.weatherManager.RainRate <= 0f || (map.weatherManager.RainRate != 0f && map.roofGrid.Roofed(parent.Position))) && (map.skyManager.CurSkyGlow > lightRequired && !map.roofGrid.Roofed(parent.Position)))
            {
                if (temp < 2f)
                {
                    return 0.5f;
                }
                if (temp > 22f)
                {
                    float extraTemp = temp - 20f;
                    return 1f + (extraTemp / 10f);
                }
                return 1f;
            }
            if (map.weatherManager.RainRate > 0.01f && !map.roofGrid.Roofed(parent.Position))
            {
                if (temp < 2f)
                {
                    return -0.2f;
                }
                if (temp > 3f)
                {
                    float extraTemp = temp - 3f;
                    return -1f - (extraTemp / 10f);
                }
                return -1f;
            }
            return 0f; //I would like to know how you get the game to actually return this value.
        }

        public int TicksUntilDryOrRuined(float temp)
        {
            float dryRate = DryRate(temp, parent.Map);
            if (dryRate == 0f)
            {
                return 72000000;
            }
            float ticksToDry = (float)Props.TicksToDryStart - DryProgress;
            float ticksToRuin = Mathf.Abs((float)Props.TicksToRuinStart) - DryProgress; // Changed + to -
            if (DryProgress > 0f)
            {
                return Mathf.RoundToInt(ticksToDry / dryRate);
            }
            return Mathf.RoundToInt(ticksToRuin / Mathf.Abs(dryRate));
        }

        private void Tick(int interval)
        {
            float dryProgress = DryProgress;
            float temp = DryRate(parent.AmbientTemperature, parent.Map);
            DryProgress += temp * (float)interval;
            if (Stage == DryStage.Dried)
            {
                Thing thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(Props.thingToTurnInto));
                thing.stackCount = parent.stackCount;
                IntVec3 loc = parent.Position;
                Map map = parent.Map;
                if (parent.IsInAnyStorage() && parent.SpawnedOrAnyParentSpawned)
                {
                    Messages.Message("MessageDriedInStorage".Translate(parent.Label, parent).CapitalizeFirst(), new TargetInfo(parent.PositionHeld, parent.MapHeld), MessageTypeDefOf.NeutralEvent);
                }
                parent.Destroy();
                GenPlace.TryPlaceThing(thing, loc, map, ThingPlaceMode.Direct);
            }
            if (Stage == DryStage.Ruined)
            {
                Thing thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(Props.thingToRuinInto));
                thing.stackCount = parent.stackCount;
                IntVec3 loc = parent.Position;
                Map map = parent.Map;
                if (parent.IsInAnyStorage() && parent.SpawnedOrAnyParentSpawned)
                {
                    Messages.Message("MessageRuinedInStorage".Translate(parent.Label, parent).CapitalizeFirst(), new TargetInfo(parent.PositionHeld, parent.MapHeld), MessageTypeDefOf.NegativeEvent);
                }
                parent.Destroy();
                GenPlace.TryPlaceThing(thing, loc, map, ThingPlaceMode.Direct);
            }
        }

        public override void PreAbsorbStack(Thing otherStack, int count)
        {
            float t = (float)count / (float)(parent.stackCount + count);
            float dryProgress = ((ThingWithComps)otherStack).GetComp<CompBrickDrying>().DryProgress;
            DryProgress = Mathf.Lerp(DryProgress, dryProgress, t);
        }

        public override string CompInspectStringExtra()
        {
            float dryRate = DryRate(Mathf.RoundToInt(parent.AmbientTemperature), parent.Map);
            StringBuilder stringBuilder = new StringBuilder();
            if (dryRate != 0f)
            {
                int ticksUntilDryOrRuined = TicksUntilDryOrRuinedAtCurrentConditions;
                if (dryRate > 0f)
                {
                    stringBuilder.Append("EM_CurrentlyDrying".Translate(ticksUntilDryOrRuined.ToStringTicksToPeriod()) + ".");
                }
                if (dryRate < 0f)
                {
                    stringBuilder.Append("EM_CurrentlyRuining".Translate(Mathf.Abs(ticksUntilDryOrRuined).ToStringTicksToPeriod()) + ".");
                }
            }
            else
            {
                stringBuilder.Append("EM_NotDrying".Translate(lightRequired.ToStringPercent()) + ".");
            }

            // DEBUG.
            stringBuilder.AppendInNewLine("DEBUG_DryRate".Translate(dryRate.ToStringPercent()) + ".");

            return stringBuilder.ToString();
        }

    }
}
