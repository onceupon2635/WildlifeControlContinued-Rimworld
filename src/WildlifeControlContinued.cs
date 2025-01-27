using System;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace WildlifeControlContinued
{
    public class AnimalLimitSettings : ModSettings
    {
        public int maxWildAnimals = 100;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxWildAnimals, "maxWildAnimals", 100);
        }
    }

    public class WildlifeControlContinued : Mod
    {
        private readonly AnimalLimitSettings settings;

        public WildlifeControlContinued(ModContentPack content) : base(content)
        {
            settings = new AnimalLimitSettings();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Label("Max Wild Animals: " + settings.maxWildAnimals);

            // Create Rect objects for the slider and text box
            Rect sliderRect = new Rect(inRect.x, inRect.y + 0, inRect.width * 0.6f, 30);
            Rect textBoxRect = new Rect(sliderRect.xMax, inRect.y + 0, inRect.width * 0.3f, 30);

            // Update slider and text box to use the new Rect objects
            settings.maxWildAnimals = (int)Widgets.HorizontalSlider(sliderRect, settings.maxWildAnimals, 0, 1000, true, null, "0", "1000", 1);
            string maxWildAnimalsText = settings.maxWildAnimals.ToString();
            Widgets.TextFieldNumeric(textBoxRect, ref settings.maxWildAnimals, ref maxWildAnimalsText, 0, 1000);

            // Move Restore Defaults button to the bottom right corner
            Rect restoreDefaultsRect = new Rect(inRect.width - 150, inRect.height - 40, 140, 30);
            if (Widgets.ButtonText(restoreDefaultsRect, "Restore Defaults"))
            {
                settings.maxWildAnimals = 100;
            }

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return "Wildlife Control";
        }
    }

    public class WildlifeControlContinuedController : GameComponent
    {
        private int nextCheckTick;
        private static bool animalRemoved;

        public WildlifeControlContinuedController(Game game) : base()
        {
        }

        public override void GameComponentTick()
        {
            if (Find.TickManager.TicksGame >= nextCheckTick)
            {
                CheckAnimalLimit();
                nextCheckTick = Find.TickManager.TicksGame + (animalRemoved ? 2 : GenDate.TicksPerDay);
            }
        }

        private static void CheckAnimalLimit()
        {
            AnimalLimitSettings settings = LoadedModManager.GetMod<WildlifeControlContinued>().GetSettings<AnimalLimitSettings>();
            int maxWildAnimals = settings.maxWildAnimals;

            foreach (Map map in Find.Maps)
            {
                var wildAnimals = map.mapPawns.AllPawnsSpawned.Where(p => p.RaceProps.Animal && p.Faction == null).ToList();

                wildAnimals = wildAnimals
                    .GroupBy(a => a.def)
                    .OrderByDescending(g => g.Count())
                    .SelectMany(g => g)
                    .OrderByDescending(a => a.health.summaryHealth.SummaryHealthPercent < 1)
                    .ThenByDescending(a => a.health.hediffSet.hediffs.Any(h => h.IsPermanent()))
                    .ThenByDescending(a => a.ageTracker.AgeBiologicalTicks / a.RaceProps.lifeExpectancy)
                    .ToList();

                if (wildAnimals.Count > maxWildAnimals)
                {
                    Pawn animalToRemove = wildAnimals.First();
                    animalToRemove.Destroy();
                    animalRemoved = true;
                }
                else
                {
                    animalRemoved = false;
                }
            }
        }
    }
}