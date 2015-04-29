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

namespace multimedia
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int CurrentSongIndex = -1;
        bool DraggingProgress = false;

        IWavePlayer waveOutDevice;
        AudioFileReader audioFileReader;
        DispatcherTimer ProgressUpdater;

        List<DataItem> PlaylistData = new List<DataItem>();
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
                    String[] files = Directory.GetFiles(FolderName, "*.*", SearchOption.AllDirectories).Where(s => s.EndsWith(".mp3") || s.EndsWith(".wav")).Cast<String>().ToArray();
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
                            PlaylistData.Add(new DataItem
                            {
                                FilePath = file,
                                Ticks = UInt32.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Media.Duration).ValueAsObject.ToString()),
                                Name = fileinfo.Properties.GetProperty(SystemProperties.System.Title).ValueAsObject.ToString(),
                                Artist = fileinfo.Properties.GetProperty(SystemProperties.System.Music.DisplayArtist).ValueAsObject.ToString(),
                                Album = fileinfo.Properties.GetProperty(SystemProperties.System.Music.AlbumTitle).ValueAsObject.ToString(),
                                Year = fileinfo.Properties.GetProperty(SystemProperties.System.Media.Year).ValueAsObject.ToString(),
                                Genre = ((string[])fileinfo.Properties.GetProperty(SystemProperties.System.Music.Genre).ValueAsObject)[0],
                                Format = fileinfo.Properties.GetProperty(SystemProperties.System.FileExtension).ValueAsObject.ToString().Substring(1),
                                Duration = TimeSpan.FromTicks(UInt32.Parse(fileinfo.Properties.GetProperty(SystemProperties.System.Media.Duration).ValueAsObject.ToString())).ToString(@"mm\:ss"),
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
            }
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            if (waveOutDevice != null)
            {
                waveOutDevice.Pause();

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
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
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
            waveOutDevice = new WaveOut();
            audioFileReader = new AudioFileReader(((DataItem)datagridPlaylistInfo.Items[SongIndex]).FilePath);

            waveOutDevice.Init(audioFileReader);

            sliderFrom.Maximum = sliderProgress.Maximum = sliderTo.Maximum = ((DataItem)datagridPlaylistInfo.Items[SongIndex]).Ticks;
            sliderFrom.Value = sliderProgress.Value = 0;
            sliderTo.Value = sliderTo.Maximum;

            txtblkSongName.Text = ((DataItem)datagridPlaylistInfo.Items[SongIndex]).Name;
            txtblkProgress.Text = "00:00";

            CurrentSongIndex = SongIndex;
        }

        private void sliderProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DraggingProgress && waveOutDevice.PlaybackState == PlaybackState.Paused)
            {
                uint Progress = UInt32.Parse(Math.Round(sliderProgress.Value).ToString());
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

                    PreparePlay(CurrentSongIndex);

                    if (oldPlaybackState == PlaybackState.Playing)
                    {
                        ((Storyboard)this.Resources["Play"]).Begin();
                        waveOutDevice.Play();
                    }
                    else
                    {
                        ((Storyboard)this.Resources["Pause"]).Begin();
                        waveOutDevice.Play();
                        waveOutDevice.Pause();
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

                    PreparePlay(CurrentSongIndex);

                    if (oldPlaybackState == PlaybackState.Playing)
                    {
                        ((Storyboard)this.Resources["Play"]).Begin();
                        waveOutDevice.Play();
                    }
                    else
                    {
                        ((Storyboard)this.Resources["Pause"]).Begin();
                        waveOutDevice.Play();
                        waveOutDevice.Pause();
                    }
                }
            }
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
