using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api.V5;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Songify_Slim.Views;

namespace Songify_Slim.Util.General
{
    internal static class IoManager
    {
        private static bool _isWriting = false;

        public static void WriteOutput(string songPath, string currSong)
        {
            try
            {
                string interpretedText = InterpretEscapeCharacters(currSong);
                if (songPath.ToLower().Contains("songify.txt") && Settings.Settings.AppendSpaces && !string.IsNullOrEmpty(currSong))
                    interpretedText = interpretedText.PadRight(interpretedText.Length + Settings.Settings.SpaceCount);
                else if (Settings.Settings.AppendSpacesSplitFiles && !string.IsNullOrEmpty(currSong))
                    interpretedText = interpretedText.PadRight(interpretedText.Length + Settings.Settings.SpaceCount);
                File.WriteAllText(songPath, interpretedText);
            }
            catch (Exception e)
            {
                Logger.LogExc(e);
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
            WriteOutput(GlobalObjects.RootDirectory + "/Requester.txt", string.IsNullOrEmpty(requester) ? "" : Settings.Settings.RequesterPrefix + requester);
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
                            Logger.LogExc(e.Error);
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MainWindow main = Application.Current.MainWindow as MainWindow;
                            main?.img_cover.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
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
                                Debug.WriteLine(ex);
                            }

                        try
                        {
                            File.Move(canvasTmp, canvasPath);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
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
                    main?.img_cover.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        main.SetCanvas(canvasPath);
                    }));
                });
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
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

                if (string.IsNullOrEmpty(cover))
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
                        using WebClient webClient = new();
                        Uri uri = new(cover);
                        await webClient.DownloadFileTaskAsync(uri, coverTemp);

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
                        Logger.LogExc(ex);
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
                Logger.LogExc(ex);
            }
            finally
            {
                _downloadSemaphore.Release();
            }
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
                        Logger.LogExc(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogExc(ex);
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