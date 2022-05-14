﻿namespace VFECore.Abilities
{
    using System.Collections.Generic;
    using RimWorld.Planet;
    using Verse;
    using Verse.AI;

    public class JobDriver_CastAbilityOnce : JobDriver
    {
        private CompAbilities cachedComp;
        public CompAbilities CompAbilities
        {
            get
            {
                if (cachedComp is null)
                {
                    cachedComp = this.pawn.GetComp<CompAbilities>();
                }
                return cachedComp;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        public override string GetReport() => this.CompAbilities.currentlyCasting.def.JobReportString;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);


            //yield return Toils_Combat.GotoCastPosition(TargetIndex.A);

            CompAbilities comp     = this.CompAbilities;
            Toil          waitToil = Toils_General.Wait(comp.currentlyCasting.GetCastTimeForPawn(), TargetIndex.A);
            waitToil.WithProgressBarToilDelay(TargetIndex.C);
            waitToil.AddPreInitAction(() => comp.currentlyCasting.PreWarmupAction());

            if (this.TargetA.Pawn != this.pawn)
                waitToil.AddPreTickAction(() =>
                                          {
                                              if (comp.currentlyCasting.def.drawAimPie && Find.Selector.IsSelected(this.pawn))
                                                  GenDraw.DrawAimPie(this.pawn, this.TargetA, this.ticksLeftThisToil, 0.2f);
                                          });

            comp.currentlyCasting.WarmupToil(waitToil);
            yield return waitToil;

            Toil castToil = new Toil
                            {
                                initAction = () =>
                                             {
                                                 GlobalTargetInfo[] targets = comp.currentlyCastingTargets;

                                                 if (targets.Length == 1 && targets[0].Map == this.pawn.Map)
                                                     comp.currentlyCasting.Cast(targets[0].Thing != null ? new LocalTargetInfo(targets[0].Thing) : new LocalTargetInfo(targets[0].Cell));
                                                 else
                                                     comp.currentlyCasting.Cast(targets);
                                             },
                                defaultCompleteMode = ToilCompleteMode.Instant,
                                atomicWithPrevious  = true
                            };
            yield return castToil;

            this.AddFinishAction(() => comp.currentlyCasting.EndCastJob());
        }
    }
}
