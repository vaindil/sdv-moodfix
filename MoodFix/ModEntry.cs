using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

namespace MoodFix
{
    public class ModEntry : Mod
    {
        private readonly List<AnimalWrapper> _animals = new List<AnimalWrapper>();

        public override void Entry(IModHelper helper)
        {
            SaveEvents.AfterLoad += Initialize;
            SaveEvents.AfterReturnToTitle += KillItWithFire;
        }

        private void Initialize(object sender, EventArgs e)
        {
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

                // Check if the animal is already being tracked
                var existing = _animals.Find(a => a.Animal == animal);

                if (existing != null)
                {
                    // If the happiness didn't change then there's no reason to run the following calculations
                    if (existing.CurrentHappiness != animal.happiness.Value)
                    {
                        // These are used for the following check to fix bug where animal happiness drops after 6pm
                        var happinessChange = existing.CurrentHappiness - animal.happiness.Value;
                        var isAnimalIndoors = ((AnimalHouse)animal.home.indoors.Value).animals.TryGetValue(animal.myID.Value, out _);

                        // If the time is 6pm or later, the animal is indoors, and the happiness change was less than 10,
                        // this is the bug. Just revert the drop to correct the problem.
                        if (Game1.timeOfDay >= 1800 && isAnimalIndoors && happinessChange > 0 && happinessChange <= 10)
                        {
                            // Purposely commented out, this would be a bit too much spam
                            // Monitor.Log($"Fixing animal happiness: {animal.name}, from {animal.happiness} to {existing.CurrentHappiness}");

                            animal.happiness.Value = (byte)existing.CurrentHappiness;
                        }
                    }

                    // Animal is taken care of so remove it from the list
                    animals.RemoveAt(i);
                }
            }

            // These animals are new to the party (they weren't removed by the previous loop)
            foreach (var animal in animals)
            {
                _animals.Add(new AnimalWrapper(animal));
                Monitor.Log($"New animal detected: {animal.type} {animal.displayName}");
            }
        }

        private void KillItWithFire(object sender, EventArgs e)
        {
            GameEvents.QuarterSecondTick -= CheckAnimalHappiness;
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
            CurrentHappiness = animal.happiness.Value;
        }

        /// <summary>
        /// The animal's internal ID
        /// </summary>
        public FarmAnimal Animal { get; set; }

        /// <summary>
        /// The current happiness of the animal to compare against when the value changes
        /// </summary>
        public int CurrentHappiness { get; set; }
    }
}
