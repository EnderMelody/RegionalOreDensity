using HarmonyLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using RegionalOreDensity.lib;
using RegionalOreDensity.config;
using RegionalOreDensity.handler;

namespace RegionalOreDensity
{
    public class RegionalOreDensityModSystem : ModSystem
    {
        private bool isConfigLoadSuccessful;
        Harmony harmony = new(ROD_VarRef.HarmonyID);


        public override void StartPre(ICoreAPI api)
        {
            ROD_VarRef.API ??= api; //KEEP THIS FIRST
            switch (api.Side)
            {
                case (EnumAppSide.Server): { ROD_VarRef.API_Server = (ICoreServerAPI)api; break; }
                case (EnumAppSide.Client): { ROD_VarRef.API_Client = (ICoreClientAPI)api; break; }
                default: { break; }
            }

            try { ROD_Config_Manager.Config_Conjure(); }
            catch (Exception ex) { api.Logger.Error("[{0}]: (Config_Conjure) Exception caught: {1}", [ROD_VarRef.ModName_Trunc, ex.Message]); Config_Reference.Config_Loaded = new Config_Reference(); ROD_Config_Manager.Config_ResetIfError(); }

            isConfigLoadSuccessful = !string.IsNullOrWhiteSpace(Config_Reference.Config_Loaded?.ToString());
        }
        
        public override void Start(ICoreAPI api)
        {
            if (!isConfigLoadSuccessful) { return; } //if config fails to load do not run any mod code

            ROD_Handle_Input.Create_Commands();
            try { harmony.PatchAll(); } catch (Exception e) { ROD_Function_General.Log_Error("Harmony patches failed to apply. Exception: {0}", loggers: [e.ToString()]); }
        }

        public override void Dispose()
        {
            harmony.UnpatchAll();
            ROD_Config_Manager.Config_Unload();
        }
    }
}
