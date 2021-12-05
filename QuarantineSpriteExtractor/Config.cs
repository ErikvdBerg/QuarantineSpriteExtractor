using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace QuarantineSpriteExtractor
{
    public class Config
    {  
        public string QuarantineInstallationFolder{ get; set; }
        public string OutputFolder{ get; set; }
        public string OutputFileType{ get; set; }
        public string DefaultPalette{ get; set; }
        public bool BackgroundTransparent{ get; set; }

        public List<Palette> PaletteMappings{ get; set; }
    }

    public class Palette
    {
        public string src{ get; set; }
        public List<string> Sprites{ get; set; }
    } 
}
