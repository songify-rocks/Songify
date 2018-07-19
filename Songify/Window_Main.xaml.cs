using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using SpotifyAPI;
using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;
using SpotifyAPI.Web;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Songify
{
    public partial class MainWindow : MetroWindow
    {
        public static SpotifyLocalAPI _spotify;
        public static SpotifyWebAPI _spotifyWeb;
        public static string artist, title, album;
        public static MainWindow mw;
        public static string version;
        private System.Reflection.Assembly assembly;
        private string[] colors = new string[] { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        private System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
        private FileVersionInfo fvi;
        private System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();
        private bool playlist = false;
        private SpotifyLocalAPIConfig splc;
        private int trackLength;
        public MainWindow()
        {
            InitializeComponent();
            if (Settings.GetProxyHost() != "" || Settings.GetProxyPort() != "")
            {
                splc = new SpotifyLocalAPIConfig
                {
                    ProxyConfig = new ProxyConfig()
                    {
                        Host = Settings.GetProxyHost(),
                        Port = int.Parse(Settings.GetProxyPort()),
                        Username = Settings.GetProxyUser(),
                        Password = Settings.GetProxyPass()
                    }
                };

                _spotify = new SpotifyLocalAPI(splc)
                {
                    ListenForEvents = true,
                };
            }
            else
            {
                _spotify = new SpotifyLocalAPI()
                {
                    ListenForEvents = true,
                };
            }

            _spotify.OnPlayStateChange += OnPlayStateChange;
            _spotify.OnTrackChange += OnTrackChange;
            _spotify.OnTrackTimeChange += OnTrackTimeChange;

            _spotifyWeb = new SpotifyWebAPI()
            {
                UseAuth = true
            };
        }

        private delegate void UpdateStatusDelegate(string value);

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

        public async void GetSpotifyInfo()
        {
            StatusResponse status = null;

            if (!SpotifyLocalAPI.IsSpotifyRunning())
            {
                WriteConsole("Spotify is not running.", null);
                await Dispatcher.BeginInvoke((Action)(async () =>
                {
                    var result = await this.ShowMessageAsync("Information", "Spotify is not running. Do you want to start it?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                    if (result == MessageDialogResult.Affirmative)
                    {
                        SpotifyLocalAPI.RunSpotify();
                        return;
                    }
                }
                ));
                return;
            }

            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                WriteConsole("Spotify WebHelper Service is not running.", null);
                return;
            }
            try
            {
                if (!_spotify.Connect())
                    return;
            }
            catch (Exception ex)
            {
                WriteConsole("Couldn't connect to the Spotify Webservice. Please try again.", ex);
            }
            status = _spotify.GetStatus();

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
                        Lbl_Artist.Content = artist;
                        Lbl_Song.Content = title;
                        Lbl_Album.Content = album;
                        File.WriteAllText(Settings.GetDirectory() + "/Songify.txt", Settings.GetCustomOutput().Replace("{artist}", artist).Replace("{title}", title).Replace("{album}", album));
                        Title = "Songify | " + artist + " - " + title;
                        trackLength = status.Track.Length;
                        if (Settings.GetShowAlbumArt())
                        {
                            bmp = new Bitmap(await status.Track.GetAlbumArtAsync(SpotifyAPI.Local.Enums.AlbumArtSize.Size640, null));
                            albumImage.Source = CreateBitmapSourceFromGdiBitmap(bmp);
                        }
                        else
                        {
                            albumImage.Source = null;
                        }
                        if (Settings.GetDownloadAlbumArt())
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
                        WriteConsole(null, null);
                    }
                    catch (Exception ex)
                    {
                        WriteConsole("Couldn't connect to the Spotify Webservice. Please try again.", ex);
                    }
                }
                ));
            }
        }

        public void OnTrackChange(object sender, TrackChangeEventArgs e)
        {
            StartSpotify();
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

        public async void StartSpotify()
        {
            try
            {
                await Task.Run(() => GetSpotifyInfo());
            }
            catch (Exception ex)
            {
                WriteConsole(ex.Message, ex);
            }

            if (!Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(new Action(StartSpotify));
                return;
            }

            if (HasStatus(_spotify))
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(MainWindow))
                    {
                        if (_spotify.GetStatus().Playing)
                        {
                            (window as MainWindow).playpauseVisualBrush.Visual = (Visual)FindResource("appbar_control_pause");
                        }
                        else
                        {
                            (window as MainWindow).playpauseVisualBrush.Visual = (Visual)FindResource("appbar_control_play");
                        }
                    }
                }
            }
        }

        public void WriteConsole(string msg, Exception ex)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                System.Windows.Controls.Label lbl = (System.Windows.Controls.Label)StatusBar.Items[0];
                if (msg == null)
                    lbl.Content = "";
                else
                    lbl.Content = DateTime.Now.ToString("hh:mm:ss") + ": " + msg;
                if (ex != null)
                {
                    Console.WriteLine(ex.Source);
                    try
                    {
                        // string path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Songify";
                        string path = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location).ToString();
                        if (!Directory.Exists(path))
                            Directory.CreateDirectory(path);
                        if (!File.Exists(path + "/log.txt"))
                            File.Create(path + "/log.txt");
                        File.AppendAllText(path + "/log.txt", DateTime.Now.ToString("hh:mm:ss") + ": " + ex.Message + Environment.NewLine);
                    }
                    catch (Exception exc)
                    {
                        lbl.Content = DateTime.Now.ToString("hh:mm:ss") + ": " + exc.Message;
                    }
                }
            }));
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

        private void Bnt_playNext_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _spotify.Skip();
            }
            catch (Exception ex)
            {
                WriteConsole(ex.Message, ex);
            }
        }

        private void Bnt_playPause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (HasStatus(_spotify))
                {
                    if (_spotify.GetStatus().Playing)
                    {
                        _spotify.Pause();
                        playpauseVisualBrush.Visual = (Visual)FindResource("appbar_control_play");
                    }
                    else
                    {
                        _spotify.Play();
                        playpauseVisualBrush.Visual = (Visual)FindResource("appbar_control_pause");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteConsole(ex.Message, ex);
            }
        }

        private void Bnt_playPrevious_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _spotify.Previous();
            }
            catch (Exception ex)
            {
                WriteConsole(ex.Message, ex);
            }
        }

        private void Btn_About_Click(object sender, RoutedEventArgs e)
        {
            if (flyout_About.IsOpen)
                flyout_About.IsOpen = false;
            else
                flyout_About.IsOpen = true;
        }

        private void Btn_Discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://discordapp.com/invite/H8nd4T4");
        }

        private void Btn_Donate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.me/inzaniity");
        }

        private void Btn_GitHub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/Inzaniity/Songify");
        }

        private void Btn_Settings_Click(object sender, RoutedEventArgs e)
        {
            bool isWindowOpen = false;

            foreach (Window w in System.Windows.Application.Current.Windows)
            {
                if (w is Window_Settings)
                {
                    isWindowOpen = true;
                }
            }

            if (!isWindowOpen)
            {
                Window_Settings windowSettings = new Window_Settings
                {
                    Owner = this,
                    Topmost = false
                };
                windowSettings.Show();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StartSpotify();
        }

        private async void CheckForUpdates()
        {
            var latest = Updater.getLatestRelease();
            int currentVersion = int.Parse(version.Replace(".", "").Remove(MainWindow.version.Replace(".", "").Length - 1));
            int newestRelease = int.Parse(latest.Name.Replace("v", "").Replace(".", ""));

            if (newestRelease > currentVersion)
            {
                var result = await this.ShowMessageAsync("Notification", "There is a newer version available. Do you want to update?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings { AffirmativeButtonText = "Yes", NegativeButtonText = "No" });
                if (result == MessageDialogResult.Affirmative)
                {
                    System.Diagnostics.Process.Start(latest.HtmlUrl);
                }
            }
        }

        private bool HasStatus(SpotifyLocalAPI spotifyLocalAPI)
        {
            if (spotifyLocalAPI.GetStatus() != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnPlayStateChange(object sender, PlayStateEventArgs e)
        {
            try
            {
                if (!_spotify.GetStatus().Playing)
                {
                    if (Settings.GetCustomPauseEnabled())
                        File.WriteAllText(Settings.GetDirectory() + "/Songify.txt", Settings.GetCustomPauseText());
                    else
                        File.WriteAllText(Settings.GetDirectory() + "/Songify.txt", Settings.GetCustomOutput().Replace("{artist}", artist).Replace("{title}", title).Replace("{album}", album));
                    if (Settings.GetDeleteAlbumArtOnpause())
                    {
                        if (File.Exists(Settings.GetDirectory() + "/AlbumCover.jpg"))
                        {
                            File.Delete(Settings.GetDirectory() + "/AlbumCover.jpg");
                        }
                    }
                }
                else
                {
                    StartSpotify();
                }
            }
            catch (Exception ex)
            {
                WriteConsole("Oops! Something went wrong... maybe clicking on \"Refresh\" will help?", ex);
            }
        }

        private void OnTrackTimeChange(object sender, TrackTimeChangeEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                {
                    lbl_endTime.Content = TimeSpan.FromSeconds(trackLength).ToString(@"mm\:ss");
                    lbl_currentTime.Content = TimeSpan.FromSeconds(e.TrackTime).ToString(@"mm\:ss");

                    if ((string)Lbl_Artist.Content != artist || (string)Lbl_Song.Content != title)
                    {
                        StartSpotify();
                    }
                }
                catch
                {
                }
            }));
        }

        private void Sognify_MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            assembly = System.Reflection.Assembly.GetExecutingAssembly();
            fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            version = fvi.FileVersion;
            ThemeHandler.ApplyTheme();
            tBlock_about.Text = "Songify " + version + " © Jan Blömacher";
            StartSpotify();
            try
            {
                Updater.checkForUpdates(new Version(version));
            }
            catch (Exception ex)
            {
                WriteConsole("Unable to check for newer version.", ex);
            }
            Console.WriteLine(Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location));
        }
    }
}