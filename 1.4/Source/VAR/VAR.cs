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

namespace VAR
{
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
                float damage = __result * (__instance.GetComp<CompCustomProjectile>()?.Props.damageMultiplier ?? 1f) ;
                __result = (int)damage;
                return;
            }
        }
        
        [HarmonyPatch(typeof(Projectile), "ArmorPenetration", MethodType.Getter)]
        public static class Projectile_ArmorPenetration_Patch
        {
            public static void Postfix(ref Projectile __instance, ref float __result)
            {
                __result *= __instance.GetComp<CompCustomProjectile>()?.Props.apMultiplier ?? 1f;
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
        public static CompProperties_CustomProjectile customProjectile;
        [HarmonyPatch(typeof(Verb_LaunchProjectile), "Projectile", MethodType.Getter)]
        public static class Verb_LaunchProjectile_Projectile_Patch 
        {
            public static void Postfix(ref Verb __instance, ref ThingDef __result)
            {
                if (__result == null)
                {
                    return;
                }
                CompProperties_CustomProjectile customprojectiles = __result.GetCompProperties<CompProperties_CustomProjectile>();
                if (customprojectiles != null)
                {
                    __result = __instance.verbProps.defaultProjectile;
                    customProjectile = customprojectiles;
                }
            }
        }
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
                MethodBase from = AccessTools.PropertyGetter(typeof(ProjectileProperties), "damageDef");
                MethodBase from2 = AccessTools.PropertyGetter(typeof(ExtraDamage), "amount");
                MethodBase from3 = AccessTools.Method(typeof(ExtraDamage), "AdjustedArmorPenetration");

                MethodBase to = AccessTools.Method(typeof(ProjectileCompCombiner), "assignDef");
                MethodBase to2 = AccessTools.Method(typeof(ProjectileCompCombiner), "Damage");
                MethodBase to3 = AccessTools.Method(typeof(ProjectileCompCombiner), "ArmorP");

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.operand as MethodBase == from)
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, to);
                        continue;
                    }
                    if (instruction.operand as MethodBase == from2)
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, to2);
                        continue;
                    }
                    if (instruction.operand as MethodBase == from3)
                    {
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
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
                MethodBase from = AccessTools.PropertyGetter(typeof(LogEntry_DamageResult), "AssociateWithLog");
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
                return __result * (__instance.GetComp<CompCustomProjectile>()?.Props.damageMultiplier ?? 1f);
            }
            public static float ArmorP(float __result, Projectile __instance)
            {
                return __result * (__instance.GetComp<CompCustomProjectile>()?.Props.apMultiplier ?? 1f);
            }
            public static Projectile assignComp(Projectile projectile)
            {
                if (customProjectile != null)
                {
                    CompCustomProjectile compCustomProjectile = new CompCustomProjectile();
                    compCustomProjectile.Initialize(customProjectile);
                    projectile.AllComps.Add(compCustomProjectile);
                    customProjectile = null;
                }
                return projectile;
            }
            public static DamageDef assignDef(DamageDef damageDef, Projectile projectile)
            {
                CompCustomProjectile comp = projectile.GetComp<CompCustomProjectile>();
                if (comp != null && comp.Props.damageDef != null)
                {
                    return comp.Props.damageDef;
                }
                return damageDef;
            }
            public static void extraDamagesBullet(Projectile_Explosive projectile, Thing hitThing, BattleLogEntry_RangedImpact battleLogEntry_RangedImpact, bool instigatorGuilty)
            {
                CompCustomProjectile comp = projectile.GetComp<CompCustomProjectile>();
                if (comp != null)
                {
                    foreach (ExtraDamage extraDamage in comp.Props.extraDamages)
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
                    foreach (ExtraDamage extraDamage in comp.Props.extraDamages)
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
        public List<ExtraDamage> extraDamages= new List<ExtraDamage>();
        //replaces damage def, ex. bullet toxic, hediff appliers which need/scale with damage values
        public DamageDef damageDef;
    }
    public class CompCustomProjectile : ThingComp
    {
        public CompProperties_CustomProjectile Props => (CompProperties_CustomProjectile)props;
    }




    public class CompChangeableAmmo : CompChangeableProjectile
    {
        public new CompProperties_ChangeableAmmo Props => (CompProperties_ChangeableAmmo)props;
        public override void Initialize(CompProperties props)
        {
            this.props = props;
        }
        public override StorageSettings GetParentStoreSettings()
        {
            return Props.fixedStorageSettings;
        }
    }
    public class CompProperties_ChangeableAmmo : CompProperties_ChangeableProjectile
    {

        public CompProperties_ChangeableAmmo()
        {
            compClass = typeof(CompChangeableAmmo);
        }


    }
}
