using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace multimedia
{
	/// <summary>
	/// Interaction logic for CustomMessageBox.xaml
	/// </summary>
	public partial class CustomMessageBox : Window
	{
        public CustomMessageBox(string MSG)
        {   
            InitializeComponent();
            Message.Text = MSG;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
		
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
	}
}