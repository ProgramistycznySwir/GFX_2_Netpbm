using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyNetpbm;


public static partial class Netpbm
{
    static readonly HashSet<string> validExtensions = new() { "pbm", "pgm", "ppm" };


    public static (NetpbmInfo info, Color[,] bitmap) Load(string filepath, bool ignoreExtension = false)
    {
        if (File.Exists(filepath) is false)
            throw (new FileNotFoundException(filepath));

        var file = new FileInfo(filepath);
        if (ignoreExtension is false && validExtensions.Contains(file.Extension))
            throw (new FileFormatException(filepath));

        Format format = Enum.Parse<Format>(File.ReadLines(filepath).First().Take(2).ToArray());
        return format.IsBin() ? LoadBin(filepath, true) : LoadPlain(filepath, true);
    }

    public enum Format { P1, P2, P3, P4, P5, P6 }

    public record struct NetpbmInfo(Format Format, int ColorMaxValue, int SizeX, int SizeY);
}

public static class NetpbmFormat_Ext
{
    public static bool IsPlain(this Netpbm.Format self)
        => self is Netpbm.Format.P1 or Netpbm.Format.P2 or Netpbm.Format.P3;
    public static bool IsBin(this Netpbm.Format self)
        => !(self.IsPlain());

    public static bool IsPbm(this Netpbm.Format self)
        => self is Netpbm.Format.P1 or Netpbm.Format.P2;

    public static int BytesPerPixel(this Netpbm.Format self, int maxColor = 255)
        => self switch
        {
            Netpbm.Format.P5 => maxColor <= 255 ? 1 : 2,
            Netpbm.Format.P6 => 3,
            _ => 1
        };
}