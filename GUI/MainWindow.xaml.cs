using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ChromeAppCreator.Annotations;
using ChromeAppCreator.Logic;
using Microsoft.Win32;
using Newtonsoft.Json;
using Image = System.Drawing.Image;
using Path = System.IO.Path;

namespace ChromeAppCreator.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            _baseTitle = Title;
        }

        public IconEntryImageConverter ImagePathConverter { get; set; } = new IconEntryImageConverter();

        private static readonly int[] IconSizes = {16, 32, 48, 128};

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static string BaseDirectory = "";

        private readonly string _baseTitle;
        private string _fileName;
        private string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                BaseDirectory = Path.GetDirectoryName(_fileName);
                Title = _baseTitle + (_fileName != null ? " | " + _fileName : "");
            }
        }

        private Manifest _manifest = new Manifest();
        private Manifest Manifest
        {
            get { return _manifest; }
            set
            {
                _manifest = value;
                OnPropertyChanged(nameof(MName));
                OnPropertyChanged(nameof(MDescription));
                OnPropertyChanged(nameof(MVersion));
                OnPropertyChanged(nameof(MWebUrl));

                try
                {
                    var maxSize = _manifest.Icons.Max(item => item.Size);
                    var maxIcon = _manifest.Icons.First(item => item.Size == maxSize);
                    MIcon = maxIcon.File;
                }
                catch
                {
                    MIcon = "";
                }
                
                OnPropertyChanged(nameof(MIcon));
            }
        }

        public string MName
        {
            get { return _manifest.Name; }
            set
            {
                _manifest.Name = value;
                OnPropertyChanged();
            }
        }

        public string MDescription
        {
            get { return _manifest.Description; }
            set
            {
                _manifest.Description = value;
                OnPropertyChanged();
            }
        }

        public string MVersion
        {
            get { return _manifest.Version; }
            set
            {
                _manifest.Version = value;
                OnPropertyChanged();
            }
        }

        public string MWebUrl
        {
            get { return _manifest.WebUrl; }
            set
            {
                _manifest.WebUrl = value;
                OnPropertyChanged();
            }
        }

        private string _icon;
        public string MIcon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        private void MenuItem_New_Clicked(object sender, RoutedEventArgs e)
        {
            FileName = null;
            Manifest = new Manifest();
            MIcon = "";
        }

        private void MenuItem_Open_Clicked(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "Manifest File (*.json)|manifest.json" };
            if (openFileDialog.ShowDialog() == true)
            {
                FileName = openFileDialog.FileName;

                using (var file = new JsonTextReader(File.OpenText(FileName)))
                {
                    var serializer = new JsonSerializer { Formatting = Formatting.Indented };
                    Manifest = serializer.Deserialize<Manifest>(file);
                }
            }
        }

        private void SaveFile()
        {
            var path = Path.GetDirectoryName(FileName);

            if (Path.IsPathRooted(MIcon))
            {
                _manifest.Icons.Clear();

                var image = Image.FromFile(MIcon);

                foreach (var iconSize in IconSizes)
                {
                    var fileName = iconSize.ToString() + ".png";
                    var resizedImage = ResizeImage(image, iconSize, iconSize);
                    resizedImage.Save(path + Path.DirectorySeparatorChar + fileName);
                    _manifest.Icons.Add(new IconEntry(iconSize, fileName));
                }
            }

            using (var file = File.CreateText(FileName))
            {
                var serializer = new JsonSerializer { Formatting = Formatting.Indented };
                serializer.Serialize(file, Manifest);
            }
        }

        private void MenuItem_Save_Clicked(object sender, RoutedEventArgs e)
        {
            var fileName = FileName;

            if (fileName == null)
            {
                var saveFileDialog = new SaveFileDialog { Filter = "Manifest File (*.json)|manifest.json" };
                if (saveFileDialog.ShowDialog() == true)
                {
                    FileName = saveFileDialog.FileName;
                    fileName = saveFileDialog.FileName;
                }
                else
                    return;
            }

            SaveFile();
        }

        private void MenuItem_SaveAs_Clicked(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog { Filter = "Manifest File (*.json)|manifest.json" };

            if (saveFileDialog.ShowDialog() == true)
            {
                MIcon = Path.GetDirectoryName(FileName) + Path.DirectorySeparatorChar + MIcon;
                FileName = saveFileDialog.FileName;
                SaveFile();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Button_ChangeImage_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "PNG Files (*.png) | *.png" };
            if (openFileDialog.ShowDialog() == true)
            {
                MIcon = openFileDialog.FileName;
            }
        }
    }

    public class IconEntryImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string path = (string)value;

            try
            {
                if (Path.IsPathRooted(path))
                    return new BitmapImage(new Uri(path));

                //RELATIVE
                return new BitmapImage(new Uri(MainWindow.BaseDirectory + Path.DirectorySeparatorChar + path));
            }
            catch (Exception)
            {
                return new BitmapImage();
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
