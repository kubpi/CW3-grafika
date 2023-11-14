using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows;

namespace CW3_grafika
{
    public class ImageViewModel : INotifyPropertyChanged
    {
        public ICommand LoadImageCommand { get; private set; }
        public ICommand SaveImageCommand { get; private set; }

        private ImageBase _image;
        public ImageBase Image
        {
            get { return _image; }
            set
            {
                _image = value;
                OnPropertyChanged(nameof(Image));
                OnPropertyChanged(nameof(ConvertedImage)); // Dodaj to
            }
        }

        public ImageViewModel()
        {
            LoadImageCommand = new RelayCommand(LoadImage);
            SaveImageCommand = new RelayCommand(SaveImage);
        }

        private void LoadImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PBM Images (*.pbm)|*.pbm|PGM Images (*.pgm)|*.pgm",
                Title = "Open Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Image = LoadImage(openFileDialog.FileName);
            }
        }

        private ImageBase LoadImage(string filePath)
        {
            // Wczytaj zawartość pliku i określ format na podstawie nagłówka
            using (var stream = File.OpenRead(filePath))
            using (var reader = new BinaryReader(stream))
            {
                string header = ReadHeader(reader);

                if (header == "P1" || header == "P4")
                {
                    return LoadPbmImage(reader);
                }
                else if (header == "P2" || header == "P5")
                {
                    return LoadPgmImage(reader);
                }
                else
                {
                    throw new InvalidOperationException("Niewspierany format pliku.");
                }
            }
        }

        private ImageBase LoadPbmImage(BinaryReader reader)
        {
            var dimensions = ReadDimensions(reader);
            int width = dimensions.Item1;
            int height = dimensions.Item2;

            // Wczytaj piksele
            bool[,] pixels = ReadPixels(reader, width, height);

            return new PbmImage { Width = width, Height = height, Pixels = pixels };
        }

        private ImageBase LoadPgmImage(StreamReader reader)
        {
            var dimensions = ReadDimensions(reader);
            int width = dimensions.Item1;
            int height = dimensions.Item2;

            // Wczytaj maksymalną wartość piksela
            int maxValue = ReadMaxValue(reader);

            // Wczytaj piksele
            byte[,] pixels = ReadPgmPixels(reader, width, height);

            return new PgmImage { Width = width, Height = height, MaxValue = maxValue, Pixels = pixels };
        }

        private int ReadMaxValue(StreamReader reader)
        {
            string line = reader.ReadLine();
            return int.Parse(line);
        }

        private byte[,] ReadPgmPixels(StreamReader reader, int width, int height)
        {
            byte[,] pixels = new byte[height, width];
            int maxValue = 255; // Domyślna wartość dla formatu PGM

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int value = reader.Read();
                    pixels[y, x] = (byte)value;
                }
            }

            return pixels;
        }

        private string ReadHeader(BinaryReader reader)
        {
            StringBuilder header = new StringBuilder();
            char ch;
            while ((ch = reader.ReadChar()) != '\n')
            {
                header.Append(ch);
            }
            return header.ToString();
        }

        private Tuple<int, int> ReadDimensions(BinaryReader reader)
        {
            string line = "";
            char ch;
            while ((ch = reader.ReadChar()) != '\n')
            {
                line += ch;
            }
            var parts = line.Split(' ');
            return Tuple.Create(int.Parse(parts[0]), int.Parse(parts[1]));
        }

        private bool[,] ReadPixels(BinaryReader reader, int width, int height)
        {
            bool[,] pixels = new bool[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x % 8 == 0) // Czytaj nowy bajt dla każdych 8 pikseli
                    {
                        byte b = reader.ReadByte();
                        for (int bit = 0; bit < 8 && (x + bit) < width; bit++)
                        {
                            pixels[y, x + bit] = (b & (1 << (7 - bit))) != 0;
                        }
                    }
                }
            }
            return pixels;
        }

        private void SaveImage()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PBM Image (*.pbm)|*.pbm|PGM Image (*.pgm)|*.pgm",
                Title = "Save Image"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var formatChoice = MessageBox.Show("Czy chcesz zapisać obraz w formacie binarnym?",
                                                   "Wybierz format",
                                                   MessageBoxButton.YesNo,
                                                   MessageBoxImage.Question);

                bool saveAsBinary = formatChoice == MessageBoxResult.Yes;
                SaveImage(Image, saveFileDialog.FileName, saveAsBinary);
            }
        }

        private void SaveImage(ImageBase image, string filePath, bool saveAsBinary)
        {
            // Określ, czy zapisywać jako PBM czy PGM, na podstawie typu obrazu
            if (image is PbmImage)
            {
                SavePbmImage((PbmImage)image, filePath, saveAsBinary);
            }
            else if (image is PgmImage)
            {
                SavePgmImage((PgmImage)image, filePath, saveAsBinary);
            }
            else
            {
                throw new InvalidOperationException("Niewspierany typ obrazu.");
            }
        }

        private void SavePbmImage(PbmImage pbmImage, string filePath, bool saveAsBinary)
        {
            using (var stream = File.OpenWrite(filePath))
            {
                if (saveAsBinary)
                {
                    SaveP4(pbmImage, stream);
                }
                else
                {
                    SaveP1(pbmImage, stream);
                }
            }
        }

        private void SaveP1(PbmImage pbmImage, FileStream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                // Zapisz nagłówek P1
                writer.WriteLine("P1");
                // Zapisz wymiary obrazu
                writer.WriteLine($"{pbmImage.Width} {pbmImage.Height}");

                // Zapisz piksele
                for (int y = 0; y < pbmImage.Height; y++)
                {
                    for (int x = 0; x < pbmImage.Width; x++)
                    {
                        writer.Write(pbmImage.Pixels[y, x] ? "1 " : "0 ");
                    }
                    writer.WriteLine();
                }
            }
        }

        private void SaveP4(PbmImage pbmImage, FileStream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                // Zapisz nagłówek P4
                writer.Write(Encoding.ASCII.GetBytes("P4\n"));
                // Zapisz wymiary obrazu
                writer.Write(Encoding.ASCII.GetBytes($"{pbmImage.Width} {pbmImage.Height}\n"));

                // Zapisz piksele
                for (int y = 0; y < pbmImage.Height; y++)
                {
                    for (int x = 0; x < pbmImage.Width; x += 8)
                    {
                        byte b = 0;
                        for (int bit = 0; bit < 8 && (x + bit) < pbmImage.Width; bit++)
                        {
                            if (pbmImage.Pixels[y, x + bit])
                            {
                                b |= (byte)(1 << (7 - bit));
                            }
                        }
                        writer.Write(b);
                    }
                }
            }
        }

        private void SavePgmImage(PgmImage pgmImage, string filePath, bool saveAsBinary)
        {
            using (var stream = File.OpenWrite(filePath))
            {
                SaveP5(pgmImage, stream);
            }
        }

        private void SaveP5(PgmImage pgmImage, FileStream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                // Zapisz nagłówek P5
                writer.Write(Encoding.ASCII.GetBytes("P5\n"));
                // Zapisz wymiary obrazu
                writer.Write(Encoding.ASCII.GetBytes($"{pgmImage.Width} {pgmImage.Height}\n"));
                // Zapisz maksymalną wartość piksela
                writer.Write(Encoding.ASCII.GetBytes($"{pgmImage.MaxValue}\n"));

                // Zapisz piksele
                for (int y = 0; y < pgmImage.Height; y++)
                {
                    for (int x = 0; x < pgmImage.Width; x++)
                    {
                        writer.Write(pgmImage.Pixels[y, x]);
                    }
                }
            }
        }

        // ... reszta kodu ...

        public BitmapSource ConvertedImage
        {
            get { return ConvertImageToBitmapSource(_image); }
        }

        private BitmapSource ConvertImageToBitmapSource(ImageBase image)
        {
            try
            {
                if (image == null)
                {
                    Debug.WriteLine("Image is null");
                    return null;
                }

                int width = image.Width;
                int height = image.Height;
                Debug.WriteLine($"Converting Image: width={width}, height={height}");
                var pixels = new byte[width * height * 4];

                if (image is PbmImage pbmImage)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = (y * width + x) * 4;
                            byte color = pbmImage.Pixels[y, x] ? (byte)0 : (byte)255;
                            pixels[index] = color;     // Blue
                            pixels[index + 1] = color; // Green
                            pixels[index + 2] = color; // Red
                            pixels[index + 3] = 255;   // Alpha
                        }
                    }
                }
                else if (image is PgmImage pgmImage)
                {
                    int maxValue = pgmImage.MaxValue;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int index = (y * width + x) * 4;
                            byte value = (byte)(pgmImage.Pixels[y, x] * 255 / maxValue);
                            pixels[index] = value;     // Blue
                            pixels[index + 1] = value; // Green
                            pixels[index + 2] = value; // Red
                            pixels[index + 3] = 255;   // Alpha
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("Unsupported image type");
                    return null;
                }

                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
                Debug.WriteLine("Image converted successfully");
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in ConvertToBitmapSource: " + ex.Message);
                throw; // Rzuć ponownie wyjątek, aby ułatwić debugowanie
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
