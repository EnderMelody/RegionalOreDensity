using System;
using System.Collections.Generic;
using RegionalOreDensity.lib;
using RegionalOreDensity.config;

namespace RegionalOreDensity.handler
{
    /// <summary>
    /// Contains valid density map types (AreaType).
    /// Cardinal applies in a direction relative to spawn.
    /// Climate is determined by the climate(surprise).
    /// Region allows specifying a square area to apply.
    /// Hostrock applies based on the rock hosting the vein.
    /// </summary>
    internal enum Enum_Ore_DensityType { cardinal, climate, region, hostrock }
    internal enum Enum_DensityDirection { c, n, ne, e, se, s, sw, w, nw }

    internal class ROD_Handle_Collection
    {
        internal static List<Dictionary<string, Dictionary<string, float[]>>> List_OreMod_Cache = new();

        internal class PropNames
        {
            internal static string name_AreaType = "AreaType";

            internal static string name_Required = "Req";
            internal static string name_Val1 = "Val1";
            internal static string name_Val2 = "Val2";

            internal static string name_MultiChance = "Multi_Chance";
            internal static string name_MultiWidth = "Multi_Width";
            internal static string name_MultiHeight = "Multi_Height";
            internal static string name_MultiGrade = "Multi_Grade";
            internal static string name_Bleed = "Dist_Bleed";
            internal static string name_Overwrite = "Overwrite";

            internal static string name_Direction = "Direction";
            internal static string name_Dist = "Distance";

            internal static string name_Temp = "TempAvg";
            internal static string name_Rainfall = "RainfallAvg";
            internal static string name_Geo = "GeoAct";
            internal static string name_Fert = "Fertility";
            internal static string name_Stab = "Stability";
            internal static string name_Forest = "Forest";
            internal static string name_Shrub = "Shrub";
            internal static string name_SeaLevel = "SeaLevelDist";

            internal static string name_1X = "Corner1_X";
            internal static string name_1Z = "Corner1_Z";
            internal static string name_1Y = "Corner1_Y";
            internal static string name_2X = "Corner2_X";
            internal static string name_2Z = "Corner2_Z";
            internal static string name_2Y = "Corner2_Y";

            internal static string name_HostBlock = "HostBlock";
        }


        //for validating properties
        /// <summary>
        /// Contains all valid AreaTypes, their valid properties, and which are required.
        /// General fields requirements are applied independently from type specific.
        /// Req: 0 = not required, 1 = required, 2 = at least 1 required.
        /// Min/Max: Clamps the vlaue of the prop in the range. Empty = no clamp. Min is value inside the area, Max is value outside the area.
        /// </summary>
        internal static class AreaType_Props
        {
            /// <summary>
            /// AreaType.ALL valid props.
            /// Multi_Size: Determines the size multiplier within/outside the area.
            /// Multi_Chance: Determines the gen chance multiplier within/outside the area.
            /// Dist_Bleed: Distance over which ore gen multipliers fall off linearly from min to max value outside the area. Default 0.
            /// </summary>
            internal static Dictionary<string, Dictionary<string, float[]>> Props_General = new()
            {
                [PropNames.name_MultiChance] = new() { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f], [PropNames.name_Val2] = [0f], },
                [PropNames.name_MultiWidth] = new()  { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f], [PropNames.name_Val2] = [0f], },
                [PropNames.name_MultiHeight] = new() { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f], [PropNames.name_Val2] = [0f], },
                [PropNames.name_MultiGrade] = new()  { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f], [PropNames.name_Val2] = [0f], },
                [PropNames.name_Bleed] = new()       { [PropNames.name_Val1] = [0f], [PropNames.name_Val2] = [0f], },
                [PropNames.name_Overwrite] = new()   { [PropNames.name_Val1] = [0f], [PropNames.name_Val2] = [0f, 1f], },

            };

            /// <summary>
            /// AreaType.Cardinal's valid props.
            /// Direction: Determines the direction from spawn that the DensityMap will change the oregen chance. Valid inputs: C(enter), N, NE, E, SE, S, SW, W, NW. Defaults to 0/C.
            /// Distance: Determines the distance from spawn that the multiplier will start. If Direction is C, this is inverted. Defaults to 10000.
            /// </summary>
            internal static Dictionary<string, Dictionary<string, float[]>> Props_Cardinal = new()
            {
                [PropNames.name_Direction] = new() { [PropNames.name_Required] = [0f], [PropNames.name_Val1] = [0f, 8f], },
                [PropNames.name_Dist] = new()      { [PropNames.name_Required] = [0f], [PropNames.name_Val1] = [0f], },
            };

            /// <summary>
            /// AreaType.Climate's valid props.
            /// All: The value of the chunk's wgen climate data.
            /// </summary>
            internal static Dictionary<string, Dictionary<string, float[]>> Props_Climate = new()
            {
                [PropNames.name_Temp] = new()     { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [], [PropNames.name_Val2] = [], },
                [PropNames.name_Rainfall] = new() { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f, 100f], [PropNames.name_Val2] = [0f, 100f], },
                [PropNames.name_Geo] = new()      { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f, 100f], [PropNames.name_Val2] = [0f, 100f], },
                [PropNames.name_Fert] = new()     { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f, 100f], [PropNames.name_Val2] = [0f, 100f], },
                [PropNames.name_Stab] = new()     { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f, 100f], [PropNames.name_Val2] = [0f, 100f], },
                [PropNames.name_Forest] = new()   { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f, 100f], [PropNames.name_Val2] = [0f, 100f], },
                [PropNames.name_Shrub] = new()    { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f, 100f], [PropNames.name_Val2] = [0f, 100f], },
                [PropNames.name_SeaLevel] = new() { [PropNames.name_Required] = [2f], [PropNames.name_Val1] = [0f, 100f], [PropNames.name_Val2] = [0f, 100f], },
            };

            /// <summary>
            /// AreaType.Region's valid props.
            /// Corner1/2_X/Z: the coords of the oposite corners of a square region.
            /// Corner 2 defaults to the oposite side of the map (flipped around spawn).
            /// Y value: not required but allows height. Defaults to full world height.
            /// </summary>
            internal static Dictionary<string, Dictionary<string, float[]>> Props_Region = new()
            {
                [PropNames.name_1X] = new() { [PropNames.name_Required] = [1f], },//TODO: combine these to min/max
                [PropNames.name_1Z] = new() { [PropNames.name_Required] = [1f], },
                [PropNames.name_1Y] = new() { [PropNames.name_Required] = [0f], },
                //assume inverted values from corner1
                [PropNames.name_2X] = new() { [PropNames.name_Required] = [0f], },
                [PropNames.name_2Z] = new() { [PropNames.name_Required] = [0f], },
                [PropNames.name_2Y] = new() { [PropNames.name_Required] = [0f], },
            };

            /// <summary>
            /// AreaType.HostRock's valid props.
            /// RockType: A LIST of different rocks that the DensityMap will count.
            /// </summary>
            internal static Dictionary<string, Dictionary<string, float[]>> Props_HostBlock = new()
            {
                [PropNames.name_HostBlock] = new() { [PropNames.name_Required] = [1f], },//TODO: find a better way to do this then referencing an seperate list
            };
        }
    }
}
