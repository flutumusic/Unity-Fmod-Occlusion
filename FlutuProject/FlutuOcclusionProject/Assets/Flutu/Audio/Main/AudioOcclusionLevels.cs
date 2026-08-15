using System.Collections.Generic;

namespace Flutu.Audio
{
    public static class AudioOcclusionLevels
    {
        public static readonly float low = 0.40f;
        public static readonly float medium = 0.60f;
        public static readonly float high = 0.80f;
        public static readonly float occludeAll = 1.00f;

        public static readonly Dictionary<string, float> values = new()
        {
            { "Low", low },
            { "Medium", medium },
            { "High", high },
            { "Occlude All", occludeAll }
        };
    }
}
