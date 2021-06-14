using System;
using Zenject;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using IPA.Utilities;
using System.Reflection;
using DiSounds.Managers;
using DiSounds.Components;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace DiSounds.HarmonyPatches
{
    [HarmonyPatch(typeof(ResultsViewController), "DidActivate")]
    internal class ResultViewActivated
    {
        private static readonly MethodInfo _rootMethod = typeof(SongPreviewPlayer).GetMethod("CrossfadeTo", new Type[] { typeof(AudioClip), typeof(float), typeof(float), typeof(float) });
        private static readonly FieldInfo _songPreview = typeof(ResultsViewController).GetField("_songPreviewPlayer", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _fireworksController = typeof(ResultsViewController).GetField("_fireworksController", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _completion = typeof(ResultsViewController).GetField("_levelCompletionResults", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo _highscore = typeof(ResultsViewController).GetField("_newHighScore", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo _crossfadeOverride = SymbolExtensions.GetMethodInfo(() => CrossfadeOverride(null!, null!, null!, false, null!, 0, 0, 0));

        public static FieldAccessor<ResultsViewController, bool>.Accessor HighScore = FieldAccessor<ResultsViewController, bool>.GetAccessor("_newHighScore");
        public static FieldAccessor<ResultsViewController, SongPreviewPlayer>.Accessor PreviewPlayer = FieldAccessor<ResultsViewController, SongPreviewPlayer>.GetAccessor("_songPreviewPlayer");
        public static FieldAccessor<ResultsViewController, FireworksController>.Accessor FireworkController = FieldAccessor<ResultsViewController, FireworksController>.GetAccessor("_fireworksController");
        public static FieldAccessor<ResultsViewController, LevelCompletionResults>.Accessor Completion = FieldAccessor<ResultsViewController, LevelCompletionResults>.GetAccessor("_levelCompletionResults");
        public static FieldAccessor<FireworksController, FireworkItemController.Pool>.Accessor FireworkControllerPool = FieldAccessor<FireworksController, FireworkItemController.Pool>.GetAccessor("_fireworkItemPool");
        public static FieldAccessor<MemoryPoolBase<FireworkItemController>, DiContainer>.Accessor MemoryPoolBaseDiContainer = FieldAccessor<MemoryPoolBase<FireworkItemController>, DiContainer>.GetAccessor("_container");

        internal static void Postfix(SongPreviewPlayer ____songPreviewPlayer, FireworksController ____fireworksController, LevelCompletionResults ____levelCompletionResults)
        {
            if (____levelCompletionResults.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed)
            {
                CrossfadeOverride(____songPreviewPlayer, ____fireworksController, ____levelCompletionResults, false, null!, -4f, 0f, 1f);
            } 
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            int previewPos = -1;
            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                if (code.Is(OpCodes.Ldfld, _songPreview))
                {
                    previewPos = i;
                }

                if (code.Calls(_rootMethod))
                {
                    codes[i] = new CodeInstruction(OpCodes.Callvirt, _crossfadeOverride);
                }
            }

            if (previewPos != -1)
            {
                codes.InsertRange(previewPos + 1, new List<CodeInstruction>()
                {
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, _fireworksController),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, _completion),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Ldfld, _highscore),
                });
            }

            return codes.AsEnumerable();
        }

        private static void CrossfadeOverride(SongPreviewPlayer songPreviewPlayer, FireworksController fireworksController, LevelCompletionResults completionResults, bool wasHighScore, AudioClip audioClip, float volume, float startTime, float duration)
        {
            var player = songPreviewPlayer;
            DiContainer container = null!;

            // We can do this the easy way, or the hard way.
            if (player is DisoPreviewPlayer disoPlayer)
            {
                container = disoPlayer.Container;
            }
            else
            {
                var fireworks = fireworksController;
                var pool = FireworkControllerPool(ref fireworks);
                var poolBase = pool as MemoryPoolBase<FireworkItemController>;
                container = MemoryPoolBaseDiContainer(ref poolBase!);
            }

            var config = container.Resolve<Config>();
            if (config.ResultSoundsEnabled || config.ResultFailedSoundsEnabled)
            {
                _ = container.Resolve<ResultsSoundManager>().PlayRandomAudio(audioClip, completionResults, wasHighScore);
            }
            else
            {
                if (wasHighScore && audioClip != null)
                {
                    player.CrossfadeTo(audioClip, volume, startTime, duration);
                }
            }
        }
    }
}