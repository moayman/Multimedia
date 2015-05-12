using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using NAudio.Wave;
using System.Diagnostics;

namespace multimedia
{
    public partial class MainWindow : Window
    {
        int CurrentSongIndex = -1;
        bool DraggingProgress = false;

        IWavePlayer waveOutDevice;
        AudioFileReader audioFileReader;
        DispatcherTimer ProgressUpdater;

        List<DataItem> PlaylistData = new List<DataItem>();
        private void playlist_hide_STBRD()
        {
            Storyboard ST = new Storyboard();
            ST.Name = "playlistHide2";
            
            ThicknessAnimationUsingKeyFrames TA = new ThicknessAnimationUsingKeyFrames();
            SplineThicknessKeyFrame SKF = new SplineThicknessKeyFrame();
            SKF.KeyTime = KeyTime.FromTimeSpan(new TimeSpan(0, 0, 0, 1));
            SKF.Value = new Thickness(this.Width*2/3, 55, 10, 145);

            TA.SetValue(Storyboard.TargetNameProperty, datagridPlaylistInfo);
            TA.SetValue(Storyboard.TargetPropertyProperty,Margin);
            TA.KeyFrames.Add(SKF);
            ST.Children.Add(TA);
            

        }
        public MainWindow()
        {
            InitializeComponent();

            datagridPlaylistInfo.ItemsSource = PlaylistData;
            ProgressUpdater = new DispatcherTimer(new TimeSpan(10000),DispatcherPriority.Normal,UpdateProgress,this.Dispatcher);
            ProgressUpdater.Start();
           
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            btnStop_Click(sender, e);
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Title = "Select Playlist Folder";
            CommonFileDialogResult result = dialog.ShowDialog();
            if(result == CommonFileDialogResult.Ok)
            {
                this.Cursor = System.Windows.Input.Cursors.Wait;
                LoadingGrid.Visibility = Visibility.Visible;
                ((Storyboard)this.Resources["Loading"]).Begin(this, true);
                String FolderName = dialog.FileName;
                txtboxFolder.Text = FolderName;
                PlaylistData.Clear();
                CurrentSongIndex = -1;
                datagridPlaylistInfo.Items.Refresh();
                new Thread(() => 
                {
                    String[] files = Directory.GetFiles(FolderName, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mp3") || s.EndsWith(".wav") || s.EndsWith(".mp4")).Cast<String>().ToArray();
                    if (files.Length != 0)
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                          DispatcherPriority.Send,
                          new Action(() =>
                          {
                              if (datagridPlaylistInfo.IsEnabled == false)
                                  datagridPlaylistInfo.IsEnabled = true;
                          }));
                        foreach (String file in files)
                        {
                            
                            ShellObject fileinfo = ShellObject.FromParsingName(file);
                            double divideby = 1024 * 1024;
                            double filesize = 0;
                            SizeUnit su = SizeUnit.None;
                            while (filesize == 0)
                            {
                                filesize = (double.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Size).ValueAsObject.ToString()) / divideby);
                                divideby /= 1024;
                                su++;
                            }
                            filesize = Math.Round(filesize, 2);
                            String name, artist, album, year, genre, duration;
                            uint ticks;
                            try 
	                        {	        
		                        name = fileinfo.Properties.GetProperty(SystemProperties.System.Title).ValueAsObject.ToString();
	                        }
	                        catch (Exception)
	                        {
                                name = fileinfo.Properties.GetProperty(SystemProperties.System.FileName).ValueAsObject.ToString();
	                        }
                            try 
	                        {
                                artist = fileinfo.Properties.GetProperty(SystemProperties.System.Music.DisplayArtist).ValueAsObject.ToString();
	                        }
	                        catch (Exception)
	                        {
                                artist = String.Empty;
	                        }
                            try
                            {
                                album = fileinfo.Properties.GetProperty(SystemProperties.System.Music.AlbumTitle).ValueAsObject.ToString();
                            }
                            catch (Exception)
                            {
                                album = String.Empty;
                            }
                            try
                            {
                                year = fileinfo.Properties.GetProperty(SystemProperties.System.Media.Year).ValueAsObject.ToString();
                            }
                            catch (Exception)
                            {
                                year = String.Empty;
                            }
                            try
                            {
                                genre = ((string[])fileinfo.Properties.GetProperty(SystemProperties.System.Music.Genre).ValueAsObject)[0];
                            }
                            catch (Exception)
                            {
                                genre = String.Empty;
                            }
                            try
                            {

                                ticks = UInt32.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Media.Duration).ValueAsObject.ToString());
                               // ticks = 0;
                            }
                            catch(Exception)
                            {

                                try
                                {
                                    
                                    ticks=(UInt32)(1 * ulong.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Media.Duration).ValueAsObject.ToString()));
                                    System.Console.WriteLine(ulong.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Media.Duration).ValueAsObject.ToString()));
                                }
                                catch (Exception)
                                {
                                    ticks = 0;
                                }
                                
                            }
                            try
                            {
                                duration = TimeSpan.FromTicks(long.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Media.Duration).ValueAsObject.ToString())).ToString(@"mm\:ss");
                            }
                            catch (Exception)
                            {
                                duration = String.Empty;
                            }
                            PlaylistData.Add(new DataItem
                            {
                                FilePath = file,
                               
                                Ticks = ticks,//UInt32.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Media.Duration).ValueAsObject.ToString()),
                                Name = name,
                                Artist = artist,
                                Album = album,
                                Year = year,
                                Genre = genre,
                                Format = fileinfo.Properties.GetProperty(SystemProperties.System.FileExtension).ValueAsObject.ToString().Substring(1),
                                Duration = duration,//TimeSpan.FromTicks(UInt32.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Media.Duration).ValueAsObject.ToString())).ToString(@"mm\:ss"),
                                Size = filesize.ToString() + " " + su.ToString(),
                                Bitrate = (Int32.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Audio.EncodingBitrate).ValueAsObject.ToString()) / 1000).ToString() + " Kbps",
                                SampleRate = (Int32.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Audio.SampleRate).ValueAsObject.ToString()) / 1000).ToString() + " KHz",
                                Channels = fileinfo.Properties.GetProperty(SystemProperties.System.Audio.ChannelCount).ValueAsObject.ToString()
                            });

                        }
                        Application.Current.Dispatcher.BeginInvoke(
                          DispatcherPriority.Send,
                          new Action(() =>
                          {
                              datagridPlaylistInfo.Items.Refresh();
                              ((Storyboard)this.Resources["Loading"]).Stop(this);
                              LoadingGrid.Visibility = Visibility.Collapsed;
                              this.Cursor = System.Windows.Input.Cursors.Arrow;
                          }));
                    }
                }).Start();
            }
        }

        private void datagridPlaylistInfo_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (datagridPlaylistInfo.SelectedIndex != -1)
            {
                if (waveOutDevice != null)
                    waveOutDevice.Stop();

                PreparePlay(datagridPlaylistInfo.SelectedIndex);

                ((Storyboard)this.Resources["Play"]).Begin();

                waveOutDevice.Play();
                PLR.Play();
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Pause();
                PLR.Pause();
                ((Storyboard)this.Resources["Pause"]).Begin();
            }
        }

        private void datagridPlaylistInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(datagridPlaylistInfo.SelectedIndex != -1)
                ((Storyboard)this.Resources["EnablePlayer"]).Begin();
            else
                ((Storyboard)this.Resources["DisablePlayer"]).Begin();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if(datagridPlaylistInfo.SelectedIndex != -1)
            {
                if (waveOutDevice == null || waveOutDevice.PlaybackState != PlaybackState.Paused)
                    PreparePlay(datagridPlaylistInfo.SelectedIndex);

                ((Storyboard)this.Resources["Play"]).Begin();
                
                waveOutDevice.Play();
                PLR.Play();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            PLR.Stop();
            if(waveOutDevice != null)
                waveOutDevice.Stop();

            txtblkSongName.Text = String.Empty;
            txtblkProgress.Text = "00:00";
            txtblkFrom.Text = "From 00:00";
            txtblkTo.Text = "To 00:00";

            sliderFrom.Maximum = sliderProgress.Maximum = sliderTo.Maximum = 1;
            sliderFrom.Value = sliderProgress.Value = 0;
            sliderTo.Value = sliderTo.Maximum;

            CurrentSongIndex = -1;

            ((Storyboard)this.Resources["Stop"]).Begin();
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            if (waveOutDevice != null && waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                txtblkProgress.Text = audioFileReader.CurrentTime.ToString(@"mm\:ss");
                sliderProgress.Value = audioFileReader.CurrentTime.Ticks;
            }
        }

        private void PreparePlay(int SongIndex)
        {
            if (((DataItem)datagridPlaylistInfo.Items[SongIndex]).Format == "mp4")
            {
                ((Storyboard)this.Resources["PlaylistHide"]).Begin();
                PLR.Source = new Uri(((DataItem)datagridPlaylistInfo.Items[SongIndex]).FilePath);
                
                //((Storyboard)this.Resources["PlayerShow"]).Begin();
            }
            else
            {
                PLR.Stop();
                ((Storyboard)this.Resources["PlaylistShow"]).Begin();
            }
            waveOutDevice = new WaveOut();
            System.Console.WriteLine(SongIndex);
            
            audioFileReader = new AudioFileReader(((DataItem)datagridPlaylistInfo.Items[SongIndex]).FilePath);

            waveOutDevice.Init(audioFileReader);
           
            sliderFrom.Maximum = sliderProgress.Maximum = sliderTo.Maximum = ((DataItem)datagridPlaylistInfo.Items[SongIndex]).Ticks;
            sliderFrom.Value = sliderProgress.Value = 0;
            sliderTo.Value = sliderTo.Maximum;
            
            txtblkSongName.Text = ((DataItem)datagridPlaylistInfo.Items[SongIndex]).Name;
            txtblkProgress.Text = "00:00";
            txtblkFrom.Text = "From 00:00";
            txtblkTo.Text = "To 00:00";
           
            CurrentSongIndex = SongIndex;
        }

        private void sliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DraggingProgress && waveOutDevice.PlaybackState == PlaybackState.Paused)
            {
                uint Progress = UInt32.Parse(Math.Round(sliderProgress.Value).ToString());
                PLR.Position = TimeSpan.FromTicks(Progress);
                audioFileReader.CurrentTime = TimeSpan.FromTicks(Progress);
                
                txtblkProgress.Text = audioFileReader.CurrentTime.ToString(@"mm\:ss");
            }
            else if(sliderProgress.Value == sliderProgress.Maximum)
            {
                btnNext_Click(sender, e);
            }
        }

        private void sliderProgress_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            DraggingProgress = true;
            btnPause_Click(sender, e);
        }

        private void sliderProgress_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            DraggingProgress = false;
            if (sliderProgress.Value == sliderProgress.Maximum)
                btnNext_Click(sender, e);
            btnPlay_Click(sender, e);
        }

        private void btnPrev_Click(object sender, RoutedEventArgs e)
        {
            if(datagridPlaylistInfo.Items.Count != 0 && CurrentSongIndex != -1)
            {
                if (CurrentSongIndex != 0)
                    CurrentSongIndex--;
                else
                    CurrentSongIndex = datagridPlaylistInfo.Items.Count - 1;

                if (waveOutDevice != null && waveOutDevice.PlaybackState != PlaybackState.Stopped)
                {
                    PlaybackState oldPlaybackState = waveOutDevice.PlaybackState;

                    waveOutDevice.Stop();
                    PLR.Stop();

                    PreparePlay(CurrentSongIndex);

                    if (oldPlaybackState == PlaybackState.Playing)
                    {
                        ((Storyboard)this.Resources["Play"]).Begin();
                        waveOutDevice.Play();
                        PLR.Play();
                    }
                    else
                    {
                        ((Storyboard)this.Resources["Pause"]).Begin();
                        waveOutDevice.Play();
                        PLR.Play();
                        waveOutDevice.Pause();
                        PLR.Pause();
                    }
                }
            }
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (datagridPlaylistInfo.Items.Count != 0 && CurrentSongIndex != -1)
            {
                CurrentSongIndex++;
                CurrentSongIndex %= datagridPlaylistInfo.Items.Count;

                if (waveOutDevice != null && waveOutDevice.PlaybackState != PlaybackState.Stopped)
                {
                    PlaybackState oldPlaybackState = waveOutDevice.PlaybackState;

                    waveOutDevice.Stop();
                    PLR.Stop();

                    PreparePlay(CurrentSongIndex);

                    if (oldPlaybackState == PlaybackState.Playing)
                    {
                        ((Storyboard)this.Resources["Play"]).Begin();
                        waveOutDevice.Play();
                        PLR.Play();
                    }
                    else
                    {
                        ((Storyboard)this.Resources["Pause"]).Begin();
                        waveOutDevice.Play();
                        PLR.Play();
                        waveOutDevice.Pause();
                        PLR.Pause();
                    }
                }
            }
        }

        //https://ffmpeg.org/ffmpeg.html
        private void btnConvert_Click(object sender, RoutedEventArgs e)
        {
           
            if(waveOutDevice.PlaybackState != PlaybackState.Stopped && CurrentSongIndex != -1)
            {
                if (((DataItem)datagridPlaylistInfo.Items[CurrentSongIndex]).Format == "wav")
                {
                    if (sliderTo.Value > sliderFrom.Value)
                    {
                        btnPause_Click(sender, e);
                        CommonSaveFileDialog dialog = new CommonSaveFileDialog();
                        dialog.Filters.Add(new CommonFileDialogFilter("MP3 files","*.mp3"));
                        dialog.Title = "Save MP3 file";
                        CommonFileDialogResult result = dialog.ShowDialog();
                        if (result == CommonFileDialogResult.Ok)
                        {
                            this.Cursor = System.Windows.Input.Cursors.Wait;
                            ConvertingGrid.Visibility = Visibility.Visible;
                            ((Storyboard)this.Resources["Converting"]).Begin(this, true);
                            ((Storyboard)this.Resources["DisablePlayer"]).Begin();
                            btnConvert.IsEnabled = btnBrowse.IsEnabled = false;
                            String from = TimeSpan.FromTicks(UInt32.Parse(Math.Round(sliderFrom.Value).ToString())).ToString(@"hh\:mm\:ss\.fff");
                            String to = TimeSpan.FromTicks(UInt32.Parse(Math.Round(sliderTo.Value).ToString())).ToString(@"hh\:mm\:ss\.fff");
                            String filename = "\"" + dialog.FileName;
                            if (dialog.FileName.Length < 3 || dialog.FileName.Substring(dialog.FileName.Length - 3) != "mp3")
                                filename += ".mp3";
                            filename += "\"";
                            String args = String.Format("-i {0} -ss {1} -to {2} -vn -ar 44100 -ac 2 -ab 320k -f mp3 {3} -y", "\"" + ((DataItem)datagridPlaylistInfo.Items[CurrentSongIndex]).FilePath + "\"", from, to, filename);
                            new Thread(() =>
                            {
                                var converter = new Process
                                {
                                    StartInfo = new ProcessStartInfo
                                    {
                                        FileName = "ffmpeg.exe",
                                        Arguments = args,
                                        UseShellExecute = false,
                                        RedirectStandardOutput = true,
                                        CreateNoWindow = true
                                    }
                                };
                                converter.Start();
                                converter.WaitForExit();
                                Application.Current.Dispatcher.BeginInvoke(
                                  DispatcherPriority.Send,
                                  new Action(() =>
                                  {
                                      ((Storyboard)this.Resources["Converting"]).Stop(this);
                                      ConvertingGrid.Visibility = Visibility.Collapsed;
                                      ((Storyboard)this.Resources["EnablePlayer"]).Begin();
                                      btnConvert.IsEnabled = btnBrowse.IsEnabled = true;
                                      this.Cursor = System.Windows.Input.Cursors.Arrow;
                                      new CustomMessageBox("Done converting").ShowDialog();
                                  }));
                            }).Start();
                        }
                    }
                    else
                    {
                        new CustomMessageBox("Invalid from and to values").ShowDialog();
                    }
                }
                else
                {
                    new CustomMessageBox("Only wav files can be converted").ShowDialog();
                }
                
            }
        }

        private void sliderFrom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(txtblkFrom != null)
                txtblkFrom.Text = "From " + TimeSpan.FromTicks(UInt32.Parse(Math.Round(sliderFrom.Value).ToString())).ToString(@"mm\:ss");
        }

        private void sliderTo_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(txtblkTo != null)
                txtblkTo.Text = "To " + TimeSpan.FromTicks(UInt32.Parse(Math.Round(sliderTo.Value).ToString())).ToString(@"mm\:ss");
        }
    }
    public class DataItem
    {
        public string FilePath { get; set; }
        public uint Ticks { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Format { get; set; }
        public string Duration { get; set; }
        public string Size { get; set; }
        public string Bitrate { get; set; }
        public string SampleRate { get; set; }
        public string Channels { get; set; }
    }

    enum SizeUnit
    {
        None,
        MB,
        KB,
        B
    }
}