using Songify_Slim.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Songify_Slim.Util.Configuration;

namespace Songify_Slim.Util.General
{
    internal static class IoManager
    {
        private static bool _isWriting = false;

        public static void WriteOutput(string songPath, string currSong)
        {
            if (currSong == null)
            {
                File.WriteAllText(songPath, "");
                return;
            }

            try
            {
                string interpretedText = InterpretEscapeCharacters(currSong);
                if (songPath.ToLower().Contains("songify.txt") && Settings.AppendSpaces && !string.IsNullOrEmpty(currSong))
                    interpretedText = interpretedText.PadRight(interpretedText.Length + Settings.SpaceCount);
                else if (Settings.AppendSpacesSplitFiles && !string.IsNullOrEmpty(currSong))
                    interpretedText = interpretedText.PadRight(interpretedText.Length + Settings.SpaceCount);

                if (interpretedText.Trim().StartsWith("-"))
                {
                    interpretedText = interpretedText.Remove(0, 1).Trim();
                }

                File.WriteAllText(songPath, interpretedText);
            }
            catch (Exception e)
            {
                Logger.Error(LogSource.Core, $"Error writing to file: {songPath}", e);
            }
        }

        public static string InterpretEscapeCharacters(string input)
        {
            if (input == null)
                return null;
            string replacedInput = input
                .Replace(@"\t", "\t")
                .Replace(@"\n", Environment.NewLine)
                .Replace(@"\r", "\r");

            // Split the replaced input into lines
            string[] lines = replacedInput.Split([Environment.NewLine], StringSplitOptions.None);

            // Trim the leading spaces from each line
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimStart(' ');
            }

            // Join the lines back into a single string
            string result = string.Join(Environment.NewLine, lines);

            return result;
        }

        public static void WriteSplitOutput(string artist, string title, string extra, string requester = "")
        {
            // Writes the output to 2 different text files

            if (!File.Exists(GlobalObjects.RootDirectory + "/Artist.txt"))
                File.Create(GlobalObjects.RootDirectory + "/Artist.txt").Close();

            if (!File.Exists(GlobalObjects.RootDirectory + "/Title.txt"))
                File.Create(GlobalObjects.RootDirectory + "/Title.txt").Close();

            if (!File.Exists(GlobalObjects.RootDirectory + "/Requester.txt"))
                File.Create(GlobalObjects.RootDirectory + "/Requester.txt").Close();

            WriteOutput(GlobalObjects.RootDirectory + "/Artist.txt", artist);
            WriteOutput(GlobalObjects.RootDirectory + "/Title.txt", title + extra);
            WriteOutput(GlobalObjects.RootDirectory + "/Requester.txt", string.IsNullOrEmpty(requester) ? "" : Settings.RequesterPrefix + requester);
        }

        public static async void DownloadCanvas(string canvasUrl, string canvasPath)
        {
            string canvasTmp = $"{GlobalObjects.RootDirectory}/tmp.mp4";
            try
            {
                if (string.IsNullOrEmpty(canvasUrl))
                {
                    try
                    {
                        File.Delete(canvasPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else
                {
                    WebClient webClient = new();

                    webClient.DownloadFileCompleted += (sender, e) =>
                    {
                        if (e.Error != null)
                        {
                            Logger.Error(LogSource.Core, "Error downloading canvas.", e.Error);
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MainWindow main = Application.Current.MainWindow as MainWindow;
                            main?.ImgCover.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                            {
                                main.StopCanvas();
                            }));
                        });

                        const int tries = 5;
                        if (canvasPath == "" || canvasTmp == "") return;
                        if (File.Exists(canvasPath))
                            try
                            {
                                for (int i = 0; i < tries; i++)
                                {
                                    if (IsFileLocked(new FileInfo(canvasPath)))
                                    {
                                        Thread.Sleep(1000);
                                        if (i < tries) continue;
                                        return;
                                    }

                                    break;
                                }
                                File.Delete(canvasPath);
                            }
                            catch (Exception ex)
                            {
                            }

                        try
                        {
                            File.Move(canvasTmp, canvasPath);
                        }
                        catch (Exception ex)
                        {
                        }
                        _isWriting = false;
                    };

                    Uri uri = new(canvasUrl.Replace("\"", ""));
                    // Downloads the album cover to the filesystem

                    if (_isWriting) return;
                    _isWriting = true;
                    await webClient.DownloadFileTaskAsync(uri, canvasTmp);
                }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MainWindow main = Application.Current.MainWindow as MainWindow;
                    main?.ImgCover.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        main.SetCanvas(canvasPath);
                    }));
                });
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Error downloading canvas", ex);
            }
        }

        private static readonly SemaphoreSlim _downloadSemaphore = new(1, 1);

        public static async Task DownloadCover(string cover, string coverPath)
        {
            string coverTemp = $"{GlobalObjects.RootDirectory}/tmp_{Path.GetFileName(coverPath)}";

            try
            {
                // Stop the animation before downloading
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is MainWindow main)
                    {
                        main.StopCanvas();
                    }
                });

                await _downloadSemaphore.WaitAsync(); // Prevent overlapping downloads

                if (string.IsNullOrWhiteSpace(cover))
                {
                    // Create an empty transparent PNG
                    using Bitmap bmp = new(640, 640);
                    using Graphics g = Graphics.FromImage(bmp);
                    g.Clear(Color.Transparent);
                    bmp.Save(coverPath, ImageFormat.Png);
                }
                else
                {
                    try
                    {
                        // Ensure temp dir exists
                        Directory.CreateDirectory(Path.GetDirectoryName(coverTemp)!);

                        if (IsDataUrl(cover))
                        {
                            // data:image/...;base64,XXXX
                            WriteDataUrlToFile(cover, coverTemp);
                        }
                        else if (Uri.TryCreate(cover, UriKind.Absolute, out Uri uri))
                        {
                            if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                            {
                                using WebClient webClient = new();
                                await webClient.DownloadFileTaskAsync(uri, coverTemp);
                            }
                            else if (uri.Scheme == Uri.UriSchemeFile)
                            {
                                File.Copy(uri.LocalPath, coverTemp, overwrite: true);
                            }
                            else
                            {
                                // Unknown scheme – try as local path
                                if (File.Exists(cover))
                                    File.Copy(cover, coverTemp, overwrite: true);
                                else
                                    throw new InvalidOperationException($"Unsupported URI scheme: {uri.Scheme}");
                            }
                        }
                        else
                        {
                            // Not a URI – treat as local path
                            if (File.Exists(cover))
                                File.Copy(cover, coverTemp, overwrite: true);
                            else
                                throw new FileNotFoundException("Cover path not found", cover);
                        }

                        // Wait for the file to be unlocked if necessary
                        const int tries = 5;
                        for (int i = 0; i < tries; i++)
                        {
                            if (IsFileLocked(new FileInfo(coverPath)))
                            {
                                await Task.Delay(1000);
                                continue;
                            }
                            break;
                        }

                        if (File.Exists(coverPath))
                        {
                            File.Delete(coverPath);
                        }

                        File.Move(coverTemp, coverPath);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LogSource.Core, "Error downloading cover", ex);

                        // Fallback: write a transparent image if anything failed
                        using Bitmap bmp = new(640, 640);
                        using Graphics g = Graphics.FromImage(bmp);
                        g.Clear(Color.Transparent);
                        bmp.Save(coverPath, ImageFormat.Png);
                    }
                }

                // Update the image in the UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is MainWindow main)
                    {
                        main.SetCoverImage(coverPath);
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Error downloading cover", ex);
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        }

        private static readonly Regex DataUrlRegex = new(
            @"^data:(?<mime>[\w/+.\-]+)?(?:;charset=[\w\-]+)?;base64,(?<data>[A-Za-z0-9+/=]+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static bool IsDataUrl(string s) => s != null && DataUrlRegex.IsMatch(s);

        private static void WriteDataUrlToFile(string dataUrl, string path)
        {
            Match m = DataUrlRegex.Match(dataUrl);
            if (!m.Success) throw new FormatException("Invalid data URL");

            string b64 = m.Groups["data"].Value;
            byte[] bytes = Convert.FromBase64String(b64);
            File.WriteAllBytes(path, bytes);
        }

        private static readonly SemaphoreSlim _imageDownloadLock = new(1, 1);

        public static async Task DownloadImage(string cover, string coverPath)
        {
            string coverTemp = $"{GlobalObjects.RootDirectory}/tmp_{Path.GetFileName(coverPath)}";

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is MainWindow main)
                    {
                        main.StopCanvas();
                    }
                });

                await _imageDownloadLock.WaitAsync();

                if (string.IsNullOrEmpty(cover))
                {
                    using Bitmap bmp = new(640, 640);
                    using Graphics g = Graphics.FromImage(bmp);
                    g.Clear(Color.Transparent);
                    bmp.Save(coverPath, ImageFormat.Png);
                }
                else
                {
                    try
                    {
                        using WebClient webClient = new();
                        Uri uri = new(cover);
                        await webClient.DownloadFileTaskAsync(uri, coverTemp);

                        // Wait for the cover file to be ready
                        const int tries = 5;
                        for (int i = 0; i < tries; i++)
                        {
                            if (IsFileLocked(new FileInfo(coverPath)))
                            {
                                await Task.Delay(1000);
                                continue;
                            }
                            break;
                        }

                        if (File.Exists(coverPath))
                            File.Delete(coverPath);

                        File.Move(coverTemp, coverPath);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(LogSource.Core, "Error processing image download", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(LogSource.Core, "Error in image download process", ex);
            }
            finally
            {
                _imageDownloadLock.Release();
            }
        }

        private static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
                stream.Close();
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
    }
}