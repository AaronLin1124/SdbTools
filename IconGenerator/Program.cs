using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

var sizes = new int[] { 16, 32, 48, 64, 128, 256 };

foreach (var size in sizes)
{
    using var bmp = new Bitmap(size, size);
    using var g = Graphics.FromImage(bmp);
    
    g.SmoothingMode = SmoothingMode.HighQuality;
    g.TextRenderingHint = TextRenderingHint.AntiAlias;
    
    var rect = new Rectangle(0, 0, size, size);
    using var brush = new LinearGradientBrush(
        rect,
        Color.FromArgb(30, 136, 229),
        Color.FromArgb(21, 101, 192),
        LinearGradientMode.Vertical);
    
    g.FillRectangle(brush, rect);
    
    using var font = new Font("Arial", size * 0.28f, FontStyle.Bold);
    using var textBrush = new SolidBrush(Color.White);
    
    var format = new StringFormat
    {
        Alignment = StringAlignment.Center,
        LineAlignment = StringAlignment.Center
    };
    
    g.DrawString("SDB", font, textBrush, size / 2f, size / 2f, format);
    
    var outputPath = $"SDbTools_{size}x{size}.png";
    bmp.Save(outputPath, ImageFormat.Png);
    System.Console.WriteLine($"Saved: {outputPath}");
}

var images = new Bitmap[sizes.Length];
for (int i = 0; i < sizes.Length; i++)
{
    images[i] = new Bitmap($"SDbTools_{sizes[i]}x{sizes[i]}.png");
}

using var stream = new FileStream("SDbTools.ico", FileMode.Create);
using var writer = new BinaryWriter(stream);

writer.Write((short)0);
writer.Write((short)1);
writer.Write((short)sizes.Length);

var imageDataOffset = 6 + 16 * sizes.Length;
var imageDataList = new List<byte[]>();

for (int i = 0; i < sizes.Length; i++)
{
    using var ms = new MemoryStream();
    images[i].Save(ms, ImageFormat.Png);
    var imageData = ms.ToArray();
    imageDataList.Add(imageData);
    
    writer.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
    writer.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
    writer.Write((byte)0);
    writer.Write((byte)0);
    writer.Write((short)1);
    writer.Write((short)32);
    writer.Write(imageData.Length);
    writer.Write(imageDataOffset);
    
    imageDataOffset += imageData.Length;
}

foreach (var imageData in imageDataList)
{
    writer.Write(imageData);
}

writer.Close();

foreach (var img in images) img.Dispose();

System.Console.WriteLine("Created: SDbTools.ico");
