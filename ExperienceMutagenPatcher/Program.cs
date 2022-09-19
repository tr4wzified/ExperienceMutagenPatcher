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
                var race = raceGetter.Record;
                var contexts = state.LinkCache.ResolveAllContexts<IRace, IRaceGetter>(race.FormKey).ToList();
                var originalModKey = contexts[^1].ModKey;

                if (alreadyIncludedModKeys.Contains(originalModKey))
                    continue;

                float startingHealth = race.Starting.Values.ElementAt(0);
                if (!generatedData.ContainsKey(originalModKey))
                    generatedData.Add(originalModKey, new List<string>());

                // Not using Math.Round since the result will be different from JS Math.round in the original zEdit patcher
                // Using this trick from https://stackoverflow.com/questions/1862992/how-close-is-the-javascript-math-round-to-the-c-sharp-math-round (second answer)
                var xp = Math.Floor((startingHealth / 10) + 0.5);
                var sentence1 = @$";{race.EditorID} ""{race.Name}""";
                var sentence2 = @$"00{race.FormKey.IDString()} = {xp}";
                var sentence = sentence1 + "\n" + sentence2;
                generatedData[originalModKey].Add(sentence);
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
