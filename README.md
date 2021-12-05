# Quarantine Sprite Extractor

.NET based sprite extractor for the 1994 pc game "Quarantine" from GameTek/Imagexcel.
Extract sprites from .SPR files to .ppm or .png images. 

Github: https://github.com/ErikvdBerg/QuarantineSpriteExtractor
Homepage: https://www.net-developer.nl/quarantine-sprite-extractor

Based on the work of Colin Bourassa: https://github.com/colinbourassa/quarantine-decode
and Stephen Bogner: https://www.codeproject.com/Articles/18968/PixelMap-Class-and-PNM-Image-Viewer

HOW TO USE:
1. Edit the QuarantineInstallationFolder node in the Config.xml file to point to your Quarantine installation folder containing the .SPR and .IMG files.
2. Create a folder for the extracted sprites and edit the OutputFolder node in the Config.xml file to this folder.
3. Run QuarantineSpriteExtractor.exe.