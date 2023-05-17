using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

// show messages for gene spawner genes
namespace zed_0xff.GeneCollectorQOL
{
    [HarmonyPatch]
    public static class Patch_WVC_GeneSpawner
    {
        /*
        public static void HandlePatch(Harmony har)
        {
            if (LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == "wvc.sergkart.races.biotech")) {
                har.Patch(TargetMethod(), new HarmonyMethod(typeof(Patch_WVC_GeneSpawner).GetMethod("Transpiler", BindingFlags.Static | BindingFlags.Public)));
            }
        }
        */

        public static MethodBase TargetMethod()
        {
            MethodInfo mi = AccessTools.FirstMethod(AccessTools.TypeByName("WVC.Gene_Spawner"), m => m.Name == "SpawnItems");
            return mi;
        }

        static MethodInfo origTryPlaceThing = HarmonyLib.AccessTools.Method(
                typeof(GenPlace),
                nameof(GenPlace.TryPlaceThing),
                new[] {typeof(Thing), typeof(IntVec3), typeof(Map), typeof(ThingPlaceMode), typeof(Action<Thing,int>), typeof(Predicate<IntVec3>), typeof(Rot4)}
                );

        static MethodInfo myTryPlaceThing = HarmonyLib.AccessTools.Method(
                typeof(Patch_WVC_GeneSpawner),
                nameof(Patch_WVC_GeneSpawner.TryPlaceThing),
                new[] {typeof(Thing), typeof(IntVec3), typeof(Map), typeof(ThingPlaceMode), typeof(Action<Thing,int>), typeof(Predicate<IntVec3>), typeof(Rot4)}
                );

        static MethodInfo myShowMsg = HarmonyLib.AccessTools.Method(
                typeof(Patch_WVC_GeneSpawner),
                nameof(Patch_WVC_GeneSpawner.showMsg)
                );

        public static float sat => 1.0f;
        public static Color yellow => new Color(sat, sat, 0);
        public static Color green  => new Color(0,   sat, 0);
        public static Color red    => new Color(sat, 0,   0);

        static bool TryPlaceThing(Thing thing, IntVec3 center, Map map, ThingPlaceMode mode, Action<Thing, int> placedAction, Predicate<IntVec3> nearPlaceValidator, Rot4 rot){
            if( thing is Genepack gp){
                string text = gp.GeneSet.GenesListForReading.Select((Func<GeneDef, string>)(x => x.label)).ToCommaList().CapitalizeFirst();
                switch( GeneCache.GenepackStatus(gp) ){
                    case 0:
                        text = null;
                        break;
                    case 1:
                        text = text.Colorize(red);
                        break;
                    case 2:
                        text = text.Colorize(yellow);
                        break;
                }
                if( text != null ){
//                    Messages.Message(
//                            "GeneExtractionComplete".Translate(pawn.Named("PAWN")) + ": " + text,
//                            new LookTargets((TargetInfo)pawn, (TargetInfo)gp),
//                            MessageTypeDefOf.PositiveEvent);
                }
            }
            return GenPlace.TryPlaceThing(thing, center, map, mode, placedAction, nearPlaceValidator, rot);
        }

        static void showMsg(Pawn pawn, Thing thing){
            if( thing is Genepack gp){
                // TODO: config
                string text = gp.GeneSet.GenesListForReading.Select((Func<GeneDef, string>)(x => x.label)).ToCommaList().CapitalizeFirst();
                switch( GeneCache.GenepackStatus(gp) ){
                    case 0:
                        text = null;
                        break;
                    case 1:
                        text = text.Colorize(red);
                        break;
                    case 2:
                        text = text.Colorize(yellow);
                        break;
                }
                if( text != null ){
                    Messages.Message(
                            (string)("GenepackSpawned".Translate(pawn.Named("PAWN"), thing.def.label)) + text,
                            new LookTargets((TargetInfo)pawn, (TargetInfo)gp),
                            MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
//                if ( code.opcode == OpCodes.Call && (MethodInfo)code.operand == origTryPlaceThing ){
//                    code.operand = myTryPlaceThing;
//                }
            var codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count; i++){
                if (codes[i].opcode == OpCodes.Ret && i == codes.Count - 1){
                    yield return new CodeInstruction(OpCodes.Ldarg_1, null); // Pawn
                    yield return new CodeInstruction(OpCodes.Ldloc_0, null); // Genepack
                    yield return new CodeInstruction(OpCodes.Call, myShowMsg);
                }
                yield return codes[i];
            }
            // ldloc.0 => Genepack
            // ldarg.1 => Pawn
            //return instructions;
        }
    }
}