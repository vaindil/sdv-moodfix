using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace MoodFix
{
    public class ModEntry : Mod
    {
        private List<AnimalWrapper> _animals = new List<AnimalWrapper>();

        public override void Entry(IModHelper helper)
        {
            SaveEvents.AfterLoad += Initialize;
        }

        private void Initialize(object sender, EventArgs e)
        {
            if (!Game1.player.professions.Contains(2) && !Game1.player.professions.Contains(3))
            {
                Monitor.Log("Profession not found, will continue to check");
                GameEvents.OneSecondTick += Initialize;
                return;
            }

            GameEvents.OneSecondTick -= Initialize;

            foreach (var animal in Game1.getFarm().getAllFarmAnimals())
            {
                _animals.Add(new AnimalWrapper(animal));
            }

            GameEvents.QuarterSecondTick += CheckAnimalHappiness;
        }

        private void CheckAnimalHappiness(object sender, EventArgs e)
        {
            var animals = Game1.getFarm().getAllFarmAnimals();
            _animals.RemoveAll(a => !animals.Contains(a.Animal));

            // Loop in reverse order so elements can be removed from the list safely
            for (var i = animals.Count - 1; i >= 0; i--)
            {
                var animal = animals[i];
                var existing = _animals.Find(a => a.Animal == animal);
                if (existing != null)
                {
                    if (existing.CurrentHappiness != animal.happiness && existing.WasOverflown(animal.happiness))
                    {
                        animal.happiness = 255;
                        existing.CurrentHappiness = 255;
                        Monitor.Log($"Happiness overflow detected: {animal.type} {animal.displayName}, setting to 255");
                    }

                    animals.RemoveAt(i);
                }
            }

            // These animals are new to the party
            foreach (var animal in animals)
            {
                _animals.Add(new AnimalWrapper(animal));
                Monitor.Log($"New animal detected: {animal.type} {animal.displayName}");
            }
        }
    }

    /// <summary>
    /// Wrapper around farm animals to track information
    /// </summary>
    internal class AnimalWrapper
    {
        public AnimalWrapper(FarmAnimal animal)
        {
            Animal = animal;
            CurrentHappiness = animal.happiness;
            HappinessChangeWhenPetted = (40 - animal.happinessDrain) * 2;
        }

        public bool WasOverflown(int newHappiness)
        {
            if (newHappiness == (CurrentHappiness + HappinessChangeWhenPetted) - 256)
                return true;

            return false;
        }

        /// <summary>
        /// The animal's internal ID
        /// </summary>
        public FarmAnimal Animal { get; set; }

        /// <summary>
        /// The current happiness of the animal to compare against when the value changes
        /// </summary>
        public int CurrentHappiness { get; set; }

        /// <summary>
        /// The amount the animal's happiness will change when petted
        /// </summary>
        public int HappinessChangeWhenPetted { get; set; }
    }
}
