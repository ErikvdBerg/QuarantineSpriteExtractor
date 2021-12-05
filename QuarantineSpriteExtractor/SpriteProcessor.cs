using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Drawing.Imaging;

namespace QuarantineSpriteExtractor
{
    public class SpriteProcessor
    { 
        const int PALETTE_DATA_OFFSET = 13;
        const int PALETTE_SIZE_COLORS = 256;  

        public Dictionary<string, Color[]> Palettes{ get; private set; }

        public SpriteProcessor()
        {
            Palettes = new Dictionary<string, Color[]>();
        }

        /// <summary>
        /// Load and parse a palette file if it not yet exists.
        /// </summary>
        /// <param name="paletteFilePath"></param> 
        public void LoadPalette(string paletteFilePath)
        {
            if(!Palettes.ContainsKey(paletteFilePath))
            {
                var palette = new Color[PALETTE_SIZE_COLORS];

                using(var paletteFileStream = File.OpenRead(paletteFilePath))
                {  
                    // The palette starts at a particular byte position.
                    paletteFileStream.Position = PALETTE_DATA_OFFSET;
                    
                    // Assume the palette is 256 * 3 bytes long.
                    for(var i = 0; i < PALETTE_SIZE_COLORS; i++)
                    { 
                        // Read the bytes into a RGB color.
                        palette[i] = new Color(){
                            Red = paletteFileStream.ReadByte(),
                            Green = paletteFileStream.ReadByte(),
                            Blue = paletteFileStream.ReadByte() 
                        };  
                    }

                    Palettes.Add(paletteFilePath, palette);
                }
            }
        }    
        
        /// <summary>
        /// Extract sprites from .spr file.
        /// </summary>
        /// <param name="spritesFilePath"></param>
        /// <returns></returns>
        public List<Sprite> ExtractSprites(string spritesFilePath)
        {   
            var sprites = new List<Sprite>();

            using(var spriteFileStream = File.OpenRead(spritesFilePath))
            {  
                // The first byte in the .spr file is the number of sprites in the file.
                var numberOfSprites = spriteFileStream.ReadByte(); 

                // The next bytes are the widths and heights of the individual sprites in the file.
                var widthsAndHeights = new List<WidthAndHeight>(); 
                for(var i = 0; i < numberOfSprites; i++)
                {
                    widthsAndHeights.Add(new WidthAndHeight(){ 
                        Width = spriteFileStream.ReadByte(), 
                        Height = spriteFileStream.ReadByte()
                    });
                }

                // The rest of the bytes are the sprites.
                for(var i = 0; i < numberOfSprites; i++)
                {
                    var sprite = new Sprite(){
                        Width = widthsAndHeights[i].Width,
                        Height = widthsAndHeights[i].Height
                    };  
                     
                    // Calculate the number of pixels by multiplying width and height.
                    var numberOfPixels = sprite.Width * sprite.Height; 
                    
                    // Each byte represents a pixel in the sprite, read the pixels(bytes) for this sprite. 
                    sprite.Pixels = new byte[numberOfPixels];
                    spriteFileStream.Read(sprite.Pixels, 0, numberOfPixels);
                    
                    sprites.Add(sprite);
                } 
            }

            return sprites;
        }

        /// <summary>
        /// Save a single sprite to a .ppm image (filestream or memorystream).
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="originalSpritesFilePath"></param>
        /// <param name="newPath"></param>
        /// <param name="index"></param>
        /// <param name="paletteFilePath"></param>
        public void SaveSpriteAsPPM(Sprite sprite, string originalSpritesFilePath, string newPath, int index, string paletteFilePath)
        {
            var fileName = CreateFileName(originalSpritesFilePath, index, "ppm");

            using(var streamWriter = new StreamWriter(Path.Combine(newPath, fileName))) 
            {
                // Convert the sprite to a .ppm image and write to disk.
                WritePPMToStream(streamWriter, sprite, paletteFilePath);
            }
        }

        /// <summary>
        /// Save a single sprite to a .png image file on disk.
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="originalSpritesFilePath"></param>
        /// <param name="newPath"></param>
        /// <param name="index"></param>
        /// <param name="paletteFilePath"></param>
        /// <param name="transparent"></param>
        public void SaveSpriteAsPng(Sprite sprite, string originalSpritesFilePath, string newPath, int index, string paletteFilePath, bool transparent)
        {
            var fileName = CreateFileName(originalSpritesFilePath, index, "png");

            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream, Encoding.ASCII);
            
            // Convert the sprite to a .ppm image in memory first.
            WritePPMToStream(streamWriter, sprite, paletteFilePath);

            stream.Position = 0;

            // Convert the .ppm image to a bitmap.
            var pixelMap = new PixelMap.PixelMap(stream);

            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 1L);

            if(transparent)
                pixelMap.BitMap.MakeTransparent(System.Drawing.Color.Black);

            // Save the bitmap as a .png image to disk.
            pixelMap.BitMap.Save(Path.Combine(newPath, fileName), GetEncoder(ImageFormat.Png), encoderParameters);
          
        }

        string CreateFileName(string spritesFilePath, int index, string extension)
        {
            var originalName = Path.GetFileNameWithoutExtension(spritesFilePath);

            return string.Format("{0}_{1}.{2}", originalName, index, extension);
        }

        StreamWriter WritePPMToStream(StreamWriter streamWriter, Sprite sprite, string paletteFilePath)
        { 
            // Write the header to the ppm file.
            streamWriter.WriteLine("P3"); // This means 256 RGB colors in ASCII.
            // Write the width and height to the file.
            streamWriter.WriteLine(string.Format("{0} {1}", sprite.Width, sprite.Height));
            streamWriter.WriteLine("255"); // Maximum amount of colors.

            // Write the rgb pixels for the sprite.
            for(var i = 0; i < sprite.Pixels.Length; i++)
            {
                // Retrieve the color for this sprite's pixel from the palette.
                var rgbColor = Palettes[paletteFilePath][sprite.Pixels[i]];

                // Write the RGB pixel to a new line.
                streamWriter.WriteLine(
                    string.Format("{0} {1} {2}", rgbColor.Red, rgbColor.Green, rgbColor.Blue)
                );
            } 

            return streamWriter;
        }
         
        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
