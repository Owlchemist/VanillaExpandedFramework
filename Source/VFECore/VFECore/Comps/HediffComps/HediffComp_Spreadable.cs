﻿using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace VFECore
{
	public enum RaceCategory
	{
		Humanlike,
		Animal,
		Mechanoid,
		Insect
	}
	public class HediffCompProperties_Spreadable : HediffCompProperties
	{
		public float radiusToSpread;

		public float severityToInfect;

		public float diseaseContractChance;

		public IntRange spreadingTickInterval;

		public float socialInteractionsContractChance;

		public bool requiresLineOfSightToSpread;

		public List<RaceCategory> raceCategories;

		public List<ThingDef> apparelsPreventingSpreading;

		public FleckDef fleckDefOnPawn;

		public IntRange fleckSpawnInterval;

		public Color fleckColor;
		public HediffCompProperties_Spreadable()
		{
			compClass = typeof(HediffComp_Spreadable);
		}
	}

	[StaticConstructorOnStartup]
	public class HediffComp_Spreadable : HediffComp
	{
		public HediffCompProperties_Spreadable Props => base.props as HediffCompProperties_Spreadable;
		private static readonly Vector3 BreathOffset = new Vector3(0f, 0f, -0.04f);
		public override void CompPostPostAdd(DamageInfo? dinfo)
		{
			base.CompPostPostAdd(dinfo);
			nextSpreadingTick = Find.TickManager.TicksGame + Props.spreadingTickInterval.RandomInRange;
			nextFleckSpawnTick = Find.TickManager.TicksGame + Props.fleckSpawnInterval.RandomInRange;
		}

		public int nextSpreadingTick;
		public int nextFleckSpawnTick;
		public override void CompPostTick(ref float severityAdjustment)
		{
			base.CompPostTick(ref severityAdjustment);
			if (nextSpreadingTick >= Find.TickManager.TicksGame)
			{
				if (this.Pawn.Map != null)
				{
					foreach (var thing in GenRadial.RadialDistinctThingsAround(this.Pawn.Position, this.Pawn.Map, Props.radiusToSpread, true))
					{
						if (thing is Pawn pawn && pawn != this.Pawn && (!Props.requiresLineOfSightToSpread || GenSight.LineOfSight(Pawn.Position, pawn.Position, pawn.Map)))
						{
							TrySpreadDiseaseOn(pawn);
						}
					}
				}

				else if (this.Pawn.GetCaravan() is Caravan caravan)
				{
					foreach (var pawn in caravan.PawnsListForReading)
					{
						TrySpreadDiseaseOn(pawn);
					}
				}
				nextSpreadingTick = Find.TickManager.TicksGame + Props.spreadingTickInterval.RandomInRange;
			}

			if (Props.fleckDefOnPawn != null && nextFleckSpawnTick >= Find.TickManager.TicksGame && this.Pawn.Map != null)
			{
				var pawn = Pawn;
				ThrowFleck(pawn.Drawer.DrawPos + pawn.Drawer.renderer.BaseHeadOffsetAt(pawn.Rotation) + pawn.Rotation.FacingCell.ToVector3() * 0.21f + BreathOffset
					, pawn.Map, pawn.Rotation.AsAngle, pawn.Drawer.tweener.LastTickTweenedVelocity);
				nextFleckSpawnTick = Find.TickManager.TicksGame + Props.fleckSpawnInterval.RandomInRange;
			}
		}
		public void TrySpreadDiseaseOn(Pawn pawn)
        {
			if (CanCatchDisease(pawn) && Rand.Chance(Props.diseaseContractChance))
            {
				HealthUtility.AdjustSeverity(pawn, this.Def, Props.severityToInfect);
			}
		}
		private bool CanCatchDisease(Pawn pawn)
        {
			return (Props.raceCategories is null || RaceCanCatchDisease(pawn)) && pawn.health.immunity.DiseaseContractChanceFactor(this.Def) > 0.001f
				&& (Props.apparelsPreventingSpreading is null || !Props.apparelsPreventingSpreading.Any(x => pawn.WearsApparel(x)));
		}

		private bool RaceCanCatchDisease(Pawn pawn)
        {
			foreach (var category in Props.raceCategories)
            {
				switch (category)
                {
					case RaceCategory.Humanlike:
						if (pawn.RaceProps.Humanlike)
						{
							return true;
						}
						break;
					case RaceCategory.Animal:
						if (pawn.RaceProps.Animal)
						{
							return true;
						}
						break;
					case RaceCategory.Mechanoid:
						if (pawn.RaceProps.IsMechanoid)
						{
							return true;
						};
						break;
					case RaceCategory.Insect:
						if (pawn.RaceProps.Insect)
						{
							return true;
						};
						break;
				}
            }
			return false;
        }
        public void ThrowFleck(Vector3 loc, Map map, float throwAngle, Vector3 inheritVelocity)
		{
			if (loc.ToIntVec3().ShouldSpawnMotesAt(map))
			{
				FleckCreationData dataStatic = FleckMaker.GetDataStatic(loc + new Vector3(Rand.Range(-0.005f, 0.005f), 0f, Rand.Range(-0.005f, 0.005f)), map, Props.fleckDefOnPawn, Rand.Range(0.6f, 0.7f));
				dataStatic.rotationRate = Rand.RangeInclusive(-240, 240);
				dataStatic.velocityAngle = throwAngle + (float)Rand.Range(-10, 10);
				dataStatic.velocitySpeed = Rand.Range(0.1f, 0.8f);
				dataStatic.velocity = inheritVelocity * 0.5f;
				dataStatic.instanceColor = Props.fleckColor;
				map.flecks.CreateFleck(dataStatic);
			}
		}
        public override void CompExposeData()
        {
            base.CompExposeData();
			Scribe_Values.Look(ref nextSpreadingTick, "nextSpreadingTick");
			Scribe_Values.Look(ref nextFleckSpawnTick, "nextFleckSpawnTick");
		}
	}
}