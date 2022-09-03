﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace VCR
{
	public class DamageWorker_Bullet : DamageWorker_AddInjury
	{
		public static bool active;
		protected override BodyPartRecord ChooseHitPart(DamageInfo dinfo, Pawn pawn)
		{
			if (!active)
            {
				return base.ChooseHitPart(dinfo, pawn);
            }
			BodyPartRecord randomNotMissingPart = pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, dinfo.Height, dinfo.Depth);
			if (randomNotMissingPart.depth != BodyPartDepth.Inside && Rand.Chance(def.stabChanceOfForcedInternal))
			{
				BodyPartRecord randomNotMissingPart2 = pawn.health.hediffSet.GetRandomNotMissingPart(dinfo.Def, BodyPartHeight.Undefined, BodyPartDepth.Inside, randomNotMissingPart);
				if (randomNotMissingPart2 != null)
				{
					return randomNotMissingPart2;
				}
			}
			return randomNotMissingPart;
		}

		protected override void ApplySpecialEffectsToPart(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageResult result)
		{
			base.ApplySpecialEffectsToPart(pawn, totalDamage, dinfo, result);
			if (!active)
            {
				return;
            }
			float stoppingPower = (float)(dinfo.Weapon?.projectile.StoppingPower ?? dinfo.Def.defaultStoppingPower);
			if (stoppingPower <= 0.5)
            {
				int num = ((def.cutExtraTargetsCurve != null) ? GenMath.RoundRandom(def.cutExtraTargetsCurve.Evaluate(Rand.Value)) : 0);
				List<BodyPartRecord> list2 = null;
				if (num != 0)
				{
					IEnumerable<BodyPartRecord> enumerable = dinfo.HitPart.GetDirectChildParts();
					if (dinfo.HitPart.parent != null)
					{
						enumerable = enumerable.Concat(dinfo.HitPart.parent);
						if (dinfo.HitPart.parent.parent != null)
						{
							enumerable = enumerable.Concat(dinfo.HitPart.parent.GetDirectChildParts());
						}
					}
					list2 = (from x in enumerable.Except(dinfo.HitPart).InRandomOrder().Take(num)
							 where !x.def.conceptual
							 select x).ToList();
				}
				else
				{
					list2 = new List<BodyPartRecord>();
				}
				list2.Add(dinfo.HitPart);
				int num2= list2.Count;
				for (int j = 0; j < num2; j++)
				{
					DamageInfo dinfo3 = dinfo;
					dinfo3.SetHitPart(list2[j]);
					FinalizeAndAddInjury(pawn, totalDamage/num2, dinfo3, result);
				}
				return;
			}




			totalDamage = ReduceDamageToPreserveOutsideParts(totalDamage, dinfo, pawn);
			List<BodyPartRecord> list = new List<BodyPartRecord>();
			for (BodyPartRecord bodyPartRecord = dinfo.HitPart; bodyPartRecord != null; bodyPartRecord = bodyPartRecord.parent)
			{
				list.Add(bodyPartRecord);
				if (bodyPartRecord.depth == BodyPartDepth.Outside)
				{
					break;
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				BodyPartRecord bodyPartRecord2 = list[i];
				float totalDamage2 = ((list.Count != 1) ? ((bodyPartRecord2.depth == BodyPartDepth.Outside) ? (totalDamage * 0.75f) : (totalDamage * 0.4f)) : totalDamage);
				DamageInfo dinfo2 = dinfo;
				dinfo2.SetHitPart(bodyPartRecord2);
				FinalizeAndAddInjury(pawn, totalDamage2, dinfo2, result);
			}
		}
	}

}
