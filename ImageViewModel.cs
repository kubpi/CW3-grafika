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

        private bool isLoading;
        public bool IsLoading
        {
            get { return isLoading; }
            set
            {
                isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        private bool isImageVisible;
        public bool IsImageVisible
        {
            get { return isImageVisible; }
            set
            {
                isImageVisible = value;
                OnPropertyChanged(nameof(IsImageVisible));
            }
        }


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

        public ImageViewModel()
        {
            LoadImageCommand = new RelayCommand(async () => await EnqueueLoadImageAsync());
        }

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
            IsLoading = true; 
            IsImageVisible = false; 
            ConvertedPbmImage = null; 

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

            IsLoading = false; // Ustawia IsLoading na false po zakończeniu wczytywania
            IsImageVisible = true; // Wyświetla obraz po zakończeniu wczytywania
        }


        private void LoadPbmImage(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (StreamReader sr = new StreamReader(fs))
            {
                string magicNumber = sr.ReadLine();
                while (!sr.EndOfStream && sr.Peek() == '#')
                {
                    sr.ReadLine();
                }

                if (magicNumber == "P1" || magicNumber == "P4")
                {
                    int width, height;
                    string[] dimensions = sr.ReadLine().Split(' ');
                    width = int.Parse(dimensions[0]);
                    height = int.Parse(dimensions[1]);

                    byte[] pixelData = new byte[width * height];
                    int dataIndex = 0;

                    if (magicNumber == "P1")
                    {
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
                    string line;
                    do
                    {
                        line = sr.ReadLine();
                    } while (!string.IsNullOrEmpty(line) && line.StartsWith("#"));

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
                    MessageBox.Show("Nieobsługiwany format PPM, oczekiwano P3 lub P6.");
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
                    MessageBox.Show($"Nieprawidłowy nagłówek PPM. Szerokość: {width}, Wysokość: {height}, MaxValue: {maxValue}");
                    return;
                }

                byte[] pixelData = new byte[width * height * 3];

                if (magicNumber == "P3")
                {
                    await ReadP3PixelDataAsync(br, pixelData, maxValue).ConfigureAwait(false);
                }
                else // Format P6
                {
                    await ReadP6PixelDataAsync(br, pixelData, width * height * 3).ConfigureAwait(false);
                }

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
            byte[] buffer = new byte[8192];
            StringBuilder stringBuilder = new StringBuilder();

            while (dataIndex < pixelData.Length)
            {
                int bytesRead = await br.BaseStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                stringBuilder.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

                string text = stringBuilder.ToString();
                int lastNewLine = text.LastIndexOf('\n');
                if (lastNewLine == -1)
                    continue;

                var lines = text.Substring(0, lastNewLine).Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                stringBuilder = new StringBuilder(text.Substring(lastNewLine + 1));

                foreach (string line in lines)
                {
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
                                return;
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
                    throw new EndOfStreamException("Nie udało się wczytać wystarczającej ilości danych pikselowych dla formatu P6.");
                }
                Array.Copy(buffer, 0, pixelData, bytesRead, read);
                bytesRead += read;
            }
        }
    }
}
