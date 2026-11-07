using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Vintagestory.API.MathTools;
using Vintagestory.ServerMods;
using RegionalOreDensity.lib;

namespace RegionalOreDensity.handler
{
    //[HarmonyPatch (typeof(Vintagestory.ServerMods.GenDeposits), nameof(Vintagestory.ServerMods.GenDeposits.GenDeposit))]
    [HarmonyPatch (typeof(Vintagestory.ServerMods.GenDeposits), nameof(Vintagestory.ServerMods.GenDeposits.GeneratePartial))]
    public static class ROD_HarmonyPatch_GenDeposits
    {
        /// <summary>
        /// Patches the local variable [qModified@222]<br></br>
        /// to multiply by [Multi_Chance] results.<br></br>
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyTranspiler] public static IEnumerable<CodeInstruction> Patch_Trans_qModified(IEnumerable<CodeInstruction> instructions)
        {
            var matchedCode = new CodeMatcher(instructions);
            //string modifiedVar = "quantity";
            string multiType = ROD_Handle_Collection.PropNames.name_MultiChance;

            matchedCode.MatchStartForward(
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloc_S && ci.operand is LocalBuilder local && local.LocalIndex == 7),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GenDeposits), "TriesPerChunk")),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloc_S && ci.operand is LocalBuilder local && local.LocalIndex == 8),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GenDeposits), "chanceMultiplier")),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloc_S && ci.operand is LocalBuilder local && local.LocalIndex == 7),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DepositVariant), "ScaleWithWorldHeight")),
                new CodeMatch(OpCodes.Brtrue_S),
                new CodeMatch(OpCodes.Ldc_R4),
                new CodeMatch(OpCodes.Br_S),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloc_S && ci.operand is LocalBuilder local && local.LocalIndex == 5),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(ci => ci.opcode == OpCodes.Stloc_S && ci.operand is LocalBuilder local && local.LocalIndex == 9)
                );

            if (matchedCode.IsValid) { ROD_Function_General.Log_Debug("Harmony patch applied"); }
            else { ROD_Function_General.Log_Debug("Harmony patch failed"); return matchedCode.InstructionEnumeration(); }

            matchedCode.Advance(15);
            matchedCode.Insert(
                new CodeInstruction(OpCodes.Ldloc_S, 9),
                new CodeInstruction(OpCodes.Ldloc_S, 7),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositVariant), "Code")),//variant.code
                new CodeInstruction(OpCodes.Ldstr, multiType),
                new CodeInstruction(OpCodes.Ldloc_1),
                new CodeInstruction(OpCodes.Ldc_I4_S, 32),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Ldc_I4_S, -99),
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldc_I4_S, 32),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(BlockPos), [typeof(int), typeof(int), typeof(int)])),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ROD_Handle_DensityMap.Apply_Modifier), nameof(ROD_Handle_DensityMap.Apply_Modifier.ApplyModifiers), [typeof(string), typeof(string), typeof(BlockPos)])),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Stloc_S, 9)
                );

            /*matchedCode.MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(LCGRandom), "depositRand")),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloc_S && ci.operand is LocalBuilder local && local.LocalIndex == 11),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(LCGRandom), "SetWorldSeed")),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(GenDeposits), "depositRand")),
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldloc_2),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(LCGRandom), "InitPositionSeed", [typeof(int), typeof(int)]))
                );

            if (matchedCode.IsInvalid) { return matchedCode.InstructionEnumeration(); }

            matchedCode.Advance(5);
            matchedCode.Insert(
                new CodeInstruction(OpCodes.Ldloc_S, 10),
                new CodeInstruction(OpCodes.Conv_R4),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositGeneratorBase), "variant")),//variant
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositVariant), "Code")),//variant.code
                new CodeInstruction(OpCodes.Ldstr, multiType),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(GenDeposits), "tmpPos")),//tmpPos
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ROD_Handle_DensityMap.Apply_Modifier), nameof(ROD_Handle_DensityMap.Apply_Modifier.ApplyModifiers), [typeof(string), typeof(string), typeof(BlockPos)])),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MathF), nameof(MathF.Round), [typeof(float)])),
                new CodeInstruction(OpCodes.Conv_I4),
                new CodeInstruction(OpCodes.Stloc_S, 10)
                );*/

            return matchedCode.InstructionEnumeration();
        }
    }

    /// <summary>
    /// Holds harmony patches for [Vintagestory.ServerMods.DiscDepositGenerator.GenDeposit()]<br></br>
    /// For applying region multipliers from [ROD_Handle_DensityMap.ApplyModifiers()]<br></br>
    /// Based on the cached Config's list [Config_OreDensity.List_OreMods]
    /// </summary>
    [HarmonyPatch (typeof(Vintagestory.ServerMods.DiscDepositGenerator), nameof(Vintagestory.ServerMods.DiscDepositGenerator.GenDeposit))]
    public static class ROD_HarmonyPatch_DiscDepositGenerator
    {

        //see if i can patch into the noisemap system itself and layer my density multipliers directly over it. otherwise find the ore spawn method and indirectly patch the generation values with my multipliers.
        //world noisemaps -> vintagestory.servermod.gendeposite.  getOrCreateMapLayer ? OnMapRegionGen ?
        //ore generation code on chunk generation -> vintagestory.servermod.gendeposit. GenDeposit ?
        //actual placing of deposit -> Vintagestory.ServerMods.DiscDepositGenerator .gendeposit() ?
        // on chunk gen is probably going to be better as it allows the mod to be safely added and removed after the noisemaps are generated
        //chance in gendeposits.generatepartial quantity@230
        //radiusX/Z@205
        //depoitThickness@224
        //(grade) val@222

        /*private static readonly AccessTools.FieldRef<DepositGeneratorBase, DepositVariant> VarientRef = AccessTools.FieldRefAccess<DepositGeneratorBase, DepositVariant>("varient");

        public static void Prefix(DiscDepositGenerator __instance)
        {
            DepositVariant varient = VarientRef.Invoke((DepositGeneratorBase)(object)__instance);

            StoreVariable(varient)
        }*/


        /// <summary>
        /// Patches the local variable [radiusX@205]<br></br>
        /// to multiply by [Multi_Width] results.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyTranspiler] public static IEnumerable<CodeInstruction> Patch_Trans_RadiuxX(IEnumerable<CodeInstruction> instructions)
        {
            var matchedCode = new CodeMatcher(instructions);
            string modifiedVar = "radiusX";
            string multiType = ROD_Handle_Collection.PropNames.name_MultiWidth;

            matchedCode.MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Conv_R4),
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(OpCodes.Conv_I4),
                new CodeMatch(OpCodes.Sub),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DiscDepositGenerator), modifiedVar))
                );

            if (matchedCode.IsValid) { ROD_Function_General.Log_Debug("Harmony patch applied"); }
            else { ROD_Function_General.Log_Debug("Harmony patch failed"); return matchedCode.InstructionEnumeration(); }

            matchedCode.Advance(9);
            matchedCode.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DiscDepositGenerator), modifiedVar)),
                new CodeInstruction(OpCodes.Conv_R4),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositGeneratorBase), "variant")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositVariant), "Code")),
                new CodeInstruction(OpCodes.Ldstr, multiType),
                new CodeInstruction(OpCodes.Ldarg_S, 5),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ROD_Handle_DensityMap.Apply_Modifier), nameof(ROD_Handle_DensityMap.Apply_Modifier.ApplyModifiers), [typeof(string), typeof(string), typeof(BlockPos)])),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MathF), nameof(MathF.Round), [typeof(float)])),
                new CodeInstruction(OpCodes.Conv_I4),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(DiscDepositGenerator), modifiedVar))
                );

            return matchedCode.InstructionEnumeration();
        }

        /// <summary>
        /// Patches the local variable [radiusZ@206]<br></br>
        /// to multiply by [Multi_Width] results.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyTranspiler] public static IEnumerable<CodeInstruction> Patch_Trans_RadiuxZ(IEnumerable<CodeInstruction> instructions)//TODO: switch multiwidth to have both a z and x multiplier option
        {
            var matchedCode = new CodeMatcher(instructions);
            string modifiedVar = "radiusZ";
            string multiType = ROD_Handle_Collection.PropNames.name_MultiWidth;

            matchedCode.MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Conv_R4),
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Mul),
                new CodeMatch(OpCodes.Conv_I4),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DiscDepositGenerator), modifiedVar))
                );

            if (matchedCode.IsValid) { ROD_Function_General.Log_Debug("Harmony patch applied"); }
            else { ROD_Function_General.Log_Debug("Harmony patch failed"); return matchedCode.InstructionEnumeration(); }

            matchedCode.Advance(9);
            matchedCode.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DiscDepositGenerator), modifiedVar)),
                new CodeInstruction(OpCodes.Conv_R4),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositGeneratorBase), "variant")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositVariant), "Code")),
                new CodeInstruction(OpCodes.Ldstr, multiType),
                new CodeInstruction(OpCodes.Ldarg_S, 5),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ROD_Handle_DensityMap.Apply_Modifier), nameof(ROD_Handle_DensityMap.Apply_Modifier.ApplyModifiers), [typeof(string), typeof(string), typeof(BlockPos)])),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MathF), nameof(MathF.Round), [typeof(float)])),
                new CodeInstruction(OpCodes.Conv_I4),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(DiscDepositGenerator), modifiedVar))
                );

            return matchedCode.InstructionEnumeration();
        }


        /// <summary>
        /// Patches the variable [depositThickness@224] <br></br>
        /// to multiply by [Multi_Height] results.
        /// </summary>
        /// <param name="instructions"></param>
        /// <returns></returns>
        [HarmonyTranspiler] public static IEnumerable<CodeInstruction> Patch_Trans_DepositThickness(IEnumerable<CodeInstruction> instructions)
        {
            var matchedCode = new CodeMatcher(instructions);
            string modifiedVar = "depoitThickness";//not "depoSit"
            string multiType = ROD_Handle_Collection.PropNames.name_MultiHeight;

            matchedCode.MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloc_S && ci.operand is LocalBuilder local && local.LocalIndex == 7),
                new CodeMatch(OpCodes.Conv_I4),
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(DepositGeneratorBase), "DepositRand")),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(LCGRandom), "NextFloat")),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloc_S && ci.operand is LocalBuilder local && local.LocalIndex == 7),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloc_S && ci.operand is LocalBuilder local && local.LocalIndex == 7),
                new CodeMatch(OpCodes.Conv_I4),
                new CodeMatch(OpCodes.Conv_R4),
                new CodeMatch(OpCodes.Sub),
                new CodeMatch(OpCodes.Clt),
                new CodeMatch(OpCodes.Add),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(DiscDepositGenerator), modifiedVar))
                );

            if (matchedCode.IsValid) { ROD_Function_General.Log_Debug("Harmony patch applied"); }
            else { ROD_Function_General.Log_Debug("Harmony patch failed"); return matchedCode.InstructionEnumeration(); }

            matchedCode.Advance(14);
            matchedCode.Insert(
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DiscDepositGenerator), modifiedVar)),
                new CodeInstruction(OpCodes.Conv_R4),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositGeneratorBase), "variant")),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(DepositVariant), "Code")),
                new CodeInstruction(OpCodes.Ldstr, multiType),
                new CodeInstruction(OpCodes.Ldarg_S, 5),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ROD_Handle_DensityMap.Apply_Modifier), nameof(ROD_Handle_DensityMap.Apply_Modifier.ApplyModifiers), [typeof(string), typeof(string), typeof(BlockPos)])),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MathF), nameof(MathF.Round), [typeof(float)])),
                new CodeInstruction(OpCodes.Conv_I4),
                new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(DiscDepositGenerator), modifiedVar))
                );

            return matchedCode.InstructionEnumeration();
        }
    }
}
