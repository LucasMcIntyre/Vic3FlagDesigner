using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using ImageMagick;
using WpfApp3;
using System.Threading;
using System.Text.Json;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;

namespace Vic3FlagDesigner
{
    public partial class MainWindow : Window
    {
        private Image? selectedAddonImage = null;

        public ObservableCollection<ImageData> FolderImages { get; set; } = new();
        public ObservableCollection<ImageData> UserImages { get; set; } = new();

        private Image baseLayerImage; 
        private ImageData baseLayerData;

        private Color BackgroundColor1 = Colors.Red;
        private Color BackgroundColor2 = Colors.Yellow;
        private Color BackgroundColor3 = Colors.White;

        private CancellationTokenSource? _cancellationTokenSource;

        private const string SettingsFile = "settings.json";
        private SettingsData DesignerSettingsData;

        public MainWindow()
        {
            InitializeComponent();

            SetSettings();
            UserImageList.ItemsSource = UserImages;
            UserImageList.ItemTemplate = CreateImageTemplate();
            UserImageList.HorizontalContentAlignment = HorizontalAlignment.Center;
            // Attach selection changed event
            UserImageList.SelectionChanged += UserImageList_SelectionChanged;
        }

        private DataTemplate CreateImageTemplate()
        {
            DataTemplate template = new DataTemplate(typeof(ImageData));
            FrameworkElementFactory imageFactory = new FrameworkElementFactory(typeof(Image));
            imageFactory.SetValue(Image.WidthProperty, 192.0);
            imageFactory.SetValue(Image.HeightProperty, 128.0);
            imageFactory.SetValue(Image.StretchProperty, Stretch.UniformToFill);
            imageFactory.SetBinding(Image.SourceProperty, new System.Windows.Data.Binding("ImageSource"));
            template.VisualTree = imageFactory;
            return template;
        }

        public void SetSettings()
        {
            if (File.Exists(SettingsFile))
            {
                DesignerSettingsData = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(SettingsFile));
            }
            else
            {
                DesignerSettingsData = new SettingsData();
            }
        }


        private void LoadFolder_Click(object sender, RoutedEventArgs e)
        {
            OpenImagesWindow("pattern");
        }

        private void AddEmblem_Click(object sender, RoutedEventArgs e)
        {
            OpenImagesWindow("emblem");
        }

        private void OpenImagesWindow(string imageType)
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (string.Equals(imageType, "emblem"))
            {
                if (!string.IsNullOrEmpty(DesignerSettingsData.EmblemFolder) && Directory.Exists(DesignerSettingsData.EmblemFolder))
                {
                    dialog.SelectedPath = DesignerSettingsData.EmblemFolder;
                }
            }
            else if (string.Equals(imageType, "pattern"))
            {
                if (!string.IsNullOrEmpty(DesignerSettingsData.PatternFolder) && Directory.Exists(DesignerSettingsData.PatternFolder))
                {
                    dialog.SelectedPath = DesignerSettingsData.PatternFolder;
                }
            }
            if (dialog.ShowDialog() == true)
            {
                var images = new ObservableCollection<ImageData>();
                var files = Directory.GetFiles(dialog.SelectedPath, "*.dds", SearchOption.TopDirectoryOnly);

                // Add pattern_solid.dds from the exe folder if imageType is "pattern"
                if (string.Equals(imageType, "pattern"))
                {
                    string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string patternFilePath = Path.Combine(exeDirectory, "pattern_solid.dds");

                    if (File.Exists(patternFilePath))
                    {
                        var patternImage = new ImageData
                        {
                            ImageSource = LoadDDS(patternFilePath),
                            ImagePath = patternFilePath
                        };

                        images.Add(patternImage);
                    }
                }
                _cancellationTokenSource = new CancellationTokenSource();
                var loadingWindow = new LoadingWindow();
                loadingWindow.Show();

                Task.Run(() => LoadImagesParallel(files, images, _cancellationTokenSource.Token, loadingWindow))
                    .ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            loadingWindow.Close();
                            if (!_cancellationTokenSource.Token.IsCancellationRequested)
                            {
                                var window = new ImagesWindow(images, this, imageType);
                                window.Show();
                            }
                        });
                    }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }


        private void LoadImagesParallel(string[] files, ObservableCollection<ImageData> images, CancellationToken token, LoadingWindow loadingWindow)
        {
            int total = files.Length;
            int processed = 0;

            Parallel.ForEach(files, (file, state) =>
            {
                if (token.IsCancellationRequested)
                {
                    state.Break();
                    return;
                }

                BitmapImage? image = LoadDDS(file);
                if (image != null && image.PixelWidth == 768 && image.PixelHeight == 512)
                {
                    Dispatcher.Invoke(() => images.Add(new ImageData { ImageSource = image, ImagePath = file, X=384, Y=256 }));
                }

                Interlocked.Increment(ref processed);
                Dispatcher.Invoke(() => loadingWindow.UpdateProgress(processed, total));
            });
        }

        private BitmapImage LoadDDS(string filePath)
        {
            using (var image = new MagickImage(filePath))
            {
                // Ensure 32-bit RGBA format to preserve colors
                image.Depth = 8;
                image.Alpha(AlphaOption.On);

                MorphologySettings morphologySettings = new MorphologySettings();
                morphologySettings.Kernel = Kernel.Diamond;
                morphologySettings.Method = MorphologyMethod.Dilate;
                morphologySettings.Iterations = 1;

                // Apply Dilation to slightly expand colors
                image.Morphology(morphologySettings);

                morphologySettings.Method = MorphologyMethod.Erode;
                // Apply Erosion to restore edges
                image.Morphology(morphologySettings);

                return ConvertToBitmapImage(image);
            }
        }

        private BitmapImage ConvertToBitmapImage(MagickImage image)
        {
            using (var stream = new MemoryStream())
            {
                image.Write(stream, MagickFormat.Png); // Convert to PNG (lossless)
                stream.Position = 0;

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze(); // Allow UI thread access

                return bitmap;
            }
        }

        public void AddBackgroundImageToMainWindow(ImageData image)
        {
            if (image != null)
            {
                SetBaseLayerImage(image);
                BackgroundColor1 = image.Color1 ?? Colors.Red;
                BackgroundColor2 = image.Color2 ?? Colors.Yellow;
                BackgroundColor3 = image.Color3 ?? Colors.White;
            }
        }

        public void AddImageToMainWindow(ImageData image)
        {
            if (image != null)
            {
                UserImages.Insert(0, image);
                AddOverlayImage(image);
                RebuildCanvasLayers();
            }
        }

        private void SetBaseLayerImage(ImageData imageData)
        {
            ImageCanvas.Children.Clear(); // Remove existing layers
            baseLayerData = imageData;

            baseLayerImage = new Image
            {
                Source = imageData.ImageSource,
                Width = 768,
                Height = 512,
                Stretch = Stretch.Fill // Ensure full coverage
            };

            Canvas.SetLeft(baseLayerImage, 0);
            Canvas.SetTop(baseLayerImage, 0);

            RebuildCanvasLayers();
        }

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.dds",
                Multiselect = true
            };

            if (!string.IsNullOrEmpty(DesignerSettingsData.TextureFolder) && Directory.Exists(DesignerSettingsData.TextureFolder))
            {
                openDialog.InitialDirectory = DesignerSettingsData.TextureFolder;
            }

            if (openDialog.ShowDialog() == true)
            {
                foreach (string filename in openDialog.FileNames.Reverse())
                {
                    BitmapImage newImage;

                    if (Path.GetExtension(filename).Equals(".dds", StringComparison.OrdinalIgnoreCase))
                    {
                        newImage = LoadDDS(filename);
                    }
                    else
                    {
                        newImage = new BitmapImage(new Uri(filename));
                    }

                    // Get actual image dimensions
                    int imgWidth = newImage.PixelWidth;
                    int imgHeight = newImage.PixelHeight;

                    // Validate image size
                    if (imgWidth > 1536 || imgHeight > 1024)
                    {
                        string errorMessage = $"Image '{Path.GetFileName(filename)}' is too large!\n" +
                                              $"Size: {imgWidth}x{imgHeight}px\n" +
                                              $"Max allowed: 1536x1024px.";
                        MessageBox.Show(errorMessage, "Image Size Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        continue; // Skip this image
                    }

                    // Initialize the image data with original size
                    var imageData = new ImageData
                    {
                        ImageSource = newImage,
                        ImagePath = filename,
                        X = 384, // Keep centered
                        Y = 256,
                        IsEmblem = false
                    };

                    UserImages.Insert(0, imageData);
                    AddOverlayImage(imageData);
                }

                RebuildCanvasLayers();
            }
        }

        private void UserImageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UserImageList.SelectedItem is ImageData selectedImageData)
            {
                // Find the matching Image element in the canvas
                foreach (UIElement element in ImageCanvas.Children)
                {
                    if (element is Image image && image.Source == selectedImageData.ImageSource)
                    {
                        HighlightSelectedImage(image, selectedImageData);
                        return;
                    }
                }
            }
            selectedAddonImage = null; // No image selected
        }


        private void HighlightSelectedImage(Image image, ImageData imageData)
        {
            selectedAddonImage = image;

            // Update sliders with stored position, scale, and rotation values
            SliderX.Value = imageData.X;
            SliderY.Value = imageData.Y;
            SliderScaleX.Value = imageData.ScaleX;
            SliderScaleY.Value = imageData.ScaleY;
            SliderRotation.Value = imageData.Rotation;
        }

        private void SliderX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedAddonImage != null)
            {
                var imageData = UserImages.FirstOrDefault(img => img.ImageSource == selectedAddonImage.Source);
                if (imageData != null)
                {
                    imageData.X = e.NewValue;
                    Canvas.SetLeft(selectedAddonImage, e.NewValue - (selectedAddonImage.Width / 2));
                }
            }
        }

        private void SliderY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedAddonImage != null)
            {
                var imageData = UserImages.FirstOrDefault(img => img.ImageSource == selectedAddonImage.Source);
                if (imageData != null)
                {
                    imageData.Y = e.NewValue;
                    Canvas.SetTop(selectedAddonImage, e.NewValue - (selectedAddonImage.Height / 2));
                }
            }
        }

        private void SliderScaleX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedAddonImage != null)
            {
                var imageData = UserImages.FirstOrDefault(img => img.ImageSource == selectedAddonImage.Source);
                if (imageData != null)
                {
                    imageData.ScaleX = e.NewValue;
                    ApplyTransform(selectedAddonImage, imageData);
                }
            }
        }

        private void SliderScaleY_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedAddonImage != null)
            {
                var imageData = UserImages.FirstOrDefault(img => img.ImageSource == selectedAddonImage.Source);
                if (imageData != null)
                {
                    imageData.ScaleY = e.NewValue;
                    ApplyTransform(selectedAddonImage, imageData);
                }
            }
        }

        private void SliderRotation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selectedAddonImage != null)
            {
                var imageData = UserImages.FirstOrDefault(img => img.ImageSource == selectedAddonImage.Source);
                if (imageData != null)
                {
                    imageData.Rotation = e.NewValue;
                    ApplyTransform(selectedAddonImage, imageData);
                }
            }
        }

        private void ApplyTransform(Image image, ImageData imageData)
        {
            TransformGroup transformGroup = new TransformGroup();

            // Apply scaling (must be applied first)
            ScaleTransform scaleTransform = new ScaleTransform(imageData.ScaleX, imageData.ScaleY, image.Width / 2, image.Height / 2);
            transformGroup.Children.Add(scaleTransform);

            // Apply rotation (centered)
            RotateTransform rotateTransform = new RotateTransform(imageData.Rotation, image.Width / 2, image.Height / 2);
            transformGroup.Children.Add(rotateTransform);

            // Apply transformation group
            image.RenderTransform = transformGroup;
        }

        private void RebuildCanvasLayers()
        {
            ImageCanvas.Children.Clear(); // Clear existing layers

            // Add the background image first (if set)
            if (baseLayerImage != null)
            {
                ImageCanvas.Children.Add(baseLayerImage);
            }

            // Add overlay images while keeping their saved positions and scales
            foreach (var imageData in UserImages)
            {
                AddOverlayImage(imageData);
            }
        }


        private void AddOverlayImage(ImageData imageData)
        {
            Image overlay = new Image
            {
                Source = imageData.ImageSource,
                Stretch = Stretch.None, // Prevent any scaling
                Width = imageData.ImageSource.Width,
                Height = imageData.ImageSource.Height
            };

            // Set the position based on the center of the image
            Canvas.SetLeft(overlay, imageData.X - (overlay.Width / 2));
            Canvas.SetTop(overlay, imageData.Y - (overlay.Height / 2));

            // Apply stored scale & rotation
            ApplyTransform(overlay, imageData);

            // Enable selection and modification
            overlay.MouseDown += (s, e) => HighlightSelectedImage(overlay, imageData);

            ImageCanvas.Children.Add(overlay);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var window = new SettingsWindow(this);
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Settings: {ex.Message}", "Settings", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Project Files (*.json)|*.json"
            };
            if (!string.IsNullOrEmpty(DesignerSettingsData.ModFolder) && Directory.Exists(DesignerSettingsData.ModFolder))
            {
                openDialog.InitialDirectory = DesignerSettingsData.ModFolder;
            }
            if (openDialog.ShowDialog() == true)
            {
                try
                {
                    CodeGenerator.LoadProject(openDialog.FileName);
                    MessageBox.Show("Code Generated successfully!", "Generate", MessageBoxButton.OK, MessageBoxImage.Information);
                    // You can now access CodeGenerator.GlobalProjectData throughout your program
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading project data: {ex.Message}", "Generate", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (baseLayerData != null)
            {
                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Project Files (*.json)|*.json",
                    DefaultExt = "json"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    baseLayerData.Color1 = BackgroundColor1;
                    baseLayerData.Color2 = BackgroundColor2;
                    baseLayerData.Color3 = BackgroundColor3;

                    // Pass country tag and name when saving
                    ProjectFileManager.SaveProject(saveDialog.FileName, new List<ImageData>(UserImages), baseLayerData,
                        CountryTagTextBox.Text, CountryNameTextBox.Text);

                    MessageBox.Show("Project saved successfully!", "Save", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
                MessageBox.Show("A Pattern must be set", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }


        private void Open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Project Files (*.json)|*.json"
            };

            if (openDialog.ShowDialog() == true)
            {
                var (loadedImages, backgroundImage, countryTag, countryName) = ProjectFileManager.LoadProject(openDialog.FileName);

                // Populate text boxes with loaded data
                CountryTagTextBox.Text = countryTag;
                CountryNameTextBox.Text = countryName;

                if (backgroundImage != null)
                {
                    backgroundImage.ImageSource = ImageColorProcessor.ApplyColorReplacement(backgroundImage.ImageSource, backgroundImage.Color1?? Colors.Red, backgroundImage.Color2 ?? Colors.Yellow, backgroundImage.Color3 ?? Colors.White, Colors.Red, Colors.Yellow, Colors.White);
                    SetBaseLayerImage(backgroundImage);
                    bool alreadyExists = FolderImages.Any(img => img.ImageSource?.UriSource == backgroundImage.ImageSource?.UriSource);
                    if (!alreadyExists)
                    {
                        FolderImages.Insert(0, backgroundImage);
                    }
                }
                else
                {
                    MessageBox.Show("Warning: No valid background image was found in the project.", "Load Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                if (loadedImages.Count > 0)
                {
                    UserImages.Clear();
                    foreach (var image in loadedImages)
                    {
                        image.ImageSource = ImageColorProcessor.ApplyColorReplacement(image.ImageSource, image.Color1 ?? Color.FromRgb(0, 0, 128), image.Color2 ?? Color.FromRgb(0, 255, 128), image.Color3 ?? Color.FromRgb(255, 0, 128), Color.FromRgb(0, 0, 128), Color.FromRgb(0, 255, 128), Color.FromRgb(255, 0, 128));
                        UserImages.Add(image);
                    }
                    RebuildCanvasLayers();
                    MessageBox.Show("Project loaded successfully!", "Open", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("No images were loaded.", "Open", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = UserImageList.SelectedIndex;
            if (selectedIndex > 0)
            {
                var item = UserImages[selectedIndex];
                UserImages.RemoveAt(selectedIndex);
                UserImages.Insert(selectedIndex - 1, item);
                UserImageList.SelectedIndex = selectedIndex - 1;
                RebuildCanvasLayers();
            }
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = UserImageList.SelectedIndex;
            if (selectedIndex < UserImages.Count - 1 && selectedIndex >= 0)
            {
                var item = UserImages[selectedIndex];
                UserImages.RemoveAt(selectedIndex);
                UserImages.Insert(selectedIndex + 1, item);
                UserImageList.SelectedIndex = selectedIndex + 1;
                RebuildCanvasLayers();
            }
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = UserImageList.SelectedIndex;
            if (selectedIndex >= 0)
            {
                UserImages.RemoveAt(selectedIndex);
                RebuildCanvasLayers();
            }
        }
    }

    public class ImageData
    {
        public BitmapImage? ImageSource { get; set; }
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double ScaleX { get; set; } = 1.0;
        public double ScaleY { get; set; } = 1.0;
        public double Rotation { get; set; } = 0.0; // Default rotation
        public string ImagePath { get; set; }
        public Color? Color1 { get; set; }
        public Color? Color2 { get; set; }
        public Color? Color3 { get; set; }
        public bool IsEmblem { get; set; } = false;
    }

}