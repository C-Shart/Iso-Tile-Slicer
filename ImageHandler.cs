using IronSoftware.Drawing;
using System.Text;
using Color = IronSoftware.Drawing.Color;
using Point = IronSoftware.Drawing.Point;

namespace IsoTiloSlicer
{
    internal class ImageHandler(string imagePath = "", int tileWidth = 110, int tileHeight = 78, string outputDirectory = "out")
    {
        public Color BackgroundColor { get; set; } = Color.Transparent;
        public string ImagePath { get; set; } = imagePath;
        public int TileWidth { get; set; } = tileWidth;
        public int TileHeight { get; set; } = tileHeight;
        public string OutputDirectory { get; set; } = outputDirectory;
        public AnyBitmap OriginalImage { get; set; }
        public List<AnyBitmap> Slices { get; private set; } = new List<AnyBitmap>();
        public string LastErrorMessage { get; private set; } = string.Empty;
        public string FileNameFormat { get; set; } = "{0}";
        public int StartingFileNumber { get; set; } = 0;
        public List<Point> TileLeftTop {get; set; } = [];
        public List<Point> TileLeftBottom {get; set; } = [];
        public List<Point> TileRightTop {get; set; } = [];
        public List<Point> TileRightBottom {get; set; } = [];

        private int xSlices = 0, ySlices = 0;

        public bool Process()
        {
            if (File.Exists(ImagePath))
            {
                Point top = new(TileWidth / 2, 0);
                Point left = new(0, TileHeight / 2);
                Point bottom = new(TileWidth / 2, TileHeight - 1);
                Point right = new(TileWidth - 1, TileHeight / 2);

                TileLeftTop = GetPointsInBresenhamLine(left, top);
                TileLeftBottom = GetPointsInBresenhamLine(left, bottom);
                TileRightTop = GetPointsInBresenhamLine(right, top);
                TileRightBottom = GetPointsInBresenhamLine(right, bottom);
                OriginalImage = AnyBitmap.FromFile(ImagePath);

                xSlices = (int)Math.Ceiling((double)OriginalImage.Width / (double)TileWidth) + 1;
                ySlices = 2 * (int)Math.Ceiling((double)OriginalImage.Height / (double)TileHeight) + 1;

                SplitImage();
                SaveImages();
                CreateHtmlLayout();
                Console.WriteLine("Sliced succesfully.");
                return true;
            }
            else
            {
                LastErrorMessage = "Image path could not be found";
                Console.WriteLine(LastErrorMessage);
            }
            return false;
        }

        private void SplitImage()
        {
            for (int row = 0; row < ySlices; row++)
            {
                for (int col = 0; col < xSlices; col++)
                {
                    // Used to compare all pixels in the slice
                    List<Color> pixelCompare = [];

                    var start = GetSectionStartPosition(row, col);
                    AnyBitmap slice = new(TileWidth, TileHeight, BackgroundColor);

                    for (int y = 0; y < TileHeight; y++)
                    {
                        for (int x = 0; x < TileWidth; x++)
                        {
                            Point currentPixel = new(x, y);

                            bool outsideLeftTop = false;
                            bool outsideLeftBottom = false;
                            bool outsideRightTop = false;
                            bool outsideRightBottom = false;

                            // Check each tile quadrant to see if the pixel is outside of the tile border
                            if (x < TileWidth / 2 && y < TileHeight / 2)
                            {
                                outsideLeftTop = LeftOfBresenham(TileLeftTop, currentPixel, true) && AboveBresenham(TileLeftTop, currentPixel);
                            }
                            else if (x >= TileWidth / 2 && y <= TileHeight / 2)
                            {
                                outsideRightTop = !LeftOfBresenham(TileRightTop, currentPixel) && AboveBresenham(TileRightTop, currentPixel);
                            }
                            else if (x < TileWidth / 2 && y > TileHeight / 2)
                            {
                                outsideLeftBottom = LeftOfBresenham(TileLeftBottom, currentPixel) && !AboveBresenham(TileLeftBottom, currentPixel);
                            }
                            else if (x >= TileWidth / 2 && y >= TileHeight / 2)
                            {
                                outsideRightBottom = !LeftOfBresenham(TileRightBottom, currentPixel) && !AboveBresenham(TileRightBottom, currentPixel);
                            }

                            // Skip the pixel if it's outside the original image dimensions or the tile
                            if (x + start.X < 0
                                || x + start.X >= OriginalImage.Width
                                || x >= TileWidth
                                || y + (int)start.Y < 0
                                || y + (int)start.Y >= OriginalImage.Height
                                || outsideLeftTop
                                || outsideLeftBottom
                                || outsideRightTop
                                || outsideRightBottom)
                            {
                                continue;
                            }
                            else
                            {
                                // Set slice pixel, add pixel to compare list
                                Color sourcePixel = OriginalImage.GetPixel(x + (int)start.X, y + (int)start.Y);
                                slice.SetPixel(x, y, sourcePixel);
                                pixelCompare.Add(sourcePixel);
                            }
                        }
                    }

                    // Add to slices only if all pixels are NOT the same color
                    if (pixelCompare.Distinct().Count() > 1)
                    {
                        Slices.Add(slice);
                    }
                }
            }
        }

        private static bool LeftOfBresenham(List<Point> line, Point pixel, bool topLeft = false)
        {
            bool leftOfLine = false;
            List<Point> yMatch = new([.. line.Where(point => point.Y == pixel.Y)]);
            if (yMatch.Count > 0)
            {
                Point lineX = yMatch.First();
                if (!topLeft)
                {
                    leftOfLine = lineX.X >= pixel.X;
                }
                else
                {
                    leftOfLine = lineX.X > pixel.X;
                }
            }
            return leftOfLine;
        }

        private static bool AboveBresenham(List<Point> line, Point pixel)
        {
            bool aboveLine = false;
            List<Point> xMatch = new([.. line.Where(point => point.X == pixel.X)]);
            if (xMatch.Count > 0)
            {
                Point lineY = xMatch.First();
                aboveLine = lineY.Y >= pixel.Y;
            }
            return aboveLine;
        }

        private Point GetSectionStartPosition(int row, int column)
        {
            int x = column * TileWidth;
            int y = (row * (TileHeight / 2)) - (TileHeight / 2);

            if (row % 2 == 0) //Number is even
            {
                x -= TileWidth / 2;
            }

            return new Point(x, y);
        }

        private AnyBitmap GenSampleGrid()
        {
            AnyBitmap grid = new(TileWidth, TileHeight);
            Color gColor = new(100, 0, 255, 0);
            List<Point> points = [];

            points.AddRange(TileLeftTop);
            points.AddRange(TileLeftBottom);
            points.AddRange(TileRightBottom);
            points.AddRange(TileRightTop);

            foreach (Point point in points)
            {
                grid.SetPixel((int)point.X, (int)point.Y, gColor);
            }

            return grid;
        }

        public void SaveImages()
        {
            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }

            int startingNumber = StartingFileNumber;
            foreach (var image in Slices)
            {
                image.SaveAs(Path.Combine(OutputDirectory, string.Format(FileNameFormat + ".png", startingNumber)), AnyBitmap.ImageFormat.Png);
                startingNumber++;
            }

            GenSampleGrid().SaveAs(Path.Combine(OutputDirectory, "grid.png"), AnyBitmap.ImageFormat.Png);
        }

        public void CreateHtmlLayout()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<body bgcolor='{BackgroundColor.ToHtmlCssColorCode()}' style='padding: 0px; border: 0px; margin: 0px;'>\n");

            int startingNumber = StartingFileNumber;

            string[][] tables = new string[ySlices][];

            for (int i = 0; i < tables.Length; i++)
            {
                tables[i] = new string[xSlices];
            }


            int c = 0, r = 0;
            foreach (var image in Slices)
            {
                string fname = string.Format(FileNameFormat + ".png", startingNumber);

                int gx = c;
                int gy = r;

                Point position = GetSectionStartPosition(gy, gx);

                tables[r][c] = $"<img src='{fname}' title='{fname} ({gx}, {gy})' style='" +
                    "position: absolute;" +
                    $"top: {position.Y * 2 + TileHeight};" +
                    $"left: {position.X * 2 + TileWidth}" +
                    "'>";
                c++;

                if (c >= xSlices)
                {
                    c = 0;
                    r++;
                }
                startingNumber++;
            }

            for (int i = 0; i < tables.Length; i++) //Rows
            {
                for (int ic = 0; ic < tables[i].Length; ic++) //Column
                {
                    sb.AppendLine(tables[i][ic]);
                }
            }

            sb.AppendLine($"<div style=\"background-image: url('grid.png'); position: absolute; top: 0; left: 0; width: 100%; height: 100%; pointer-events: none;\"></div>");

            sb.AppendLine("</body>");

            try
            {
                File.WriteAllText(Path.Combine(OutputDirectory, "layout.html"), sb.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static List<Point> GetPointsInBresenhamLine(Point origin, Point target)
        {
            List<Point> points = [];
            double x = origin.X;
            double y = origin.Y;

            float dx = Math.Abs((float)(target.X - x));
            float dy = -Math.Abs((float)(target.Y - y));

            int sx = (x < target.X) ? 1 : -1;
            int sy = (y < target.Y) ? 1 : -1;

            var err = dx + dy;

            var longer = (dx > Math.Abs(dy)) ? dx : Math.Abs(dy);

            for (int i = 0; i <= longer; i++)
            {
                var point = new Point(x, y);
                points.Add(point);

                var e2 = 2 * err;
                if (e2 >= dy)
                {
                    if (x == target.X) { break; }
                    err += dy;
                    x += sx;
                }
                if (e2 <= dx)
                {
                    if (y == target.Y) { break; }
                    err += dx;
                    y += sy;
                }
            }

            return points;
        }
    }
}
