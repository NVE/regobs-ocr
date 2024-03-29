﻿namespace SnowProfileScanner.Models
{
    public class SnowProfile
    {
        public List<Layer> Layers { get; set; }
        public double? AirTemp { get; set; }
        public List<SnowTemperature> SnowTemp { get; set; }

        public SnowProfile()
        {
            SnowTemp = new List<SnowTemperature>();
            Layers = new List<Layer>();
        }

        public class SnowTemperature
        {
            public double? Depth { get; set; }
            public double? Temp { get; set; }
        }

        public class Layer
        {
            public double? Thickness { get; set; }
            public string? Hardness { get; set; }
            public string? Grain { get; set; }
            public string? GrainSecondary { get; set; }
            public double? Size { get; set; }
            public double? SizeMax { get; set; }
            public string? LWC { get; set; }
        }
    }
}
