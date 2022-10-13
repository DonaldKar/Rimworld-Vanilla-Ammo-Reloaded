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
                float damage = __result * (__instance.def.GetModExtension<CustomProjectile>()?.damageMultiplier ?? 1f) ;
                __result = (int)damage;
                return;
            }
        }
        [HarmonyPatch(typeof(Projectile), "ArmorPenetration", MethodType.Getter)]
        public static class Projectile_ArmorPenetration_Patch
        {
            public static void Postfix(ref Projectile __instance, ref float __result)
            {
                __result *= __instance.def.GetModExtension<CustomProjectile>()?.apMultiplier ?? 1f;
                return;
            }
        }

        [HarmonyPatch(typeof(Projectile_Explosive), "Explode")]
        public static class Projectile_Explosive_Explode_Patch
        {
            public static void Postfix(ref Projectile_Explosive __instance)
            {
                if (__instance.def.projectile.extraDamages == null)
                {
                    return;
                }
                {
                    foreach (ExtraDamage extraDamage in __instance.def.projectile.extraDamages)
                    {
                        if (Rand.Chance(extraDamage.chance))
                        {
                            GenExplosion.DoExplosion(__instance.Position, __instance.Map, __instance.def.projectile.explosionRadius, extraDamage.def, __instance.Launcher, (int)extraDamage.amount, extraDamage.AdjustedArmorPenetration());
                        }
                    }
                    return;
                }
            }
        }

        [HarmonyPatch(typeof(Verb_LaunchProjectile), "Projectile", MethodType.Getter)]
        public static class Verb_LaunchProjectile_Projectile_Patch
        {
            public static void Postfix(ref Verb __instance, ref ThingDef __result)
            {
                CustomProjectile customprojectiles = __result.GetModExtension<CustomProjectile>();
                if (customprojectiles != null)
                {
                    __result = __instance.verbProps.defaultProjectile;
                    if (__result == null)
                    {
                        return;
                    }
                    __result.modExtensions.Add(customprojectiles);
                    __result.projectile.extraDamages.AddRange(customprojectiles.extraDamages);
                }
            }
        }

        public class CustomProjectile : DefModExtension
        {
            public float damageMultiplier = 1f;
            public float apMultiplier = 1f;
            public List<ExtraDamage> extraDamages = new List<ExtraDamage>();
        }
    }
}
