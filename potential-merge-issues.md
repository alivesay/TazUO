# FontsLoader.cs ~ 630-730
 pcl = HuesLoader.Instance.ApplyHueRgba8888(pic, charColor);
 pcl = FileManager.Hues.GetColor(pic, charColor);

~1700
datacolor =        HuesHelper.RgbaToArgb((HuesLoader.Instance.GetHueColorRgba8888(cell, color) << 8) | 0xFF);
                  datacolor = HuesHelper.RgbaToArgb(
                       (FileManager.Hues.GetPolygoneColor(cell, color) << 8) | 0xFF
                   );



# GumpsLoader
~170
val = HuesLoader.Instance.ApplyHueRgba5551(gmul[i].Value, color);
value = FileManager.Hues.GetColor16(value, color);


# Other
Change ProfileManager.CurrentProfile.DisplayPartyChatOverhead to OverheadPartyMessages


# TargetManager.cs
Some vars changed to non static

# MacroButtonGump.cs
RunMacro method changed to World.Macros instead of GameScene

# Minimapgump.cs
Hueloader changes

# Art.cs
ln 161

# WorldMapGump.cs
line 1319
