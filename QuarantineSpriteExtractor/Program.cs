using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Serialization;

namespace QuarantineSpriteExtractor
{
    class Program
    {
        static Config config;
        static string[] paletteFilePaths;
        static string[] spritesFilePaths;
        static SpriteProcessor spriteProcessor;

        static void Main(string[] args)
        {   
            try
            {
                LoadConfig();   
                spriteProcessor = new SpriteProcessor();
                LoadPalettesOrSpritesPaths("img");  
                LoadPalettesOrSpritesPaths("spr"); 
                CreateOutputFolder();

                ExtractSprites();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.Read();
        }

        /// <summary>
        /// Deserialize config.xml to Config object.
        /// </summary>
        static void LoadConfig()
        { 
            Console.WriteLine("Reading config.xml");
            Console.WriteLine();

            try
            {
                var serializer = new XmlSerializer(typeof(Config));  
                var configXml = "Config.xml";
#if DEBUG
                configXml = @"..\..\Config.xml";
#endif
                using(var fs = File.OpenRead(configXml))
                {
                    config = (Config)serializer.Deserialize(fs);
                }  

            }
            catch
            {
                Console.WriteLine("Reading config failed.");
                throw;
            }  

            Console.WriteLine("Quarantine installation folder: " + config.QuarantineInstallationFolder);
            Console.WriteLine("Sprites output folder: " + config.OutputFolder);
            Console.WriteLine("Output file type: " + config.OutputFileType);
            Console.WriteLine("Default palette file: " + config.DefaultPalette);
            Console.WriteLine("Background transparent: " + config.BackgroundTransparent);

            Console.WriteLine();
        }

        /// <summary>
        /// Load all .img or .spr file paths from quarantine installation folder as paletteFilePaths or spritesFilePaths.
        /// </summary>
        static void LoadPalettesOrSpritesPaths(string type)
        {
            if(!Directory.Exists(config.QuarantineInstallationFolder))
                throw new Exception("Quarantine installation folder could not be found: " + config.QuarantineInstallationFolder);

            var files = Directory.GetFiles(
                config.QuarantineInstallationFolder, 
                "*." + type, 
                SearchOption.AllDirectories
            );

            if(type == "img")
            {
                paletteFilePaths = files;
                if(paletteFilePaths == null || paletteFilePaths.Length == 0)
                    throw new Exception("No .img files containing palettes found in quarantine installation folder: " + config.QuarantineInstallationFolder);

                Console.WriteLine(".img files in quarantine installation folder: " + paletteFilePaths.Length.ToString()); 
            }
            else
            {
                spritesFilePaths = files;
                if(spritesFilePaths == null || spritesFilePaths.Length == 0)
                    throw new Exception("No .spr files containing sprites found in quarantine installation folder: " + config.QuarantineInstallationFolder);

                Console.WriteLine(".spr files in quarantine installation folder: " + spritesFilePaths.Length.ToString()); 
            } 
        } 
     
        static void CreateOutputFolder()
        {
            try
            {
                Directory.CreateDirectory(config.OutputFolder);
            }
            catch
            {
                Console.WriteLine("Creating output folder failed.");
                throw;
            }
        }

        static void ExtractSprites()
        {
            var failCounter = 0;

            foreach(var spritesFilePath in spritesFilePaths)
            { 
                Console.Write("Processing: " + Path.GetFileName(spritesFilePath));

                // Retrieve paletteFilePath for this sprites file from config mapping or default.
                var paletteFilePath = RetrievePaletteFilePathForSpritesFilePath(spritesFilePath);
                Console.Write(" Palette: " + Path.GetFileName(paletteFilePath));

                if(string.IsNullOrEmpty(paletteFilePath) || !File.Exists(paletteFilePath))
                {
                    failCounter++;
                    Console.Write(" FAILED! palette file not found.");
                    continue;
                }

                // Load palette in sprite processor.
                try
                {
                    spriteProcessor.LoadPalette(paletteFilePath);
                }
                catch
                {
                    failCounter++;
                    Console.Write(string.Format(" FAILED! paletteFile {0} could not be loaded.", paletteFilePath));
                    continue;
                }

                // Extract sprites from sprites file.
                List<Sprite> sprites;
                try
                {
                    sprites = spriteProcessor.ExtractSprites(spritesFilePath);
                }
                catch
                {
                    failCounter++;
                    Console.Write(string.Format(" FAILED! sprites could not be extracted from {0}.", spritesFilePath));
                    continue;
                }

                // Save sprites to output folder.
                switch(config.OutputFileType)
                {
                    case "ppm":
                    {
                        for(var i = 0; i < sprites.Count; i++)
                        { 
                            spriteProcessor.SaveSpriteAsPPM(sprites[i], spritesFilePath, config.OutputFolder, i, paletteFilePath); 
                        }
                    }
                    break;
                    case "png":
                    {
                        try
                        {
                            for(var i = 0; i < sprites.Count; i++)
                            {  
                                spriteProcessor.SaveSpriteAsPng(sprites[i], spritesFilePath, config.OutputFolder, i, paletteFilePath, config.BackgroundTransparent);  
                            } 
                        }
                        catch
                        {
                            failCounter++;
                            Console.Write(string.Format(" FAILED! {0}.", spritesFilePath));
                            continue;
                        } 
                    }
                    break;
                    default:
                    {
                        failCounter++;
                        Console.Write(string.Format(" FAILED! output file type {0} not supported.", config.OutputFileType));
                        continue;
                    } 
                }

                Console.Write(" SUCCESS!");
                Console.WriteLine();
            }
        
            Console.WriteLine("{0}/{1} FAILED. {2}/{3} SUCCESS.", failCounter, spritesFilePaths.Length, spritesFilePaths.Length - failCounter, spritesFilePaths.Length);
        }

        /// <summary>
        /// Retrieve the palette filepath for a specific sprite.
        /// </summary>
        /// <param name="spritesFilePath"></param>
        /// <returns></returns>
        static string RetrievePaletteFilePathForSpritesFilePath(string spritesFilePath)
        {
            // Check if sprite has palette mapping in config.
            var spriteName = Path.GetFileName(spritesFilePath);
            var paletteMapping = config.PaletteMappings.FirstOrDefault(pm => pm.Sprites.Contains(spriteName));

            if(paletteMapping != null && !string.IsNullOrEmpty(paletteMapping.src))
            {
                return paletteFilePaths.FirstOrDefault(
                    pfp => Path.GetFileName(pfp).Equals(paletteMapping.src, StringComparison.InvariantCultureIgnoreCase)
                );
            }
            else  
            {
                // Return the default paletteFilePath from the config.
                return paletteFilePaths.FirstOrDefault(
                    pfp => Path.GetFileName(pfp).Equals(config.DefaultPalette, StringComparison.InvariantCultureIgnoreCase)
                );
            }
        }
    }
}
