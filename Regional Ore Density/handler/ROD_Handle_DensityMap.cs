using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using RegionalOreDensity.lib;
using RegionalOreDensity.config;

namespace RegionalOreDensity.handler
{
    public class ROD_Handle_DensityMap
    {
        private static ICoreAPI API = ROD_VarRef.API;
        private static ICoreClientAPI API_Client = ROD_VarRef.API_Client;
        private static ICoreServerAPI API_Server = ROD_VarRef.API_Server;


        internal class Validate_List
        {
            static bool wasClamped;


            /// <summary>
            /// Starts the validation proccess of the config's ore modds list.
            /// </summary>
            internal static void Validate_Start()
            {
                var refList = Config_Reference.Config_Loaded!.Config_OreDensity.List_OreMods;
                List<Dictionary<string, Dictionary<string, float[]>>> List_BadOreMods = new();

                for (int listPos = 0; listPos < refList.Count; listPos++)
                {
                    foreach (string key_OreCode in refList[listPos].Keys)
                    {
                        Dictionary<string, Dictionary<string, float[]>> dict_ValidProps = new();
                        Dictionary<string, Dictionary<string, float[]>> dict_CurrentConfig = refList[listPos];


                        //if (Validate_BlockExists(key_OreCode)) { } else { List_BadOreMods.Add(refList[listPos]); ReportFailure(1, [listPos, key_OreCode]); continue; } //Cant use this as it needs to run way later then the config is loaded. may find a way to add it back.
                        if (dict_CurrentConfig[key_OreCode].ContainsKey(ROD_Handle_Collection.PropNames.name_AreaType)) { } else { List_BadOreMods.Add(refList[listPos]); ReportFailure(2, [listPos, key_OreCode]); continue; }

                        switch (MathF.Round(refList[listPos][key_OreCode][ROD_Handle_Collection.PropNames.name_AreaType][0]))
                        {
                            case (1f): { dict_ValidProps = ROD_Handle_Collection.AreaType_Props.Props_Cardinal; break; }
                            case (2f): { dict_ValidProps = ROD_Handle_Collection.AreaType_Props.Props_Climate; break; }
                            case (3f): { dict_ValidProps = ROD_Handle_Collection.AreaType_Props.Props_Region; break; }
                            case (4f): { dict_ValidProps = ROD_Handle_Collection.AreaType_Props.Props_HostBlock; break; }
                            default: { break; }
                        }

                        if (dict_ValidProps is not null && dict_ValidProps.Count > 0) { } else { List_BadOreMods.Add(refList[listPos]); ReportFailure(2, [listPos, key_OreCode]); continue; }
                        if (Validate_Props(listPos, dict_CurrentConfig[key_OreCode], dict_ValidProps) && Validate_Props(listPos, dict_CurrentConfig[key_OreCode], ROD_Handle_Collection.AreaType_Props.Props_General) &&
                            Validate_Clamps(listPos, key_OreCode, ref dict_CurrentConfig, dict_ValidProps) && Validate_Clamps(listPos, key_OreCode, ref dict_CurrentConfig, ROD_Handle_Collection.AreaType_Props.Props_General))
                        { } else { List_BadOreMods.Add(refList[listPos]); ReportFailure(3, [listPos, key_OreCode]); continue; }

                        //successful validation
                        ROD_Handle_Collection.List_OreMod_Cache.Add(new() { [key_OreCode] = dict_CurrentConfig[key_OreCode] });
                    }
                }

                if (wasClamped) { ReportFailure(5); }
                if (Config_Reference.Config_Loaded.Config_System.System_Debug_Acc.AllowConfigOverwrite) { Config_Reference.Config_Loaded.Config_OreDensity.List_OreMods = ROD_Handle_Collection.List_OreMod_Cache; Config_Reference.Config_Loaded.Config_OreDensity.List_OreMods.AddRange(List_BadOreMods); }//TODO: make sure badoremods works
            }


            /// <summary>
            /// Ensures each ore in the config's list has a domain. Assumes "game:" if none is given.
            /// Not needed anymore since wildcard check already does this. Remove at own leisure.
            /// </summary>
            internal static void Validate_Domains()
            {
                var listRef = Config_Reference.Config_Loaded!.Config_OreDensity.List_OreMods;
                
                for (int listPos = 0; listPos < listRef.Count; listPos++)
                {
                    foreach (string key in listRef[listPos].Keys.ToList())
                    {
                        if (key.Contains(':')) { continue; }
                        else
                        {
                            if (listRef[listPos].Remove(key, out var newDict))
                            {
                                string newKey = "game:" + key;
                                listRef[listPos].Add(newKey, newDict);
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Ensures the block code given is referencing a valid block registered by the game.
            /// Must be called after all assets have finalized (AssetsFinalize, AssetsLoaded is still to early).
            /// </summary>
            /// <param name="key_OreCode"></param>
            /// <returns></returns>
            internal static bool Validate_BlockExists(string key_OreCode)
            {
                if (API.World.GetBlock(new AssetLocation(key_OreCode)) is not null) { return true; }

                return false;
            }

            /// <summary>
            /// Verifies the required properties of the AreaType exist.
            /// </summary>
            /// <param name="listPos"></param>
            /// <param name="dict_CurrentConfig"></param>
            /// <param name="dict_ValidProps"></param>
            /// <returns></returns>
            internal static bool Validate_Props(int listPos, Dictionary<string, float[]> dict_CurrentConfig, Dictionary<string, Dictionary<string, float[]>> dict_ValidProps)
            {
                bool softProp = false;
                bool softPropRequired = false;


                foreach (var key in dict_ValidProps.Keys)
                {
                    if (dict_ValidProps[key].ContainsKey(ROD_Handle_Collection.PropNames.name_Required) && dict_ValidProps[key][ROD_Handle_Collection.PropNames.name_Required].Length > 0) { } else { continue; }
                    switch (dict_ValidProps[key][ROD_Handle_Collection.PropNames.name_Required][0])
                    {
                        case (0f): { continue; }
                        case (1f):
                            {
                                if (dict_CurrentConfig.TryGetValue(key, out float[]? val) && val is not null && val.Length > 0) { continue; }
                                return false;
                            }
                        case (2f):
                            {
                                softPropRequired = true;
                                if (dict_CurrentConfig.ContainsKey(key) && dict_CurrentConfig[key].Length > 0) { softProp = true; }
                                continue;
                            }
                        default: { return false; }
                    }
                }

                if (softProp || softPropRequired is false) { return true; }
                else { return false; }
            }

            /// <summary>
            /// Verifies the properties of the AreaType are populated as required and clamps them.
            /// </summary>
            /// <param name="listPos"></param>
            /// <param name="dict_CurrentConfig"></param>
            /// <param name="dict_ValidProps"></param>
            /// <returns></returns>
            internal static bool Validate_Clamps(int listPos, string key_OreCode, ref Dictionary<string, Dictionary<string, float[]>> dict_CurrentConfig, Dictionary<string, Dictionary<string, float[]>> dict_ValidProps)
            {
                foreach (var key in dict_ValidProps.Keys)
                {
                    if (dict_CurrentConfig[key_OreCode].ContainsKey(key))
                    {
                        int validProps_Length = 2;
                        var dict_CurrentOre = dict_CurrentConfig[key_OreCode];

                        for (int valPos = 0; valPos < validProps_Length; valPos++)
                        {
                            string value = "Val" + (valPos + 1).ToString();//watch out for hard coded dict key name here. if key is changed in the dict it must be updated here.

                            if (dict_CurrentOre[key].Length > valPos && dict_ValidProps[key].TryGetValue(value, out float[]? valArray) && valArray is not null && valArray.Length > 0)
                            {
                                float valid_Min = valArray[0];
                                float valid_Max = (valArray.Length >= 2 ? valArray[1] : float.MaxValue);
                                if (dict_CurrentOre[key][valPos] < valid_Min || (dict_CurrentOre[key].Length > 1 && dict_CurrentOre[key][valPos] > valid_Max))
                                {
                                    ReportFailure(4, [listPos, key_OreCode, key, value]); wasClamped = true;
                                    dict_CurrentOre[key][valPos] = Math.Clamp(dict_CurrentOre[key][valPos], valid_Min, valid_Max);
                                }
                            }
                        }
                    }
                    else if (dict_ValidProps[key].TryGetValue(ROD_Handle_Collection.PropNames.name_Required, out float[]? isReq) && isReq is not null && isReq.Length > 0 && isReq[0] is 1) { return false; } //should never be true
                }

                return true;
            }


            /// <summary>
            /// Logs a failure in loading the config's OreDensity list.
            /// 0: List/Dict is invalid. 1: AreaType is invalid. 2: Has invalid required props.
            /// </summary>
            /// <param name="failureType"></param>
            /// <param name="caller"></param>
            internal static void ReportFailure(int failureType, object[]? passedLoggers = null, [CallerMemberName] string caller = "")
            {
                string logOutput = "Error when validating config/" + nameof(ROD_Config_Main.OreDensity) + "; ";

                switch (failureType)
                {
                    case (0): { logOutput += "List or dictionaries were missing or malformed. " + nameof(ROD_Config_Main.OreDensity.List_OreMods) + " in config will be reset."; break; }
                    case (1): { logOutput += "Ore '[{0}]{1}' did not match to any registered block in the game and will be skipped."; break; }
                    case (2): { logOutput += "Ore '[{0}]{1}' had missing or invalid key for [AreaType] and will be skipped."; break; }
                    case (3): { logOutput += "Ore '[{0}]{1}' had missing or invalid values for the required props and will be skipped."; break; }
                    case (4): { logOutput += "Ore '[{0}]{1}' had a property '{2}[{3}]' out of bounds and was clamped. This change was" + (Config_Reference.Config_Loaded!.Config_System.System_Debug_Acc.AllowConfigOverwrite ? "" : " NOT") + " reflected in the config."; break; }
                    case (5): { logOutput += "At least one value in the OreDensity config was clamped. This can lead to ore generation changes that were not intended. PLEASE VERIFY YOUR CONFIG VALUES."; break; }
                    default: { ROD_Function_General.Log_Error("Invalid error type in [ReportFailure] call."); break; }
                }

                ROD_Function_General.Log_Error(logOutput, caller, passedLoggers ?? []);
            }

        }


        internal class Apply_Modifier
        {
            /// <summary>
            /// Main module. Calculates the ore code's possible modifiers and returns a final multiplier total of the valid mods.<br></br><br></br>
            /// nodeType: Type of ore the deposit is generating as. Checked against all AreaType mods in the config list. [varient.code] from GenDeposits.GenDeposit<br></br>
            /// multiType: Type of multiplier requested by the call. Valid types: ["Multi_"+ "Chance", "Width", "Height", "Grade"]. See [ROD_Handle_Collection] for details.<br></br>
            /// blockPos: The BLOCK coord of the ore deposit generating. chunkPos will be derived from this, which is the blockPos / chunk size. [depoCenterPos] from GenDeposits.GenDeposit
            /// </summary>
            /// <param name="nodeType">Type of ore the deposit is generating as. Checked against all AreaType mods in the config list.</param>
            /// <param name="multiType">Type of multiplier requested by the call. Valid types are "Multi_Size" and "Multi_Chance". See [ROD_Handle_Collection] for details. </param>
            /// <param name="blockPos">The BLOCK coord of the ore deposit generating. chunkPos will be derived from this, which is the blockPos / chunk size.</param>
            /// <returns>Final multiplier of the valid AreaType multipliers for that specific ore totalled.</returns>
            internal static float ApplyModifiers(string nodeType, string multiType, BlockPos ogBlockPos)
            {
                if (API.Side is not EnumAppSide.Server) { return 1f; }
                if (ogBlockPos.X > API_Server.WorldManager.MapSizeX || ogBlockPos.Z > API_Server.WorldManager.MapSizeZ) { ReportFailure(0, [ogBlockPos]); return 1f; }
                BlockPos blockPos = ogBlockPos.Copy();
                {
                    BlockPos spawnPos = new BlockPos(API.World.BlockAccessor.MapSizeX / 2, API.World.BlockAccessor.MapSizeY / 2, API.World.BlockAccessor.MapSizeZ / 2);
                    //try { spawnPos = API.World.DefaultSpawnPosition.AsBlockPos; } catch (Exception e) { }//world spawn does not exist when first generating chunks around... world spawn
                    blockPos.X -= spawnPos.X; blockPos.Z -= spawnPos.Z;
                }
                (int x, int y, int z) ogChunkPos = (ogBlockPos.X / GlobalConstants.ChunkSize, ogBlockPos.Y / GlobalConstants.ChunkSize, ogBlockPos.Z / GlobalConstants.ChunkSize);//keeping chunkPos as a int array to enforce the difference between chunkPos and blockPos
                (int x, int y, int z) chunkPos  = (blockPos.X / GlobalConstants.ChunkSize, blockPos.Y / GlobalConstants.ChunkSize, blockPos.Z / GlobalConstants.ChunkSize);

                var list_OreMods = ROD_Handle_Collection.List_OreMod_Cache;
                float multiplier = 1f;
                bool overwritten = false;


                for (int listPos = 0; listPos < list_OreMods.Count; listPos++)
                {
                    foreach (string key_Ore in list_OreMods[listPos].Keys)
                    {
                        bool doOverwrite = false;
                        float bleedPercent = 0f;
                        float typedMulti = 1f;
                        if (list_OreMods[listPos][key_Ore].TryGetValue(multiType, out var multiMinMax) && multiMinMax is not null && multiMinMax.Length > 0) { } else { continue; }
                        { if (list_OreMods[listPos][key_Ore].TryGetValue(ROD_Handle_Collection.PropNames.name_Overwrite, out var tmp) && tmp is not null && tmp.Length > 0 && MathF.Round(tmp[0]) is 1f) { doOverwrite = true; } else if (overwritten) { continue; } }

                        if (Match_OreType(key_Ore.ToLower(), nodeType.ToLower()))
                        {
                            //check multiplier conditions
                            if (list_OreMods[listPos][key_Ore].TryGetValue(ROD_Handle_Collection.PropNames.name_AreaType, out float[]? areaType) && areaType is not null && areaType.Length > 0)
                            switch (Math.Round(areaType[0]))
                            {
                                    case (1f): { bleedPercent = Verify_AreaType.Cardinal(list_OreMods[listPos][key_Ore], chunkPos, blockPos); break; }
                                    case (2f): { bleedPercent = Verify_AreaType.Climate(list_OreMods[listPos][key_Ore], ogChunkPos, ogBlockPos); break; }
                                    case (3f): { bleedPercent = Verify_AreaType.Region(list_OreMods[listPos][key_Ore], chunkPos, blockPos); break; }
                                    case (4f): { bleedPercent = Verify_AreaType.HostRock(list_OreMods[listPos][key_Ore], chunkPos, ogBlockPos); break; }
                                    default: { continue; }
                            }

                            //bleed calc
                            //if (bleedPercent is not 1f)
                            {
                                float min;
                                float max;

                                {
                                    min = MathF.Min(multiMinMax[0], (multiMinMax.Length > 1 ? multiMinMax[1] : 0f));
                                    max = MathF.Max(multiMinMax[0], (multiMinMax.Length > 1 ? multiMinMax[1] : 0f));
                                }
                                typedMulti = min + ((max - min) * bleedPercent);
                            }

                            //apply multiplier
                            if (doOverwrite) { multiplier = typedMulti; overwritten = true; }
                            else { multiplier *= typedMulti; }
                            ROD_Function_General.Log_Debug_Verbose("Successfully calculated and applied multiplier type [{0}]x{1} to ore type [{2}]!", loggers: [multiType, multiplier, nodeType]);
                        }//do not break as ore multipliers can be stacked
                    }
                }

                return multiplier;
            }

            internal static void ReportFailure(int failureType, object[]? passedLoggers = null, [CallerMemberName] string caller = "")
            {
                string logOutput = "Error when getting multipliers; ";

                switch (failureType)
                {
                    case (0): { logOutput += "blockPos [{0}] was out of world map limits."; break; }
                    default: { ROD_Function_General.Log_Error("Invalid error type in [ReportFailure] call."); break; }
                }

                ROD_Function_General.Log_Error(logOutput, caller, passedLoggers ?? []);
            }


            internal static bool Match_OreType(string oreName, string nodeType)
            {
                //Block? nodeBlock = API.World.GetBlock(new AssetLocation(nodeType)); if (nodeBlock is null) { return false; } return ROD_Function_General.Match_Wildcard([oreName], nodeBlock);
                if (nodeType.Contains(oreName) || oreName.Contains(nodeType)) { return true; }

                return false;
            }

            /// <summary>
            /// checks the conditions of any AreaType. Returns a % that determines the % of the AreaTypes multipliers. 1f being 100% condition fufilled and dropping off based on bleed distance, down to 0f.
            /// </summary>
            internal class Verify_AreaType
            {
                internal static float Cardinal(Dictionary<string, float[]> dict_OreProps, (int x, int y, int z) chunkPos, BlockPos blockPos)//TODO: convert to clamped distance check?
                {//Not sure the best way to do corners. Either cut off at cardinal change or overflow somewhat over into them. So should NE cut off exactly at the east/west border or should the spawn line be allowed to overflow past it preventing it from creating a perfect square cutoff?
                    float dict_Direction = 0f;
                    float dict_Distance = 10000f;
                    float dict_Bleed = 0f;
                    float bleedDist = 0f;


                    {
                        if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Direction, out float[]? dir) && dir is not null && dir.Length > 0) { dict_Direction = MathF.Round(dir[0]); }
                        if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Dist, out float[]? dist) && dist is not null && dist.Length > 0) { dict_Distance = MathF.Round(dist[0]); }
                    }
                    
                    switch (dict_Direction)
                    {
                        //Correct signage by rotating around spawn so as to always be comparing as if direction is N or NE to simplify distance check.
                        case (0f or 1f or 2f): { break; }//No rotation needed
                        case (3f or 4f): { var tmp = chunkPos.x; chunkPos.x = -(chunkPos.z); chunkPos.z = tmp; break; }
                        case (5f or 6f): { chunkPos.x = -(chunkPos.x); chunkPos.z = -(chunkPos.z); break; }
                        case (7f or 8f): { var tmp = chunkPos.x; chunkPos.x = chunkPos.z; chunkPos.z = -(tmp); break; }
                        default: { return 0f; }

                        /*case (0f): { break; }
                        case (1f): { if (distance <= chunkPos.z) { return 1f; } else { break; } }
                        case (3f): { if (distance <= chunkPos.x) { return 1f; } else { break; } }
                        case (5f): { if (-(distance) >= chunkPos.z) { return 1f; } else { break; } }
                        case (7f): { if (-(distance) >= chunkPos.x) { return 1f; } else { break; } }
                        case (2f or 4f or 6f or 8f):
                            {//make sure to update bleed clac if i change it
                                if (direction is 2f or 4f) { if (MathF.Sign(chunkPos.x) is 1) { } else { break; } }
                                else if (direction is 6f or 8f) { if (MathF.Sign(chunkPos.x) is -1) { } else { break; } }

                                if (direction is 2f or 8f) { if (MathF.Sign(chunkPos.z) is 1) { } else { break; } }
                                else if (direction is 4f or 6f) { if (MathF.Sign(chunkPos.z) is -1) { } else { break; } }

                                if (distance <= MathF.Abs(chunkPos.x) + MathF.Abs(chunkPos.z)) { return 1f; }
                                break;
                            }
                        default: { return 0f; }*/
                    }

                    //calc dist
                    switch (dict_Direction)//Only check x and z depending on which cardinal direction
                    {
                        case (0f):
                            {
                                if (MathF.Abs(chunkPos.x) <= dict_Distance && MathF.Abs(chunkPos.z) <= dict_Distance) { return 1f; }
                                else { bleedDist = ROD_Function_General.Calc_Triangulate(MathF.Max(MathF.Abs(chunkPos.x) - dict_Distance, 0f), MathF.Max(MathF.Abs(chunkPos.z) - dict_Distance, 0f)); }//TODO: check this
                                break;
                            }
                        case (1f or 3f or 5f or 7f)://single direction
                            {
                                if (-(chunkPos.z) >= dict_Distance) { return 1f; }
                                else { bleedDist = dict_Distance - -(chunkPos.z); }
                                break;
                            }
                        case (2f or 4f or 6f or 8f)://combo direction
                            {
                                if (MathF.Sign(chunkPos.x) is 1 && MathF.Sign(chunkPos.z) is 1 && (chunkPos.x + -(chunkPos.z)) >= dict_Distance) { return 1f; }
                                else { bleedDist = dict_Distance - (chunkPos.x + -(chunkPos.z)); }
                                break;
                            }//TODO: add 9f for "> x distance from spawn in any direction"
                        default: { return 0f; }
                    }

                    //calc bleed dist
                    { if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Bleed, out float[]? bld) && bld is not null && bld.Length > 0) { dict_Bleed = bld[0]; } }
                    if (dict_Bleed > 0f && bleedDist < dict_Bleed)
                    {
                        return MathF.Max((dict_Bleed - bleedDist) / dict_Bleed, 0f);
                        /*if (dict_Direction is 2f or 4f or 6f or 8f)
                        {
                            if (Math.Abs(chunkPos.x) <= dict_Distance && Math.Abs(chunkPos.z) <= dict_Distance)
                            {
                                float slopePointZ = dict_Distance - MathF.Abs(chunkPos.x) - MathF.Abs(chunkPos.y);
                                float slopeDist = MathF.Sqrt(MathF.Pow(chunkPos.x, 2f) + MathF.Pow(slopePointZ, 2f));
                                //BlockPos blockPos = new BlockPos(chunkPos.x, API.World.SeaLevel, (int)slopePointZ);

                                //if ((blockPos.DistanceTo(chunkPos.x, API.World.SeaLevel, chunkPos.z) is float nodeDist) && nodeDist < distance) { return MathF.Round((distance - nodeDist) / distance, 2); }


                                if (slopeDist is < 1 and > 0) { return MathF.Round(slopeDist, 2); }//TODO: add check for too far
                            }
                            else
                            {
                                float nodeDist = MathF.Max(chunkPos.x, chunkPos.y);

                                if (dict_Distance - nodeDist > 0) { return MathF.Round((dict_Distance - nodeDist) / dict_Distance, 2); }
                            }
                        }
                        else if (dict_Distance is 1f or 3f or 5f or 7f)
                        {

                        }*/
                    }

                    return 0f;
                }

                internal static float Climate(Dictionary<string, float[]> dict_OreProps, (int x, int y, int z) chunkPos, BlockPos blockPos)//TODO: add hemisphere and lat/long?
                {
                    if (API_Server.World.BlockAccessor.GetClimateAt(blockPos, EnumGetClimateMode.WorldGenValues) is ClimateCondition climate) { } else { return 0f; }//TODO: climate is null idk why
                    //IntDataMap2D intDataMap2D = blockAccessor.GetMapRegion(depoCenterPos.X / regionSize, depoCenterPos.Z / regionSize)?.ClimateMap; ?

                    float dict_Bleed = 0f; { if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Bleed, out float[]? bld) && bld is not null && bld.Length > 0) { dict_Bleed = bld[0]; } }
                    float bleedDistPrct = 0f;
                    int bleedCount = 0;


                    if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Temp, out var tempAvg) && tempAvg is not null && tempAvg.Length > 0)
                    {
                        float min = tempAvg[0];
                        float max = tempAvg.Length > 1 ? tempAvg[1] : min;

                        if (climate.Temperature >= min && MathF.Round(climate.Temperature, 1) <= max) { bleedDistPrct++; bleedCount++; }
                        else
                        {
                            if (dict_Bleed > 0f)
                            {
                                if (climate.Temperature < min) { bleedDistPrct += (dict_Bleed - (min - climate.Temperature)) / dict_Bleed; bleedCount++; }
                                else { bleedDistPrct += (dict_Bleed - (climate.Temperature - max)); bleedCount++; }//climate.? > max
                            }
                            else { bleedCount++; }
                        }
                    }
                    if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Rainfall, out var rainfallAvg) && rainfallAvg is not null && rainfallAvg.Length > 0)
                    {
                        float min = rainfallAvg[0];
                        float max = rainfallAvg.Length > 1 ? rainfallAvg[1] : min;

                        if (climate.Rainfall >= min && MathF.Round(climate.Rainfall, 1) <= max) { bleedDistPrct++; bleedCount++; }
                        else
                        {
                            if (dict_Bleed > 0f)
                            {
                                if (climate.Rainfall < min) { bleedDistPrct += (dict_Bleed - (min - climate.Rainfall)) / dict_Bleed; bleedCount++; }
                                else { bleedDistPrct += (dict_Bleed - (climate.Rainfall - max)); bleedCount++; }//climate.? > max
                            }
                        }
                    }
                    if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Geo, out var geoAct) && geoAct is not null && geoAct.Length > 0)
                    {
                        float min = geoAct[0];
                        float max = geoAct.Length > 1 ? geoAct[1] : min;

                        if (climate.GeologicActivity >= min && MathF.Round(climate.GeologicActivity, 1) <= max) { bleedDistPrct++; bleedCount++; }
                        else
                        {
                            if (dict_Bleed > 0f)
                            {
                                if (climate.GeologicActivity < min) { bleedDistPrct += (dict_Bleed - (min - climate.GeologicActivity)) / dict_Bleed; bleedCount++; }
                                else { bleedDistPrct += (dict_Bleed - (climate.GeologicActivity - max)); bleedCount++; }//climate.? > max
                            }
                        }
                    }
                    if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Fert, out var fertility) && fertility is not null && fertility.Length > 0)
                    {
                        float min = fertility[0];
                        float max = fertility.Length > 1 ? fertility[1] : min;

                        if (climate.Fertility >= min && MathF.Round(climate.Fertility, 1) <= max) { bleedDistPrct++; bleedCount++; }
                        else
                        {
                            if (dict_Bleed > 0f)
                            {
                                if (climate.Fertility < min) { bleedDistPrct += (dict_Bleed - (min - climate.Fertility)) / dict_Bleed; bleedCount++; }
                                else { bleedDistPrct += (dict_Bleed - (climate.Fertility - max)); bleedCount++; }//climate.? > max
                            }
                        }
                    }
                    if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Stab, out var stability) && stability is not null && stability.Length > 0)
                    {
                        float min = stability[0];
                        float max = stability.Length > 1 ? stability[1] : min;

                        //get position stability
                        if (API.World.Config.TryGetBool("temporalStability") is true)
                        {
                            float posStability = API.ModLoader.GetModSystem<SystemTemporalStability>().GetTemporalStability(blockPos);//i found it

                            if (posStability >= min && posStability <= max) { bleedDistPrct++; bleedCount++; }
                            else
                            {
                                if (dict_Bleed > 0f)
                                {
                                    if (posStability < min) { bleedDistPrct += (dict_Bleed - (min - posStability)) / dict_Bleed; bleedCount++; }
                                    else { bleedDistPrct += (dict_Bleed - (posStability - max)); bleedCount++; }//climate.? > max
                                }
                            }
                        }
                    }
                    if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Forest, out var forest) && forest is not null && forest.Length > 0)
                    {
                        float min = forest[0];
                        float max = forest.Length > 1 ? forest[1] : min;

                        if (climate.ForestDensity >= min && MathF.Round(climate.ForestDensity, 1) <= max) { bleedDistPrct++; bleedCount++; }
                        else
                        {
                            if (dict_Bleed > 0f)
                            {
                                if (climate.ForestDensity < min) { bleedDistPrct += (dict_Bleed - (min - climate.ForestDensity)) / dict_Bleed; bleedCount++; }
                                else { bleedDistPrct += (dict_Bleed - (climate.ForestDensity - max)); bleedCount++; }//climate.? > max
                            }
                        }
                    }
                    if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Shrub, out var shrub) && shrub is not null && shrub.Length > 0)
                    {
                        float min = shrub[0];
                        float max = shrub.Length > 1 ? shrub[1] : min;

                        if (climate.ShrubDensity >= min && MathF.Round(climate.ShrubDensity, 1) <= max) { bleedDistPrct++; bleedCount++; }
                        else
                        {
                            if (dict_Bleed > 0f)
                            {
                                if (climate.ShrubDensity < min) { bleedDistPrct += (dict_Bleed - (min - climate.ShrubDensity)) / dict_Bleed; bleedCount++; }
                                else { bleedDistPrct += (dict_Bleed - (climate.ShrubDensity - max)); bleedCount++; }//climate.? > max
                            }
                        }
                    }
                    if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_SeaLevel, out var sea) && sea is not null && sea.Length > 0)
                    {
                        float min = sea[0];
                        float max = sea.Length > 1 ? sea[1] : min;
                        int seaLevel = ((int)(100f * (float)blockPos.Y / 255f)); //from VintageStory.ServerMods.WgenCommand

                        if (seaLevel >= min && seaLevel <= max) { bleedDistPrct++; bleedCount++; }
                        else
                        {
                            if (dict_Bleed > 0f)
                            {
                                if (seaLevel < min) { bleedDistPrct += (dict_Bleed - (min - seaLevel)) / dict_Bleed; bleedCount++; }
                                else { bleedDistPrct += (dict_Bleed - (seaLevel - max)); bleedCount++; }//climate.? > max
                            }
                        }
                    }
                    if (bleedDistPrct == bleedCount) { return 1f; }

                    //calc bleed dist; This is not based on the distance from the closest climate matching chunk as that would be way too resource intense (or a pain using the noisemaps). Ill consider it in the future.
                    bleedDistPrct /= bleedCount;
                    if (bleedDistPrct is < 1f and > 0f) { return bleedDistPrct; }

                    return 0f;
                }

                internal static float Region(Dictionary<string, float[]> dict_OreProps, (int x, int y, int z) chunkPos, BlockPos blockPos)
                {
                    //top left and bottom right
                    (float x, float y, float z) corner1;
                    (float x, float y, float z) corner2;
                    float dict_Bleed = 0f;
                    float bleedDist = 0f;

                    //set corners
                    {
                        List<float> allX = new(); List<float> allY = new(); List<float> allZ = new();

                        if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_1X, out var x1) && x1 is not null && x1.Length > 0) { allX.Add(x1[0]); }
                        else { allX.Add(0f); }
                        if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_2X, out var x2) && x2 is not null && x2.Length > 0) { allX.Add(x2[0]); }
                        else { corner2.x = -(allX[0]); }
                        if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_1Y, out var y1) && y1 is not null && y1.Length > 0) { allY.Add(y1[0]); }
                        else { allY.Add(-1f); }
                        if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_2Y, out var y2) && y2 is not null && y2.Length > 0) { allY.Add(y2[0]); }
                        else { if (allY[0] is -1f) { allY.Add(-1f); } else { allY.Add(API_Server.WorldManager.MapSizeY / API_Server.WorldManager.ChunkSize); } }
                        if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_1Z, out var z1) && z1 is not null && z1.Length > 0) { allZ.Add(z1[0]); }
                        else { allZ.Add(0f); }
                        if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_2Z, out var z2) && z2 is not null && z2.Length > 0) { allZ.Add(z2[0]); }
                        else { allZ.Add(-(allZ[0])); }

                        corner1 = (allX.Max(), allY.Max(), allZ.Max());
                        corner2 = (allX.Min(), allY.Min(), allZ.Min());
                    }

                    //calc distance
                    (float x, float y, float z) clampChunk = (Math.Clamp(chunkPos.x, corner2.x, corner1.x), Math.Clamp(chunkPos.y, corner2.y, corner1.y), (Math.Clamp(chunkPos.z, corner2.z, corner1.z)));
                    float chunkDistXZ = ROD_Function_General.Calc_Triangulate(chunkPos.x - clampChunk.x, chunkPos.z - clampChunk.z);

                    if (corner1.y is not -1 && chunkPos.y is >= 0)
                    {
                        float chunkDistXY = ROD_Function_General.Calc_Triangulate(chunkPos.x - clampChunk.x, chunkPos.y - clampChunk.y);
                        float ChunkDistFinal = ROD_Function_General.Calc_Triangulate(chunkDistXZ, chunkDistXY);
                        bleedDist = ChunkDistFinal;
                    }
                    else { bleedDist = chunkDistXZ; }
                    if (bleedDist == 0) { return 1f; }

                    //calc bleed
                    { if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_Bleed, out float[]? bld) && bld is not null && bld.Length > 0) { dict_Bleed = bld[0]; } }
                    if (dict_Bleed > 0f)
                    {
                        if (bleedDist <= 0f) { return 1f; }
                        if (bleedDist <= dict_Bleed)
                        {
                            return (dict_Bleed - bleedDist) / dict_Bleed;
                        }
                        else { return 0f; }
                    }

                    return 0f;
                }

                internal static float HostRock(Dictionary<string, float[]> dict_OreProps, (int x, int y, int z) chunkPos, BlockPos blockPos)
                {
                    if (API.World.BlockAccessor.GetBlock(blockPos) is Block rockBlock) { } else { return 0f; }
                    string[] config_HostRocks = Config_Reference.Config_Loaded!.Config_OreDensity.List_HostRocks;
                    if (dict_OreProps.TryGetValue(ROD_Handle_Collection.PropNames.name_HostBlock, out var listPos_HostBlock) && listPos_HostBlock is not null && listPos_HostBlock.Length > 0) { } else { return 0f; }

                    foreach (int listPos in listPos_HostBlock)
                    {
                        if (listPos > config_HostRocks.Length || listPos < 0) { continue; }

                        if (ROD_Function_General.Match_Wildcard([config_HostRocks[listPos]], rockBlock))
                        {
                            return 1f;
                        }
                    }
                    
                    return 0f;
                }
            }
        }
    }
}
