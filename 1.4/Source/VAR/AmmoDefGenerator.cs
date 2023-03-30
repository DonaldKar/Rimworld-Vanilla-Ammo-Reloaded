using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VAR;
using Verse;

namespace VAR
{
	public static class DefGenerator_GenerateImpliedDefs_PreResolve_Patch
	{
		public static void Prefix()
		{
			//Log.Message("generating ammo defs");
			foreach (ThingDef item in AmmoDefGenerator.ImpliedThingDefs())
			{
				if (item == null)
                {
					Log.Message("null ammo def generated");
					continue;
                }
				//Log.Message(item.defName);
				DefGenerator.AddImpliedDef(item);
			}
		}
    }

	public class AmmoDefBase : Def
    {
		public List<ThingDefCountClass> costList = new List<ThingDefCountClass>();
		public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();
		public Color color;
		public List<CompProperties> comps = new List<CompProperties>();
	}
	public class AmmoDefTemplate : Def
    {
		public List<StatModifier> statBases = new List<StatModifier>();
		public List<ThingDefCountClass> costList = new List<ThingDefCountClass>();
		public StyleCategoryDef dominantStyleCategory;
		public List<ThingDef> buildingPrerequisites = new List<ThingDef>();
		public List<ResearchProjectDef> researchPrerequisites = new List<ResearchProjectDef>();
		public TechLevel minTechLevelToBuild;
		public TechLevel maxTechLevelToBuild;
		public AltitudeLayer altitudeLayer = AltitudeLayer.Item;
		public float uiOrder = 2999f;
		[NoTranslate]
		public string uiIconPath;
		public List<IconForStuffAppearance> uiIconPathsStuff = new List<IconForStuffAppearance>();
		public Vector2 uiIconOffset;
		public Color uiIconColor = Color.white;
		public int uiIconForStackCount = -1;
		//public Type thingClass;
		public ThingCategory category = ThingCategory.Item;
		public TickerType tickerType= TickerType.Never;
		public int stackLimit = 1;
		public IntVec2 size = new IntVec2(1, 1);
		public bool destroyable = true;
		public bool rotatable = true;
		public bool smallVolume;
		public bool useHitPoints = true;
		public List<CompProperties> comps = new List<CompProperties>();
		[NoTranslate]
        public string devNote;
		public List<ThingDefCountClass> smeltProducts;
        public bool smeltable;
        public bool burnableByRecipe;
        public bool randomizeRotationOnSpawn;
		public List<DamageMultiplier> damageMultipliers = new List<DamageMultiplier>();
        public RecipeMakerProperties recipeMaker = new RecipeMakerProperties();
        public bool forceDebugSpawnable;
        public bool intricate;
        public bool scatterableOnMapGen = true;
        public float generateCommonality = 1f;
        public float generateAllowChance = 1f;
        public FloatRange startingHpRange = FloatRange.One;
		[NoTranslate]
		public List<string> thingSetMakerTags = new List<string>();
		public List<RecipeDef> recipes = new List<RecipeDef>();
        public bool messageOnDeteriorateInStorage = true;
        public bool deteriorateFromEnvironmentalEffects = true;
        public bool canDeteriorateUnspawned;
        public bool canLoadIntoCaravan = true;
        public FloatRange displayNumbersBetweenSameDefDistRange = FloatRange.Zero;
        public int minRewardCount = 1;
        public bool preventSkyfallersLandingOn;
        public FactionDef requiresFactionToAcquire;
        public OrderedTakeGroupDef orderedTakeGroup;
        public int allowedArchonexusCount;
        public int possessionCount;
        public bool notifyMapRemoved;
        public bool canScatterOver = true;
        public GraphicData graphicData;
		public DrawerType drawerType = DrawerType.MapMeshOnly;
        public float hideAtSnowDepth = 99999f;
		public List<ThingStyleChance> randomStyle = new List<ThingStyleChance>();
        public float randomStyleChance;
        public bool canEditAnyStyle;
        public bool selectable=true;
        public bool neverMultiSelect;
        public bool hasTooltip;
        public bool seeThroughFog;
        public bool drawGUIOverlay=true;
        public bool drawGUIOverlayQuality = true;
        public ResourceCountPriority resourceReadoutPriority = ResourceCountPriority.Middle;
        public bool resourceReadoutAlwaysShow;
        public ConceptDef storedConceptLearnOpportunity;
        public float uiIconScale = 1f;
        public bool hasCustomRectForSelector;
        public bool alwaysHaulable=true;
        public bool designateHaulable;
		public List<ThingCategoryDef> thingCategories = new List<ThingCategoryDef>();
		public bool socialPropernessMatters;
        public bool stealable = true;
        public SoundDef soundDrop;
		public SoundDef soundPickup;
		public SoundDef soundInteract;
		public SoundDef soundImpactDefault;
		public SoundDef soundPlayInstrument;
		public SoundDef soundOpen;
		public Tradeability tradeability = Tradeability.All;
		[NoTranslate]
		public List<string> tradeTags = new List<string>();
		public bool tradeNeverStack;
		public bool tradeNeverGenerateStacked;
		public bool healthAffectsPrice = true;
		public TechLevel techLevel;
		public List<string> weaponTags = new List<string>();
        public bool destroyOnDrop;
        public SoundDef meleeHitSound;
        public IngestibleProperties ingestible;
	}
    public class AmmoDefPrimary : AmmoDefBase
    {
		public float DamageMultiplier;
		public float APMultiplier;
	}
	public class AmmoDefSecondary : AmmoDefBase
	{
		public float DamageMultiplier;
		public float APMultiplier;
		public DamageDef NeolithicDamageDef;
		public DamageDef MedievalDamageDef;
		public DamageDef IndustrialDamageDef;
		public DamageDef SpacerDamageDef;
		public DamageDef UltratechDamageDef;
		public DamageDef ArchotechDamageDef;
		public List<ExtraDamage> extraDamages = new List<ExtraDamage>();
	}
	public static class AmmoDefGenerator
    {
		public static IEnumerable<ThingDef> ImpliedThingDefs()
		{
			//Log.Message("looking for template");
			foreach (AmmoDefTemplate allDef in DefDatabase<AmmoDefTemplate>.AllDefs)
			{
				//Log.Message("found template");
				foreach (AmmoDefPrimary primary in DefDatabase<AmmoDefPrimary>.AllDefs)
				{
					//Log.Message("found primary");

					foreach (AmmoDefSecondary secondary in DefDatabase<AmmoDefSecondary>.AllDefs)
					{
						//Log.Message("found secondary");

						ThingDef thingDef = BaseAmmo(allDef);
						thingDef.defName = primary.defName + "_" + secondary.defName + "_" + thingDef.defName;
						thingDef.label = primary.label + secondary.label + thingDef.label;//(armor piercing/softbody/polymertipped/steelcore + incendiary/toxic/emp/cryogenic/corrosive/(Gaseous/smoking/infectious)/fragmenting?HighExplosive?(bomb)/penetrator(pierce)/flechette(cut)/"less lethal"(blunt)/(radioactive) + (techlevel) ammo
						thingDef.description = thingDef.description + "\n\n" + primary.description + "\n\n" + secondary.description;
						thingDef.comps.AddRange(primary.comps);
						thingDef.comps.AddRange(secondary.comps);
						thingDef.graphicData.color = primary.color;
						thingDef.graphicData.colorTwo = secondary.color;
						thingDef.researchPrerequisites.AddRange(primary.researchPrerequisites);
						thingDef.researchPrerequisites.AddRange(secondary.researchPrerequisites);
						thingDef.costList.AddRange(primary.costList);
						thingDef.costList.AddRange(secondary.costList);

						float num = 0f;
						float num2 = thingDef.recipeMaker.workAmount/60f;
						float num3 = thingDef.recipeMaker.productCount;
						if (thingDef.costList != null)
						{
							for (int j = 0; j < thingDef.CostList.Count; j++)
							{
								ThingDefCountClass thingDefCountClass = thingDef.costList[j];
								int num4 = thingDefCountClass.thingDef.smallVolume ? 10 : 1;
								num += (float)thingDefCountClass.count * thingDefCountClass.thingDef.BaseMarketValue * num4;
							}
						}
						if(num2 > 2f)
						{
							num += num2 * 0.0036f;
						}
						num /= num3;
						StatModifier stat = new StatModifier();
						stat.stat = StatDefOf.MarketValue;
						stat.value = num;
						thingDef.statBases.Add(stat);

						CompProperties_CustomProjectile comp = new CompProperties_CustomProjectile
						{
							compClass = typeof(CompCustomProjectile),
							damageMultiplier = primary.DamageMultiplier*secondary.DamageMultiplier,
							apMultiplier = primary.APMultiplier*secondary.APMultiplier,
							extraDamages = secondary.extraDamages,
						};
						thingDef.description += "\n\nDamage Multiplier: " + (primary.DamageMultiplier * secondary.DamageMultiplier).ToString() + "\nArmor Piercing Multiplier: " + (primary.APMultiplier * secondary.APMultiplier).ToString();
						switch (thingDef.techLevel)
						{
							case TechLevel.Neolithic:
								comp.damageDef = secondary.NeolithicDamageDef ?? null;
								break;
							case TechLevel.Medieval:
								comp.damageDef = secondary.MedievalDamageDef ?? null;
								break;
							case TechLevel.Industrial:
								comp.damageDef = secondary.IndustrialDamageDef ?? null;
								break;
							case TechLevel.Spacer:
								comp.damageDef = secondary.SpacerDamageDef ?? null;
								break;
							case TechLevel.Ultra:
								comp.damageDef = secondary.UltratechDamageDef ?? null;
								break;
							case TechLevel.Archotech:
								comp.damageDef = secondary.ArchotechDamageDef ?? null;
								break;
							default:
								break;
						}
						thingDef.comps.Add(comp);
						yield return thingDef;
					}
				}
			}
		}
		public static ThingDef BaseAmmo(AmmoDefTemplate def)
		{
			ThingDef thingDef = new ThingDef();
			thingDef.resourceReadoutPriority = def.resourceReadoutPriority;
			thingDef.drawerType = def.drawerType;
			thingDef.category = def.category;
			thingDef.thingClass = typeof(ThingWithComps);
			//Log.Message("log1");
			thingDef.costList = new List<ThingDefCountClass>();
			if (def.costList != null)
			{
				thingDef.costList.AddRange(def.costList);
			}
			//Log.Message("log2");
			thingDef.researchPrerequisites = new List<ResearchProjectDef>();
			if (def.researchPrerequisites != null)
			{
				thingDef.researchPrerequisites.AddRange(def.researchPrerequisites);
			}
			//Log.Message("log3");
			//Log.Message("log3.1");
			thingDef.graphicData = new GraphicData();
			//Log.Message("log4");
			thingDef.graphicData.texPath = def.graphicData.texPath;
			thingDef.graphicData.maskPath= def.graphicData.maskPath;
			thingDef.graphicData.graphicClass=def.graphicData.graphicClass;
			thingDef.graphicData.shaderType = def.graphicData.shaderType;
            thingDef.graphicData.shaderParameters = def.graphicData.shaderParameters;
			thingDef.graphicData.drawSize = def.graphicData.drawSize;
			thingDef.graphicData.drawOffset = def.graphicData.drawOffset;
			thingDef.graphicData.drawOffsetNorth=def.graphicData.drawOffsetNorth;
			thingDef.graphicData.drawOffsetEast = def.graphicData.drawOffsetEast;
			thingDef.graphicData.drawOffsetSouth=def.graphicData.drawOffsetSouth;
			thingDef.graphicData.drawOffsetWest = def.graphicData.drawOffsetWest;
			thingDef.graphicData.onGroundRandomRotateAngle=def.graphicData.onGroundRandomRotateAngle;
			thingDef.graphicData.drawRotated = def.graphicData.drawRotated;
			thingDef.graphicData.allowFlip = def.graphicData.allowFlip;
			thingDef.graphicData.flipExtraRotation=def.graphicData.flipExtraRotation;
			thingDef.graphicData.renderInstanced=def.graphicData.renderInstanced;
			thingDef.graphicData.allowAtlasing = def.graphicData.allowAtlasing;
			thingDef.graphicData.renderQueue=def.graphicData.renderQueue;
			thingDef.graphicData.overlayOpacity=def.graphicData.overlayOpacity;
			thingDef.graphicData.shadowData=def.graphicData.shadowData;
			thingDef.graphicData.damageData=def.graphicData.damageData;
			thingDef.graphicData.linkType = def.graphicData.linkType;
			thingDef.graphicData.linkFlags=def.graphicData.linkFlags;
			thingDef.graphicData.asymmetricLink=def.graphicData.asymmetricLink;
			//Log.Message("log5");
			thingDef.useHitPoints = def.useHitPoints;
			thingDef.thingSetMakerTags = new List<string>();
			if (def.thingSetMakerTags != null)
			{
				thingDef.thingSetMakerTags.AddRange(def.thingSetMakerTags);
			}
			//Log.Message("log6");
			thingDef.statBases = new List<StatModifier>();
			if (def.statBases != null)
			{
				thingDef.statBases.AddRange(def.statBases);
			}
			//Log.Message("log7");
			thingDef.altitudeLayer = def.altitudeLayer;
			thingDef.comps = new List<CompProperties>();
			if (def.comps != null)
			{
				thingDef.comps.AddRange(def.comps);
			}
			//Log.Message("log8");
			thingDef.soundDrop = def.soundDrop;
			thingDef.soundPickup = def.soundPickup;
			thingDef.soundInteract = def.soundInteract;
			thingDef.soundImpactDefault = def.soundImpactDefault;
			thingDef.soundPlayInstrument = def.soundPlayInstrument;
			thingDef.soundOpen = def.soundOpen;
			thingDef.tickerType = def.tickerType;
			thingDef.rotatable = def.rotatable;
			thingDef.pathCost = DefGenerator.StandardItemPathCost;
			thingDef.modContentPack = def.modContentPack;
			thingDef.tradeability = def.tradeability;
			thingDef.tradeTags = new List<string>();
			if (def.tradeTags != null)
			{
				thingDef.tradeTags.AddRange(def.tradeTags);
			}
			//Log.Message("log9");
			thingDef.tradeNeverStack = def.tradeNeverStack;
			thingDef.tradeNeverGenerateStacked = def.tradeNeverGenerateStacked;
			thingDef.techLevel= def.techLevel;
			thingDef.minTechLevelToBuild= def.minTechLevelToBuild;
			thingDef.maxTechLevelToBuild = def.maxTechLevelToBuild;
			thingDef.description = def.description;
			thingDef.thingCategories = new List<ThingCategoryDef>();
			if (def.thingCategories != null)
			{
				thingDef.thingCategories.AddRange(def.thingCategories);
			}
			thingDef.defName = def.defName;
			thingDef.label = def.label;
			thingDef.stackLimit = def.stackLimit;
			thingDef.socialPropernessMatters = def.socialPropernessMatters;
			thingDef.healthAffectsPrice = def.healthAffectsPrice;
			thingDef.ingestible = def.ingestible;

			thingDef.uiIconPath = def.uiIconPath;
			thingDef.uiIconPathsStuff = def.uiIconPathsStuff;
			thingDef.uiIconOffset = def.uiIconOffset;
			thingDef.uiIconColor = def.uiIconColor;
			thingDef.uiOrder = def.uiOrder;
			thingDef.uiIconForStackCount = def.uiIconForStackCount;
			thingDef.dominantStyleCategory = def.dominantStyleCategory;
			//Log.Message("log10");
			thingDef.buildingPrerequisites = new List<ThingDef>();
			if (def.buildingPrerequisites != null)
			{
				thingDef.buildingPrerequisites.AddRange(def.buildingPrerequisites);
			}
			thingDef.weaponTags = new List<string>();
			if (def.weaponTags != null)
			{
				thingDef.weaponTags.AddRange(def.weaponTags);
			}
			thingDef.destroyOnDrop= def.destroyOnDrop;
			thingDef.meleeHitSound= def.meleeHitSound;
			thingDef.stealable = def.stealable;
			//Log.Message("log11");

			thingDef.size = def.size;
			thingDef.destroyable = def.destroyable;
			thingDef.smallVolume = def.smallVolume;
			thingDef.devNote = def.devNote;
			if (def.smeltProducts != null)
			{
				thingDef.smeltProducts = new List<ThingDefCountClass>();
				thingDef.smeltProducts.AddRange(def.smeltProducts);
			}
			thingDef.smeltable = def.smeltable;
			thingDef.burnableByRecipe = def.burnableByRecipe;
			thingDef.randomizeRotationOnSpawn=def.randomizeRotationOnSpawn;
			thingDef.damageMultipliers = new List<DamageMultiplier>();
			if (def.damageMultipliers != null)
			{
				thingDef.damageMultipliers.AddRange(def.damageMultipliers);
			}
			//Log.Message("log12");

			thingDef.recipeMaker = def.recipeMaker;
			thingDef.forceDebugSpawnable=def.forceDebugSpawnable;
			thingDef.intricate=def.intricate;
			thingDef.scatterableOnMapGen = def.scatterableOnMapGen;
			thingDef.generateCommonality = def.generateCommonality;
			thingDef.generateAllowChance = def.generateAllowChance;
			thingDef.startingHpRange = def.startingHpRange;
			thingDef.recipes = new List<RecipeDef>();
			if (def.recipes != null)
			{
				thingDef.recipes.AddRange(def.recipes);
			}
			thingDef.messageOnDeteriorateInStorage=def.messageOnDeteriorateInStorage;
			thingDef.deteriorateFromEnvironmentalEffects = def.deteriorateFromEnvironmentalEffects;
			thingDef.canDeteriorateUnspawned=def.canDeteriorateUnspawned;
			thingDef.canLoadIntoCaravan = def.canLoadIntoCaravan;
			thingDef.displayNumbersBetweenSameDefDistRange = def.displayNumbersBetweenSameDefDistRange;
			thingDef.minRewardCount = def.minRewardCount;
			thingDef.preventSkyfallersLandingOn=def.preventSkyfallersLandingOn;
			thingDef.requiresFactionToAcquire=def.requiresFactionToAcquire;
			thingDef.orderedTakeGroup=def.orderedTakeGroup;
			thingDef.allowedArchonexusCount=def.allowedArchonexusCount;
			thingDef.possessionCount=def.possessionCount;
			thingDef.notifyMapRemoved=def.notifyMapRemoved;
			thingDef.canScatterOver = def.canScatterOver;
			//Log.Message("log13");

			thingDef.hideAtSnowDepth = def.hideAtSnowDepth;
			thingDef.randomStyle = new List<ThingStyleChance>();
			if (def.randomStyle != null)
			{
				thingDef.randomStyle.AddRange(def.randomStyle);
			}
			thingDef.randomStyleChance=def.randomStyleChance;
			thingDef.canEditAnyStyle=def.canEditAnyStyle;
			thingDef.selectable=def.selectable;
			thingDef.neverMultiSelect=def.neverMultiSelect;
			thingDef.hasTooltip=def.hasTooltip;
			thingDef.seeThroughFog=def.seeThroughFog;
			thingDef.drawGUIOverlay=	def.drawGUIOverlay;
			thingDef.drawGUIOverlayQuality = def.drawGUIOverlayQuality;
			thingDef.resourceReadoutAlwaysShow=def.resourceReadoutAlwaysShow;
			thingDef.storedConceptLearnOpportunity=def.storedConceptLearnOpportunity;
			thingDef.uiIconScale = def.uiIconScale;
			thingDef.hasCustomRectForSelector=def.hasCustomRectForSelector;
			thingDef.alwaysHaulable = def.alwaysHaulable;
			thingDef.designateHaulable = def.designateHaulable;
			//Log.Message("log14");

			return thingDef;
		}
	}
}
