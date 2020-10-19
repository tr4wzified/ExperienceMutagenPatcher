using System;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ExperienceMutagenPatcher
{
    public class Program
    {
        private static string loadPath = "";
        public static int Main(string[] args)
        {
            var done = SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                new UserPreferences
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher
                    {
                        TargetRelease = GameRelease.SkyrimSE
                    }
                }); ;
            File.Delete(loadPath + @"\Null");
            return done;
        }

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            var csv = new StringBuilder();
            foreach (var race in state.LoadOrder.PriorityOrder.WinningOverrides<IRaceGetter>())
            {
                float startingHealth = race.Starting.Values.ElementAt(0);
                // Not using Math.Round since the result will be different from JS Math.round
                // Using this trick from https://stackoverflow.com/questions/1862992/how-close-is-the-javascript-math-round-to-the-c-sharp-math-round (second answer
                csv.Append(race.EditorID + "," + Math.Floor((startingHealth / 10) + 0.5) + '\n');
            }
            loadPath = state.Settings.DataFolderPath;
            Directory.CreateDirectory(state.Settings.DataFolderPath + @"\SKSE\Plugins\Experience\Races\");
            File.WriteAllText(state.Settings.DataFolderPath + @"\SKSE\Plugins\Experience\Races\experiencePatch.csv", csv.ToString());
        }
    }
}
