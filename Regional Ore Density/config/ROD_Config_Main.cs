using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RegionalOreDensity.handler;
using System;
using System.Collections.Generic;
using System.Text;

namespace RegionalOreDensity.config
{
    public class ROD_Config_Main
    {
        public class Config_System
        {
            [JsonProperty(Order = -2)] public string Comment_System_Title => "-------------------- Global System Toggles --------------------";
            [JsonProperty(Order = -2)] public string Comment_DefaultResetTip => "Config manager will reset values that are null or invalid. Simply delete any value you want to reset and load the mod. Some values have a clamp and will reset when out of bounds.";

            public System_Debug System_Debug_Acc = new();
            public class System_Debug
            {
                public bool DebugLogging { get; set; } = Default_DebugLogging; public string Comment_DebugLogging => $"Default: {Default_DebugLogging}; Toggle; Enables debug logging output";
                public const bool Default_DebugLogging = false;
                public bool DebugLogging_Verbose { get; set; } = Default_DebugLogging_Verbose; public string Comment_DebugLogging_Verbose => $"Default: {Default_DebugLogging_Verbose}; Toggle; Enables verbose debug logging output";
                public const bool Default_DebugLogging_Verbose = false;
                public bool AllowConfigOverwrite { get; set; } = Default_AllowConfigOverwrite; public string Comment_AllowConfigOverwrite => $"Default: {Default_AllowConfigOverwrite}; Toggle; Enables OreDensity.List_DensityWeights to be overwritten by its clamp values." +
                    $"Also unstacks lists so only 1 ore is in each list for easier legibility. Values are not clamped or overwritten if the ore fails is missing required properties.";
                public const bool Default_AllowConfigOverwrite = true;
            }
        }

        public class OreDensity
        {
            public string Comment_HostRockList = "A list of rock types to use to reference for the [HostRock] [AreaType]. When setting which host rock types to check for for the multiplier, insert the list position of the rock type in this list, starting from 0.\n" +
                "Sorry about this mess, I need to figure out a better way to do this.";
            public string[] List_HostRocks = ["Add Rock Types To Use In HostRock Area Type Index. This is position 0", "This is position 1"];
            public string Comment_OreDensityDict = "Holds all ore [AreaType] modifiers. Loaded only on world start which means changing this will NOT update until the world is restarted. Modifiers will not be applied to already existing chunks.";
            public string Comment_VerifyYourConfig = "Make sure your config is set up correctly as changes will not apply to preexisting chunks. When setting up your config, turn on the mod's debug output and be sure to pay attention to the logs. I recommend testing your config on a temporary world before you commit to a proper save.";
            public string Comment_SeeModPage = "Refer to the mod page for details on how to use the modifier list.";//ore-poor-nativecopper-granite
            public List<Dictionary<string, Dictionary<string, float[]>>> List_OreMods = new();
        }
    }


    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Config_Reference
    {
        public static Config_Reference? Config_Loaded { get; set; }
        public const string Config_FilePath = $"{ROD_VarRef.ModName}_Config.json";
        [JsonExtensionData] public Dictionary<string, JToken>? Config_UnknownValues { get; set; } = [];

        public ROD_Config_Main.Config_System Config_System { get; set; } = new();
        public ROD_Config_Main.OreDensity Config_OreDensity { get; set; } = new();
    }
}
