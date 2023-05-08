using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ammunition.Logic;
using Verse;
using Ammunition.Components;
using System.Reflection;

namespace VAR.LTS
{

	[StaticConstructorOnStartup]
	public static class LTS
	{

		static LTS()
		{
			//Harmony.DEBUG = true;
			new Harmony("VARLTS.Mod").PatchAll();
		}
	}

	public class VanillaAmmoModLTS : Mod
	{
		public VanillaAmmoModLTS(ModContentPack pack) : base(pack)
		{
			Harmony harmony = new Harmony(id: "VARLTS.Mod");

			harmony.Patch(AccessTools.Method(typeof(AmmoDefGenerator), "ImpliedThingDefs"),
						  postfix: new HarmonyMethod(typeof(AmmoDefSeeder), "Seeder"));
			harmony.Patch(AmmoComp_LTS_Patch.ammocheckMethod,
						  postfix: new HarmonyMethod(typeof(AmmoNeedLTS_Patch), "Postfix"));
		}
	}
	public static class AmmoDefSeeder
    {
		public static void Seeder(IEnumerable<ThingDef> __result)
        {
			if (!VAR.LTSAutoPatch)
            {
				return;
            }
			IEnumerable<Ammunition.Defs.AmmoCategoryDef> ammolist = DefDatabase<Ammunition.Defs.AmmoCategoryDef>.AllDefs;
			foreach (ThingDef ammoDef in __result)
            {
				ammoDef.thingClass = typeof(Ammunition.Things.Ammo);
				foreach (ThingCategoryDef cat in ammoDef.thingCategories)
                {
					foreach (Ammunition.Defs.AmmoCategoryDef ammos in ammolist)
					{
						if (cat.label == ammos.label)
                        {
							ammos.ammoDefs.Add(ammoDef.defName);
                        }
					}
                }
			}
        }
    }

	[HarmonyPatch(typeof(ProjectileCompCombiner), "assignComp")]
	public static class AmmoComp_LTS_Patch
    {
		public static Type AmmoLogicType = AccessTools.TypeByName("Ammunition.Logic.AmmoLogic");

		public static MethodInfo ammocheckMethod = AccessTools.Method(AmmoLogicType, "AmmoCheck");

		public static class ammoDel
        {
			public delegate bool ammocheckDel(Pawn pawn, Thing weapon, out KitComponent kitComp, bool consumeAmmo);

			public static readonly ammocheckDel ammocheckDelegate =
				AccessTools.MethodDelegate<ammocheckDel>(ammocheckMethod);

		}

		public static void Postfix(ref Projectile __result, Verb_LaunchProjectile verb)
        {
			if (ammoDel.ammocheckDelegate(verb.CasterPawn, verb.EquipmentSource, out KitComponent comp, false))
			{
				//Log.Message("got kit");
				if (comp?.LastUsedAmmo != null)
				{
					//Log.Message("got ammo");

					CompProperties_CustomProjectile props = comp.LastUsedAmmo.GetCompProperties<CompProperties_CustomProjectile>();
					if (props != null)
                    {
						__result.GetComp<CompCustomProjectile>()?.assignProps(props);
						//Log.Message("got comp");
					}
				}
			}
		}
    }
	//[HarmonyPatch(AmmoComp_LTS_Patch.ammocheckMethod,"AmmoCheck")]
	public static class AmmoNeedLTS_Patch
    {
		public static void Postfix(ref bool __result)
        {
			if (!VAR.needAmmo)
			{
				__result = true;
			}
			return;
		}
    }


}
