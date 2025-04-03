using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Vic3FlagDesigner
{
    public class ColorReplaceEffect : ShaderEffect
    {
        private static readonly PixelShader pixelShader = new PixelShader()
        {
            // Update the UriSource to point to your compiled shader file.
            UriSource = new Uri("/Vic3FlagDesigner;component/ColorReplace.ps", UriKind.Relative)
        };

        public ColorReplaceEffect()
        {
            this.PixelShader = pixelShader;
            UpdateShaderValue(InputProperty);
            UpdateShaderValue(SelectedColor1Property);
            UpdateShaderValue(NewColor1Property);
            UpdateShaderValue(SelectedColor2Property);
            UpdateShaderValue(NewColor2Property);
            UpdateShaderValue(SelectedColor3Property);
            UpdateShaderValue(NewColor3Property);
            UpdateShaderValue(ToleranceProperty);
        }

        // The brush that is the input for the shader.
        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }
        public static readonly DependencyProperty InputProperty =
            ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(ColorReplaceEffect), 0);

        // Dependency property for SelectedColor1
        public Color SelectedColor1
        {
            get { return (Color)GetValue(SelectedColor1Property); }
            set { SetValue(SelectedColor1Property, value); }
        }
        public static readonly DependencyProperty SelectedColor1Property =
            DependencyProperty.Register("SelectedColor1", typeof(Color), typeof(ColorReplaceEffect),
                new UIPropertyMetadata(Colors.Red, PixelShaderConstantCallback(0)));

        // Dependency property for NewColor1
        public Color NewColor1
        {
            get { return (Color)GetValue(NewColor1Property); }
            set { SetValue(NewColor1Property, value); }
        }
        public static readonly DependencyProperty NewColor1Property =
            DependencyProperty.Register("NewColor1", typeof(Color), typeof(ColorReplaceEffect),
                new UIPropertyMetadata(Colors.Red, PixelShaderConstantCallback(1)));

        // Repeat for SelectedColor2/NewColor2 and SelectedColor3/NewColor3
        public Color SelectedColor2
        {
            get { return (Color)GetValue(SelectedColor2Property); }
            set { SetValue(SelectedColor2Property, value); }
        }
        public static readonly DependencyProperty SelectedColor2Property =
            DependencyProperty.Register("SelectedColor2", typeof(Color), typeof(ColorReplaceEffect),
                new UIPropertyMetadata(Colors.Yellow, PixelShaderConstantCallback(2)));

        public Color NewColor2
        {
            get { return (Color)GetValue(NewColor2Property); }
            set { SetValue(NewColor2Property, value); }
        }
        public static readonly DependencyProperty NewColor2Property =
            DependencyProperty.Register("NewColor2", typeof(Color), typeof(ColorReplaceEffect),
                new UIPropertyMetadata(Colors.Yellow, PixelShaderConstantCallback(3)));

        public Color SelectedColor3
        {
            get { return (Color)GetValue(SelectedColor3Property); }
            set { SetValue(SelectedColor3Property, value); }
        }
        public static readonly DependencyProperty SelectedColor3Property =
            DependencyProperty.Register("SelectedColor3", typeof(Color), typeof(ColorReplaceEffect),
                new UIPropertyMetadata(Colors.White, PixelShaderConstantCallback(4)));

        public Color NewColor3
        {
            get { return (Color)GetValue(NewColor3Property); }
            set { SetValue(NewColor3Property, value); }
        }
        public static readonly DependencyProperty NewColor3Property =
            DependencyProperty.Register("NewColor3", typeof(Color), typeof(ColorReplaceEffect),
                new UIPropertyMetadata(Colors.White, PixelShaderConstantCallback(5)));

        // Tolerance property
        public double Tolerance
        {
            get { return (double)GetValue(ToleranceProperty); }
            set { SetValue(ToleranceProperty, value); }
        }
        public static readonly DependencyProperty ToleranceProperty =
            DependencyProperty.Register("Tolerance", typeof(double), typeof(ColorReplaceEffect),
                new UIPropertyMetadata(0.1, PixelShaderConstantCallback(6)));
    }
}
