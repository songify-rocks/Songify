using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using SpotifyAPI.Local; //Base Namespace
using SpotifyAPI.Local.Models; //Models for the JSON-responses
using System;
using MahApps.Metro;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;

namespace Songify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private SpotifyLocalAPI _spotify;
        private string[] colors = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        private System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
        private string artist, title, album;

        private delegate void UpdateStatusDelegate(string value);

        public MainWindow()
        {
            InitializeComponent();
            _spotify = new SpotifyLocalAPI
            {
                ListenForEvents = true
            };
            _spotify.OnPlayStateChange += OnPlayStateChange;
            _spotify.OnTrackChange += OnTrackChange;
        }

        private void txtbx_customoutput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string txt = txtbx_customoutput.Text;
            txt = txt.Replace("{artist}", artist);
            txt = txt.Replace("{title}", title);
            txt = txt.Replace("{album}", album);

            lbl_livepreview.Content = txt;
        }

        private void btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            flyout_About.IsOpen = false;
            if (flyout_Settings.IsOpen)
                flyout_Settings.IsOpen = false;
            else
                flyout_Settings.IsOpen = true;
        }

        private void Sognify_MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            #region Theme

            foreach (string s in colors)
            {
                ComboBox_Color.Items.Add(s);
            }

            foreach (string s in ComboBox_Color.Items)
            {
                if (s == Settings.GetColor())
                {
                    ComboBox_Color.SelectedItem = s;
                }
            }
            if (Settings.GetTheme() == "BaseDark") { themeToggleSwitch.IsChecked = true; } else { themeToggleSwitch.IsChecked = false; }
            ApplyTheme();

            #endregion Theme

            #region Settings

            if (Settings.GetDirectory() == "")
                Settings.SetDirectory(System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            txtbx_customoutput.Text = Settings.GetCustomOutput();
            Txtbx_outputdirectory.Text = Settings.GetDirectory();
            albumCoverToggleSwitch.IsChecked = Settings.getShowAlbumArt();
            if (Settings.getShowAlbumArt())
                warningLabel.Visibility = Visibility.Visible;
            else
                warningLabel.Visibility = Visibility.Hidden;

            pausetextToggleSwitch.IsChecked = Settings.getCustomPauseEnabled();
            downloadAlbumArtToggleSwitch.IsChecked = Settings.getDownloadAlbumArt();

            Txtbx_Pausetext.Text = Settings.GetCustomPauseText();

            #endregion Settings

            startSpotify();
        }

        private async void startSpotify()
        {
            try
            {
                await Task.Run(() => getSpotifyInfo());
            }
            catch (Exception ex)
            {
                WriteConsole("Ooops! Something went wrong. Try again with the \"Refresh\"-Button.", ex);
            }
        }

        private void ApplyTheme()
        {
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);
            ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Settings.GetColor()), ThemeManager.GetAppTheme(Settings.GetTheme()));
        }

        public async void getSpotifyInfo()
        {
            StatusResponse status = null;

            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                WriteConsole("Spotify is not running.", null);
                return; //Make sure the spotify client is running
            }

            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                WriteConsole("Spotify WebHelper Service is not running.", null);
                return; //Make sure the WebHelper is running
            }
            try
            {
                if (!_spotify.Connect())
                    return; //We need to call Connect before fetching infos, this will handle Auth stuff
            }
            catch (Exception ex)
            {
                // Could not Connect to the webservice
                WriteConsole("Couldn't connect to the Spotify Webservice. Please try again.", ex);
            }
            status = _spotify.GetStatus(); //status contains infos

            if (status != null)
            {
                await Dispatcher.BeginInvoke((Action)(async () =>
                {
                    try
                    {
                        Bitmap bmp = null;
                        artist = status.Track.ArtistResource.Name;
                        title = status.Track.TrackResource.Name;
                        album = status.Track.AlbumResource.Name;
                        txtbx_customoutput.AppendText("%TEMP%");
                        txtbx_customoutput.Text = txtbx_customoutput.Text.Replace("%TEMP%", "");
                        Lbl_Artist.Content = artist;
                        Lbl_Song.Content = title;
                        Lbl_Album.Content = album;
                        File.WriteAllText(Settings.GetDirectory() + "/Songify.txt", Settings.GetCustomOutput().Replace("{artist}", artist).Replace("{title}", title).Replace("{album}", album));
                        if (Settings.getShowAlbumArt())
                        {
                            bmp = new Bitmap(await status.Track.GetAlbumArtAsync(SpotifyAPI.Local.Enums.AlbumArtSize.Size640, null));
                            albumImage.Source = CreateBitmapSourceFromGdiBitmap(bmp);
                        }
                        if (Settings.getDownloadAlbumArt())
                        {
                            if (bmp == null)
                                bmp = new Bitmap(await status.Track.GetAlbumArtAsync(SpotifyAPI.Local.Enums.AlbumArtSize.Size640, null));
                            try
                            {
                                SaveJPG(bmp);
                            }
                            catch (Exception ex)
                            {
                                WriteConsole(ex.Message, ex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteConsole("Couldn't connect to the Spotify Webservice. Please try again.", ex);
                    }
                }
                ));
            }
        }

        public void SaveJPG(Bitmap bitmap)
        {
            ImageCodecInfo imageCodecInfo;
            Encoder encoder;
            EncoderParameter encoderParameter;
            EncoderParameters encoderParameters;
            imageCodecInfo = GetEncoderInfo("image/jpeg");
            encoder = Encoder.Quality;
            encoderParameters = new EncoderParameters(1);
            encoderParameter = new EncoderParameter(encoder, 75L);
            encoderParameters.Param[0] = encoderParameter;
            bitmap.Save(Settings.GetDirectory() + "/AlbumCover.jpg", imageCodecInfo, encoderParameters);
        }

        public static BitmapSource CreateBitmapSourceFromGdiBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapdata = bitmap.LockBits(rect, ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                int bufferSize = rect.Width * rect.Height * 4;
                return BitmapSource.Create(bitmap.Width, bitmap.Height, (double)bitmap.HorizontalResolution, (double)bitmap.VerticalResolution, PixelFormats.Bgra32, (BitmapPalette)null, bitmapdata.Scan0, bufferSize, bitmapdata.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapdata);
            }
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        public void OnTrackChange(object sender, TrackChangeEventArgs e)
        {
            startSpotify();
        }

        private void OnPlayStateChange(object sender, PlayStateEventArgs e)
        {
            try
            {
                if (!_spotify.GetStatus().Playing)
                {
                    if (Settings.getCustomPauseEnabled())
                        File.WriteAllText(Settings.GetDirectory() + "/Songify.txt", Settings.GetCustomPauseText());
                    else
                        File.WriteAllText(Settings.GetDirectory() + "/Songify.txt", Settings.GetCustomOutput().Replace("{artist}", artist).Replace("{title}", title).Replace("{album}", album));
                }
                else
                {
                    startSpotify();
                }
            }
            catch (Exception ex)
            {
                WriteConsole("Oops! Something went wrong... maybe clicking on \"Refresh\" will help?", ex);
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Settings.SetColor(ComboBox_Color.SelectedValue.ToString());
            ApplyTheme();
        }

        private void btn_save_Click(object sender, RoutedEventArgs e)
        {
            Settings.SetCustomOutputy(txtbx_customoutput.Text);
            Settings.SetCustomPauseText(Txtbx_Pausetext.Text);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            startSpotify();
        }

        private void btn_About_Click(object sender, RoutedEventArgs e)
        {
            flyout_Settings.IsOpen = false;

            if (flyout_About.IsOpen)
                flyout_About.IsOpen = false;
            else
                flyout_About.IsOpen = true;
        }

        private void btn_Donate_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.paypal.me/inzaniity");
        }

        private void btn_Discord_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://discordapp.com/invite/H8nd4T4");
        }

        private void MenuItem_InsertArtist_Click(object sender, RoutedEventArgs e)
        {
            AppendText(txtbx_customoutput, "{artist}");
        }

        private void MenuItem_InsertTitle_Click(object sender, RoutedEventArgs e)
        {
            AppendText(txtbx_customoutput, "{title}");
        }

        private void MenuItem_InsertAlbum_Click(object sender, RoutedEventArgs e)
        {
            AppendText(txtbx_customoutput, "{album}");
        }

        private void AppendText(TextBox tb, string text)
        {
            tb.AppendText(text);
            tb.Select(txtbx_customoutput.Text.Length, 0);
            tb.ContextMenu.IsOpen = false;
        }

        private void albumCoverToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            Settings.setShowAlbumArt((bool)albumCoverToggleSwitch.IsChecked);
            if (Settings.getShowAlbumArt())
                warningLabel.Visibility = Visibility.Visible;
            else
            {
                warningLabel.Visibility = Visibility.Hidden;
                albumImage.Source = null;
            }
        }

        private void pausetextToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)pausetextToggleSwitch.IsChecked)
                Settings.setCustomPauseEnabled(true);
            else
                Settings.setCustomPauseEnabled(false);
        }

        private void btn_GitHub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Inzaniity/Songify");
        }

        private void themeToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            if (themeToggleSwitch.IsChecked == true)
            {
                Settings.SetTheme("BaseDark");
            }
            else
            {
                Settings.SetTheme("BaseLight");
            }
            ApplyTheme();
        }

        private void downloadAlbumArtToggleSwitch_IsCheckedChanged(object sender, EventArgs e)
        {
            if ((bool)downloadAlbumArtToggleSwitch.IsChecked)
                Settings.setDownloadAlbumArt(true);
            else
                Settings.setDownloadAlbumArt(false);
        }

        private void Btn_Outputdirectory_Click(object sender, RoutedEventArgs e)
        {
            fbd.Description = "Path where the text file should be";
            fbd.SelectedPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            Txtbx_outputdirectory.Text = fbd.SelectedPath;
            Settings.SetDirectory(fbd.SelectedPath);
        }

        public void WriteConsole(string msg, Exception ex)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                Label lbl = (Label)StatusBar.Items[0];
                lbl.Content = DateTime.Now.ToString("hh:mm:ss") + ": " + msg;
                if (ex != null)
                {
                    try
                    {
                        string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Songify";
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                        if (!File.Exists(path + "/log.txt"))
                            File.Create(path + "/log.txt");
                        File.AppendAllText(Environment.SpecialFolder.MyDocuments + "/Songify" + "/log.txt", DateTime.Now.ToString("hh:mm:ss") + ": " + ex.Message + Environment.NewLine);
                    }
                    catch (Exception exc)
                    {
                        lbl.Content = DateTime.Now.ToString("hh:mm:ss") + ": " + exc.Message;
                    }
                }
            }));
        }
    }
}