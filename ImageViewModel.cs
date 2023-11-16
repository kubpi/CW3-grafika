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
        public RelayCommand SaveImageCommand { get; private set; }

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
            SaveImageCommand = new RelayCommand(async () => await SaveImageAsync());
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

        private async Task SaveImageAsync()
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "PBM Files (*.pbm)|*.pbm|PGM Files (*.pgm)|*.pgm|PPM Files (*.ppm)|*.ppm|All Files (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string fileExtension = Path.GetExtension(filePath).ToLower();
                bool isBinary = MessageBox.Show("Czy chcesz zapisać w formacie binarnym?", "Format zapisu", MessageBoxButton.YesNo) == MessageBoxResult.Yes;

                if (fileExtension == ".pbm")
                {
                    await SavePbmImageAsync(filePath, isBinary);
                }
                else if (fileExtension == ".pgm")
                {
                    await SavePgmImageAsync(filePath, isBinary);
                }
                else if (fileExtension == ".ppm")
                {
                    await SavePpmImageAsync(filePath, isBinary);
                }
                else
                {
                    MessageBox.Show("Nieobsługiwany format pliku.");
                }
            }
        }

        private async Task SavePbmImageAsync(string filePath, bool isBinary)
        {
            byte[] pixelData = GetPixelDataFromBitmapSource(); // Konwersja BitmapSource na tablicę bajtów

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                if (isBinary)
                {
                    // Nagłówek P4
                    await fs.WriteAsync(Encoding.ASCII.GetBytes("P4\n"));
                    await fs.WriteAsync(Encoding.ASCII.GetBytes($"{convertedImage.PixelWidth} {convertedImage.PixelHeight}\n"));

                    int rowLength = (convertedImage.PixelWidth + 7) / 8; // Dopełnienie do pełnych bajtów
                    byte[] binaryData = new byte[rowLength * convertedImage.PixelHeight];

                    for (int y = 0; y < convertedImage.PixelHeight; y++)
                    {
                        for (int x = 0; x < convertedImage.PixelWidth; x++)
                        {
                            int byteIndex = y * rowLength + x / 8;
                            int bitIndex = 7 - (x % 8);
                            // Ustaw bit na 1 jeśli piksel jest czarny (wartość większa od zera)
                            if (pixelData[y * convertedImage.PixelWidth + x] > 0)
                            {
                                binaryData[byteIndex] |= (byte)(1 << bitIndex);
                            }
                            // Bity są domyślnie ustawione na 0 (białe piksele), więc nie trzeba nic robić
                        }
                    }

                    await fs.WriteAsync(binaryData, 0, binaryData.Length);
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("P1");
                        sw.WriteLine($"{convertedImage.PixelWidth} {convertedImage.PixelHeight}");

                        for (int y = 0; y < convertedImage.PixelHeight; y++)
                        {
                            for (int x = 0; x < convertedImage.PixelWidth; x++)
                            {
                                if (x > 0 && x % convertedImage.PixelWidth == 0)
                                {
                                    sw.WriteLine();
                                }
                                sw.Write(pixelData[y * convertedImage.PixelWidth + x] == 0 ? "0 " : "1 ");
                            }
                            sw.WriteLine();
                        }
                    }
                }
            }
        }

        private async Task SavePgmImageAsync(string filePath, bool isBinary)
        {
            byte[] pixelData = GetPixelDataFromBitmapSource();

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                if (isBinary)
                {
                    // Zapisz nagłówek w formacie tekstowym
                    await fs.WriteAsync(Encoding.ASCII.GetBytes("P5\n"));
                    await fs.WriteAsync(Encoding.ASCII.GetBytes($"{convertedImage.PixelWidth} {convertedImage.PixelHeight}\n"));
                    await fs.WriteAsync(Encoding.ASCII.GetBytes("255\n"));

                    // Zapisz dane pikselowe w formacie binarnym
                    await fs.WriteAsync(pixelData, 0, pixelData.Length);
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("P2");
                        sw.WriteLine($"{convertedImage.PixelWidth} {convertedImage.PixelHeight}");
                        sw.WriteLine("255");

                        for (int i = 0; i < pixelData.Length; i++)
                        {
                            if (i % convertedImage.PixelWidth == 0 && i != 0)
                            {
                                sw.WriteLine();
                            }
                            sw.Write($"{pixelData[i]} ");
                        }
                    }
                }
            }
        }

        private async Task SavePpmImageAsync(string filePath, bool isBinary)
        {
            byte[] pixelData = GetPixelDataFromBitmapSource(); // Ta funkcja konwertuje BitmapSource na tablicę bajtów

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                if (isBinary)
                {
                    await fs.WriteAsync(Encoding.ASCII.GetBytes("P6\n"));
                    await fs.WriteAsync(Encoding.ASCII.GetBytes($"{convertedImage.PixelWidth} {convertedImage.PixelHeight}\n"));
                    await fs.WriteAsync(Encoding.ASCII.GetBytes("255\n"));
                    await fs.WriteAsync(pixelData, 0, pixelData.Length);
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.WriteLine("P3");
                        sw.WriteLine($"{convertedImage.PixelWidth} {convertedImage.PixelHeight}");
                        sw.WriteLine("255");

                        int pixelCount = 0;
                        for (int i = 0; i < pixelData.Length; i += 3)
                        {
                            sw.Write($"{pixelData[i]} {pixelData[i + 1]} {pixelData[i + 2]} ");
                            pixelCount++;

                            if (pixelCount % convertedImage.PixelWidth == 0)
                            {
                                sw.WriteLine();
                            }
                        }
                    }
                }
            }
        }


        private byte[] GetPixelDataFromBitmapSource()
        {
            if (convertedImage == null)
                throw new InvalidOperationException("BitmapSource is null");

            int stride = (convertedImage.PixelWidth * convertedImage.Format.BitsPerPixel + 7) / 8;
            byte[] pixelData = new byte[convertedImage.PixelHeight * stride];
            convertedImage.CopyPixels(pixelData, stride, 0);

            return pixelData;
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
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    string magicNumber = new string(br.ReadChars(2));
                    br.ReadChar(); // Read the newline character following the magic number

                    // Skip any comment lines
                    while (br.PeekChar() == '#')
                    {
                        while (br.ReadChar() != '\n') ; // Read to the end of the line
                    }

                    int width = ReadNextValue(br);
                    int height = ReadNextValue(br);

                    byte[] pixelData = new byte[width * height];
                    if (magicNumber == "P4")
                    {
                        // Read the pixel data for P4 format
                        int rowLength = (width + 7) / 8;
                        for (int y = 0; y < height; y++)
                        {
                            byte[] row = br.ReadBytes(rowLength);
                            for (int x = 0; x < width; x++)
                            {
                                int byteIndex = x / 8;
                                int bitIndex = 7 - (x % 8);
                                // Set the pixel value based on the bit at the bitIndex
                                pixelData[y * width + x] = (row[byteIndex] & (1 << bitIndex)) != 0 ? (byte)255 : (byte)0;
                            }
                        }
                    }
                    else if (magicNumber == "P1")
                    {
                        // Skip whitespaces if any before reading the pixel values
                        while (br.PeekChar() == ' ' || br.PeekChar() == '\n' || br.PeekChar() == '\r')
                        {
                            br.ReadChar(); // Read and ignore the whitespace
                        }

                        int pixelIndex = 0;
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                char pixelChar = br.ReadChar();
                                while (pixelChar != '0' && pixelChar != '1')
                                {
                                    // Skip any non-digit characters
                                    pixelChar = br.ReadChar();
                                }
                                pixelData[pixelIndex++] = pixelChar == '1' ? (byte)255 : (byte)0;
                            }
                        }
                    }

                    else
                    {
                        MessageBox.Show("Nieobsługiwany format PBM.");
                        return;
                    }

                    // Create the bitmap
                    BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixelData, width);
                    ConvertedPbmImage = bitmap; // Assuming ConvertedPbmImage is a property that will refresh the UI
                }
            }
        }

        private int ReadNextValue(BinaryReader br)
        {
            string numberString = "";
            char nextChar;
            // Read until a digit is found
            while (!char.IsDigit(nextChar = br.ReadChar())) ;
            numberString += nextChar;
            // Read until a non-digit is found
            while (char.IsDigit(nextChar = br.ReadChar()))
            {
                numberString += nextChar;
            }
            return int.Parse(numberString);
        }


        /*private void LoadPbmImage(string filePath)
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
                        // Przewiń strumień, aby ominąć nagłówek i dotrzeć do danych pikselowych
                        fs.Seek(0, SeekOrigin.Begin);  // Wróć na początek pliku
                        sr.ReadLine();  // Przeczytaj linijkę z magicNumber jeszcze raz
                        sr.ReadLine();  // Przeczytaj linijkę z wymiarami jeszcze raz

                        // Czytanie danych binarnych pikseli
                        int bufferSize = (width + 7) / 8 * height; // Oblicz rozmiar bufora danych pikselowych
                        byte[] buffer = new byte[bufferSize];
                        fs.Read(buffer, 0, bufferSize); // Czytaj dane pikselowe bezpośrednio z FileStream

                        for (int i = 0; i < width * height; i++)
                        {
                            int byteIndex = i / 8;
                            int bitIndex = 7 - (i % 8);
                            byte pixelValue = (byte)((buffer[byteIndex] >> bitIndex) & 0x01);
                            pixelData[i] = (byte)(pixelValue * 255);
                        }

                        // Reszta kodu bez zmian...
                    }


                    BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixelData, width);
                    ConvertedPbmImage = bitmap;
                }
                else
                {
                    MessageBox.Show("Nieobsługiwany format PBM.");
                }
            }
        }*/

        private void LoadPgmImage(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open))
            using (BinaryReader br = new BinaryReader(fs))
            {
                // Read magic number (P2 or P5)
                string magicNumber = new string(br.ReadChars(2));
                br.ReadChar(); // Read the newline character following the magic number

                // Skip any comment lines
                while (br.PeekChar() == '#')
                {
                    while (br.ReadChar() != '\n') ; // Read to the end of the line
                }

                // Read width, height and max value
                int width = ReadNextValue(br);
                int height = ReadNextValue(br);
                int maxValue = ReadNextValue(br);

                byte[] pixelData = new byte[width * height];

                if (magicNumber == "P5")
                {
                    // Read the pixel data for P5 format
                    for (int i = 0; i < pixelData.Length; i++)
                    {
                        pixelData[i] = br.ReadByte();
                    }
                }
                else if (magicNumber == "P2")
                {
                    // Read and convert the pixel data for P2 format
                    for (int i = 0; i < pixelData.Length; i++)
                    {
                        int pixelValue = ReadNextValue(br);
                        pixelData[i] = (byte)(pixelValue * 255 / maxValue);
                    }
                }
                else
                {
                    MessageBox.Show("Nieobsługiwany format PGM.");
                    return;
                }

                // Create BitmapSource from pixel data
                BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixelData, width);
                ConvertedPbmImage = bitmap; // Assuming ConvertedPbmImage is a property that will refresh the UI
            }
        }

        /*private int ReadNextValue(BinaryReader br)
        {
            StringBuilder numberStringBuilder = new StringBuilder();
            char nextChar;
            // Skip any whitespaces or newlines
            while (char.IsWhiteSpace(nextChar = br.ReadChar())) ;

            numberStringBuilder.Append(nextChar);
            // Read until next whitespace
            while (!char.IsWhiteSpace(nextChar = br.ReadChar()))
            {
                numberStringBuilder.Append(nextChar);
            }

            if (int.TryParse(numberStringBuilder.ToString(), out int result))
            {
                return result;
            }
            else
            {
                throw new InvalidDataException("Invalid number format in PGM file.");
            }
        }*/


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
