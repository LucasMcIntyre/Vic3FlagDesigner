using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;

namespace Vic3FlagDesigner
{
    public partial class ImagesWindow : Window, INotifyPropertyChanged
    {
        public ObservableCollection<ImageData> FolderImages { get; set; }
        public ImageData SelectedImage { get; set; }

        private int _gridColumns;
        private string ImageType = string.Empty;

        private Color OriginalColor1 = Colors.Red;      // Default replacement color
        private Color OriginalColor2 = Colors.Yellow;
        private Color OriginalColor3 = Colors.White;

        private Color? color1;
        public Color? Color1
        {
            get => color1;
            set
            {
                color1 = value;
                OnPropertyChanged(nameof(Color1));
            }
        }

        private Color? color2;
        public Color? Color2
        {
            get => color2;
            set
            {
                color2 = value;
                OnPropertyChanged(nameof(Color2));
            }
        }

        private Color? color3;
        public Color? Color3
        {
            get => color3;
            set
            {
                color3 = value;
                OnPropertyChanged(nameof(Color3));
            }
        }
        public int GridColumns
        {
            get => _gridColumns;
            set
            {
                _gridColumns = value;
                OnPropertyChanged(nameof(GridColumns));
            }
        }

        private MainWindow _mainWindow;

        public ImagesWindow(ObservableCollection<ImageData> images, MainWindow mainWindow, string SentImageType)
        {
            InitializeComponent();
            FolderImages = images;
            _mainWindow = mainWindow;
            ImageType = SentImageType;
            DataContext = this;
            UpdateGridColumns();  // Set initial column count

            this.SizeChanged += (s, e) => UpdateGridColumns(); // Update when resized

            if (string.Equals(ImageType, "emblem"))
            {
                OriginalColor1 = Color.FromRgb(0, 0, 128);
                OriginalColor2 = Color.FromRgb(0, 255, 128);
                OriginalColor3 = Color.FromRgb(255, 0, 128);
                color1 = Color.FromRgb(0, 0, 128);
                color2 = Color.FromRgb(0, 255, 128);
                color3 = Color.FromRgb(255, 0, 128);
            }
            else
            {
                OriginalColor1 = Colors.Red;
                OriginalColor2 = Colors.Yellow;
                OriginalColor3 = Colors.White;
                color1 = Colors.Red;
                color2 = Colors.Yellow;
                color3 = Colors.White;
            }
        }

        private void UpdateGridColumns()
        {
            GridColumns = Math.Max((int)(this.ActualWidth / 200), 1); // Ensure at least 1 column
        }

        private void Image_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedImage != null)
            {
                SelectedImage.Color1 = color1;
                SelectedImage.Color2 = color2; 
                SelectedImage.Color3 = color3;
                if (string.Equals(ImageType, "emblem"))
                {
                    SelectedImage.X = 384; SelectedImage.Y = 256; SelectedImage.IsEmblem = true;
                    _mainWindow.AddImageToMainWindow(SelectedImage);
                }
                else
                {
                    _mainWindow.AddBackgroundImageToMainWindow(SelectedImage);
                }
                Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ColorPicker_Closed(object sender, RoutedEventArgs e)
        {
            var colorPicker = (Xceed.Wpf.Toolkit.ColorPicker)sender;
            colorPicker.GetBindingExpression(Xceed.Wpf.Toolkit.ColorPicker.SelectedColorProperty)?.UpdateSource();

            ApplyColorReplacementToAllBackgrounds(sender);
        }

        private async void ApplyColorReplacementToAllBackgrounds(object ColorPickerObj)
        {
            ProcessingProgressBar.Visibility = Visibility.Visible;
            ProcessingProgressBar.Value = 0;

            int totalImages = FolderImages.Count;
            int processedImages = 0;

            for (int i = 0; i < totalImages; i++)
            {
                int currentIndex = i;
                var imageSource = FolderImages[currentIndex].ImageSource;
                string ImageFilePath = FolderImages[currentIndex].ImagePath;

                // Pass colors to processing method
                var processedImage = await Task.Run(() =>
                    ImageColorProcessor.ApplyColorReplacement(imageSource, color1 ?? Colors.Red, color2 ?? Colors.Yellow, color3 ?? Colors.White, OriginalColor1, OriginalColor2, OriginalColor3));

                FolderImages[currentIndex] = new ImageData { ImageSource = processedImage, ImagePath = ImageFilePath};
                processedImages++;
                ProcessingProgressBar.Value = (processedImages / (double)totalImages) * 100;
            }

            FolderImageList.Items.Refresh();
            ProcessingProgressBar.Visibility = Visibility.Collapsed;

            if (ColorPickerObj == ColorPicker1)
                OriginalColor1 = ColorPicker1.SelectedColor ?? Colors.Red;
            else if (ColorPickerObj == ColorPicker2)
                OriginalColor2 = ColorPicker2.SelectedColor ?? Colors.Yellow;
            else if (ColorPickerObj == ColorPicker3)
                OriginalColor3 = ColorPicker3.SelectedColor ?? Colors.White;
        }
    }
}