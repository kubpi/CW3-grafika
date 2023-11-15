﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Threading.Tasks;

namespace CW3_grafika
{
    public class ImageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isPbmChecked;
        private bool isPgmChecked;
        private bool isPpmChecked;
        private BitmapSource convertedImage;
        private EventQueue eventQueue = new EventQueue();

        public bool IsPbmChecked
        {
            get { return isPbmChecked; }
            set
            {
                isPbmChecked = value;
                OnPropertyChanged(nameof(IsPbmChecked));
            }
        }

        public bool IsPgmChecked
        {
            get { return isPgmChecked; }
            set
            {
                isPgmChecked = value;
                OnPropertyChanged(nameof(IsPgmChecked));
            }
        }

        public bool IsPpmChecked
        {
            get { return isPpmChecked; }
            set
            {
                isPpmChecked = value;
                OnPropertyChanged(nameof(IsPpmChecked));
            }
        }

        public BitmapSource ConvertedPbmImage
        {
            get { return convertedImage; }
            set
            {
                convertedImage = value;
                OnPropertyChanged(nameof(ConvertedPbmImage));
            }
        }

        public RelayCommand LoadImageCommand { get; private set; }
        public RelayCommand SaveImageCommand { get; private set; }

        public ImageViewModel()
        {
            LoadImageCommand = new RelayCommand(async () => await EnqueueLoadImageAsync());
            SaveImageCommand = new RelayCommand(async () => await EnqueueSaveImageAsync());
        }

        //
        private async Task EnqueueSaveImageAsync()
        {
            await eventQueue.EnqueueAsync(async () =>
            {
                await SaveImageAsync();
            });
        }

        private async Task SaveImageAsync()
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PBM Files (*.pbm)|*.pbm|PGM Files (*.pgm)|*.pgm|PPM Files (*.ppm)|*.ppm|All Files (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string fileExtension = Path.GetExtension(filePath);

                if (IsPbmChecked && (fileExtension == ".pbm" || fileExtension == ".P1" || fileExtension == ".P4"))
                {
                    SavePbmImage(filePath);
                }
                else if (IsPgmChecked && (fileExtension == ".pgm" || fileExtension == ".P2" || fileExtension == ".P5"))
                {
                    SavePgmImage(filePath);
                }
                else if (IsPpmChecked && (fileExtension == ".ppm" || fileExtension == ".P3" || fileExtension == ".P6"))
                {
                    await SavePpmImageAsync(filePath);
                }
                else
                {
                    MessageBox.Show("Nieobsługiwany format pliku.");
                }
            }
        }

        private void SavePbmImage(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine("P1");
                // Dodaj pozostałą część nagłówka
                int width = (int)ConvertedPbmImage.Width;
                int height = (int)ConvertedPbmImage.Height;
                sw.WriteLine($"{width} {height}");

                // Zapisz dane obrazu PBM tekstowo
                byte[] pixelData = new byte[width * height];
                ConvertedPbmImage.CopyPixels(pixelData, width, 0);

                for (int i = 0; i < pixelData.Length; i++)
                {
                    byte pixelValue = pixelData[i];
                    sw.Write(pixelValue == 255 ? "1" : "0");
                }
            }
        }

        private void SavePgmImage(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine("P2");
                // Dodaj pozostałą część nagłówka
                int width = (int)ConvertedPbmImage.Width;
                int height = (int)ConvertedPbmImage.Height;
                sw.WriteLine($"{width} {height}");
                sw.WriteLine("255"); // Zakładamy maksymalną wartość dla obrazu PGM

                // Zapisz dane obrazu PGM tekstowo
                byte[] pixelData = new byte[width * height];
                ConvertedPbmImage.CopyPixels(pixelData, width, 0);

                for (int i = 0; i < pixelData.Length; i++)
                {
                    byte pixelValue = pixelData[i];
                    sw.Write($"{pixelValue} ");
                }
            }
        }

        private async Task SavePpmImageAsync(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine("P3");
                // Dodaj pozostałą część nagłówka
                int width = (int)ConvertedPbmImage.Width;
                int height = (int)ConvertedPbmImage.Height;
                sw.WriteLine($"{width} {height}");
                sw.WriteLine("255"); // Zakładamy maksymalną wartość dla obrazu PPM

                // Zapisz dane obrazu PPM tekstowo
                byte[] pixelData = new byte[width * height * 3];
                ConvertedPbmImage.CopyPixels(pixelData, width * 3, 0);

                for (int i = 0; i < pixelData.Length; i += 3)
                {
                    byte r = pixelData[i];
                    byte g = pixelData[i + 1];
                    byte b = pixelData[i + 2];
                    sw.Write($"{r} {g} {b}   ");
                }
            }
        }
        //

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task EnqueueLoadImageAsync()
        {
            await eventQueue.EnqueueAsync(async () =>
            {
                await LoadImageAsync();
            });
        }

        private async Task LoadImageAsync()
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "PBM Files (*.pbm)|*.pbm|PGM Files (*.pgm)|*.pgm|PPM Files (*.ppm)|*.ppm|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileExtension = Path.GetExtension(filePath);

                if (IsPbmChecked && (fileExtension == ".pbm" || fileExtension == ".P1" || fileExtension == ".P4"))
                {
                    LoadPbmImage(filePath);
                }
                else if (IsPgmChecked && (fileExtension == ".pgm" || fileExtension == ".P2" || fileExtension == ".P5"))
                {
                    LoadPgmImage(filePath);
                }
                else if (IsPpmChecked && (fileExtension == ".ppm" || fileExtension == ".P3" || fileExtension == ".P6"))
                {
                    await LoadPpmImageAsync(filePath);
                }
                else
                {
                    MessageBox.Show("Nieobsługiwany format pliku.");
                }
            }
        }

        private void LoadPbmImage(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (StreamReader sr = new StreamReader(fs))
            {
                string magicNumber = sr.ReadLine();
                while (!sr.EndOfStream && sr.Peek() == '#')
                {
                    sr.ReadLine(); // Pomijanie linii z komentarzami
                }

                if (magicNumber == "P1" || magicNumber == "P4")
                {
                    // Wczytaj obraz PBM
                    int width, height;
                    string[] dimensions = sr.ReadLine().Split(' ');
                    width = int.Parse(dimensions[0]);
                    height = int.Parse(dimensions[1]);

                    byte[] pixelData = new byte[width * height];
                    int dataIndex = 0;

                    if (magicNumber == "P1")
                    {
                        // Wczytywanie w formie tekstowej
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            foreach (char c in line)
                            {
                                if (c == '0')
                                {
                                    pixelData[dataIndex++] = 0;
                                }
                                else if (c == '1')
                                {
                                    pixelData[dataIndex++] = 255;
                                }
                            }
                        }
                    }
                    else if (magicNumber == "P4")
                    {
                        // Wczytywanie w formie binarnej
                        int bufferSize = (int)Math.Ceiling((double)(width * height) / 8);
                        byte[] buffer = new byte[bufferSize];
                        fs.Read(buffer, 0, bufferSize);
                        for (int i = 0; i < width * height; i++)
                        {
                            int byteIndex = i / 8;
                            int bitIndex = 7 - (i % 8);
                            byte pixelValue = (byte)((buffer[byteIndex] >> bitIndex) & 0x01);
                            pixelData[dataIndex++] = (byte)(pixelValue * 255);
                        }
                    }

                    BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixelData, width);
                    ConvertedPbmImage = bitmap;
                }
                else
                {
                    MessageBox.Show("Nieobsługiwany format PBM.");
                }
            }
        }

        private void LoadPgmImage(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (StreamReader sr = new StreamReader(fs))
            {
                string magicNumber = sr.ReadLine();
                if (magicNumber == "P2")
                {
                    // Pomijanie komentarzy
                    string line;
                    do
                    {
                        line = sr.ReadLine();
                    } while (!string.IsNullOrEmpty(line) && line.StartsWith("#"));

                    // Wczytaj obraz PGM
                    int width, height, maxValue;
                    string[] dimensions = line.Split(' ');
                    if (dimensions.Length == 2 && int.TryParse(dimensions[0], out width) && int.TryParse(dimensions[1], out height) && width > 0 && height > 0)
                    {
                        if (int.TryParse(sr.ReadLine(), out maxValue) && maxValue >= 0)
                        {
                            byte[] pixelData = new byte[width * height];
                            int dataIndex = 0;

                            for (int i = 0; i < height; i++)
                            {
                                string[] lineValues = sr.ReadLine().Split(' ');
                                foreach (string value in lineValues)
                                {
                                    if (int.TryParse(value, out int pixelValue))
                                    {
                                        pixelValue = (int)((double)pixelValue / maxValue * 255);
                                        pixelData[dataIndex++] = (byte)Math.Max(0, Math.Min(255, pixelValue));
                                    }
                                }
                            }

                            BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixelData, width);
                            ConvertedPbmImage = bitmap;
                        }
                        else
                        {
                            MessageBox.Show("Nieprawidłowa wartość maksymalna.");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Nieprawidłowe wymiary obrazu.");
                    }
                }
                else
                {
                    MessageBox.Show("Nieobsługiwany format PGM.");
                }
            }
        }

        private async Task LoadPpmImageAsync(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (BinaryReader br = new BinaryReader(fs))
            {
                string magicNumber = await ReadNextNonCommentLineAsync(br).ConfigureAwait(false);
                if (magicNumber != "P3" && magicNumber != "P6")
                {
                    MessageBox.Show("Unsupported PPM format, expected P3 or P6.");
                    return;
                }

                int width = 0, height = 0, maxValue = 0;
                bool headerParsed = false;

                while (!headerParsed)
                {
                    string line = await ReadNextNonCommentLineAsync(br).ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        if (width == 0 && int.TryParse(part, out width))
                            continue;
                        if (height == 0 && int.TryParse(part, out height))
                            continue;
                        if (maxValue == 0 && int.TryParse(part, out maxValue))
                        {
                            headerParsed = true;
                            break;
                        }
                    }
                }

                if (width <= 0 || height <= 0 || maxValue <= 0)
                {
                    MessageBox.Show($"Invalid PPM header. Width: {width}, Height: {height}, MaxValue: {maxValue}");
                    return;
                }

                byte[] pixelData = new byte[width * height * 3];

                if (magicNumber == "P3")
                {
                    await ReadP3PixelDataAsync(br, pixelData, maxValue).ConfigureAwait(false);
                }
                else // P6 format
                {
                    await ReadP6PixelDataAsync(br, pixelData, width * height * 3).ConfigureAwait(false);
                }

                // Utwórz bitmapę na wątku UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, pixelData, width * 3);
                    ConvertedPbmImage = bitmap;
                });
            }
        }

        private async Task<string> ReadNextNonCommentLineAsync(BinaryReader br)
        {
            StringBuilder line = new StringBuilder();
            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                // Czytaj bajt synchronicznie, ale użycie 'await Task.Run()' zapewnia, że operacja nie będzie blokowała interfejsu użytkownika
                char c = await Task.Run(() => (char)br.ReadByte()).ConfigureAwait(false);
                if (c == '\n' || c == '\r')
                {
                    string result = line.ToString().Trim();
                    if (!string.IsNullOrEmpty(result) && !result.StartsWith("#"))
                    {
                        return result;
                    }
                    line.Clear();
                }
                else
                {
                    line.Append(c);
                }
            }
            return line.ToString().Trim();
        }

        private async Task ReadP3PixelDataAsync(BinaryReader br, byte[] pixelData, int maxValue)
        {
            int dataIndex = 0;
            byte[] buffer = new byte[8192]; // Bufor o stałym rozmiarze dla optymalizacji odczytu
            StringBuilder stringBuilder = new StringBuilder();

            while (dataIndex < pixelData.Length)
            {
                // Odczytaj fragment danych do bufora
                int bytesRead = await br.BaseStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break; // Zakończ jeśli nie ma więcej danych do odczytu
                }

                // Dodaj odczytane dane do StringBuildera
                stringBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                // Przetwarzaj linie tekstu
                string text = stringBuilder.ToString();
                int lastNewLine = text.LastIndexOf('\n');
                if (lastNewLine == -1)
                    continue; // Jeśli w buforze nie ma pełnych linii, kontynuuj odczyt

                var lines = text.Substring(0, lastNewLine).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                stringBuilder = new StringBuilder(text.Substring(lastNewLine + 1)); // Zachowaj niepełne linie na następny cykl

                foreach (string line in lines)
                {
                    // Pomijaj linie komentarzy
                    if (line.StartsWith("#"))
                        continue;

                    var values = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var value in values)
                    {
                        if (int.TryParse(value, out int pixelValue))
                        {
                            pixelValue = (int)((double)pixelValue / maxValue * 255);
                            pixelData[dataIndex++] = (byte)Math.Max(0, Math.Min(255, pixelValue));
                            if (dataIndex >= pixelData.Length)
                            {
                                return; // Zakończ, gdy wszystkie dane zostały wczytane
                            }
                        }
                    }
                }
            }
        }

        private async Task ReadP6PixelDataAsync(BinaryReader br, byte[] pixelData, int pixelCount)
        {
            int bytesRead = 0;
            int bytesToRead = pixelCount;
            while (bytesRead < bytesToRead)
            {
                int chunkSize = Math.Min(4096, bytesToRead - bytesRead);
                byte[] buffer = new byte[chunkSize];
                int read = await br.BaseStream.ReadAsync(buffer, 0, chunkSize).ConfigureAwait(false);
                if (read == 0)
                {
                    // Jeśli nie ma więcej danych do odczytania, a nie wczytano wystarczającej ilości danych pikselowych
                    throw new EndOfStreamException("Nie udało się wczytać wystarczającej ilości danych pikselowych dla formatu P6.");
                }
                Array.Copy(buffer, 0, pixelData, bytesRead, read);
                bytesRead += read;
            }
        }
    }
}
