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

        private Color selectedColorForColor1 = Colors.Red;      // Default replacement color
        private Color selectedColorForColor2 = Colors.Yellow;
        private Color selectedColorForColor3 = Colors.White;
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
                selectedColorForColor1 = ColorPicker1.SelectedColor ?? Color.FromRgb(0, 0, 128);
                selectedColorForColor2 = ColorPicker2.SelectedColor ?? Color.FromRgb(0, 255, 128);
                selectedColorForColor3 = ColorPicker3.SelectedColor ?? Color.FromRgb(255, 0, 128);
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
                SelectedImage.Color1 = selectedColorForColor1;
                SelectedImage.Color2 = selectedColorForColor2; 
                SelectedImage.Color3 = selectedColorForColor3;
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

            // Capture colors on UI thread before processing
            var color1 = ColorPicker1.SelectedColor ?? Colors.Red;
            var color2 = ColorPicker2.SelectedColor ?? Colors.Yellow;
            var color3 = ColorPicker3.SelectedColor ?? Colors.White;

            if (string.Equals(ImageType, "emblem"))
            {
                color1 = ColorPicker1.SelectedColor ?? Color.FromRgb(0, 0, 128);
                color2 = ColorPicker2.SelectedColor ?? Color.FromRgb(0, 255, 128);
                color3 = ColorPicker3.SelectedColor ?? Color.FromRgb(255, 0, 128);
            }

            int totalImages = FolderImages.Count;
            int processedImages = 0;

            for (int i = 0; i < totalImages; i++)
            {
                int currentIndex = i;
                var imageSource = FolderImages[currentIndex].ImageSource;
                string ImageFilePath = FolderImages[currentIndex].ImagePath;

                // Pass colors to processing method
                var processedImage = await Task.Run(() =>
                    ImageColorProcessor.ApplyColorReplacement(imageSource, color1, color2, color3, selectedColorForColor1, selectedColorForColor2, selectedColorForColor3));

                FolderImages[currentIndex] = new ImageData { ImageSource = processedImage, ImagePath = ImageFilePath};
                processedImages++;
                ProcessingProgressBar.Value = (processedImages / (double)totalImages) * 100;
            }

            FolderImageList.Items.Refresh();
            ProcessingProgressBar.Visibility = Visibility.Collapsed;

            if (ColorPickerObj == ColorPicker1)
                selectedColorForColor1 = ColorPicker1.SelectedColor ?? Colors.Red;
            else if (ColorPickerObj == ColorPicker2)
                selectedColorForColor2 = ColorPicker2.SelectedColor ?? Colors.Yellow;
            else if (ColorPickerObj == ColorPicker3)
                selectedColorForColor3 = ColorPicker3.SelectedColor ?? Colors.White;
        }
    }
}