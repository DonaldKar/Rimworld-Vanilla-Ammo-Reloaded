using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse.AI.Group;
using MonoMod.Utils;
using Verse.Sound;
using System.Xml;
using MVCF.Reloading.Comps;
using MVCF.VerbComps;
using MVCF.Comps;
using MVCF.Commands;
using Verse.AI;
using MVCF.Utilities;

namespace VAR
{
    public class VanillaAmmoMod : Mod
    {
        public VanillaAmmoMod(ModContentPack pack) : base(pack)
        {
            Harmony harmony = new Harmony(id: "VAR.Mod");

            harmony.Patch(AccessTools.Method(typeof(AdditionalVerbProps), "ConfigErrors", new Type[] { typeof(ThingDef) }),
                          prefix: new HarmonyMethod(typeof(AdditionalVerbProps_ConfigErrors_Patch), "Prefix"));
        }
    }
    [StaticConstructorOnStartup]
    public static class VAR
    {
        static VAR()
        {
            //Harmony.DEBUG = true;
            new Harmony("VAR.Mod").PatchAll();
        }
        [HarmonyPatch(typeof(Projectile), "DamageAmount", MethodType.Getter)]
        public static class Projectile_DamageAmount_Patch
        {
            public static void Postfix(ref Projectile __instance, ref int __result)
            {
                float damage = __result * (__instance.GetComp<CompCustomProjectile>()?.damageMultiplier ?? 1f) ;
                __result = (int)damage;
                return;
            }
        }
        
        [HarmonyPatch(typeof(Projectile), "ArmorPenetration", MethodType.Getter)]
        public static class Projectile_ArmorPenetration_Patch
        {
            public static void Postfix(ref Projectile __instance, ref float __result)
            {
                __result *= __instance.GetComp<CompCustomProjectile>()?.apMultiplier ?? 1f;
                return;
            }
        }
        [HarmonyPatch(typeof(Projectile_Explosive), "Explode")]
        public static class Projectile_Explosive_Explode_Patch
        {
            public static void Postfix(ref Projectile_Explosive __instance)
            {
                ProjectileCompCombiner.extraDamagesExplosive(__instance);
                if (__instance.def.projectile.extraDamages != null)
                {
                    foreach (ExtraDamage extraDamage in __instance.def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            GenExplosion.DoExplosion(__instance.Position, __instance.Map, __instance.def.projectile.explosionRadius, extraDamage.def, __instance.Launcher, (int)ProjectileCompCombiner.Damage(extraDamage.amount, __instance), ProjectileCompCombiner.ArmorP(extraDamage.AdjustedArmorPenetration(), __instance));
                        }
                    }
                }
            }
        }
        //public static CompProperties_CustomProjectile customProjectile;
        //[HarmonyPatch(typeof(Verb_LaunchProjectile), "Projectile", MethodType.Getter)]
        //public static class Verb_LaunchProjectile_Projectile_Patch
        //{
        //    public static void Postfix(Verb_LaunchProjectile __instance, ref ThingDef __result)
        //    {
        //        if (__result == null)
        //        {
        //            return;
        //        }
        //        CompProperties_CustomProjectile customprojectiles = __result.GetCompProperties<CompProperties_CustomProjectile>();
        //        if (customprojectiles != null)
        //        {
        //            __result = __instance.verbProps.defaultProjectile;
        //            customProjectile = customprojectiles;
        //        }
        //    }
        //}
        [HarmonyPatch(typeof(Verb_LaunchProjectile), "TryCastShot")]
        public static class Verb_LaunchProjectile_TryCastShot_Patch//loads comp into projectile
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodBase to = AccessTools.Method(typeof(ProjectileCompCombiner), "assignComp");

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder lb && lb.LocalIndex == 7)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, to);
                        yield return instruction;
                        continue;
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }
        [HarmonyPatch]
        public static class Projectile_DamageDef_Transpiler
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(Bullet), "Impact");
                yield return AccessTools.Method(typeof(Projectile_Explosive), "Impact");
                yield return AccessTools.Method(typeof(Projectile_Explosive), "Explode");
            }
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo from = AccessTools.Field(typeof(ProjectileProperties), "damageDef");
                FieldInfo from2 = AccessTools.Field(typeof(ExtraDamage), "amount");
                MethodBase from3 = AccessTools.Method(typeof(ExtraDamage), "AdjustedArmorPenetration");

                MethodBase to = AccessTools.Method(typeof(ProjectileCompCombiner), "assignDef");
                MethodBase to2 = AccessTools.Method(typeof(ProjectileCompCombiner), "Damage");
                MethodBase to3 = AccessTools.Method(typeof(ProjectileCompCombiner), "ArmorP");

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.operand as FieldInfo == from)
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        //yield return new CodeInstruction(OpCodes.Castclass, typeof(Projectile));
                        yield return new CodeInstruction(OpCodes.Call, to);
                        continue;
                    }
                    if (instruction.operand as FieldInfo == from2)
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        //yield return new CodeInstruction(OpCodes.Castclass, typeof(Projectile));
                        yield return new CodeInstruction(OpCodes.Call, to2);
                        continue;
                    }
                    if (instruction.operand as MethodBase == from3)
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        //yield return new CodeInstruction(OpCodes.Castclass, typeof(Projectile));
                        yield return new CodeInstruction(OpCodes.Call, to3);
                        continue;
                    }
                    yield return instruction;
                }
            }
        }
        [HarmonyPatch(typeof(Bullet), "Impact")]
        public static class Bullet_ExtraDamages_Transpiler
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodBase from = AccessTools.PropertyGetter(typeof(DamageWorker.DamageResult), "AssociateWithLog");
                MethodBase to = AccessTools.Method(typeof(ProjectileCompCombiner), "extraDamagesBullet");
                bool first = true;

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.operand as MethodBase == from && first)
                    {
                        first = false;
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldloc_2);
                        yield return new CodeInstruction(OpCodes.Ldloc_3);
                        yield return new CodeInstruction(OpCodes.Call, to);
                        continue;
                    }
                    yield return instruction;
                }
            }
        }
        public static class ProjectileCompCombiner
        {
            public static float Damage(float __result, Projectile __instance)
            {
                return __result * (__instance.GetComp<CompCustomProjectile>()?.damageMultiplier ?? 1f);
            }
            public static float ArmorP(float __result, Projectile __instance)
            {
                return __result * (__instance.GetComp<CompCustomProjectile>()?.apMultiplier ?? 1f);
            }
            public static Projectile assignComp(Projectile projectile, Verb_LaunchProjectile verb)
            {
                VerbComp_Reloadable_ChangeableAmmo_Infinite comp = verb.Managed().TryGetComp<VerbComp_Reloadable_ChangeableAmmo_Infinite>();
                if (comp != null && comp.Projectile != null)
                {
                    projectile.GetComp<CompCustomProjectile>().assignProps(comp.Projectile);
                    comp.Projectile = null;
                }
                return projectile;
            }
            public static DamageDef assignDef(DamageDef damageDef, Projectile projectile)
            {
                CompCustomProjectile comp = projectile.GetComp<CompCustomProjectile>();
                if (comp != null && comp.damageDef != null)
                {
                    return comp.damageDef;
                }
                return damageDef;
            }
            public static void extraDamagesBullet(Bullet projectile, Thing hitThing, BattleLogEntry_RangedImpact battleLogEntry_RangedImpact, bool instigatorGuilty)
            {
                if (hitThing == null)
                {
                    return;
                }
                CompCustomProjectile comp = projectile.GetComp<CompCustomProjectile>();
                if (comp != null)
                {
                    foreach (ExtraDamage extraDamage in comp.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            DamageInfo dinfo2 = new DamageInfo(extraDamage.def, Damage(extraDamage.amount, projectile), ArmorP(extraDamage.AdjustedArmorPenetration(), projectile), projectile.ExactRotation.eulerAngles.y, projectile.Launcher, null, projectile.EquipmentDef, DamageInfo.SourceCategory.ThingOrUnknown, projectile.intendedTarget.Thing, instigatorGuilty);
                            hitThing.TakeDamage(dinfo2).AssociateWithLog(battleLogEntry_RangedImpact);
                        }
                    }
                }
            }
            public static void extraDamagesExplosive(Projectile_Explosive projectile)
            {
                CompCustomProjectile comp = projectile.GetComp<CompCustomProjectile>();
                if (comp != null)
                {
                    foreach (ExtraDamage extraDamage in comp.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            GenExplosion.DoExplosion(projectile.Position, projectile.Map, projectile.def.projectile.explosionRadius, extraDamage.def, projectile.Launcher, (int)Damage(extraDamage.amount, projectile), ArmorP(extraDamage.AdjustedArmorPenetration(), projectile));
                        }
                    }
                }
            }
        }
    }
    public class CompProperties_CustomProjectile: CompProperties
    {
        public CompProperties_CustomProjectile()
        {
            compClass = typeof(CompCustomProjectile);
        }
        //primaries
        public float damageMultiplier = 1f;
        public float apMultiplier = 1f;
        //secondaries
        //adds extra damages, emp, burns, shocks, extinguish, flame, toxgas, etc
        public List<ExtraDamage> extraDamages;
        //replaces damage def, ex. bullet toxic, hediff appliers which need/scale with damage values
        public DamageDef damageDef;
    }
    //public class CustomProjectileHolder
    //{
    //    //primaries
    //    public float damageMultiplier = 1f;
    //    public float apMultiplier = 1f;
    //    //secondaries
    //    //adds extra damages, emp, burns, shocks, extinguish, flame, toxgas, etc
    //    public List<ExtraDamage> extraDamages;
    //    //replaces damage def, ex. bullet toxic, hediff appliers which need/scale with damage values
    //    public DamageDef damageDef;

    //    public CustomProjectileHolder(CompProperties_CustomProjectile Props)
    //    {
    //        damageMultiplier = Props.damageMultiplier;
    //        apMultiplier = Props.apMultiplier;
    //        extraDamages = Props.extraDamages;
    //        damageDef = Props.damageDef;
    //        Log.Message("test"+damageMultiplier.ToString()+Props.damageMultiplier.ToString());
    //    }
    //}
    public class CompCustomProjectile : ThingComp
    {
        public float damageMultiplier = 1f;
        public float apMultiplier = 1f;

        public List<ExtraDamage> extraDamages = new List<ExtraDamage>();

        public DamageDef damageDef;
        public CompProperties_CustomProjectile Props => (CompProperties_CustomProjectile)props;

        public void assignProps(CompProperties_CustomProjectile customProjectile)
        {
            damageMultiplier=customProjectile.damageMultiplier;
            Log.Message(damageMultiplier.ToString());
            Log.Message(customProjectile.damageMultiplier.ToString());
            apMultiplier=customProjectile.apMultiplier;
            extraDamages=customProjectile.extraDamages;
            damageDef=customProjectile.damageDef;
        }
    }
    public class VerbComp_Reloadable_ChangeableAmmo_Infinite : VerbComp_Reloadable_ChangeableAmmo
    {
        private readonly ThingOwner<Thing> loadedAmmo = new ThingOwner<Thing>();

        public Thing nextAmmoItem;
        public Pawn Pawn => parent?.Manager?.Pawn;
        public override bool Available()
        {
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
            Log.Message("start projectile transfer");
            //ThingDef def = nextAmmoItem?.def;

            ThingDef def = base.ProjectileOverride(oldProjectile);
            if (def != null)
            {
                Log.Message("found ammo, checking comp");
                CompProperties_CustomProjectile customprojectiles = def.GetCompProperties<CompProperties_CustomProjectile>();
                if (customprojectiles != null)
                {
                    Log.Message("found comp damage mult: " + customprojectiles.damageMultiplier.ToString());

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
            if (nextAmmoItem != null)
            {
                nextAmmoItem.stackCount--;
                if (nextAmmoItem.stackCount == 0)
                {
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
                    ___label=parentDef.Verbs.FirstOrDefault((VerbProperties v) => v.label != null).label;
                    
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
            if (Props.AmmoFilter == null)
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
                        break;
                }
            }
            if (Props.MaxShots == 0)
            {
                Props.MaxShots = (int)Mathf.Max((6f * (VerbProps.burstShotCount) / (VerbProps.defaultProjectile?.projectile.stoppingPower ?? 0.5f)),1f);
            }
        }
    }

}
