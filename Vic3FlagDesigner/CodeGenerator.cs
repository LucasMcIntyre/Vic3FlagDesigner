using ImageMagick;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Vic3FlagDesigner
{
    public static class CodeGenerator
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true // Makes the JSON file more readable
        };
        public static ProjectSaveData? GlobalProjectData { get; private set; }

        private const string SettingsFile = "settings.json";
        private static SettingsData DesignerSettingsData;

        public static void LoadProject(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;

                if (File.Exists(SettingsFile))
                {
                    DesignerSettingsData = JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(SettingsFile));
                }
                else
                {
                    DesignerSettingsData = new SettingsData();
                }

                string json = File.ReadAllText(filePath);
                GlobalProjectData = JsonSerializer.Deserialize<ProjectSaveData>(json);

                var loadedImages = new List<ImageData>();
                ImageData backgroundImage = null;

                if (string.IsNullOrEmpty(GlobalProjectData.BackgroundImagePath) || !File.Exists(GlobalProjectData.BackgroundImagePath))
                {
                    MessageBox.Show("Background image not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                foreach (var data in GlobalProjectData.Images)
                {
                    if (!File.Exists(data.Path))
                    {
                        MessageBox.Show("Image Not Found: " + data.Path, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                BuildFolders();
                GenerateFlagDefinitions();
                GenerateCoatOfArms();

                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Reading File: {ex.Message}");
                return;
            }
        }

        private static void GenerateCoatOfArms()
        {
            if (DesignerSettingsData.ModFolder == null)
            {
                MessageBox.Show("Mod folder is not set. Please run BuildFolders first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string coaFolder = Path.Combine(DesignerSettingsData.ModFolder, "common", "coat_of_arms", "coat_of_arms");
            string filePath = Path.Combine(coaFolder, GlobalProjectData.CountryTag + "_" + GlobalProjectData.CountryName + "_coa.txt");
            var sb = new StringBuilder();

            sb.AppendLine(GlobalProjectData.CountryTag + " = {");
            sb.AppendLine($"\tpattern = \"{Path.GetFileName(GlobalProjectData.BackgroundImagePath)}\"");
            sb.AppendLine($"\tcolor1 = hsv360{{ {RgbToHsv(GlobalProjectData.BackgroundColor1)} }}");
            sb.AppendLine($"\tcolor2 = hsv360{{ {RgbToHsv(GlobalProjectData.BackgroundColor2)} }}");
            sb.AppendLine($"\tcolor3 = hsv360{{ {RgbToHsv(GlobalProjectData.BackgroundColor3)} }}");
            sb.AppendLine();

            foreach (var image in GlobalProjectData.Images)
            {
                string emblemType = image.IsEmblem ? "colored_emblem" : "textured_emblem";
                sb.AppendLine($"\t{emblemType} = {{");
                string texturePath = image.IsEmblem ? image.Path : Path.ChangeExtension(image.Path, ".dds");
                sb.AppendLine($"\t\ttexture = \"{Path.GetFileName(texturePath)}\"");

                if (image.IsEmblem)
                {
                    sb.AppendLine($"\t\tcolor1 = hsv360{{ {RgbToHsv(image.Color1)} }}");
                    sb.AppendLine($"\t\tcolor2 = hsv360{{ {RgbToHsv(image.Color2)} }}");
                    sb.AppendLine($"\t\tcolor3 = hsv360{{ {RgbToHsv(image.Color3)} }}");
                }

                double positionX = Math.Round(image.X / 768.0, 4);
                double positionY = Math.Round(image.Y / 512.0, 4);
                double scaleX = Math.Round(image.ScaleX, 4);
                double scaleY = Math.Round(image.ScaleY, 4);
                int rotation = NormalizeRotation(image.Rotation);

                sb.AppendLine($"\t\tinstance = {{ position = {{ {positionX} {positionY} }}{(rotation != 0 ? $" rotation =  {rotation} " : "")}{(scaleX != 1.0 || scaleY != 1.0 ? $" scale = {{ {scaleX} {scaleY} }}" : "")} }}");
                sb.AppendLine($"\t}}");
                sb.AppendLine();
            }
            sb.AppendLine("}");

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.AppendAllText(filePath, sb.ToString());
                //MessageBox.Show("Coat of Arms successfully generated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing to file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string RgbToHsv(Color color)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta > 0)
            {
                if (max == r)
                {
                    h = 60 * (((g - b) / delta) % 6);
                }
                else if (max == g)
                {
                    h = 60 * (((b - r) / delta) + 2);
                }
                else if (max == b)
                {
                    h = 60 * (((r - g) / delta) + 4);
                }
            }

            if (h < 0)
                h += 360;

            double s = (max == 0) ? 0 : delta / max * 100;
            double v = max * 100;

            return $"{Math.Round(h, 0)} {Math.Round(s, 0)} {Math.Round(v, 0)}";
        }


        private static void GenerateFlagDefinitions()
        {
            if (DesignerSettingsData.ModFolder == null)
            {
                MessageBox.Show("Mod folder is not set. Please run BuildFolders first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string flagDefinitionsFolder = Path.Combine(DesignerSettingsData.ModFolder, "common", "flag_definitions");
            string filePath = Path.Combine(flagDefinitionsFolder, GlobalProjectData.CountryTag + "_" + GlobalProjectData.CountryName + "_fd.txt");

            var sb = new StringBuilder();
            sb.AppendLine("#" + GlobalProjectData.CountryTag + " - " + GlobalProjectData.CountryName);
            sb.AppendLine(GlobalProjectData.CountryTag + " = {");
            sb.AppendLine("    flag_definition = {");
            sb.AppendLine("        coa = " + GlobalProjectData.CountryTag);
            sb.AppendLine("        subject_canton = " + GlobalProjectData.CountryTag);
            sb.AppendLine("        priority = 1");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.AppendAllText(filePath, sb.ToString());
                //MessageBox.Show("Flag definitions successfully generated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing to file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void BuildFolders()
        {
            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            if (!string.IsNullOrEmpty(DesignerSettingsData.ModFolder) && Directory.Exists(DesignerSettingsData.ModFolder))
            {
                dialog.SelectedPath = DesignerSettingsData.ModFolder;
            }
            if (dialog.ShowDialog() == true)
            {
                string basePath = dialog.SelectedPath;

                List<string> foldersToCreate = new List<string>
                {
                    Path.Combine(basePath, "gfx"),
                    Path.Combine(basePath, "gfx", "coat_of_arms"),
                    Path.Combine(basePath, "gfx", "coat_of_arms", "colored_emblems"),
                    Path.Combine(basePath, "gfx", "coat_of_arms", "patterns"),
                    Path.Combine(basePath, "gfx", "coat_of_arms", "textured_emblems"),
                    Path.Combine(basePath, "common"),
                    Path.Combine(basePath, "common", "coat_of_arms"),
                    Path.Combine(basePath, "common", "coat_of_arms", "coat_of_arms"),
                    Path.Combine(basePath, "common", "flag_definitions")
                };

                foreach (var folder in foldersToCreate)
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                }

                if (GlobalProjectData != null)
                {
                    // Copy background image to patterns folder
                    string patternsFolder = Path.Combine(basePath, "gfx", "coat_of_arms", "patterns");
                    if (!string.IsNullOrEmpty(GlobalProjectData.BackgroundImagePath) && File.Exists(GlobalProjectData.BackgroundImagePath))
                    {
                        string destinationPath = Path.Combine(patternsFolder, Path.GetFileName(GlobalProjectData.BackgroundImagePath));
                        File.Copy(GlobalProjectData.BackgroundImagePath, destinationPath, true);
                    }

                    // Copy other images
                    foreach (var image in GlobalProjectData.Images)
                    {
                        if (File.Exists(image.Path))
                        {
                            string destinationFolder = image.IsEmblem
                                ? Path.Combine(basePath, "gfx", "coat_of_arms", "colored_emblems")
                                : Path.Combine(basePath, "gfx", "coat_of_arms", "textured_emblems");

                            string destinationPath = Path.Combine(destinationFolder, Path.GetFileName(image.Path));

                            if (!image.IsEmblem && Path.GetExtension(image.Path).ToLower() != ".dds")
                            {
                                // Convert to DDS format
                                string ddsPath = Path.ChangeExtension(destinationPath, ".dds");
                                ConvertToDDS(image.Path, ddsPath);
                                destinationPath = ddsPath;
                            }
                            else
                            {
                                File.Copy(image.Path, destinationPath, true);
                            }
                        }
                    }
                }

                DesignerSettingsData.ModFolder = basePath;
                File.WriteAllText(SettingsFile, JsonSerializer.Serialize(DesignerSettingsData, jsonOptions));
            }
        }

        private static void ConvertToDDS(string inputPath, string outputPath)
        {
            try
            {
                using (MagickImage image = new MagickImage(inputPath))
                {
                    image.Format = MagickFormat.Dds;
                    image.Write(outputPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting {inputPath} to DDS: {ex.Message}");
            }
        }

        public static int NormalizeRotation(double angle)
        {
            // Normalize the angle within the range -180 to 180
            angle = Math.Round(angle); // Round to nearest integer

            // Convert -180 to 180 range into 0 to 360
            int normalizedAngle = (int)((angle + 360) % 360);

            return normalizedAngle;
        }

    }

}
