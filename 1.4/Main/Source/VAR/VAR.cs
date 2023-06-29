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
        public static bool needAmmo;
        public static bool LTSAutoPatch;
        static VAR()
        {
            //Harmony.DEBUG = true;
            new Harmony("VAR.Mod").PatchAll();
            SetXmlSettings();
            ApplySettings();
        }
        public static List<string> XmlSettings = new List<string>();
        public static void SetXmlSettings()
        {
            if (VanillaAmmoMod.settings.MVCFAutoPatch)
            {
                XmlSettings.Add("MVCFAutoPatch");
            }
        }
        public static void ApplySettings()
        {
            needAmmo = VanillaAmmoMod.settings.needAmmo;
            LTSAutoPatch = VanillaAmmoMod.settings.LTSAuto;//still needs restart
                
        }
    }
    //settings
    public class VanillaAmmoMod : Mod
    {
        public static VanillaAmmoSettings settings;
        public VanillaAmmoMod(ModContentPack pack) : base(pack)
        {
            
            //Harmony.DEBUG = true;

            Harmony harmony = new Harmony(id: "VAR.Mod");
            harmony.Patch(AccessTools.Method(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve"),
                prefix: new HarmonyMethod(typeof(DefGenerator_GenerateImpliedDefs_PreResolve_Patch), "Prefix"));
            
            settings = GetSettings<VanillaAmmoSettings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            settings.DoSettingsWindowContents(inRect);
        }
        public override string SettingsCategory()
        {
            return this.Content.Name;
        }
        public override void WriteSettings()
        {
            base.WriteSettings();
            VAR.ApplySettings();
        }
    }
    public class VanillaAmmoSettings : ModSettings
    {
        public Tabs curTab = Tabs.general;
        
        public bool needAmmo = false;
        
        public bool MVCFAutoPatch = false;
        public bool LTSAuto = false;
           
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref needAmmo, "needAmmo", false);
            
            Scribe_Values.Look(ref MVCFAutoPatch, "MVCFAutoPatch", false);
            Scribe_Values.Look(ref LTSAuto, "LTSAuto", false);
        }
        public enum Tabs
        {
            general
        }
        public void DoSettingsWindowContents(Rect inRect)
        {
            GUI.BeginGroup(inRect);
            Rect tabtop = new Rect(0, TabDrawer.TabHeight, inRect.width, 0);
            Rect settingSection = new Rect(0, TabDrawer.TabHeight, inRect.width, inRect.height - TabDrawer.TabHeight);

            Widgets.DrawMenuSection(settingSection);
            List<TabRecord> tablist = new List<TabRecord>();
            tablist.Add(new TabRecord("VAR.Settings".Translate(), delegate { curTab = Tabs.general; }, curTab == Tabs.general));
            TabDrawer.DrawTabs(tabtop, tablist);
            
            General(inRect.ContractedBy(20));
            GUI.EndGroup();
        }
        public void General(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("VAR.needAmmo".Translate(), ref needAmmo, "VAR.needAmmoTT".Translate());
            listingStandard.Gap();
            listingStandard.Label("VAR.XmlSettings".Translate());
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("VAR.MVCF".Translate(), ref MVCFAutoPatch, "VAR.MVCFToolTip".Translate());
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("VAR.LTSAmmo".Translate(), ref LTSAuto, "VAR.LTSAmmoTooltip".Translate());
            listingStandard.End();
        }
    }

    public class PatchOperationXmlSetting : PatchOperation
        {
        private string setting;
        
        private PatchOperation match;

        private PatchOperation nomatch;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            if (VAR.XmlSettings.Contains(setting))
            {
                if (match != null)
                {
                    return match.Apply(xml);
                }
            }
            else if (nomatch != null)
            {
                return nomatch.Apply(xml);
            }
            if (match == null)
            {
                return nomatch != null;
            }
            return true;
        }
        public override string ToString()
        {
            return $"{base.ToString()}({setting})";
        }
    }

    [HarmonyPatch(typeof(Projectile),"DrawMat", MethodType.Getter)]
    public static class Projectile_DrawMat_Patch
    {
        public static void Postfix(ref Material __result, Projectile __instance)
        {
            CompCustomProjectile comp = __instance.TryGetComp<CompCustomProjectile>();
            if (comp != null && comp.projectileGraphicData?.Graphic?.MatSingle != null)
            {
                __result = comp.projectileGraphicData.Graphic.MatSingle;
            }
        }
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

            FieldInfo field1 = AccessTools.Field(typeof(Verb), "canHitNonTargetPawnsNow");
            FieldInfo field2 = AccessTools.Field(typeof(Verb), "preventFriendlyFire");

            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder lb && lb.LocalIndex == 7)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, field1);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, field2);
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
            //public static Projectile assignComp(Projectile projectile, Verb_LaunchProjectile verb)
            //{
            //    CompProperties_CustomProjectile comp = AmmoComp(verb);
            //    if (comp != null)
            //    {
            //        Log.Message("comp is not null" + comp.damageMultiplier.ToString());
            //    }
            //    if (projectile.GetComp<CompCustomProjectile>() == null)
            //    {
            //        Log.Message("projectilecomp is null");
            //    }
            //    projectile.GetComp<CompCustomProjectile>()?.assignProps(comp);

            //    return projectile;
            //}
        public static Projectile assignComp(Projectile projectile, Verb_LaunchProjectile verb, ref ShootLine resultingLine, bool canHitNonTargetPawnsNow, bool preventFriendlyFire)
        {
            Log.Message("assigning comp");
            return projectile;
        }
        //public static CompProperties_CustomProjectile AmmoComp(Verb_LaunchProjectile verb)
        //{
        //    //VerbComp_Reloadable_ChangeableAmmo_Infinite comp = verb.Managed().TryGetComp<VerbComp_Reloadable_ChangeableAmmo_Infinite>();
        //    //if (comp != null && comp.Projectile != null)
        //    //{
        //    //    CompProperties_CustomProjectile temp = comp.Projectile;
        //    //    comp.Projectile = null;
        //    //    return temp;
        //    //}
        //    return null;
        //}

        public static void fireextraprojectiles(Projectile projectile, Verb_LaunchProjectile verb, ref ShootLine resultingLine, bool canHitNonTargetPawnsNow, bool preventFriendlyFire)
        {
            //Log.Message("assigning comp");
            CompCustomProjectile comp = projectile.GetComp<CompCustomProjectile>();
            if (comp.projectileCount > 1)
            {
                Thing caster = verb.caster;
                VerbProperties verbProps = verb.verbProps;
                LocalTargetInfo currentTarget = verb.CurrentTarget;
                Thing manningPawn = caster;
                Thing equipmentSource = verb.EquipmentSource;
                CompMannable compMannable = caster.TryGetComp<CompMannable>();
                if (compMannable != null && compMannable.ManningPawn != null)
                {
                    manningPawn = compMannable.ManningPawn;
                    equipmentSource = caster;
                }
                for (int i = 1; i < comp.projectileCount; i++)
                {
                    Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile.def, resultingLine.Source, caster.Map);
                    CompCustomProjectile comp2 = projectile2.GetComp<CompCustomProjectile>();
                    comp2.assignComps(comp);
                    if (verbProps.ForcedMissRadius > 0.5f)
                    {
                        float num = verbProps.ForcedMissRadius;
                        if (manningPawn != null && manningPawn is Pawn pawn)
                        {
                            num *= verbProps.GetForceMissFactorFor(equipmentSource, pawn);
                        }
                        float num2 = VerbUtility.CalculateAdjustedForcedMiss(num, currentTarget.Cell - caster.Position);
                        if (num2 > 0.5f)
                        {
                            IntVec3 forcedMissTarget = verb.GetForcedMissTarget(num2);
                            if (forcedMissTarget != currentTarget.Cell)
                            {
                                verb.ThrowDebugText("ToRadius");
                                verb.ThrowDebugText("Rad\nDest", forcedMissTarget);
                                ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
                                if (Rand.Chance(0.5f))
                                {
                                    projectileHitFlags = ProjectileHitFlags.All;
                                }
                                if (!canHitNonTargetPawnsNow)
                                {
                                    projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
                                }
                                projectile2.Launch(manningPawn, caster.DrawPos, forcedMissTarget, currentTarget, projectileHitFlags, preventFriendlyFire, equipmentSource);
                                continue;
                            }
                        }
                    }
                    ShotReport shotReport = ShotReport.HitReportFor(caster, verb, currentTarget);
                    Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
                    ThingDef targetCoverDef = randomCoverToMissInto?.def;
                    if (verbProps.canGoWild && !Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
                    {
                        resultingLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget);
                        verb.ThrowDebugText("ToWild" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
                        verb.ThrowDebugText("Wild\nDest", resultingLine.Dest);
                        ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
                        if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
                        {
                            projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
                        }
                        projectile2.Launch(manningPawn, caster.DrawPos, resultingLine.Dest, currentTarget, projectileHitFlags2, preventFriendlyFire, equipmentSource, targetCoverDef);
                        continue;
                    }
                    if (currentTarget.Thing != null && currentTarget.Thing.def.CanBenefitFromCover && !Rand.Chance(shotReport.PassCoverChance))
                    {
                        verb.ThrowDebugText("ToCover" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
                        verb.ThrowDebugText("Cover\nDest", randomCoverToMissInto.Position);
                        ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
                        if (canHitNonTargetPawnsNow)
                        {
                            projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
                        }
                        projectile2.Launch(manningPawn, caster.DrawPos, randomCoverToMissInto, currentTarget, projectileHitFlags3, preventFriendlyFire, equipmentSource, targetCoverDef);
                        continue;
                    }
                    ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
                    if (canHitNonTargetPawnsNow)
                    {
                        projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
                    }
                    if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
                    {
                        projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
                    }
                    verb.ThrowDebugText("ToHit" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
                    if (currentTarget.Thing != null)
                    {
                        projectile2.Launch(manningPawn, caster.DrawPos, currentTarget, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
                        verb.ThrowDebugText("Hit\nDest", currentTarget.Cell);
                        continue;
                    }
                    else
                    {
                        projectile2.Launch(manningPawn, caster.DrawPos, resultingLine.Dest, currentTarget, projectileHitFlags4, preventFriendlyFire, equipmentSource, targetCoverDef);
                        verb.ThrowDebugText("Hit\nDest", resultingLine.Dest);
                        continue;
                    }
                }
            }
        }
        public static void ThrowDebugText(this Verb verb, string text)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(verb.caster.DrawPos, verb.caster.Map, text);
            }
        }
        public static void ThrowDebugText(this Verb verb, string text, IntVec3 c)
        {
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(c.ToVector3Shifted(), verb.caster.Map, text);
            }
        }
        public static IntVec3 GetForcedMissTarget(this Verb verb,float forcedMissRadius)
        {
            //if (verb.verbProps.forcedMissEvenDispersal)
            //{
            //    if (forcedMissTargetEvenDispersalCache.Count <= 0)
            //    {
            //        forcedMissTargetEvenDispersalCache.AddRange(GenerateEvenDispersalForcedMissTargets(currentTarget.Cell, forcedMissRadius, burstShotsLeft));
            //        forcedMissTargetEvenDispersalCache.SortByDescending((IntVec3 p) => p.DistanceToSquared(Caster.Position));
            //    }
            //    if (forcedMissTargetEvenDispersalCache.Count > 0)
            //    {
            //        return forcedMissTargetEvenDispersalCache.Pop();
            //    }
            //}
            int max = GenRadial.NumCellsInRadius(forcedMissRadius);
            int num = Rand.Range(0, max);
            return verb.CurrentTarget.Cell + GenRadial.RadialPattern[num];
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
        public List<ExtraDamage> extraDamages = new List<ExtraDamage>();
        //replaces damage def, ex. bullet toxic, hediff appliers which need/scale with damage values
        public DamageDef damageDef;

        public int projectileCount = 1;

        public GraphicData projectileGraphicData;

        public List<DamageDefAdditionalHediff> additionalHediffs = new List<DamageDefAdditionalHediff>();
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

        public int projectileCount;

        public GraphicData projectileGraphicData;

        public List<DamageDefAdditionalHediff> additionalHediffs;
        public CompProperties_CustomProjectile Props => (CompProperties_CustomProjectile)props;
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref damageMultiplier, "damageMultiplier", 1f);
            Scribe_Values.Look(ref apMultiplier, "apMultiplier", 1f);
            Scribe_Values.Look(ref damageMultiplier, "damageMultiplier", 1f);
            Scribe_Values.Look(ref damageDef, "damageDef");
            Scribe_Collections.Look(ref extraDamages, "extraDamages", LookMode.Deep);
            Scribe_Values.Look(ref projectileCount, "projectileCount", 1);
            Scribe_Deep.Look(ref projectileGraphicData, "projectileGraphicData");
            Scribe_Collections.Look(ref additionalHediffs, "additionalHediffs", LookMode.Deep);
        }
        public void assignProps(CompProperties_CustomProjectile customProjectile)
        {
            damageMultiplier=customProjectile.damageMultiplier;
            Log.Message(damageMultiplier.ToString());
            Log.Message(customProjectile.damageMultiplier.ToString());
            apMultiplier=customProjectile.apMultiplier;
            extraDamages=customProjectile.extraDamages;
            damageDef=customProjectile.damageDef;
            projectileCount=customProjectile.projectileCount;
            projectileGraphicData=customProjectile.projectileGraphicData;
            additionalHediffs=customProjectile.additionalHediffs;
        }
        public void assignComps(CompCustomProjectile customProjectile)
        {
            damageMultiplier = customProjectile.damageMultiplier;
            Log.Message(damageMultiplier.ToString());
            Log.Message(customProjectile.damageMultiplier.ToString());
            apMultiplier = customProjectile.apMultiplier;
            extraDamages = customProjectile.extraDamages;
            damageDef = customProjectile.damageDef;
            projectileCount = customProjectile.projectileCount;
            projectileGraphicData = customProjectile.projectileGraphicData;
            additionalHediffs = customProjectile.additionalHediffs;
        }
    }
}
