#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

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

        private int xSlices = 0, ySlices = 0;

        public bool Process()
        {
            if (File.Exists(ImagePath))
            {
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
            Point left = new(-1, TileHeight / 2);
            Point bottom = new(TileWidth / 2, TileHeight);
            Point right = new(TileWidth, TileHeight / 2);
            Point top = new(TileWidth / 2, -1);
            List<Point> tile = [left, bottom, right, top];

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

                            if (x + start.X < 0 ||
                                x + start.X >= OriginalImage.Width ||
                                x >= TileWidth ||
                                y + (int)start.Y < 0 ||
                                y + (int)start.Y >= OriginalImage.Height)
                            {
                                continue;
                            }
                            else if (PointInPolygon(currentPixel, tile))
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

            Point left = new(0, grid.Height / 2);
            Point top = new(grid.Width / 2, grid.Height - 1);
            Point bottom = new(grid.Width / 2, 0);
            Point right = new(grid.Width - 1, grid.Height / 2);

            points.AddRange(GetPointsInBresenhamLine(left, top));
            points.AddRange(GetPointsInBresenhamLine(top, right));
            points.AddRange(GetPointsInBresenhamLine(right, bottom));
            points.AddRange(GetPointsInBresenhamLine(bottom, left));

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

        // Shamelessely stolen from https://www.geeksforgeeks.org/dsa/how-to-check-if-a-given-point-lies-inside-a-polygon/
        private static bool PointInPolygon(Point point, List<Point> polygon)
        {
            int numVertices = polygon.Count;
            double x = point.X, y = point.Y;
            bool inside = false;

            // Store the first point in the polygon and initialize the second point
            Point p1 = polygon[0], p2;

            // Loop through each edge in the polygon
            for (int i = 1; i <= numVertices; i++)
            {
                // Get the next point in the polygon
                p2 = polygon[i % numVertices];

                // Check if the point is above the minimum y coordinate of the edge
                if (y > Math.Min(p1.Y, p2.Y))
                {
                    // Check if the point is below the maximum y coordinate of the edge
                    if (y <= Math.Max(p1.Y, p2.Y))
                    {
                        // Check if the point is to the left of the maximum x coordinate of the edge
                        if (x < Math.Max(p1.X, p2.X))
                        {
                            // Calculate the x-intersection of the line connecting the point to the edge
                            double xIntersection = (y - p1.Y) * (p2.X - p1.X) / (p2.Y - p1.Y) + p1.X;

                            // Check if the point is on the same line as the edge or to the left of the x-intersection
                            // if (p1.X == p2.X || x < xIntersection)
                            if (x < xIntersection)
                            {
                                // Flip the inside flag
                                inside = !inside;
                            }
                        }
                    }
                }
                // Store the current point as the first point for the next iteration
                p1 = p2;
            }
            // Return the value of the inside flag
            return inside;
        }
    }
}
