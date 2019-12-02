using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Microsoft.Win32;

namespace OfflineHtmlImageReplacer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _programLocation;
        private string _html;
        private string _htmlFilePath;
        private string _imgFilePath;
        private string _backupHtmlFilePath;
        private readonly string[] _imgExtensions = { ".jpg", ".jpeg", ".jpe", ".jfif", ".png", ".gif", ".ico", ".bmp", ".webp" };
        private readonly string[] _htmlExtensions = { ".html", ".htm" };

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            _programLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
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
        
        private void ShowError(string error)
        {
            ErrorTextBlock.Visibility = Visibility.Visible;
            ErrorTextBlock.Text = error;
            ErrorTab.IsSelected = true;
        }

        private void HideMessages()
        {
            SuccessTextBlock.Visibility = Visibility.Hidden;
            ErrorTextBlock.Visibility = Visibility.Hidden;
        }

        private int ExcludeUriEnding(int ending)
        {
            var res = ending;
            var nextChar = _html[ending];
            if (nextChar == '?')
            {
                res = _html.Length;
                var encodedEnding = _html.IndexOf("&#39;", ending, StringComparison.CurrentCultureIgnoreCase);
                var doubleColEnding = _html.IndexOf("\"", ending, StringComparison.CurrentCultureIgnoreCase);
                var singleColEnding = _html.IndexOf("'", ending, StringComparison.CurrentCultureIgnoreCase);

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
            Console.WriteLine($"Removing: {(_html.Substring(start, end - start))}");
            sBuilder.Remove(start, end - start);
            sBuilder.Insert(start, imgFileUri);
            _html = sBuilder.ToString();
        }

        #region Events
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
                HideMessages();
                AppSettings.Default.HtmlLocation = _htmlFilePath;
                AppSettings.Default.Save();

                CreateBackup();
            }
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
                HideMessages();
                AppSettings.Default.ImgLocation = _imgFilePath;
                AppSettings.Default.Save();
            }
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void ResetHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            HideMessages();

            if (!string.IsNullOrEmpty(_backupHtmlFilePath) && !string.IsNullOrEmpty(_htmlFilePath))
            {
                if (File.Exists(_backupHtmlFilePath))
                {
                    File.Copy(_backupHtmlFilePath, _htmlFilePath, true);
                    _html = File.ReadAllText(_htmlFilePath);
                    SuccessTextBlock.Text = "HTML file was restored to its original state.";
                    SuccessTextBlock.Visibility = Visibility.Visible;
                }
            }
        }

        private void TestHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            HideMessages();

            if (!string.IsNullOrEmpty(_htmlFilePath))
            {
                ErrorTextBlock.Visibility = Visibility.Hidden;
                System.Diagnostics.Process.Start(_htmlFilePath);
            }
            else
            {
                ShowError("No HTML to test..");
            }
        }

        private void ReplaceButton_Click(object sender, RoutedEventArgs e)
        {
            HideMessages();
            SuccessTextBlock.Text = "";

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
                ShowError("Target IMG Name was not entered!");
            }
            else
            {
                var extension = Path.GetExtension(OldImgNameTextBox.Text);
                if (!_imgExtensions.Any(ext => string.Equals(ext, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    var allowedExtensions = string.Join(", ", _imgExtensions.Select(ex => "*" + ex));
                    ShowError($"Target IMG Name should contain one of {allowedExtensions} extensions!");
                }
                else
                {
                    var searchTerm = Uri.EscapeUriString(OldImgNameTextBox.Text);
                    var oldImgLocations = Regex.Matches(_html,  Regex.Escape(searchTerm), RegexOptions.IgnoreCase)
                        .Cast<Match>()
                        .Select(m => m.Index)
                        .OrderByDescending(x => x)
                        .ToList();
                    
                    if (oldImgLocations.Count == 0)
                    {
                        ShowError($"Failed to find {OldImgNameTextBox.Text} to replace HTML with");
                    }
                    else
                    {
                        string errors = null;
                        string replaces = null;
                        foreach (var oldImgLocation in oldImgLocations)
                        {
                            //Find the first occurence of "http", "file://" or "./" to the left of the match
                            var httpBeginning = _html.LastIndexOf("http", oldImgLocation, StringComparison.CurrentCultureIgnoreCase);
                            var fileBeginning = _html.LastIndexOf("file://", oldImgLocation, StringComparison.CurrentCultureIgnoreCase);
                            var relBeginning = _html.LastIndexOf("./", oldImgLocation, StringComparison.CurrentCultureIgnoreCase);
                            var beginning = httpBeginning > fileBeginning ? httpBeginning : fileBeginning;
                            beginning = beginning > relBeginning ? beginning : relBeginning;

                            //Find the first occurence of file extension to the right of the match
                            var ending = _html.IndexOf(extension, beginning + searchTerm.Length, StringComparison.CurrentCultureIgnoreCase);

                            if (beginning != -1 && ending != -1)
                            {
                                ending += extension.Length;
                                ending = ExcludeUriEnding(ending);
                                replaces += $"{beginning}->{ending}: \"{_html.Substring(beginning, ending - beginning)}\"\n";
                                ReplaceImage(beginning, ending);
                                continue;
                            }
                            errors += $"{beginning}->{ending}, ";
                        }

                        if (!string.IsNullOrEmpty(replaces))
                        {
                            File.WriteAllText($@"{_htmlFilePath}", _html);
                            SuccessTextBlock.Text = $"Successfully replaced these search matches: {replaces}";
                            SuccessTextBlock.Visibility = Visibility.Visible;
                            OutputTab.IsSelected = true;
                            AppSettings.Default.ReplaceImg = OldImgNameTextBox.Text;
                            AppSettings.Default.Save();
                        }

                        if (!string.IsNullOrEmpty(errors))
                            ShowError($"Failed to find image in indexes: {errors}");
                    }
                }
            }
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                HideMessages();

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
                        var allowedExtensions = string.Join(", ", _imgExtensions.Select(ex => "*" + ex));
                        allowedExtensions += ", " + string.Join(", ", _htmlExtensions.Select(ex => "*" + ex));
                        ShowError($"Only {allowedExtensions} extensions are allowed!");
                    }
                }
                else
                {
                    ShowError($"File: {fileName} does not exist!");
                }
            }
        }
        #endregion
    }
}