using HarmonyLib;
using MVCF.Commands;
using MVCF.Comps;
using MVCF.Reloading.Comps;
using MVCF.Utilities;
using MVCF.VerbComps;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;


namespace VAR.MVCF
{
    [StaticConstructorOnStartup]
    public static class MVCF
    {

        static MVCF()
        {
            Harmony.DEBUG = true;
            new Harmony("VARMVCF.Mod").PatchAll();
        }
    }
    public class VanillaAmmoModMVCF : Mod
    {
        public VanillaAmmoModMVCF(ModContentPack pack) : base(pack)
        {
            Harmony harmony = new Harmony(id: "VARMVCF.Mod");

            harmony.Patch(AccessTools.Method(typeof(AdditionalVerbProps), "ConfigErrors", new Type[] { typeof(ThingDef) }),
                          prefix: new HarmonyMethod(typeof(AdditionalVerbProps_ConfigErrors_Patch), "Prefix"));
        }
    }

    [HarmonyPatch(typeof(ProjectileCompCombiner), "assignComp")]
    public static class AmmoComp_MVCF_Patch
    {
        public static void Postfix(ref Projectile __result, Verb_LaunchProjectile verb)
        {
            Log.Message("pre conditionals");
            VerbComp_Reloadable_ChangeableAmmo_Infinite comp = verb.Managed().TryGetComp<VerbComp_Reloadable_ChangeableAmmo_Infinite>();
            if (comp != null && comp.Projectile != null)
            {
                Log.Message("past conditionals");
                __result.GetComp<CompCustomProjectile>().assignProps(comp.Projectile);
                comp.Projectile = null;
            }
        }
    }


    public class VerbComp_Reloadable_ChangeableAmmo_Infinite : VerbComp_Reloadable_ChangeableAmmo
    {
        private static AccessTools.FieldRef<VerbComp_Reloadable_ChangeableAmmo, ThingOwner<Thing>> loadedAmmoField = AccessTools.FieldRefAccess<VerbComp_Reloadable_ChangeableAmmo, ThingOwner<Thing>>("loadedAmmo");

        private static AccessTools.FieldRef<VerbComp_Reloadable_ChangeableAmmo, Thing> nextAmmoItemField = AccessTools.FieldRefAccess<VerbComp_Reloadable_ChangeableAmmo, Thing>("nextAmmoItem");
        public Pawn Pawn => parent?.Manager?.Pawn;
        public override bool Available()
        {
            if (VAR.needAmmo)
            {
                return base.Available();
            }
            return true;
        }
        public override IEnumerable<CommandPart> GetCommandParts(Command_VerbTargetExtended command)
        {
            yield return new CommandPart_Reloadable_Infinite
            {
                parent = command,
                Reloadable = this
            };
        }
        public CompProperties_CustomProjectile Projectile;
        public override ThingDef ProjectileOverride(ThingDef oldProjectile)
        {
            //Log.Message("start projectile transfer");
            //ThingDef def = nextAmmoItem?.def;
            ref Thing nextAmmoItem = ref nextAmmoItemField.Invoke(this);
            ThingDef def = nextAmmoItem?.def;
            if (def != null)
            {
                //Log.Message("found ammo, checking comp");
                CompProperties_CustomProjectile customprojectiles = def.GetCompProperties<CompProperties_CustomProjectile>();
                if (customprojectiles != null)
                {
                    //Log.Message("found comp damage mult: " + customprojectiles.damageMultiplier.ToString());

                    Projectile = customprojectiles;
                }
            }
            else
            {
                Projectile = null;
            }
            return oldProjectile;
        }
        public override void Notify_ShotFired()
        {
            if (ShotsRemaining > 0)
            {
                ShotsRemaining--;
            }
            if (Pawn?.CurJobDef == JobDefOf.Hunt)
            {
                Pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
            }
            ref Thing nextAmmoItem = ref nextAmmoItemField.Invoke(this);
            if (nextAmmoItem != null)
            {
                nextAmmoItem.stackCount--;
                if (nextAmmoItem.stackCount == 0)
                {
                    ThingOwner<Thing> loadedAmmo = loadedAmmoField.Invoke(this);
                    loadedAmmo.Remove(nextAmmoItem);
                    nextAmmoItem.Destroy();
                    nextAmmoItem = loadedAmmo.FirstOrFallback(null);
                }
            }
        }
    }

    public class CommandPart_Reloadable_Infinite : CommandPart_Reloadable
    {
        public override void PostInit()
        {
        }
    }

    [HarmonyPatch(typeof(CompProperties_VerbProps), "PropsFor")]
    public static class VerbProps_PropsFor_Patch
    {
        public static void Postfix(AdditionalVerbProps __result, Verb verb, List<AdditionalVerbProps> ___verbProps)
        {
            if (__result == null)
            {
                __result = ___verbProps?.FirstOrDefault(prop => prop.label == "CustomVARAmmo");
            }
        }
    }
    //[HarmonyPatch(typeof(CompProperties_VerbProps), "PostLoadSpecial")]
    //public static class Reloadable_TargetVerb_Patch
    //{
    //    public static void Prefix(ref List<AdditionalVerbProps> ___verbProps, ThingDef parent)
    //    {
    //        foreach (AdditionalVerbProps props in ___verbProps)
    //        {
    //            if (props != null && props.label == "CustomVARAmmo")
    //            {
    //                props.label = parent.Verbs.FirstOrDefault((VerbProperties v) => v.label != null).label;
    //                if (props.label==null ||props.label == "CustomVARAmmo")
    //                {
    //                    props.label = (parent.Verbs.FirstOrDefault((VerbProperties v) => v.defaultProjectile != null).defaultProjectile.ToString());

    //                }
    //            }
    //        }
    //    }
    //}

    public static class AdditionalVerbProps_ConfigErrors_Patch
    {
        public static void Prefix(ref string ___label, List<VerbCompProperties> ___comps, ThingDef parentDef)
        {
            //foreach (AdditionalVerbProps props in ___verbProps)
            //{

            if (___label == "CustomVARAmmo")
            {
                ___label = parentDef.Verbs.FirstOrDefault((VerbProperties v) => v.label != null).label;

                if (___label == null || ___label == "CustomVARAmmo")
                {

                    ___label = (parentDef.Verbs.FirstOrDefault((VerbProperties v) => v.defaultProjectile != null).defaultProjectile.ToString());

                }
                string label = ___label;

                VerbProperties props = parentDef.Verbs.FirstOrDefault((VerbProperties vb) => vb.label == label);
                if (props == null)
                {
                    return;
                }
                foreach (VerbCompProperties comp in ___comps)
                {
                    FixConfig((VerbCompProperties_Reloadable)comp, props, parentDef);
                }
            }
        }
        public static void FixConfig(VerbCompProperties_Reloadable Props, VerbProperties VerbProps, ThingDef parent)
        {
            if (Props.AmmoFilter.AnyAllowedDef == null)
            {
                TechLevel tech = parent.techLevel;
                Props.AmmoFilter = new ThingFilter();
                switch (tech)
                {
                    case TechLevel.Neolithic:
                        ThingCategoryDef named2 = DefDatabase<ThingCategoryDef>.GetNamed("NeolithicAmmo");
                        if (named2 != null)
                        {
                            Props.AmmoFilter.SetAllow(named2, allow: true);
                        }
                        break;
                    case TechLevel.Medieval:
                        ThingCategoryDef named3 = DefDatabase<ThingCategoryDef>.GetNamed("MedievalAmmo");
                        if (named3 != null)
                        {
                            Props.AmmoFilter.SetAllow(named3, allow: true);
                        }
                        break;
                    case TechLevel.Industrial:
                        ThingCategoryDef named4 = DefDatabase<ThingCategoryDef>.GetNamed("IndustrialAmmo");
                        if (named4 != null)
                        {
                            Props.AmmoFilter.SetAllow(named4, allow: true);
                        }
                        break;
                    case TechLevel.Spacer:
                        ThingCategoryDef named5 = DefDatabase<ThingCategoryDef>.GetNamed("SpacerAmmo");
                        if (named5 != null)
                        {
                            Props.AmmoFilter.SetAllow(named5, allow: true);
                        }
                        break;
                    case TechLevel.Ultra:
                        ThingCategoryDef named6 = DefDatabase<ThingCategoryDef>.GetNamed("UltraAmmo");
                        if (named6 != null)
                        {
                            Props.AmmoFilter.SetAllow(named6, allow: true);
                        }
                        break;
                    case TechLevel.Archotech:
                        ThingCategoryDef named7 = DefDatabase<ThingCategoryDef>.GetNamed("ArchotechAmmo");
                        if (named7 != null)
                        {
                            Props.AmmoFilter.SetAllow(named7, allow: true);
                        }
                        break;
                    default:
                        ThingCategoryDef named8 = DefDatabase<ThingCategoryDef>.GetNamed("IndustrialAmmo");
                        if (named8 != null)
                        {
                            Props.AmmoFilter.SetAllow(named8, allow: true);
                        }
                        break;
                }
            }
            if (Props.MaxShots == 0)
            {
                Props.MaxShots = (int)Mathf.Max((6f * (VerbProps.burstShotCount) / (VerbProps.defaultProjectile?.projectile.stoppingPower ?? 0.5f)), 1f);
            }
        }
    }
}
