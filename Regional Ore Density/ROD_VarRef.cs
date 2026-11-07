using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace RegionalOreDensity
{
    internal class ROD_VarRef
    {
        internal const string ModName = "RegionalOreDensity";
        internal const string ModName_Trunc = "ROD";
        internal const string HarmonyID = "com.endermelody.rod";

        //globally used API callers
        internal static ICoreAPI API;
        internal static ICoreClientAPI API_Client;
        internal static ICoreServerAPI API_Server;

        internal static float CancelAndReturn = 1024.4201f;
    }
}
