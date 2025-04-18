using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Document;
using System.Windows.Media;
using System.Windows.Media.Animation; // Add this namespace import

namespace ScrapLangEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            textEditor.TextChanged += TextEditor_TextChanged;

            var assembly = Assembly.GetExecutingAssembly();
            foreach (string resource in assembly.GetManifestResourceNames())
            {
                Console.WriteLine(resource);
            }
            var resourceName = "ScrapLang_Code_Editor.Resources.ScrapLang.xshd";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException($"Resource '{resourceName}' not found.");

                using (XmlReader reader = XmlReader.Create(stream))
                {
                    var highlightingDefinition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    HighlightingManager.Instance.RegisterHighlighting("ScrapLang", new[] { ".scrap" }, highlightingDefinition);
                }
            }
            textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("ScrapLang");
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "ScrapLang files (*.scrap)|*.scrap|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                textEditor.Load(openFileDialog.FileName);
            }
        }

        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "ScrapLang files (*.scrap)|*.scrap|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                textEditor.Save(saveFileDialog.FileName);
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (textEditor.CanUndo)
                textEditor.Undo();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (textEditor.CanRedo)
                textEditor.Redo();
        }

        private void TextEditor_TextChanged(object sender, EventArgs e)
        {
            var animationDuration = TimeSpan.FromMilliseconds(100);

            // Opacity animation for text only
            var opacityAnimation = new DoubleAnimation
            {
                From = 0.8,
                To = 1.0,
                Duration = new Duration(animationDuration)
            };
            textEditor.TextArea.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);

            // Subtle scale transform for text only
            var scaleTransform = textEditor.TextArea.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(1.0, 1.0);
                textEditor.TextArea.RenderTransform = scaleTransform;
            }

            // ScaleX animation
            var scaleXAnimation = new DoubleAnimation
            {
                From = 0.98,
                To = 1.0,
                Duration = new Duration(animationDuration)
            };
            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);

            // ScaleY animation
            var scaleYAnimation = new DoubleAnimation
            {
                From = 0.98,
                To = 1.0,
                Duration = new Duration(animationDuration)
            };
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
        }

        public void TextChanged(object sender, EventArgs e)
        {
            // Example: Do something when text changes.
            Console.WriteLine("Text changed!");
        }

    }
}
