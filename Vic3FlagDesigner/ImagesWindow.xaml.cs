using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        // New properties for the base/original colors present in the images.
        private Color _baseColor1;
        public Color BaseColor1
        {
            get => _baseColor1;
            set { _baseColor1 = value; OnPropertyChanged(nameof(BaseColor1)); }
        }

        private Color _baseColor2;
        public Color BaseColor2
        {
            get => _baseColor2;
            set { _baseColor2 = value; OnPropertyChanged(nameof(BaseColor2)); }
        }

        private Color _baseColor3;
        public Color BaseColor3
        {
            get => _baseColor3;
            set { _baseColor3 = value; OnPropertyChanged(nameof(BaseColor3)); }
        }

        // These are the user-selected new colors.
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

            // Set initial base and new colors depending on image type.
            if (string.Equals(ImageType, "emblem"))
            {
                // For emblem, set base colors to one set (e.g., dark blue, bright green, pinkish)
                BaseColor1 = Color.FromRgb(0, 0, 128);
                BaseColor2 = Color.FromRgb(0, 255, 128);
                BaseColor3 = Color.FromRgb(255, 0, 128);

                // Initialize new colors to the same values (user can change these later)
                Color1 = Color.FromRgb(0, 0, 128);
                Color2 = Color.FromRgb(0, 255, 128);
                Color3 = Color.FromRgb(255, 0, 128);
            }
            else
            {
                // For background images, set base colors to the defaults (red, yellow, white)
                BaseColor1 = Colors.Red;
                BaseColor2 = Colors.Yellow;
                BaseColor3 = Colors.White;

                // Initialize new colors to the same values (user can change these later)
                Color1 = Colors.Red;
                Color2 = Colors.Yellow;
                Color3 = Colors.White;
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
                // Update the image's color properties so that when added to the main window,
                // the shader effect could be bypassed for a final export if needed.
                SelectedImage.Color1 = Color1;
                SelectedImage.Color2 = Color2;
                SelectedImage.Color3 = Color3;
                if (string.Equals(ImageType, "emblem"))
                {
                    SelectedImage.X = 384;
                    SelectedImage.Y = 256;
                    SelectedImage.IsEmblem = true;
                    _mainWindow.AddImageToMainWindow(SelectedImage);
                }
                else
                {
                    _mainWindow.AddBackgroundImageToMainWindow(SelectedImage);
                }
                //Close();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// When a color picker is closed, update its binding.
        /// The shader effect in the DataTemplate (via XAML) binds to BaseColor and Color properties.
        /// </summary>
        private void ColorPicker_Closed(object sender, RoutedEventArgs e)
        {
            var colorPicker = (ColorPicker)sender;
            colorPicker.GetBindingExpression(ColorPicker.SelectedColorProperty)?.UpdateSource();
        }
    }
}
