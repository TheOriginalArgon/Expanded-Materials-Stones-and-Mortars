using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace ExpandedMaterialsStones
{
    public class CompProperties_DiggingSpot : CompProperties
    {
        public TerrainResourceDef terrainResources;

        public CompProperties_DiggingSpot()
        {
            compClass = typeof(CompDiggingSpot);
        }
    }

    public class CompDiggingSpot : ThingComp
    {
		public float portionProgress;

        public float delay = 700f;

		public int lastUsedTick = -99999;

        public bool exhausted = false;

        private int resourceGatheredTotal = 0;

        public CompProperties_DiggingSpot Props => (CompProperties_DiggingSpot)props;

        public TerrainResourceDef terrainResources => Props.terrainResources;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref portionProgress, "portionProgress", 0f);
            Scribe_Values.Look(ref delay, "delay", 0f);
            Scribe_Values.Look(ref lastUsedTick, "lastUsedTick", 0);
            Scribe_Values.Look(ref exhausted, "exhausted", false);
            Scribe_Values.Look(ref resourceGatheredTotal, "resourceGatheredTotal", 0);
        }

        public override void PostDeSpawn(Map map)
        {
            portionProgress = 0f;
            delay = 700f;
            lastUsedTick = -99999;
        }

        public void DigWorkDone(Pawn driller)
		{
			float statValue = driller.GetStatValue(StatDefOf.MiningSpeed);
			portionProgress += statValue;
			//portionYieldPct += statValue * driller.GetStatValue(StatDefOf.MiningYield) / 3000f;
			lastUsedTick = Find.TickManager.TicksGame;
			if (portionProgress > delay)
			{
				TryDigResource(driller);
				portionProgress = 0f;
				//portionYieldPct = 0f;
            }
        }

        private void TryDigResource(Pawn pawn)
        {
            TerrainDef terrainTile = parent.Map.terrainGrid.TerrainAt(parent.Position);

            if (terrainResources.terrains.Contains(terrainTile))
            {
                int index = terrainResources.terrains.IndexOf(terrainTile);
                Thing resource = ThingMaker.MakeThing(terrainResources.resources[index]);
                resource.stackCount = Mathf.RoundToInt((float)pawn.skills.GetSkill(SkillDefOf.Mining).Level * Rand.Range(terrainResources.minChance[index], terrainResources.maxChance[index]));
                delay += Rand.Range(20, 60);
                GenPlace.TryPlaceThing(resource, parent.InteractionCell, parent.Map, ThingPlaceMode.Near);
                resourceGatheredTotal += resource.stackCount;
                if (Rand.Range(0, 100) < 2)
                {
                    parent.Destroy();
                    IntVec3 pos = parent.PositionHeld;
                    Map map = parent.MapHeld;
                    Messages.Message("EM_DiggingSpotCollapsed".Translate(parent.Label, parent).CapitalizeFirst(), new TargetInfo(pos, map), MessageTypeDefOf.NegativeEvent);
                }
            }
        }


        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();

            // Gathering information for the first part of the string.
            TerrainDef terrainTile = parent.Map.terrainGrid.TerrainAt(parent.Position);
            int index = terrainResources.terrains.IndexOf(terrainTile);
            string resourceName = terrainResources.resources[index].label;

            // Setting the first line.
            stringBuilder.Append("EM_ResourceBelowSpot".Translate(resourceName));

            //Debug line to check the collapsing function
            stringBuilder.AppendInNewLine("DEBUG_ReourceGathered".Translate(resourceGatheredTotal.ToString()));



            return stringBuilder.ToString();
        }

    }

}
