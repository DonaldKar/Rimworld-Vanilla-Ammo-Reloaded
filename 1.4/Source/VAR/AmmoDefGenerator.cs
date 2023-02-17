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
	[HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
	public static class DefGenerator_GenerateImpliedDefs_PreResolve_Patch
    {
		public static void Prefix()
		{
			foreach (ThingDef item in AmmoDefGenerator.ImpliedThingDefs())
			{
				DefGenerator.AddImpliedDef(item);
			}
		}
    }

	public class AmmoDefBase : Def
    {
		public List<ThingDefCountClass> costList;
		public List<ResearchProjectDef> researchPrerequisites;
		public Color color;
		public List<CompProperties> comps = new List<CompProperties>();
	}
	public class AmmoDefTemplate : Def
    {
		public List<StatModifier> statBases;
		public List<ThingDefCountClass> costList;
		public StyleCategoryDef dominantStyleCategory;
		public List<ThingDef> buildingPrerequisites;
		public List<ResearchProjectDef> researchPrerequisites;
		public TechLevel minTechLevelToBuild;
		public TechLevel maxTechLevelToBuild;
		public AltitudeLayer altitudeLayer = AltitudeLayer.Item;
		public float uiOrder = 2999f;
		[NoTranslate]
		public string uiIconPath;
		public List<IconForStuffAppearance> uiIconPathsStuff;
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
        public List<DamageMultiplier> damageMultipliers;
        public RecipeMakerProperties recipeMaker;
        public bool forceDebugSpawnable;
        public bool intricate;
        public bool scatterableOnMapGen = true;
        public float generateCommonality = 1f;
        public float generateAllowChance = 1f;
        public FloatRange startingHpRange = FloatRange.One;
        [NoTranslate]
		public List<string> thingSetMakerTags;
        public List<RecipeDef> recipes;
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
        public List<ThingStyleChance> randomStyle;
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
        public List<ThingCategoryDef> thingCategories;
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
		public List<string> tradeTags;
		public bool tradeNeverStack;
		public bool tradeNeverGenerateStacked;
		public bool healthAffectsPrice = true;
		public TechLevel techLevel;
        public List<string> weaponTags;
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
		public DamageDef NeolithicDamageDef;
		public DamageDef MedievalDamageDef;
		public DamageDef IndustrialDamageDef;
		public DamageDef SpacerDamageDef;
		public DamageDef UltratechDamageDef;
		public DamageDef ArchotechDamageDef;
		public List<ExtraDamage> extraDamages;
	}
	public static class AmmoDefGenerator
    {
		public static IEnumerable<ThingDef> ImpliedThingDefs()
		{
			foreach (AmmoDefTemplate allDef in DefDatabase<AmmoDefTemplate>.AllDefs)
			{
				foreach (AmmoDefPrimary primary in DefDatabase<AmmoDefPrimary>.AllDefs)
				{
					foreach (AmmoDefSecondary secondary in DefDatabase<AmmoDefSecondary>.AllDefs)
					{
						ThingDef thingDef = BaseAmmo(allDef);
						thingDef.defName = primary.defName + "_" + secondary.defName + "_" + thingDef.defName;
						thingDef.label = primary.label + secondary.label + thingDef.label;//(armor piercing/softbody/polymertipped/steelcore + incindiary/toxic/emp/cryogenic/corrosive/(Gaseous/smoking/infectious)/fragmenting?HighExplosive?(bomb)/penetrator(pierce)/flechette(cut)/"less lethal"(blunt)/(radioactive) + (techlevel) ammo
						thingDef.description = primary.description + secondary.description + thingDef.description;
						thingDef.comps.AddRange(primary.comps);
						thingDef.comps.AddRange(secondary.comps);
						thingDef.graphicData.color = primary.color;
						thingDef.graphicData.colorTwo = secondary.color;
						thingDef.researchPrerequisites.AddRange(primary.researchPrerequisites);
						thingDef.researchPrerequisites.AddRange(secondary.researchPrerequisites);
						thingDef.costList.AddRange(primary.costList);
						thingDef.costList.AddRange(secondary.costList);

						CompProperties_CustomProjectile comp = new CompProperties_CustomProjectile
						{
							compClass = typeof(CompCustomProjectile),
							damageMultiplier = primary.DamageMultiplier,
							apMultiplier = primary.APMultiplier,
							extraDamages = secondary.extraDamages,
						};
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
			if (thingDef.costList == null)
			{
				thingDef.costList = new List<ThingDefCountClass>();
			}
			thingDef.costList.AddRange(def.costList);
			if (thingDef.researchPrerequisites == null)
			{
				thingDef.researchPrerequisites = new List<ResearchProjectDef>();
			}
			thingDef.researchPrerequisites.AddRange(def.researchPrerequisites);
			if (thingDef.graphicData == null)
			{
				thingDef.graphicData = new GraphicData();
			}
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

			thingDef.useHitPoints = def.useHitPoints;
			if (thingDef.thingSetMakerTags == null)
			{
				thingDef.thingSetMakerTags = new List<string>();
			}
			thingDef.thingSetMakerTags.AddRange(def.thingSetMakerTags);
			if (thingDef.statBases == null)
			{
				thingDef.statBases = new List<StatModifier>();
			}
			thingDef.statBases.AddRange(def.statBases);
			thingDef.altitudeLayer = def.altitudeLayer;
			if (thingDef.comps == null)
			{
				thingDef.comps = new List<CompProperties>();
			}
			thingDef.comps.AddRange(def.comps);
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
			if (thingDef.tradeTags == null)
			{
				thingDef.tradeTags = new List<string>();
			}
			thingDef.tradeTags.AddRange(def.tradeTags);
			thingDef.tradeNeverStack = def.tradeNeverStack;
			thingDef.tradeNeverGenerateStacked = def.tradeNeverGenerateStacked;
			thingDef.techLevel= def.techLevel;
			thingDef.minTechLevelToBuild= def.minTechLevelToBuild;
			thingDef.maxTechLevelToBuild = def.maxTechLevelToBuild;
			thingDef.description = def.description;
			if (thingDef.thingCategories == null)
			{
				thingDef.thingCategories = new List<ThingCategoryDef>();
			}
			thingDef.thingCategories.AddRange(def.thingCategories);
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
			if (thingDef.buildingPrerequisites == null)
			{
				thingDef.buildingPrerequisites = new List<ThingDef>();
			}
			thingDef.buildingPrerequisites.AddRange(def.buildingPrerequisites);

			if (thingDef.weaponTags == null)
			{
				thingDef.weaponTags = new List<string>();
			}
			thingDef.weaponTags.AddRange(def.weaponTags);
			thingDef.destroyOnDrop= def.destroyOnDrop;
			thingDef.meleeHitSound= def.meleeHitSound;
			thingDef.stealable = def.stealable;

			thingDef.size = def.size;
			thingDef.destroyable = def.destroyable;
			thingDef.smallVolume = def.smallVolume;
			thingDef.devNote = def.devNote;
			if (thingDef.smeltProducts == null)
			{
				thingDef.smeltProducts = new List<ThingDefCountClass>();
			}
			thingDef.smeltProducts.AddRange(def.smeltProducts);
			thingDef.smeltable = def.smeltable;
			thingDef.burnableByRecipe = def.burnableByRecipe;
			thingDef.randomizeRotationOnSpawn=def.randomizeRotationOnSpawn;
			if (thingDef.damageMultipliers == null)
			{
				thingDef.damageMultipliers = new List<DamageMultiplier>();
			}
			thingDef.damageMultipliers.AddRange(def.damageMultipliers);
			thingDef.recipeMaker = def.recipeMaker;
			thingDef.forceDebugSpawnable=def.forceDebugSpawnable;
			thingDef.intricate=def.intricate;
			thingDef.scatterableOnMapGen = def.scatterableOnMapGen;
			thingDef.generateCommonality = def.generateCommonality;
			thingDef.generateAllowChance = def.generateAllowChance;
			thingDef.startingHpRange = def.startingHpRange;

			if (thingDef.recipes == null)
			{
				thingDef.recipes = new List<RecipeDef>();
			}
			thingDef.recipes.AddRange(def.recipes);
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

			thingDef.hideAtSnowDepth = def.hideAtSnowDepth;
			if (thingDef.randomStyle == null)
			{
				thingDef.randomStyle = new List<ThingStyleChance>();
			}
			thingDef.randomStyle.AddRange(def.randomStyle);
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

			return thingDef;
		}
	}
}
