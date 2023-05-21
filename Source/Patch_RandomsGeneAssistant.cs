using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using System.Reflection;
using System.Reflection.Emit;

namespace zed_0xff.GeneCollectorQOL;

public static class Patch_RandomsGeneAssistant
{
    const string PackageId = "rimworld.randomcoughdrop.geneassistant";

    public static void HandlePatch(Harmony harmony) {
        if (LoadedModManager.RunningModsListForReading.Any(m => m.PackageId == PackageId)) {
            foreach( MethodBase m in TargetMethods() ){
                harmony.Patch(m,
                        transpiler: new HarmonyMethod(
                            MethodBase.GetCurrentMethod().DeclaringType.GetMethod("Transpiler", BindingFlags.Static | BindingFlags.Public),
                            after: new[] { PackageId }
                            )
                        );
            }
        }
    }

    public static IEnumerable<MethodBase> TargetMethods()
    {
        // hobtook.tradeui
        // yield return AccessTools.FirstMethod(AccessTools.TypeByName("Harmony_DialogTrade_FillMainRect"), m => m.Name == "MyDrawTradableRow");
        yield return AccessTools.Method(typeof(RimWorld.TradeUI), nameof(RimWorld.TradeUI.DrawTradeableRow));
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
    {
        MethodInfo m = AccessTools.Method(typeof(Transferable), nameof(Transferable.ThingDef));
        FieldInfo f = AccessTools.Field(typeof(ThingDefOf), nameof(ThingDefOf.Genepack));

        var codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++){
            if (
                    codes[i].Calls(m) &&
                    codes[i+1].LoadsField(f) &&
                    codes[i+2].opcode == OpCodes.Beq_S &&
                    codes[i+3].opcode == OpCodes.Ret
               ) {
                var label = gen.DefineLabel();
                yield return new CodeInstruction(OpCodes.Isinst, typeof(Genepack));
                yield return new CodeInstruction(OpCodes.Brtrue, label);
                yield return new CodeInstruction(OpCodes.Ret);
                yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
                i+=3;
            } else {
                yield return codes[i];
            }
        }
    }
}
