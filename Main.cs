using System.Diagnostics;
using MyNetpbm;

namespace GFX_2_PpmFiles;

public partial class Main : Form
{
    Size canvasSize;
    float canvasAspect;
    Color[,] currColorArray;
    Netpbm.NetpbmInfo fileInfo;

    public Main()
    {
        InitializeComponent();

        canvasSize = canvas.Size;
        canvasAspect = (float)canvasSize.Width / canvasSize.Height;
    }

    //private void InitCanvas()
    //{
    //    //Img = new Bitmap(canvas.Width, canvas.Height);
    //    //ImgTmp = (Image)Img.Clone();

    //    //Graphics gfx = Graphics.FromImage(Img);
    //    //SolidBrush brush = new(Color.White);
    //    //gfx.FillRectangle(brush, 0, 0, width: Img.Width, height: Img.Height);
    //    //canvas.Image = Img;
    //}

    private void btnLoad_Click(object sender, EventArgs e)
    {
        using OpenFileDialog dialog = new() {
                CheckFileExists = true,
                Filter = "Any Netpbm Image|*.p*m|Portable BitMap|*.pbm|Portable GrayMap|*.pgm|Portable PixMap|*.ppm",
                Title = "Load an Netpbm Image",
            };
        if (dialog.ShowDialog() is not DialogResult.OK)
            return;
        Stopwatch stopwatch = Stopwatch.StartNew();
        (Netpbm.NetpbmInfo info, Color[,] bitmap) = Netpbm.Load(dialog.FileName, ignoreExtension: true);
        currColorArray = bitmap;
        fileInfo = info;
        Bitmap imageBitmap = new Bitmap(info.SizeX, info.SizeY);
        for (int x = 0; x < info.SizeX; x++)
            for (int y = 0; y < info.SizeY; y++)
                imageBitmap.SetPixel(x, y, bitmap[x, y]);
        ToDisplay(imageBitmap);
        stopwatch.Stop();
        fileInfoLabel.Text = $"Filename: {dialog.SafeFileName}, Format: {info.Format}, Resolution: {info.SizeX}x{info.SizeY}, Loading time: {stopwatch.ElapsedMilliseconds}ms";
    }

    void ToDisplay(Bitmap imageBitmap)
    {
        using var gfx = canvas.CreateGraphics();
        gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

        (float w, float h) = (imageBitmap.Width, imageBitmap.Height);
        float aspect = (w / h) / canvasAspect;
        if (aspect > 1)
            aspect = 1 / aspect;
        (w, h) = (canvasSize.Width * aspect, canvasSize.Height);

        gfx.Clear(Color.White);
        gfx.DrawImage(imageBitmap, 0, 0, w, h);
    }

    private void saveBinImage_Click(object sender, EventArgs e)
    {
        using SaveFileDialog dialog = new() {
            Filter = "Portable BitMap|*.pbm|Portable GrayMap|*.pgm|Portable PixMap|*.ppm",
            Title = "Save an Netpbm Image",
        };
        if (dialog.ShowDialog() is not DialogResult.OK)
            return;

        string ext = dialog.FileName.Split('.').Last();
        Netpbm.Format format = ext switch {
                "pbm" => Netpbm.Format.P4,
                "pgm" => Netpbm.Format.P5,
                "ppm" => Netpbm.Format.P6,
            };

        Netpbm.SaveBin(currColorArray, dialog.FileName, fileInfo with { Format = format });
    }

    private void saveTextImage_Click(object sender, EventArgs e)
    {
        using SaveFileDialog dialog = new()
        {
            Filter = "Portable BitMap|*.pbm|Portable GrayMap|*.pgm|Portable PixMap|*.ppm",
            Title = "Save an Netpbm Image",
        };
        if (dialog.ShowDialog() is not DialogResult.OK)
            return;

        string ext = dialog.FileName.Split('.').Last();
        Netpbm.Format format = ext switch {
            "pbm" => Netpbm.Format.P1,
            "pgm" => Netpbm.Format.P2,
            "ppm" => Netpbm.Format.P3,
        };

        Netpbm.SavePlain(currColorArray, dialog.FileName, fileInfo with { Format = format });
    }
}