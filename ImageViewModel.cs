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
            using (var reader = new StreamReader(filePath))
            {
                // Pomiń nagłówek P1 i komentarze
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith("#") && line.Trim() != "P1")
                    {
                        break;
                    }
                }

                if (line == null)
                {
                    throw new InvalidOperationException("Plik PBM jest nieprawidłowy lub pusty.");
                }

                // Wczytaj wymiary obrazu
                var dimensions = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int width = int.Parse(dimensions[0]);
                int height = int.Parse(dimensions[1]);

                // Wczytaj piksele
                bool[,] pixels = new bool[height, width];
                for (int y = 0; y < height; y++)
                {
                    line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        throw new InvalidOperationException($"Brak danych dla wiersza {y}.");
                    }

                    var pixelRow = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int x = 0; x < width; x++)
                    {
                        pixels[y, x] = pixelRow[x] == "1";
                    }
                }

                return new PbmImage { Width = width, Height = height, Pixels = pixels };
            }
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
                SavePbmImage(PbmImage, saveFileDialog.FileName);
            }
        }
        private void SavePbmImage(PbmImage pbmImage, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
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
