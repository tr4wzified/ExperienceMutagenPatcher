using System;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;

namespace ExperienceMutagenPatcher
{
    public class Program
    {
        private static string loadPath = "";

        private static readonly HashSet<ModKey> alreadyIncludedModKeys = new HashSet<ModKey>()
        {
            ModKey.FromNameAndExtension("Skyrim.esm"),
            ModKey.FromNameAndExtension("Dawnguard.esm"),
            ModKey.FromNameAndExtension("Dragonborn.esm"),
        };
        public static async Task<int> Main(string[] args)
        {
            var espName = "deleteThisEspIfYouSeeIt-itsEmpty.esp";
            var done = await SynthesisPipeline.Instance
                .SetTypicalOpen(GameRelease.SkyrimSE, espName)
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .Run(args);
            File.Delete(loadPath + @$"\{espName}");
            return done;
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Dictionary<ModKey, List<string>> generatedData = new Dictionary<ModKey, List<string>>();
            var ini = new StringBuilder();
            foreach (var raceGetter in state.LoadOrder.PriorityOrder.Race().WinningContextOverrides())
            {
                if (alreadyIncludedModKeys.Contains(raceGetter.ModKey))
                    continue;

                var race = raceGetter.Record;
                float startingHealth = race.Starting.Values.ElementAt(0);
                if (!generatedData.ContainsKey(raceGetter.ModKey))
                    generatedData.Add(raceGetter.ModKey, new List<string>());

                // Not using Math.Round since the result will be different from JS Math.round
                // Using this trick from https://stackoverflow.com/questions/1862992/how-close-is-the-javascript-math-round-to-the-c-sharp-math-round (second answer
                generatedData[raceGetter.ModKey].Add($"{race.EditorID}={Math.Floor((startingHealth / 10) + 0.5)}");
            }

            generatedData = generatedData.OrderBy(kv => kv.Key.Name).ToDictionary();
            foreach (var (modKey, linesToOutput) in generatedData)
            {
                ini.AppendLine($"[{modKey.FileName}]");
                linesToOutput.ForEach(line => ini.AppendLine(line));
                ini.AppendLine();
            }

            loadPath = state.DataFolderPath;
            Directory.CreateDirectory(state.DataFolderPath + @"\SKSE\Plugins\Experience\Races\");
            File.WriteAllText(state.DataFolderPath + @"\SKSE\Plugins\Experience\Races\GeneratedExperiencePatch.ini", ini.ToString());
        }
    }
}
