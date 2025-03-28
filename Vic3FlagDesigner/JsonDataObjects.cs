using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Vic3FlagDesigner
{
    public class ImageSaveData
    {
        public string Path { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double ScaleX { get; set; }
        public double ScaleY { get; set; }
        public double Rotation { get; set; }
        public Color Color1 { get; set; }
        public Color Color2 { get; set; }
        public Color Color3 { get; set; }
        public bool IsEmblem { get; set; }
    }

    public class ProjectSaveData
    {
        public string BackgroundImagePath { get; set; }
        public List<ImageSaveData> Images { get; set; }
        public Color BackgroundColor1 { get; set; }
        public Color BackgroundColor2 { get; set; }
        public Color BackgroundColor3 { get; set; }
        public string CountryTag {  get; set; }
        public string CountryName { get; set; }

    }
}
