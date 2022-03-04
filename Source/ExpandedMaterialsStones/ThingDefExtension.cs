using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace ExpandedMaterialsStones
{
    class ThingDefExtension : DefModExtension
    {
        [Description("Adds varying yield products")]
        public List<ThingDef> resources = new List<ThingDef>();
        public List<int> amount = new List<int>();
    }

    class RecipeDefExtension : DefModExtension
    {
        [Description("Makes a recipe suitable for the varying yield products")]
        public ThingDef recipeTargetDef;
    }
}
