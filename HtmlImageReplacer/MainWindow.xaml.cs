using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace OfflineHtmlImageReplacer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _programLocation;
        private string _html;
        private string _htmlFilePath;
        private string _imgFilePath;
        private string _backupHtmlFilePath;
        private string[] _imgExtensions = { ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".gif", ".ico", ".bmp" };
        private string[] _htmlExtensions = { ".html", ".htm" };

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            _programLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            ErrorTextBlock.Visibility = Visibility.Hidden;
        }

        private void CreateBackup()
        {
            try
            {
                _backupHtmlFilePath = _htmlFilePath + "_backup";
                if (!File.Exists(_backupHtmlFilePath))
                    File.Copy(_htmlFilePath, _backupHtmlFilePath);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void LoadSettings()
        {
            var htmlLocation = AppSettings.Default.HtmlLocation;
            if (!string.IsNullOrEmpty(htmlLocation) && File.Exists(htmlLocation))
            {
                _htmlFilePath = htmlLocation;
                _html = File.ReadAllText(htmlLocation);
                HtmlFileLocationLabel.Text = htmlLocation;
                CreateBackup();
            }

            var imgLocation = AppSettings.Default.ImgLocation;
            if (!string.IsNullOrEmpty(imgLocation) && File.Exists(imgLocation))
            {
                _imgFilePath = imgLocation;
                ImgFileLocationTextBlock.Text = imgLocation;
            }

            var oldImg = AppSettings.Default.ReplaceImg;
            if (!string.IsNullOrEmpty(oldImg))
            {
                OldImgNameTextBox.Text = oldImg;
            }
        }

        private void ChooseHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            var htmlExtensions = _htmlExtensions.Select(ex => "*" + ex);
            var htmlExtensionsLabel = string.Join(", ", htmlExtensions);
            var htmlExtensionsFilter = string.Join("; ", htmlExtensions);
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = GetHtmlFileDirectory(),
                Filter = $"HTML files ({htmlExtensionsLabel}) | {htmlExtensionsFilter}"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                HtmlFileLocationLabel.Text = openFileDialog.FileName;
                _html = File.ReadAllText(openFileDialog.FileName);
                _htmlFilePath = openFileDialog.FileName;
                AppSettings.Default.HtmlLocation = _htmlFilePath;
                AppSettings.Default.Save();

                CreateBackup();
                ErrorTextBlock.Visibility = Visibility.Visible;
            }
        }

        private string GetHtmlFileDirectory()
        {
            var htmlSettingLocation = AppSettings.Default.HtmlLocation;
            if (!string.IsNullOrEmpty(htmlSettingLocation)
                && Directory.Exists(Path.GetDirectoryName(htmlSettingLocation)))
            {
                return Path.GetDirectoryName(htmlSettingLocation);
            }

            if (!string.IsNullOrEmpty(_htmlFilePath))
            {
                return Path.GetDirectoryName(_htmlFilePath);
            }

            return Path.GetDirectoryName(_programLocation);
        }

        private void ChooseImgButton_Click(object sender, RoutedEventArgs e)
        {
            var imageExtensions = _imgExtensions.Select(ex => "*" + ex);
            var imageExtensionsLabel = string.Join(", ", imageExtensions);
            var imageExtensionsFilter = string.Join("; ", imageExtensions);
            var openFileDialog = new OpenFileDialog
            {
                InitialDirectory = GetImgFileDirectory(),
                Filter = $"Image files ({imageExtensionsLabel}) | {imageExtensionsFilter}"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImgFileLocationTextBlock.Text = openFileDialog.FileName;
                _imgFilePath = openFileDialog.FileName;
                ErrorTextBlock.Visibility = Visibility.Visible;
                AppSettings.Default.ImgLocation = _imgFilePath;
                AppSettings.Default.Save();
            }
        }

        private string GetImgFileDirectory()
        {
            var imgSettingLocation = AppSettings.Default.ImgLocation;
            if (!string.IsNullOrEmpty(imgSettingLocation)
                && Directory.Exists(Path.GetDirectoryName(imgSettingLocation)))
            {
                return Path.GetDirectoryName(imgSettingLocation);
            }

            if (!string.IsNullOrEmpty(_imgFilePath))
            {
                return Path.GetDirectoryName(_imgFilePath);
            }

            return Path.GetDirectoryName(_programLocation);
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                ErrorTextBlock.Visibility = Visibility.Visible;

                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                var fullFilePath = files[0];
                var fileName = Path.GetFileName(fullFilePath);

                if (File.Exists(fullFilePath))
                {
                    var extension = Path.GetExtension(Path.GetExtension(fullFilePath));
                    if (_imgExtensions.Any(extension.Contains))
                    {
                        ImgFileLocationTextBlock.Text = fullFilePath;
                        _imgFilePath = fullFilePath;
                        AppSettings.Default.ImgLocation = _imgFilePath;
                        AppSettings.Default.Save();
                    }
                    else if (_htmlExtensions.Any(extension.Contains))
                    {
                        _html = File.ReadAllText(fullFilePath);
                        HtmlFileLocationLabel.Text = fullFilePath;
                        _htmlFilePath = fullFilePath;
                        CreateBackup();
                        AppSettings.Default.HtmlLocation = _htmlFilePath;
                        AppSettings.Default.Save();
                    }
                    else
                    {
                        var allowedExtensions = string.Join(", ", _imgExtensions.Select(ex => "*"+ ex));
                        allowedExtensions += string.Join(", ", _htmlExtensions.Select(ex => "*" + ex));
                        ShowError($"Only {allowedExtensions} extensions are allowed!");
                    }
                }
                else
                {
                    ShowError($"File: {fileName} does not exist!");
                }
            }
        }

        private void ShowError(string error)
        {
            ErrorTextBlock.Visibility = Visibility.Visible;
            ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Red);
            ErrorTextBlock.Text = error;
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void ResetHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_backupHtmlFilePath) && !string.IsNullOrEmpty(_htmlFilePath))
            {
                if (File.Exists(_backupHtmlFilePath))
                {
                    File.Copy(_backupHtmlFilePath, _htmlFilePath, true);
                    _html = File.ReadAllText(_htmlFilePath);
                }
            }
        }

        private void TestHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_htmlFilePath))
            {
                System.Diagnostics.Process.Start(_htmlFilePath);
            }
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Hidden;
            if (string.IsNullOrEmpty(_html))
            {
                ShowError("HTML File was not chosen!");
            }
            else if (string.IsNullOrEmpty(_imgFilePath))
            {
                ShowError("IMG File was not chosen!");
            }
            else if (string.IsNullOrEmpty(OldImgNameTextBox.Text))
            {
                ShowError("Old IMG File was not entered!");
            }
            else
            {
                var extension = Path.GetExtension(OldImgNameTextBox.Text);
                if (!_imgExtensions.Any(extension.Contains))
                {
                    var allowedExtensions = string.Join(", ", _imgExtensions.Select(ex => "*" + ex));
                    allowedExtensions += string.Join(", ", _htmlExtensions.Select(ex => "*" + ex));
                    ShowError($"Old IMG File should be one of {allowedExtensions} extensions!");
                }
                else
                {
                    var oldImgLocations = Regex.Matches(_html, OldImgNameTextBox.Text)
                        .Cast<Match>()
                        .Select(m => m.Index)
                        .ToList();
                    
                    if (oldImgLocations.Count == 0)
                    {
                        ShowError($"Failed to find {OldImgNameTextBox.Text} to replace HTML with");
                    }
                    else
                    {
                        var didReplaceSomething = false;
                        string errors = null;
                        var index = 1;
                        foreach (var oldImgLocation in oldImgLocations)
                        {
                            var httpBeginning = _html.LastIndexOf("http", oldImgLocation);
                            var relBeginning = _html.LastIndexOf("./", oldImgLocation);
                            var beginning = httpBeginning > relBeginning ? httpBeginning : relBeginning;
                            var ending = _html.IndexOf(extension, oldImgLocation);

                            if (beginning != -1 && ending != -1)
                            {
                                ending += extension.Length;
                                ending = ExcludeUriEnding(ending);
                                ReplaceImage(beginning, ending);
                                didReplaceSomething = true;
                                continue;
                            }
                            errors += $"{index}, ";
                            index++;
                        }

                        if (didReplaceSomething)
                        {
                            File.WriteAllText($@"{_htmlFilePath}", _html);
                            AppSettings.Default.ReplaceImg = OldImgNameTextBox.Text;
                            AppSettings.Default.Save();
                        }

                        if (!string.IsNullOrEmpty(errors))
                            ShowError($"Failed to find image beginning/ending in occurences: {errors}");
                    }
                }
            }
        }

        private int ExcludeUriEnding(int ending)
        {
            var res = ending;
            var nextChar = _html[ending ];
            if (nextChar == '?')
            {
                res = _html.Length;
                var encodedEnding = _html.IndexOf("&#39;", ending);
                var doubleColEnding = _html.IndexOf("\"", ending);
                var singleColEnding = _html.IndexOf("'", ending);

                if (encodedEnding != -1 && encodedEnding < res)
                    res = encodedEnding;

                if (doubleColEnding != -1 && doubleColEnding < res)
                    res = doubleColEnding;

                if (singleColEnding != -1 && singleColEnding < res)
                    res = singleColEnding;

            }
            return res;
        }

        private void ReplaceImage(int start, int end)
        {
            var uri = new Uri(_imgFilePath);
            var imgFileUri = uri.AbsoluteUri;
            var sBuilder = new StringBuilder(_html);
            sBuilder.Remove(start, end - start);
            sBuilder.Insert(start, imgFileUri);
            _html = sBuilder.ToString();
            ErrorTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            ErrorTextBlock.Text = "Success!";
            ErrorTextBlock.Visibility = Visibility.Visible;
        }
    }
}