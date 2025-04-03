using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using WpfApp3;

namespace Vic3FlagDesigner
{
    public static class ProjectFileManager
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true // Makes the JSON file more readable
        };

        public static void SaveProject(string filePath, List<ImageData> images, ImageData background, string countryTag, string countryName)
        {
            try
            {
                var saveData = new ProjectSaveData
                {
                    BackgroundImagePath = background?.ImagePath,
                    Images = new List<ImageSaveData>(),
                    BackgroundColor1 = background?.Color1 ?? Colors.Red,
                    BackgroundColor2 = background?.Color2 ?? Colors.Yellow,
                    BackgroundColor3 = background?.Color3 ?? Colors.White,
                    CountryTag = countryTag,  // Save country tag
                    CountryName = countryName // Save country name
                };

                foreach (var image in images)
                {
                    saveData.Images.Add(new ImageSaveData
                    {
                        Path = image.ImagePath,
                        X = image.X,
                        Y = image.Y,
                        ScaleX = image.ScaleX,
                        ScaleY = image.ScaleY,
                        Rotation = image.Rotation,
                        IsEmblem = image.IsEmblem,
                        Color1 = image.Color1 ?? Color.FromRgb(0, 0, 128),
                        Color2 = image.Color2 ?? Color.FromRgb(0, 255, 128),
                        Color3 = image.Color3 ?? Color.FromRgb(255, 0, 128)
                    });
                }

                string json = JsonSerializer.Serialize(saveData, jsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving project: {ex.Message}");
            }
        }

        public static (List<ImageData> images, ImageData background, string countryTag, string countryName) LoadProject(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return (new List<ImageData>(), null, "", "");

                string json = File.ReadAllText(filePath);
                var saveData = JsonSerializer.Deserialize<ProjectSaveData>(json);

                var loadedImages = new List<ImageData>();
                ImageData backgroundImage = null;

                if (!string.IsNullOrEmpty(saveData.BackgroundImagePath) && File.Exists(saveData.BackgroundImagePath))
                {
                    backgroundImage = new ImageData
                    {
                        ImageSource = new BitmapImage(new Uri(saveData.BackgroundImagePath)),
                        OriginalImage = new BitmapImage(new Uri(saveData.BackgroundImagePath)),
                        Color1 = saveData.BackgroundColor1,
                        Color2 = saveData.BackgroundColor2,
                        Color3 = saveData.BackgroundColor3,
                        ImagePath = saveData.BackgroundImagePath
                    };
                }

                foreach (var data in saveData.Images)
                {
                    if (File.Exists(data.Path))
                    {
                        loadedImages.Add(new ImageData
                        {
                            ImageSource = new BitmapImage(new Uri(data.Path)),
                            OriginalImage = new BitmapImage(new Uri(data.Path)),
                            X = data.X,
                            Y = data.Y,
                            ScaleX = data.ScaleX,
                            ScaleY = data.ScaleY,
                            Rotation = data.Rotation,
                            IsEmblem = data.IsEmblem,
                            Color1 = data.Color1,
                            Color2 = data.Color2,
                            Color3 = data.Color3,
                            ImagePath = data.Path
                        });
                    }
                }

                return (loadedImages, backgroundImage, saveData.CountryTag ?? "", saveData.CountryName ?? "");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading project: {ex.Message}");
                return (new List<ImageData>(), null, "", "");
            }
        }

    }

}

