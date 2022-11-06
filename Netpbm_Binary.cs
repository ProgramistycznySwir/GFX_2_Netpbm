using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyNetpbm;

public static partial class Netpbm
{
    public static (NetpbmInfo info, Color[,] bitmap) LoadBin(string filepath, bool ignoreExtension = false)
    {
        if (File.Exists(filepath) is false)
            throw (new FileNotFoundException(filepath));

        var file = new FileInfo(filepath);
        if (ignoreExtension is false && validExtensions.Contains(file.Extension))
            throw (new FileFormatException(filepath));

        using FileStream stream = file.OpenRead();
        NetpbmInfo info = ReadHeader(stream);
        // Read pixels:
        Func<int, int> x = i => i % info.SizeX;
        Func<int, int> y = i => i / info.SizeX;
        Func<int, int> normalize = x => (x * byte.MaxValue / info.ColorMaxValue);
        Func<int, Color> colorFromGrayscale = x => Color.FromArgb(x, x, x);
        var result = new Color[info.SizeX, info.SizeY];
        int pixelSize = info.Format.BytesPerPixel();
        Span<byte> buffer = stackalloc byte[1024 * pixelSize];
        //int pixelCount_ = info.SizeX * info.SizeY;
        int i = 0; // pixel index
        int byteCount = stream.Read(buffer);
        do
        {
            switch (info.Format)
            {
                case Format.P4:
                    BitArray arr = new BitArray(buffer.ToArray());
                    for (int ii = 0; ii < arr.Length; i++, ii++)
                        result[x(i), y(i)] = arr[ii] ? Color.Black : Color.White;
                    byteCount = 0;
                    break;
                case Format.P5:
                case Format.P6:
                    for (int ii = 0; ii < byteCount; i++, ii += pixelSize)
                        result[x(i), y(i)] = pixelSize switch
                        {
                            1 => colorFromGrayscale(normalize(buffer[ii])),
                            2 => colorFromGrayscale(normalize((buffer[ii] << 8) & buffer[ii + 1])),
                            3 => Color.FromArgb(
                                    red: normalize(buffer[ii]),
                                    green: normalize(buffer[ii + 1]),
                                    blue: normalize(buffer[ii + 2])
                                ),
                            _ => throw new FileFormatException("File has some impossible pixel format.")
                        };
                    byteCount = 0;
                    break;
            }
            if (byteCount is 0)
                byteCount = stream.Read(buffer);
        } while (byteCount > 0);

        return (info, result);
    }

    /// <summary>
    /// Reads header from the stream and returns control with FileStream pointer pointing at start of pixel data
    /// </summary>
    private static NetpbmInfo ReadHeader(FileStream stream)
    {
        NetpbmInfo result = new();
        List<char> currWord = new();
        bool comment = false;
        bool wordEnd = false;
        int stage = 0;
        while (true)
        {
            char letter = (char)stream.ReadByte();
            if (comment)
            {
                comment = letter is not '\n';
                continue;
            }
            if (letter is '#')
                comment = wordEnd = true;
            else if (letter is ' ' or '\t' or '\n')
                wordEnd = true;
            else
                currWord.Add(letter);

            if (wordEnd)
            {
                wordEnd = false;
                if (currWord.Any())
                {
                    switch (stage)
                    {
                        case 0: result.Format = Enum.Parse<Format>(new string(currWord.ToArray())); break;
                        case 1: result.SizeX = int.Parse(new string(currWord.ToArray())); break;
                        case 2: result.SizeY = int.Parse(new string(currWord.ToArray())); break;
                        case 3: result.ColorMaxValue = byte.Parse(new string(currWord.ToArray())); break;
                    }
                    stage++;
                    if (stage is 3 && result.Format.IsPbm()
                            || stage is 4)
                    {
                        // Clear comments - this part is ugly :/
                        if(letter is not '\n')
                            while(true)
                            {
                                letter = (char)stream.ReadByte();
                                if (letter is '\n')
                                    break;
                            }
                        return result;
                    }
                    currWord.Clear();
                }
            }
        }
    }

    public static void SaveBin(Color[,] bitmap, string filepath, NetpbmInfo info)
    {
        using FileStream stream = File.OpenWrite(filepath);

        string header = $"{info.Format}\n{info.SizeX} {info.SizeY}\n{(info.Format.IsPbm() ? "" : "255")}\n";
        stream.Write(Encoding.ASCII.GetBytes(header));

        Func<int, int> x = i => i % info.SizeX;
        Func<int, int> y = i => i / info.SizeX;
        Func<Color, int> avg = col => (col.R + col.G + col.B) / 3;
        int bufferSize = 1024 * info.Format.BytesPerPixel();
        Span<byte> buffer = stackalloc byte[bufferSize];
        int bufferIdx = 0;
        for (int i = 0; i < bitmap.Length; i++)
        {
            if(bufferIdx >= bufferSize)
            {
                // Flush buffer
                stream.Write(buffer);
                bufferIdx = 0;
            }

            if(info.Format is Format.P4)
            {
                byte arr = 0;
                for (int ii = 7; ii >= 0 && i < bitmap.Length; ii--, i++)
                    arr |= (byte)((bitmap[x(i), y(i)].R > 127 ? 0 : 1) << ii);
                buffer[bufferIdx++] = arr;
            }
            else if(info.Format is Format.P5)
            {
                buffer[bufferIdx++] = (byte)avg(bitmap[x(i), y(i)]);
            }
            else if(info.Format is Format.P6)
            {
                buffer[bufferIdx++] = bitmap[x(i), y(i)].R;
                buffer[bufferIdx++] = bitmap[x(i), y(i)].G;
                buffer[bufferIdx++] = bitmap[x(i), y(i)].B;
            }
        }
        // Flush buffer.
        stream.Write(buffer.Slice(0, bufferIdx));
    }
}
