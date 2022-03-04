using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace ExpandedMaterialsStones
{
    public class WorkGiver_DiggingSpot : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(DefDatabase<ThingDef>.GetNamed("EM_DiggingSpot"));

        public override PathEndMode PathEndMode => PathEndMode.InteractionCell;

        public override Danger MaxPathDanger(Pawn pawn)
        {
            return Danger.Deadly;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            List<Building> allBuildingsColonist = pawn.Map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                Building building = allBuildingsColonist[i];
                if (building.def == DefDatabase<ThingDef>.GetNamed("EM_DiggingSpot"))
                {
                    if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Deconstruct) == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t.Faction != pawn.Faction)
            {
                return false;
            }
            Building building = t as Building;
            if (building == null)
            {
                return false;
            }
            if (building.IsForbidden(pawn))
            {
                return false;
            }
            if (!pawn.CanReserve(building, 1, -1, null, forced))
            {
                return false;
            }
            if (building.Map.designationManager.DesignationOn(building, DesignationDefOf.Uninstall) != null)
            {
                return false;
            }
            if (building.IsBurning())
            {
                return false;
            }
            /*if ((Find.TickManager.TicksGame - t.TryGetComp<CompDiggingSpot>().lastUsedTick) < 15000)
            {
                return false;
            }*/
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("EM_DigAtSpot"), t, 1500, checkOverrideOnExpiry: true);
        }
    }

    public class JobDriver_DigAtSpot : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            CompDiggingSpot comp = job.targetA.Thing.TryGetComp<CompDiggingSpot>();
            JobDriver_DigAtSpot jobDriver_DigAtSpot = this;
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnBurningImmobile(TargetIndex.A);
            this.FailOnThingHavingDesignation(TargetIndex.A, DesignationDefOf.Uninstall);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            Toil work = new Toil();
            work.tickAction = delegate
            {
                Pawn actor = work.actor;
                ((Building)actor.CurJob.targetA.Thing).GetComp<CompDiggingSpot>().DigWorkDone(actor);
                actor.skills.Learn(SkillDefOf.Mining, 0.035f);
            };
            //work.WithProgressBarToilDelay(TargetIndex.A);
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.WithProgressBar(TargetIndex.A, () => comp.portionProgress / comp.delay);
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.InteractionCell);
            work.activeSkill = (() => SkillDefOf.Mining);
            yield return work;
        }
    }

}
