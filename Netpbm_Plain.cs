using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyNetpbm;

public static partial class Netpbm
{
    public static (NetpbmInfo info, Color[,] bitmap) LoadPlain(string filepath, bool ignoreExtension = false)
    {
        if (File.Exists(filepath) is false)
            throw (new FileNotFoundException(filepath));

        var file = new FileInfo(filepath);
        if (ignoreExtension is false && validExtensions.Contains(file.Extension))
            throw (new FileFormatException(filepath));

        using FileWordsReader reader = new FileWordsReader(file.FullName).Load();

        NetpbmInfo netpbmInfo = new();
        netpbmInfo.Format = Enum.Parse<Format>(reader.Pop());
        netpbmInfo.SizeX = int.Parse(reader.Pop());
        netpbmInfo.SizeY = int.Parse(reader.Pop());
        if (netpbmInfo.Format.IsPbm() is false)
            netpbmInfo.ColorMaxValue = int.Parse(reader.Pop());

        Func<int, int> x = i => i % netpbmInfo.SizeX;
        Func<int, int> y = i => i / netpbmInfo.SizeX;
        Func<int, int> normalize = x => (x * byte.MaxValue / netpbmInfo.ColorMaxValue);
        Func<int, Color> colorFromGrayscale = x => Color.FromArgb(x, x, x);
        int i = 0; // pixel index.
        var result = new Color[netpbmInfo.SizeX, netpbmInfo.SizeY];
        while (reader.IsEmpty is false)
        {
            if (y(i) >= netpbmInfo.SizeY)
                throw (new FileFormatException("File has more pixels than declared."));
            result[x(i), y(i)] = netpbmInfo.Format switch
            {
                Format.P1 => reader.Pop() == "1" ? Color.Black : Color.White,
                Format.P2 => colorFromGrayscale(normalize(int.Parse(reader.Pop()))),
                Format.P3 => Color.FromArgb(
                        red: normalize(int.Parse(reader.Pop())),
                        green: normalize(int.Parse(reader.Pop())),
                        blue: normalize(int.Parse(reader.Pop()))),
                _ => throw new InvalidOperationException()
            };
            i++;
        }
        if (i != netpbmInfo.SizeX * netpbmInfo.SizeY)
            throw (new FileFormatException("File has less pixels than declared"));
        return (netpbmInfo, result);
    }


    public static void SavePlain(Color[,] bitmap, string filepath, NetpbmInfo info)
    {
        using StreamWriter stream = new StreamWriter(filepath);

        stream.WriteLine(info.Format);
        stream.WriteLine(info.SizeX);
        stream.WriteLine(info.SizeY);
        if(info.Format.IsPbm() is false)
            stream.WriteLine(255);

        Func<int, int> x = i => i % info.SizeX;
        Func<int, int> y = i => i / info.SizeX;
        Func<Color, int> avg = col => (col.R + col.G + col.B) / 3;
        for (int i = 0; i < bitmap.Length; i++)
        {
            if (i % 5 == 0)
                stream.WriteLine();
            stream.Write(info.Format switch
            {
                Format.P1 => bitmap[x(i), y(i)].R > 127 ? "0 " : "1 ",
                Format.P2 => $"{(byte)avg(bitmap[x(i), y(i)])} ",
                Format.P3 => $"{bitmap[x(i), y(i)].R} {bitmap[x(i), y(i)].G} {bitmap[x(i), y(i)].B} ",
            });
        }
    }
}

public record FileWordsReader(string FileName) : IDisposable
{
    string fileContent;
    int idx;
    int Lenght;

    public FileWordsReader Load()
    {
        fileContent = File.ReadAllText(FileName);
        idx = 0;
        Lenght = fileContent.Length;
        LenghtMinusOne = Lenght - 1;
        return this;
    }

    public void Dispose() => Flush();
    public FileWordsReader Flush()
    {
        fileContent = string.Empty;
        return this;
    }

    public ReadOnlySpan<char> Pop()
    {
        int wordLenght = 0;
        for (; idx < Lenght; idx++)
        {
            if (fileContent[idx] is '#')
            {
                if (wordLenght is 0)
                    if (fileContent.AsSpan(idx).IndexOf('\n') is int i
                            && i is not -1)
                        idx += i;
                continue;
            }
            else if (fileContent[idx] is ' ' or '\t' or '\n' or '\r')
            {
                if (wordLenght is 0)
                    continue;
                break;
            }
            wordLenght++;
        }
        return fileContent.AsSpan(idx - wordLenght, wordLenght);
    }

    int LenghtMinusOne;
    public bool IsEmpty => idx >= LenghtMinusOne;
}


