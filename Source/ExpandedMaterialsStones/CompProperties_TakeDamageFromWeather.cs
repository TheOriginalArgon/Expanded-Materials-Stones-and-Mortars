using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace ExpandedMaterialsStones
{
    class CompProperties_TakeDamageFromWeather : CompProperties
    {
        public float damageRate;

        public string weather;

        public bool indifferentToRoofing;

        public string buildingThing;

        public CompProperties_TakeDamageFromWeather()
        {
            compClass = typeof(CompTakeDamageFromWeather);
        }
    }

    class CompTakeDamageFromWeather : ThingComp
    {
        CompProperties_TakeDamageFromWeather Props => (CompProperties_TakeDamageFromWeather)props;

        public bool CanRepairCurrently { get; private set; }

        public bool CanBeRepaired()
        {
            if (parent != null && parent.Stuff == DefDatabase<ThingDef>.GetNamed(Props.buildingThing) && parent.HitPoints < parent.MaxHitPoints && parent.def.building.repairable)
            {
                if (parent.Map.weatherManager.curWeather.defName == Props.weather)
                {
                    return false;
                }
                return true;
            }
            return true;
        }

        public override void CompTickRare()
        {
            if (parent.Stuff == DefDatabase<ThingDef>.GetNamed(Props.buildingThing))
            {
                WeatherDef weather = DefDatabase<WeatherDef>.GetNamed(Props.weather);
                float dmgRate = Props.damageRate;

                if (parent.Map.weatherManager.curWeather == weather && !Props.indifferentToRoofing)
                {
                    if (!RoofUtility.IsAnyCellUnderRoof(parent))
                    {
                        parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, dmgRate, 0f, -1f));
                    }
                }
                else if (parent.Map.weatherManager.curWeather == weather && Props.indifferentToRoofing)
                {
                    //for (int i = 0; 0 < 4; i++)
                    //{
                    //    IntVec3 loc = parent.Position + GenAdj.CardinalDirections[i];
                    //    if (!loc.InBounds(parent.Map))
                    //    {
                    //        continue;
                    //    }
                    //    IntVec3 checkLocation = loc;
                    //}
                    parent.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, dmgRate, 0f, -1f));
                }
            }
        }

    }
}
