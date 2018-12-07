﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace vizzy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public WpfHexaEditor.HexEditor hexa = new WpfHexaEditor.HexEditor();
        public vizzy.Visualization viz = new Visualization();
        List<PixelFormat> list_all_pixelformat;
        List<PixelFormat> list_combo_pixel_items;



        public MainWindow()
        {
            InitializeComponent();
            InitHexa();
            InitVizControls();
            var INFILE = @"..\..\..\testfiles\welcome";
            Grid.SetColumn(viz.ScrollViewer, 2);
            Grid.SetRow(viz.ScrollViewer, 1);
            grid.Children.Add(viz.ScrollViewer);
            viz.LoadData(INFILE);
            viz.UpdateImg();
            hexa.FileName = INFILE;
            this.Title = INFILE;

            viz.UpdateImg();
            SubscribeEvents();
        }

        private void InitVizControls()
        {
            List<int> list_bpp = new List<int>();
            list_all_pixelformat = new List<PixelFormat> {
                PixelFormats.Rgba128Float,
                PixelFormats.Gray32Float,
                PixelFormats.Gray16,
                PixelFormats.Prgba64,
                PixelFormats.Rgba64,
                PixelFormats.Rgb48,
                PixelFormats.Pbgra32,
                PixelFormats.Bgra32,
                PixelFormats.Bgr32,
                PixelFormats.Bgr101010,
                PixelFormats.Rgb24,
                PixelFormats.Bgr24,
                PixelFormats.Rgb128Float,
                PixelFormats.Bgr565,
                PixelFormats.Bgr555,
                PixelFormats.Gray8,
                PixelFormats.Gray4,
                PixelFormats.Gray2,
                PixelFormats.BlackWhite,
                PixelFormats.Indexed8,
                PixelFormats.Indexed4,
                PixelFormats.Indexed2,
                PixelFormats.Indexed1,
                PixelFormats.Prgba128Float,
                PixelFormats.Cmyk32};
            list_combo_pixel_items = list_all_pixelformat;
            foreach (var pixelformat in list_all_pixelformat)
            {
                list_bpp.Add((int)((PixelFormat)pixelformat).BitsPerPixel);
            }
            HashSet<int> set_bpp = new HashSet<int>(list_bpp);
            combo_bpp.ItemsSource = set_bpp.OrderByDescending(item => item);
            combo_bpp.SelectedItem = viz.PixelFormat.BitsPerPixel;
            List<PixelFormat> pixelsublist = list_all_pixelformat.Where(item => item.BitsPerPixel == (int)combo_bpp.SelectedValue).ToList<PixelFormat>();
            combo_pixel.ItemsSource = pixelsublist;
            combo_pixel.SelectedIndex = 0;
            combo_pixel.SelectedItem = viz.PixelFormat;
            txt_width.Text = viz.Width.ToString();
        }

        private void SubscribeEvents()
        {
            combo_pixel.SelectionChanged += Combo_pixel_SelectionChanged;
            viz.ImageUpdated += Viz_ImageUpdated;
            combo_bpp.SelectionChanged += Combo_bpp_SelectionChanged;

        }
        private void InitHexa()
        {
            hexa.Height = Double.NaN;
            hexa.Width= Double.NaN;

            Grid.SetRow(hexa, 1);
            Grid.SetColumn(hexa, 0);

            hexa.Margin = new Thickness(0);
            hexa.Background = new SolidColorBrush(Color.FromArgb(0xff, 0x1e, 0x1e, 0x1e));
            hexa.Foreground= new SolidColorBrush(Color.FromArgb(0xff, 0x92, 0xca, 0xf4));
            hexa.AllowAutoHightLighSelectionByte = false;
            hexa.AllowDrop = true;
            hexa.Drop += Hexa_Drop;
            hexa.FileDroppingConfirmation = false;
            hexa.SelectionStartChanged += Hexa_SelectionStartChanged;
            grid.Children.Add(hexa);
        }

        private void UpdateVizControls()
        {
            txt_width.Text = viz.Width.ToString();
            combo_bpp.SelectedValue = viz.PixelFormat.BitsPerPixel;
            combo_pixel.SelectedValue = viz.PixelFormat;
        }
        
        private void Bt_col_minus_Click(object sender, RoutedEventArgs e)
        {
            if (viz.SetWidth(viz.Width / 2)) txt_width.Text = viz.Width.ToString();

        }

        private void Bt_col_plus_Click(object sender, RoutedEventArgs e)
        {
            if (viz.SetWidth(viz.Width * 2)) txt_width.Text = viz.Width.ToString();

        }

        private void Combo_pixel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_pixel.SelectedItem != null)
            {
                viz.SetPixel((PixelFormat)combo_pixel.SelectedItem);
            }
        }

        private void Combo_bpp_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            List<PixelFormat> pixelsublist = list_all_pixelformat.Where(item => item.BitsPerPixel == (int)combo_bpp.SelectedValue).ToList<PixelFormat>();
            combo_pixel.ItemsSource = pixelsublist;
            combo_pixel.SelectedIndex = 0;
            viz.SetPixel((PixelFormat)combo_pixel.SelectedItem); 
            if (viz.Width * (int)combo_bpp.SelectedValue < 32) viz.SetWidth((int)Math.Ceiling(32.0 / (int)combo_bpp.SelectedValue));
        }

        private void Hexa_Drop(object sender, DragEventArgs e)
        {
            string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            hexa.FileName = fileList[0];
            this.Title = fileList[0];
            viz.LoadData(fileList[0]);
            viz.UpdateImg();
        }

        private void Hexa_SelectionStartChanged(object sender, EventArgs e)
        {
            Debug.WriteLine(hexa.SelectionStart);
            viz.VisOffset = hexa.SelectionStart;
            viz.UpdateImg();
        }

        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            viz.SwitchBackground(1);
        }

        private void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            viz.SwitchBackground(0);
        }

        private void Txt_width_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int step = (e.Delta > 0 ? 1 : -1);

            var firstw = viz.Width;
            var w = viz.Width + step;
            while (!viz.SetWidth(w))
            {
                w += step;
                if (w == 0)
                {
                    w = firstw;
                    break;
                }
                if (w > viz.Data.Length / viz.PixelFormat.BitsPerPixel * 8)
                {
                    w = firstw;
                    break;
                }
            }
            txt_width.Text = w.ToString();
        }

        private void PrintCallerName()
        {
            StackTrace stackTrace = new StackTrace();
            Console.WriteLine(stackTrace.GetFrame(1).GetMethod().Name);
        }

        private void Combo_bpp_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int step = (e.Delta > 0 ? -1 : 1);
            int limit = (e.Delta > 0 ? 0 : combo_bpp.Items.Count - 1);
            Debug.WriteLine("step " + step);

            if (combo_bpp.SelectedIndex != limit) { 
                combo_bpp.SelectedIndex += step;
            }
        }

        private void Combo_pixel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            int step = (e.Delta > 0 ? -1 : 1);
            int limit = (e.Delta > 0 ? 0 : combo_pixel.Items.Count - 1);
            if (combo_pixel.SelectedIndex != limit)
            {
                combo_pixel.SelectedIndex += step;
            }
        }

        private void Viz_ImageUpdated(object sender, EventArgs e)
        {
            UpdateVizControls();
        }

        private void Txt_width_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                int w;
                if (int.TryParse(txt_width.Text,out w))
                {
                    viz.SetWidth(w);
                    UpdateVizControls();
                }

            }
        }
    }
}