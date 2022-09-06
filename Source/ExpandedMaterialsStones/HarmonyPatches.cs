using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using RimWorld;
using Verse;
using HarmonyLib;

namespace ExpandedMaterialsStones
{
    [StaticConstructorOnStartup]
    public class Patcher
    {
        static Patcher()
        {
            var harmony = new Harmony("Argon.ExpandedMaterials.Stones");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(WorkGiver_Repair))]
    [HarmonyPatch(nameof(WorkGiver_Repair.HasJobOnThing))]
    class Patch_RepairingWalls
    {
        static void Postfix(Pawn pawn, Thing t, bool forced, ref bool __result)
        {
            Building building = t as Building;
            if (building.def.HasComp(typeof(CompTakeDamageFromWeather)) && building.def.IsBuildingArtificial && !building.TryGetComp<CompTakeDamageFromWeather>().CanBeRepaired())
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(GenRecipe))]
    [HarmonyPatch(nameof(GenRecipe.MakeRecipeProducts))]
     class Patch_YieldingProducts
    {
        // Calling a local private function
        private static Func<Thing, RecipeDef, Pawn, Precept_ThingStyle, Thing> postProcessProduct =
            (Func<Thing, RecipeDef, Pawn, Precept_ThingStyle, Thing>)AccessTools.Method(typeof(GenRecipe), "PostProcessProduct").CreateDelegate(typeof(Func<Thing, RecipeDef, Pawn, Precept_ThingStyle, Thing>));

        // Patch
        //static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Precept_ThingStyle precept)
        //{
        //    if (recipeDef.HasModExtension<RecipeDefExtension>())
        //    {
        //        ThingDef targetDef = recipeDef.GetModExtension<RecipeDefExtension>().recipeTargetDef;

        //        for (int i = 0; i < ingredients.Count; i++)
        //        {
        //            if (ingredients[i].def.HasModExtension<ThingDefExtension>())
        //            {
        //                Thing outputThing = ThingMaker.MakeThing(targetDef);
        //                outputThing.stackCount = ingredients[i].def.GetModExtension<ThingDefExtension>().amount[ingredients[i].def.GetModExtension<ThingDefExtension>().resources.IndexOf(targetDef)];

        //                yield return postProcessProduct(outputThing, recipeDef, worker, precept);
        //                yield break;
        //            }
        //        }
        //    }
        //}
        static IEnumerable<Thing> Postfix(IEnumerable<Thing> __result, RecipeDef recipeDef, Pawn worker, List<Thing> ingredients, Precept_ThingStyle precept)
        {
            bool anyRecipeIntercepts = recipeDef.HasModExtension<RecipeDefExtension>()
                && ingredients.Any(ingredient => ingredient.def.HasModExtension<ThingDefExtension>());
            if (!anyRecipeIntercepts)
            {
                foreach (Thing result in __result)
                    yield return result;
                yield break;
            }
            ThingDef targetDef = recipeDef.GetModExtension<RecipeDefExtension>().recipeTargetDef;

            for (int i = 0; i < ingredients.Count; i++)
            {
                if (ingredients[i].def.HasModExtension<ThingDefExtension>())
                {
                    Thing outputThing = ThingMaker.MakeThing(targetDef);
                    outputThing.stackCount = ingredients[i].def.GetModExtension<ThingDefExtension>().amount[ingredients[i].def.GetModExtension<ThingDefExtension>().resources.IndexOf(targetDef)];

                    yield return postProcessProduct(outputThing, recipeDef, worker, precept);
                    yield break;
                }
            }
        }
    }

}
