using Newtonsoft.Json;
using RegionalOreDensity.handler;
using RegionalOreDensity.lib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;

namespace RegionalOreDensity.config
{
    internal class ROD_Config_Manager
    {
        private static ICoreAPI API = ROD_VarRef.API;
        //private static ICoreClientAPI API_Client = BID_VarRef.API_Client;
        //private static ICoreServerAPI API_Server = BID_VarRef.API_Server;



        /// <summary>
        /// Begins the config loading proccess.
        /// </summary>
        internal static void Config_Conjure()
        {
            if (Config_Reference.Config_Loaded is not null) { API.Logger.Debug("[{0}]: (Config_Conjure) {1} config already loaded, skipping", [ROD_VarRef.ModName, API.Side]); return; }
            if (API.Side == EnumAppSide.Server)
            {
                API.Logger.Debug("[{0}]: (Config_Conjure) Caught server config load", [ROD_VarRef.ModName]);
                Config_LoadDisk(); Config_SaveDisk(); Config_SaveWorld();
            }
            else { API.Logger.Debug("[{0}]: (Config_Conjure) Caught non-server config load, loading from World config", [ROD_VarRef.ModName]); Config_LoadWorld(); }
        }


        //config loading
        internal static void Config_LoadDisk()
        {
            Config_Reference.Config_Loaded = API.LoadModConfig<Config_Reference>(Config_Reference.Config_FilePath);
            API.Logger.VerboseDebug($"{(Config_Reference.Config_Loaded is null ? $"[{ROD_VarRef.ModName}]: (Config_LoadDisk) Found no '{API.Side}' config, creating new" : $"[{ROD_VarRef.ModName}]: (Config_LoadDisk) Found '{API.Side}' config, loading")}");
            Config_ResetIfError(); Config_Validate();

            API.Logger.VerboseDebug("[{0}]: (Config_LoadDisk) Complete", [ROD_VarRef.ModName]);
        }


        internal static void Config_LoadWorld()
        {
            var Config_LoadedBase64 = API.World.Config.GetString(Config_Reference.Config_FilePath);
            if (string.IsNullOrWhiteSpace(Config_LoadedBase64)) { Config_Reference.Config_Loaded = new Config_Reference(); Config_Validate(); API.Logger.VerboseDebug("[{0}]: (Config_LoadWorld) Config loaded was null or whitespace, loading default config", [ROD_VarRef.ModName]); return; }

            var Config_LoadedSerialized = Encoding.UTF8.GetString(Convert.FromBase64String(Config_LoadedBase64));
            var Config_Repopulated = new Config_Reference();
            JsonConvert.PopulateObject(Config_LoadedSerialized, Config_Repopulated, new JsonSerializerSettings { ObjectCreationHandling = ObjectCreationHandling.Replace });
            Config_Reference.Config_Loaded = Config_Repopulated;
        }


        //config saving
        internal static void Config_SaveDisk()
        {
            API.StoreModConfig(Config_Reference.Config_Loaded, Config_Reference.Config_FilePath);
        }


        internal static void Config_SaveWorld()
        {
            API.World.Config.SetString(Config_Reference.Config_FilePath, Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Config_Reference.Config_Loaded, Formatting.None))));
        }


        //config validation
        internal static void Config_Validate()
        {
            Config_ResetIfError();
            Config_Validate_Clamp(Config_Reference.Config_Loaded!);
            //Config_Validate_List();
            ROD_Handle_DensityMap.Validate_List.Validate_Start();
        }


        internal static void Config_Validate_Clamp(object config_Instance, HashSet<object>? instance_Visited = null)
        {
            instance_Visited ??= new HashSet<object>(ReferenceEqualityComparer.Instance); //prevent recursive checks on already verified members
            if (!instance_Visited.Add(config_Instance)) { return; }
            foreach (var instance_Property in config_Instance.GetType().GetProperties()) //iterate through all members in the config
            {
                if (!instance_Property.CanRead || !instance_Property.CanWrite) { continue; } //check if property can be read/writen -> has valid values -> is class(recursively check nested) -> has valid attribute metadata
                var prop_type = instance_Property.PropertyType;
                object? instance_raw = instance_Property.GetValue(config_Instance);
                if (instance_raw is null) continue;

                if (prop_type.IsClass && prop_type != typeof(string) && !typeof(IEnumerable).IsAssignableFrom(prop_type) && prop_type.Namespace == $"{ROD_VarRef.ModName}.config") //check if current property is a class which nests config values; run recursive Config_ClampValue inside class
                {
                    Config_Validate_Clamp(instance_raw, instance_Visited);
                    continue;
                }

                var range = instance_Property.GetCustomAttribute<RangeAttribute>();
                if (range is null) continue;

                if (prop_type == typeof(int) || prop_type == typeof(long) || prop_type == typeof(float) || prop_type == typeof(double)) //check if current property has valid type for clamping and apply clamp
                {
                    double value = Convert.ToDouble(instance_raw);
                    double valMin = Convert.ToDouble(range.Minimum);
                    double valMax = Convert.ToDouble(range.Maximum);

                    value = Math.Clamp(value, valMin, valMax);
                    instance_Property.SetValue(config_Instance, Convert.ChangeType(value, instance_Property.PropertyType));
                    continue;
                }
            }
        }


        internal static void Config_Validate_List()
        {
        }


        //config modifiers
        internal static void Config_Unload()
        {
            if (Config_Reference.Config_Loaded is not null) { Config_Reference.Config_Loaded = null; }

            ROD_Handle_Collection.List_OreMod_Cache.Clear();
        }


        internal static void Config_ResetIfError(bool hardReset = false)
        {
            if (string.IsNullOrWhiteSpace(Config_Reference.Config_Loaded?.ToString()) || hardReset)
            {
                API.Logger.Error("[{0}]: (Config_LoadDefaultIfError) Config was missing, null or malformed; Loading default config", [ROD_VarRef.ModName]);
                Config_Reference.Config_Loaded = new Config_Reference();
            }
        }
    }
}
