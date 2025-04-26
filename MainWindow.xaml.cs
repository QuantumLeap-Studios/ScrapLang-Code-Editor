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
using System.Windows.Media.Animation;
using System.Windows.Input;
using System.IO.Compression;
using System.Collections.Generic; // Add this namespace import
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Application = System.Windows.Application;
using DiscordRPC;
using DiscordRPC.Logging;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace ScrapLangEditor
{
    public partial class MainWindow : Window
    {
        public static MainWindow instance;
        private string RecentFilesPath = $"C:\\Users\\{Environment.UserName}\\AppData\\Roaming\\ScrapLangEditor\\recentFiles.cfg";
        private string LastFilePath = $"C:\\Users\\{Environment.UserName}\\AppData\\Roaming\\ScrapLangEditor\\lastFileName.cfg";
        private DiscordRpcClient discordClient;

        private void LoadRecentFiles()
        {
            if (File.Exists(RecentFilesPath))
            {
                recentFiles = File.ReadAllLines(RecentFilesPath).ToList();
                foreach (var file in recentFiles)
                {
                    RecentFilesList.Items.Add(file);
                }
            }
        }

        private void SaveRecentFiles()
        {
            if (File.Exists(RecentFilesPath))
            {
                File.WriteAllLines(RecentFilesPath, recentFiles);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(RecentFilesPath));
                File.WriteAllLines(RecentFilesPath, recentFiles);
            }
        }

        public bool isDoneWithWhatever = false;

        private List<string> recentFiles = new List<string>();
        private List<string> fileExplorerItems = new List<string>();
        // Import the GetAsyncKeyState function from user32.dll
        // Delegate for the hook procedure
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        // Hook handle
        private IntPtr _hookID = IntPtr.Zero;

        // Hook procedure instance to prevent garbage collection
        private LowLevelMouseProc _proc = HookCallback;

        // Constants for mouse messages
        private
        const int WH_MOUSE_LL = 14;
        private
        const int WM_MOUSEWHEEL = 0x020A;

        // Import necessary Windows API functions
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Structure for mouse hook information
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // Structure for point coordinates
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        public MainWindow()
        {
            instance = this;
            InitializeComponent();
            textEditor.TextChanged += TextEditor_TextChanged;
            textEditor.MouseWheel += textEditor_MouseWheel;

            LoadRecentFiles();

            Loaded += MainWindow_Loaded;
            Closed += MainWindow_Closed;
            var assembly = Assembly.GetExecutingAssembly();
            foreach (string resource in assembly.GetManifestResourceNames())
            {
                Console.WriteLine(resource);
            }
            var resourceName = "ScrapLang_Code_Editor.Resources.ScrapLang.xshd";

            this.KeyDown += MainWindow_KeyDown; // Attach the KeyDown event

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException($"Resource '{resourceName}' not found.");

                using (XmlReader reader = XmlReader.Create(stream))
                {
                    var highlightingDefinition = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    HighlightingManager.Instance.RegisterHighlighting("ScrapLang", new[] {
            ".scrap"
          }, highlightingDefinition);
                }
            }

            discordClient = new DiscordRpcClient("1363181847709745262");

            discordClient.Logger = new ConsoleLogger() { Level = LogLevel.Warning };

            discordClient.OnReady += (sender, e) =>
            {
                Console.WriteLine("Connected to Discord as {0}", e.User.Username);
            };

            discordClient.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Presence updated.");
            };

            discordClient.Initialize();

            var timer = new System.Timers.Timer(150);
            timer.Elapsed += (sender, args) => { discordClient.Invoke(); };
            timer.Start();

            discordClient.SetPresence(new RichPresence()
            {
                Details = "Editing a ScrapLang file",
                State = $"Working on an unnamed project",
                Assets = new Assets()
                {
                    LargeImageKey = "scraplang_logo",
                    LargeImageText = "ScrapLang Editor"
                }
            });

            if (!File.Exists(LastFilePath))
            {
                textEditor.Text = @"publicated intistab counter = 0stb
invoid incrementCounter() == increment

funcvoid increment() ==
{
    incrementCounter()
}

funcvoid Awake() ==  
{
    increment()
    print(counter)
}";
            }
            else
            {
                string lastFileName = File.ReadAllText(LastFilePath);
                if (File.Exists(lastFileName))
                {
                    loaddefile(lastFileName);
                }
            }
            textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("ScrapLang");
        }

        private void AddToRecentFiles(string filePath)
        {
            if (!recentFiles.Contains(filePath))
            {
                recentFiles.Add(filePath);
                RecentFilesList.Items.Add(filePath);
                SaveRecentFiles();
            }
        }

        public void NewFile_Click(object sender, RoutedEventArgs e)
        {
            textEditor.Text = @"publicated intistab counter = 0stb
invoid incrementCounter() == increment

funcvoid increment() ==
{
    incrementCounter()
}

funcvoid Awake() ==  
{
    increment()
    print(counter)
}";
            SaveFile_Click(null, null);
            _ = WaitForSaveThenLoad();
        }

        public async Task WaitForSaveThenLoad()
        {
            while (!isDoneWithWhatever)
            {
                await Task.Delay(5); // Avoid busy-waiting by adding a small delay
            }
            loaddefile(LastFilePath);
        }


        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            Sidebar.Visibility = Sidebar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
        private void RecentFilesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (RecentFilesList.SelectedItem != null)
            {
                string filePath = RecentFilesList.SelectedItem.ToString();
                if (File.Exists(filePath))
                {
                    loaddefile(filePath);
                }
            }
        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                if (!fileExplorerItems.Contains(filePath))
                {
                    fileExplorerItems.Add(filePath);
                    FileExplorerList.Items.Add(filePath);
                }
            }
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (FileExplorerList.SelectedItem != null)
            {
                if (FileExplorerList.SelectedItem.ToString().ToLower().Contains("\\main.scrap"))
                {
                    System.Windows.MessageBox.Show("Cannot remove main.scrap file.");
                    return;
                }
                string selectedFile = FileExplorerList.SelectedItem.ToString();
                fileExplorerItems.Remove(selectedFile);
                FileExplorerList.Items.Remove(selectedFile);
            }
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "ScrapLang files (*.scrap)|*.scrap|Zip archives (*.zip)|*.zip|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                loaddefile(openFileDialog.FileName);
            }
        }

        public void loaddefile(string FileName)
        {
            string extension = Path.GetExtension(FileName).ToLowerInvariant();

            if (File.Exists(LastFilePath))
            {
                File.WriteAllText(LastFilePath, FileName);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LastFilePath));
                File.WriteAllText(LastFilePath, FileName);
            }

            if (extension == ".scrap")
            {
                textEditor.Load(FileName);
                AddToRecentFiles(FileName);
                fileExplorerItems.Clear();
                FileExplorerList.Items.Clear();
                FileExplorerList.Items.Add(FileName);
            }
            else if (extension == ".zip")
            {
                using (ZipArchive archive = ZipFile.OpenRead(FileName))
                {
                    var entry = archive.GetEntry("main.scrap");
                    if (entry != null)
                    {
                        fileExplorerItems.Clear();
                        FileExplorerList.Items.Clear();
                        FileExplorerList.Items.Add(FileName + "\\main.scrap");

                        discordClient.SetPresence(new RichPresence()
                        {
                            Details = "Editing a ScrapLang file",
                            State = $"Working on {Path.GetFileName(FileName)}",
                            Assets = new Assets()
                            {
                                LargeImageKey = "scraplang_logo",
                                LargeImageText = "ScrapLang Editor"
                            }
                        });

                        using (var reader = new StreamReader(entry.Open()))
                        {
                            textEditor.Text = reader.ReadToEnd();
                            AddToRecentFiles(FileName);
                        }

                        foreach (var file in archive.Entries)
                        {
                            if (file.Name != "main.scrap")
                            {
                                string assetPath = Path.Combine(Path.GetDirectoryName(FileName), file.Name);
                                file.ExtractToFile(assetPath, true);
                                fileExplorerItems.Add(assetPath);
                                FileExplorerList.Items.Add(assetPath);
                            }
                        }
                    }
                }
            }
        }
        private void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            isDoneWithWhatever = false;
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "ScrapLang files (*.scrap)|*.scrap|Zip archives (*.zip)|*.zip|All files (*.*)|*.*"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string extension = Path.GetExtension(saveFileDialog.FileName).ToLowerInvariant();

                AddToRecentFiles(saveFileDialog.FileName);

                if (File.Exists(LastFilePath))
                {
                    File.WriteAllText(LastFilePath, saveFileDialog.FileName);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(LastFilePath));
                    File.WriteAllText(LastFilePath, saveFileDialog.FileName);
                }

                if (extension == ".scrap")
                {
                    textEditor.Save(saveFileDialog.FileName);
                }
                else if (extension == ".zip")
                {

                    using (ZipArchive archive = ZipFile.Open(saveFileDialog.FileName, ZipArchiveMode.Create))
                    {
                        // Add main.scrap content
                        var mainEntry = archive.CreateEntry("main.scrap");
                        using (var writer = new StreamWriter(mainEntry.Open()))
                        {
                            writer.Write(textEditor.Text);
                        }

                        foreach (var file in fileExplorerItems)
                        {
                            string assetName = Path.GetFileName(file);
                            archive.CreateEntryFromFile(file, assetName);
                        }
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("Unsupported file type.");
                }
            }
            isDoneWithWhatever = true;
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
        private void TextEditor_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double maxZoom = 3; // Maximum zoom level  
            const double minZoom = 0.5; // Minimum zoom level  
            const double zoomStep = 0.1; // Zoom step  
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                var scaleTransform = textEditor.RenderTransform as ScaleTransform;
                if (scaleTransform == null)
                {
                    scaleTransform = new ScaleTransform(1.0, 1.0);
                    textEditor.RenderTransform = scaleTransform;
                }

                double newScaleX = scaleTransform.ScaleX + (e.Delta > 0 ? zoomStep : -zoomStep);
                double newScaleY = scaleTransform.ScaleY + (e.Delta > 0 ? zoomStep : -zoomStep);

                scaleTransform.ScaleX = Math.Max(minZoom, Math.Min(maxZoom, newScaleX));
                scaleTransform.ScaleY = Math.Max(minZoom, Math.Min(maxZoom, newScaleY));

                e.Handled = true;
            }
        }
        private void textEditor_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            const double zoomFactor = 0.1;
            Console.WriteLine("Mouse Wheel Scrolled");
            // Check if the Ctrl key is pressed
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                // Retrieve or create the ScaleTransform
                var scaleTransform = textEditor.RenderTransform as ScaleTransform;
                if (scaleTransform == null)
                {
                    scaleTransform = new ScaleTransform(1.0, 1.0);
                    textEditor.RenderTransform = scaleTransform;
                }

                // Adjust the scale based on the Delta value
                if (e.Delta > 0)
                {
                    scaleTransform.ScaleX += zoomFactor;
                    scaleTransform.ScaleY += zoomFactor;
                }
                else if (e.Delta < 0)
                {
                    scaleTransform.ScaleX = Math.Max(0.1, scaleTransform.ScaleX - zoomFactor);
                    scaleTransform.ScaleY = Math.Max(0.1, scaleTransform.ScaleY - zoomFactor);
                }

                // Mark the event as handled to prevent further processing
                e.Handled = true;
            }
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
        // Set up the mouse hook when the window is loaded
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hookID = SetHook(_proc);
        }

        // Remove the mouse hook when the window is closed
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            UnhookWindowsHookEx(_hookID);

            if (discordClient != null)
            {
                discordClient.Dispose();
            }
        }

        // Set the mouse hook
        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        // Hook callback function
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int delta = (short)((hookStruct.mouseData >> 16) & 0xffff);

                // Check if Ctrl key is pressed
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        const double zoomFactor = 0.1;

                        // Retrieve or create the ScaleTransform
                        var scaleTransform = instance.textEditor.RenderTransform as ScaleTransform;
                        if (scaleTransform == null)
                        {
                            scaleTransform = new ScaleTransform(1.0, 1.0);
                            instance.textEditor.RenderTransform = scaleTransform;
                        }

                        // Adjust the scale based on the delta value
                        if (delta > 0)
                        {
                            scaleTransform.ScaleX += zoomFactor;
                            scaleTransform.ScaleY += zoomFactor;
                        }
                        else if (delta < 0)
                        {
                            scaleTransform.ScaleX = Math.Max(0.1, scaleTransform.ScaleX - zoomFactor);
                            scaleTransform.ScaleY = Math.Max(0.1, scaleTransform.ScaleY - zoomFactor);
                        }
                    });
                }
            }

            return CallNextHookEx(instance._hookID, nCode, wParam, lParam);
        }

        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.S)
                {
                    AutoSaveFile();
                    e.Handled = true;
                }
            }
        }

        private void AutoSaveFile()
        {
            SaveFile_Click(null, null);
        }
    }
}