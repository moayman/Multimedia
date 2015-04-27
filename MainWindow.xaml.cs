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

namespace multimedia
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<DataItem> PlaylistData = new List<DataItem>();
        public MainWindow()
        {
            InitializeComponent();
            datagridPlaylistInfo.ItemsSource = PlaylistData;
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            //Pause if playing
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
    }
    public class DataItem
    {
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

    //loading
    //pause if playing before loading playlist
    //selected changed enables/disables player
    //add stop button
    //set sliders time
    //convert logic if wav

}
