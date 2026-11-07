using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using RegionalOreDensity.lib;
using propNames = RegionalOreDensity.handler.ROD_Handle_Collection.PropNames;

namespace RegionalOreDensity.handler
{
    internal class ROD_Handle_Input
    {
        private static ICoreAPI API = ROD_VarRef.API;
        private static ICoreClientAPI API_Client = ROD_VarRef.API_Client;
        private static ICoreServerAPI API_Server = ROD_VarRef.API_Server;

        internal static void Create_Commands()
        {
            switch (API.Side)
            {
                case (EnumAppSide.Server):
                {
                    API_Server.ChatCommands.GetOrCreate($"{ROD_VarRef.ModName_Trunc}")
                    .RequiresPrivilege(Privilege.chat)

                    .BeginSubCommand("GetModifiers")
                    .WithAlias("GetMods")
                    .WithDescription("Gets the ore map modifiers for the current chunk. Outputs results as debug output.")
                    .RequiresPlayer()
                    .RequiresPrivilege(Privilege.tp)
                    .HandleWith(Handle_Commands.GetModifiers)
                    .EndSubCommand()

                    .BeginSubCommand("SimGeneration")
                    .WithAlias("SimGen")
                    .WithDescription("Simulates a ore deposite generation multiplier check at the given coords. If none given, current location. Outputs results as debug output.")
                    .RequiresPrivilege(Privilege.tp)//chat command is admin privilage, prospecting can still reveal multipliers without op
                    .HandleWith(Handle_Commands.SimGen)
                    .WithArgs(API_Server.ChatCommands.Parsers.Word("ore name"),
                        API_Server.ChatCommands.Parsers.OptionalInt("X; Position in blocks relative to spawn. Requires both X and Z to be given if used. Y is always optional.", (int)(ROD_VarRef.CancelAndReturn * 10000)),
                        API_Server.ChatCommands.Parsers.OptionalInt("Z; Same.", (int)(ROD_VarRef.CancelAndReturn * 10000)),
                        API_Server.ChatCommands.Parsers.OptionalInt("Y; Same.", (int)(ROD_VarRef.CancelAndReturn * 10000)))
                    .EndSubCommand();

                    break;
                }
                case (EnumAppSide.Client):
                {
                    break;
                }
            }
        }

        internal class Handle_Commands()
        {
            internal static TextCommandResult GetModifiers(TextCommandCallingArgs args)
            {
                string modsOutput = "";

                if (args.Caller?.Entity is not Entity caller) { return TextCommandResult.Error("Entity was missing or null."); }
                if (caller.Pos.AsBlockPos is not BlockPos checkedPos) { return TextCommandResult.Error("Position recived was missing or null."); }
                BlockPos spawnPos = API.World.DefaultSpawnPosition.AsBlockPos;
                (int x, int y, int z) chunkPos = ((checkedPos.X - spawnPos.X) / GlobalConstants.ChunkSize, (checkedPos.Y - spawnPos.Y), (checkedPos.Z - spawnPos.Z) / GlobalConstants.ChunkSize);
                //ClimateCondition climate = API.World.BlockAccessor.GetClimateAt(checkedPos, EnumGetClimateMode.WorldGenValues);
                if (API.World.BlockAccessor.GetClimateAt(checkedPos, EnumGetClimateMode.WorldGenValues) is ClimateCondition climate) { } else { return TextCommandResult.Error("Failed to find climate data."); }

                
                modsOutput += "worldBPos: " + checkedPos.X + ", " + checkedPos.Y + ", " + checkedPos.Z;
                modsOutput += "; relativeBPos: " + (checkedPos.X - spawnPos.X) + ", " + (checkedPos.Y - spawnPos.Y) + ", " + (checkedPos.Z - spawnPos.Z);
                modsOutput += "; chunkPos: " + chunkPos.x + ", " + chunkPos.y + ", " + chunkPos.z;
                modsOutput += ";;; " + propNames.name_Temp + ": " + climate.Temperature + "o";
                modsOutput += "; " + propNames.name_Rainfall + ": " + MathF.Round(climate.Rainfall * 100) + "%";
                modsOutput += "; " + propNames.name_Geo + ": " + MathF.Round(climate.GeologicActivity * 100) + "%";
                modsOutput += "; " + propNames.name_Fert + ": " + MathF.Round(climate.Fertility * 100) + "%";
                modsOutput += "; " + propNames.name_Stab + ": " + API.ModLoader.GetModSystem<SystemTemporalStability>().GetTemporalStability(checkedPos) + ", ";
                modsOutput += "; " + propNames.name_Forest + ": " + MathF.Round(climate.ForestDensity * 100) + "%";
                modsOutput += "; " + propNames.name_Shrub + ": " + MathF.Round(climate.ShrubDensity * 100) + "%";
                modsOutput += "; " + propNames.name_SeaLevel + ": " + ((int)(100f * (float)checkedPos.Y / 255f)) + "%";
                //TODO: add other checks


                //output as debug log so as to prevent clients from "accidentally" cheating for world info.
                ROD_Function_General.Log_Debug(modsOutput);
                return TextCommandResult.Success("Valid mod variables printed to debug log.");
            }

            internal static TextCommandResult SimGen(TextCommandCallingArgs args)
            {
                int currentPosX = (int)args[1];
                int currentPosZ = (int)args[2];
                int currentPosY = (int)args[3];

                if (currentPosX == (int)(ROD_VarRef.CancelAndReturn *10000) && currentPosZ == (int)(ROD_VarRef.CancelAndReturn * 10000))
                {
                    if (args.Caller is null) { return TextCommandResult.Error("Caller was missing or null."); }
                    currentPosX = (int)args.Caller.Pos.X; currentPosZ = (int)args.Caller.Pos.Z;
                }
                else
                {
                    BlockPos spawnPos = API.World.DefaultSpawnPosition.AsBlockPos;
                    currentPosX += spawnPos.X; currentPosZ += spawnPos.Z;
                }
                if (currentPosY == (int)(ROD_VarRef.CancelAndReturn * 10000)) { currentPosY = (int)args.Caller.Pos.Y -1; }
                BlockPos blockPos = new BlockPos(currentPosX, currentPosY, currentPosZ, 0);

                Block? checkOre = API.World.GetBlock(new AssetLocation(args[0].ToString()));
                if (checkOre is not null) { } else { return TextCommandResult.Error("Block recived was not a valid block"); }

                ROD_Function_General.Log_Debug("Running ore gen simulation for ore: {0}", loggers: [args[0]]);
                float output_Chance = ROD_Handle_DensityMap.Apply_Modifier.ApplyModifiers(checkOre.Code, "Multi_Chance", blockPos);
                float output_Width = ROD_Handle_DensityMap.Apply_Modifier.ApplyModifiers(checkOre.Code, "Multi_Width", blockPos);
                float output_Height = ROD_Handle_DensityMap.Apply_Modifier.ApplyModifiers(checkOre.Code, "Multi_Height", blockPos);
                float output_Grade = ROD_Handle_DensityMap.Apply_Modifier.ApplyModifiers(checkOre.Code, "Multi_Grade", blockPos);

                //output as debug log so as to prevent clients from "accidentally" cheating for world info.
                ROD_Function_General.Log_Debug("Ore [{0}] multipliers for this chunk returned:(Multi_)+ [Chance]{1}; [Width]{2}, [Height]{3}, [Grade]{4}", loggers: [checkOre.Code, output_Chance, output_Width, output_Height, output_Grade]);
                return TextCommandResult.Success("Test successful; Printed to debug logs.");
            }
        }
    }
}
