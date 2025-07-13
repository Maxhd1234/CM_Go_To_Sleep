using UnityEngine;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace KB_Go_To_Sleep
{
    [StaticConstructorOnStartup]
    public class FloatMenuOptionProvider_Sleep : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;

        protected override bool Undrafted => true;

        protected override bool Multiselect => false;

        protected override FloatMenuOption GetSingleOptionFor(Thing thing, FloatMenuContext context)
        {
            Pawn pawn = context.FirstSelectedPawn;
            if (pawn.needs == null || pawn.needs.rest == null)
            {
                return null;
            }

            foreach (LocalTargetInfo bed in GenUI.TargetsAt(thing.DrawPos, ForSleeping(pawn), thingsOnly: true))
            {
                if (pawn.needs.rest.CurLevel > FallAsleepMaxLevel(pawn))
                {
                    return new FloatMenuOption(
                        "KB_Go_To_Sleep_Cannot_Sleep".Translate() + ": " + "KB_Go_To_Sleep_Not_Tired".Translate().CapitalizeFirst(),
                        null
                    );
                }
                else if (!pawn.CanReach(bed, PathEndMode.OnCell, Danger.Deadly))
                {
                    return new FloatMenuOption(
                        "KB_Go_To_Sleep_Cannot_Sleep".Translate() + ": " + "NoPath".Translate().CapitalizeFirst(),
                        null
                    );
                }
                else
                {
                    return FloatMenuUtility.DecoratePrioritizedTask(
                        new FloatMenuOption(
                            "KB_Go_To_Sleep_GoToSleep".Translate(),
                            delegate
                            {
                                Job job = JobMaker.MakeJob(JobDefOf.LayDown, bed.Thing);
                                pawn.jobs.TryTakeOrderedJob(job);
                            },
                            MenuOptionPriority.High
                        ),
                        pawn,
                        bed.Thing
                    );
                }
            }
            return null;
        }


        private static float WakeThreshold(Pawn p)
        {
            Lord lord = p.GetLord();
            if (lord != null && lord.CurLordToil != null && lord.CurLordToil.CustomWakeThreshold.HasValue)
            {
                return lord.CurLordToil.CustomWakeThreshold.Value;
            }
            return p.ageTracker.CurLifeStage?.naturalWakeThresholdOverride ?? 1f;
        }


        private static float FallAsleepMaxLevel(Pawn p)
        {
            return Mathf.Min(p.ageTracker.CurLifeStage?.fallAsleepMaxThresholdOverride ?? 0.75f, WakeThreshold(p) - 0.01f);
        }


        private static TargetingParameters ForSleeping(Pawn sleeper)
        {
            return new TargetingParameters
            {
                canTargetPawns = false,
                canTargetBuildings = true,
                mapObjectTargetsMustBeAutoAttackable = false,
                validator = delegate (TargetInfo targ)
                {
                    if (!targ.HasThing)
                    {
                        return false;
                    }
                    Building_Bed bed = targ.Thing as Building_Bed;
                    if (bed == null)
                    {
                        return false;
                    }
                    return (!bed.ForPrisoners && !bed.Medical);
                    //return (bed.AnyUnownedSleepingSlot || bed.CompAssignableToPawn.AssignedPawns.Contains(sleeper));
                }
            };
        }
    }
}