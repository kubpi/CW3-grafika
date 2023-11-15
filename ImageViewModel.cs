using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace CW3_grafika
{
    public class ImageViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool isPbmChecked;
        private bool isPgmChecked;
        private bool isPpmChecked;
        private BitmapSource convertedImage;

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
            LoadImageCommand = new RelayCommand(LoadImage);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadImage()
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
                    LoadPpmImage(filePath);
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




        private void LoadPpmImage(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                string magicNumber = ReadNextNonCommentLine(br).Trim();
                if (magicNumber != "P3" && magicNumber != "P6")
                {
                    MessageBox.Show("Unsupported PPM format, expected P3 or P6.");
                    return;
                }

                int width = 0, height = 0, maxValue = 0;
                bool headerParsed = false;

                // Continue reading lines until the header is fully parsed
                while (!headerParsed)
                {
                    string line = ReadNextNonCommentLine(br).Trim();
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
                    ReadP3PixelData(br, pixelData, maxValue);
                }
                else // P6 format
                {
                    // Skip any leftover whitespace and comments after the header
                    SkipWhitespaceAndComments(br);

                    // Read the binary pixel data directly
                    pixelData = br.ReadBytes(width * height * 3);
                }

                BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Rgb24, null, pixelData, width * 3);
                ConvertedPbmImage = bitmap;
            }
        }

        // Add additional methods as needed, such as ReadNextNonCommentLine, SkipWhitespaceAndComments, and ReadP3PixelData.
        private string ReadNextNonCommentLine(BinaryReader br)
        {
            StringBuilder line = new StringBuilder();
            while (br.BaseStream.Position != br.BaseStream.Length)
            {
                char c = (char)br.ReadByte();
                if (c == '\n' || c == '\r') // End of the line
                {
                    string result = line.ToString().Trim();
                    if (!string.IsNullOrEmpty(result) && !result.StartsWith("#")) // If it's not a comment line
                    {
                        return result;
                    }
                    line.Clear(); // Reset for the new line
                }
                else
                {
                    line.Append(c);
                }
            }
            return line.ToString().Trim(); // Return the last line if end of file is reached
        }

        private void SkipWhitespaceAndComments(BinaryReader br)
        {
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                char c = (char)br.PeekChar();
                if (char.IsWhiteSpace(c))
                {
                    br.ReadByte(); // Skip the whitespace
                }
                else if (c == '#')
                {
                    ReadNextNonCommentLine(br); // Skip the comment line
                }
                else
                {
                    break; // Start of pixel data
                }
            }
        }


        private void ReadP3PixelData(BinaryReader br, byte[] pixelData, int maxValue)
        {
            int index = 0;
            while (index < pixelData.Length)
            {
                string line = ReadNextNonCommentLine(br);
                var values = line.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var value in values)
                {
                    if (int.TryParse(value, out int pixelValue))
                    {
                        pixelData[index++] = (byte)((double)pixelValue / maxValue * 255);
                    }
                }
            }
        }





















        private byte ReadByte(StreamReader sr, int maxValue)
        {
            string[] values = sr.ReadLine().Split();
            foreach (var val in values)
            {
                if (int.TryParse(val, out int value))
                {
                    return (byte)(value * 255 / maxValue);
                }
            }
            return 0; // Or handle this case more appropriately
        }

        private byte[] ReadBinaryData(FileStream fs, int width, int height, int maxValue)
        {
            byte[] buffer = new byte[width * height * 3];
            fs.Read(buffer, 0, width * height * 3);

            if (maxValue == 255)
            {
                return buffer;
            }

            byte[] scaledData = new byte[buffer.Length];
            for (int i = 0; i < buffer.Length; i++)
            {
                scaledData[i] = (byte)(buffer[i] * 255 / maxValue);
            }

            return scaledData;
        }






    }
}
