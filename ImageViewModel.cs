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

        private PbmImage _pbmImage;
        public PbmImage PbmImage
        {
            get { return _pbmImage; }
            set
            {
                _pbmImage = value;
                OnPropertyChanged(nameof(PbmImage));
                OnPropertyChanged(nameof(ConvertedPbmImage)); // Dodaj to
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
                Filter = "PBM Images (*.pbm)|*.pbm",
                Title = "Open PBM Image"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                PbmImage = LoadPbmImage(openFileDialog.FileName);
            }
        }

        private PbmImage LoadPbmImage(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            using (var reader = new StreamReader(stream)) // Use StreamReader to handle text and binary
            {
                // Read the header
                string header = reader.ReadLine();

                if (header != "P4" && header != "P1")
                {
                    throw new InvalidOperationException("Unsupported file format (expected P4 or P1).");
                }

                // Read image dimensions
                var dimensions = ReadDimensions(reader);
                int width = dimensions.Item1;
                int height = dimensions.Item2;

                bool[,] pixels;
                if (header == "P4")
                {
                    pixels = ReadPixelsP4(reader, width, height);
                }
                else
                {
                    pixels = ReadPixelsP1(reader, width, height);
                }

                return new PbmImage { Width = width, Height = height, Pixels = pixels };
            }
        }

        private bool[,] ReadPixelsP1(StreamReader reader, int width, int height)
        {
            bool[,] pixels = new bool[height, width];
            for (int y = 0; y < height; y++)
            {
                var line = reader.ReadLine();
                var bits = line.Split(' ');
                for (int x = 0; x < width; x++)
                {
                    pixels[y, x] = bits[x] == "1";
                }
            }
            return pixels;
        }

        private bool[,] ReadPixelsP4(StreamReader reader, int width, int height)
        {
            // Since we've read the header and dimensions using StreamReader, 
            // we need to switch to BinaryReader for the pixel data.
            var baseStream = reader.BaseStream;
            baseStream.Seek(0, SeekOrigin.Begin); // Reset the stream position to the beginning.

            using (var binaryReader = new BinaryReader(baseStream))
            {
                // Skip over the header and dimensions which have already been read.
                SkipHeaderAndDimensions(binaryReader);

                bool[,] pixels = new bool[height, width];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x += 8)
                    {
                        byte b = binaryReader.ReadByte();
                        for (int bit = 0; bit < 8 && (x + bit) < width; bit++)
                        {
                            pixels[y, x + bit] = (b & (1 << (7 - bit))) != 0;
                        }
                    }
                }
                return pixels;
            }
        }

        private void SkipHeaderAndDimensions(BinaryReader reader)
        {
            // Read until you skip the first two lines (the header and the dimensions).
            while (reader.ReadByte() != '\n') ; // Skip header line
            while (reader.ReadByte() != '\n') ; // Skip dimensions line
        }


        private Tuple<int, int> ReadDimensions(StreamReader reader)
        {
            string line;
            do
            {
                line = reader.ReadLine();
            } while (line != null && line.StartsWith("#")); // Pomijaj linie komentarzy

            if (line == null)
            {
                throw new InvalidOperationException("Nie znaleziono wymiarów obrazu.");
            }

            var parts = line.Split(' ');
            return Tuple.Create(int.Parse(parts[0]), int.Parse(parts[1]));
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
                Filter = "PBM Image (*.pbm)|*.pbm",
                Title = "Save PBM Image"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var formatChoice = MessageBox.Show("Czy chcesz zapisać obraz w formacie binarnym P4?",
                                                   "Wybierz format",
                                                   MessageBoxButton.YesNo,
                                                   MessageBoxImage.Question);

                bool saveAsP4 = formatChoice == MessageBoxResult.Yes;
                SavePbmImage(PbmImage, saveFileDialog.FileName, saveAsP4);
            }
        }

        private void SavePbmImage(PbmImage pbmImage, string filePath, bool saveAsP4)
        {
            using (var stream = File.OpenWrite(filePath))
            {
                if (saveAsP4)
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

        public BitmapSource ConvertedPbmImage
        {
            get { return ConvertPbmToBitmapSource(_pbmImage); }
        }

        private BitmapSource ConvertPbmToBitmapSource(PbmImage pbmImage)
        {
            try
            {
                if (pbmImage == null)
                {
                    Debug.WriteLine("PbmImage is null");
                    return null;
                }

                int width = pbmImage.Width;
                int height = pbmImage.Height;
                Debug.WriteLine($"Converting PbmImage: width={width}, height={height}");
                var pixels = new byte[width * height * 4];


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

                var bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
                Debug.WriteLine("PbmImage converted successfully");
                return bitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in ConvertPbmToBitmapSource: " + ex.Message);
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
