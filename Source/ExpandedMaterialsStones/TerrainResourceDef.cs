using System.Collections.Generic;
using Verse;

namespace ExpandedMaterialsStones
{
    public class TerrainResourceDef : Def
    {

        public List<TerrainDef> terrains = new List<TerrainDef>();

        public List<TerrainDef> specialTerrains= new List<TerrainDef>();

        public List<ThingDef> resources = new List<ThingDef>();

        public List<float> minChance = new List<float>();

        public List<float> maxChance = new List<float>();
    }
}
