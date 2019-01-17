﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace vizzy
{
    public class Visualization
    {
        public int Width;
        public int PaddedWidth;
        public int Height;
        public int stride;
        public long VisOffset;
        public double ScrollOffset;
        public double Scale;
        public byte[] Data;
        public PixelFormat PixelFormat;
        public Image Img;
        public ScrollViewer ScrollViewer;

        public event EventHandler ImageUpdated;
        public EventArgs e = null;
        public delegate void EventHandler(Visualization v, EventArgs e);
        public void OnImageUpdated()
        {
            EventHandler handler = ImageUpdated;
            if (null != handler) handler(this, EventArgs.Empty);
        }

        private static ImageBrush MakeAlphaBackground()
        {
            var alpha_background = new ImageBrush(new BitmapImage(new Uri(@"pack://application:,,,/vizzy;component/Resources/back.bmp")));
            alpha_background.AlignmentY = AlignmentY.Top;
            alpha_background.AlignmentX = AlignmentX.Left;
            alpha_background.Stretch = Stretch.None;
            alpha_background.TileMode = TileMode.Tile;
            alpha_background.Viewport = new Rect(0, 0, 12, 12);
            alpha_background.Viewbox = new Rect(0, 0, 0, 0);
            alpha_background.ViewportUnits = BrushMappingMode.Absolute;
            return alpha_background;
        }
        private List<Brush> Backgrounds = new List<Brush> { Brushes.Black, MakeAlphaBackground() };

        public Visualization()
        {
            Width = 16;
            PaddedWidth = 16;
            PixelFormat = PixelFormats.Gray8;
            Scale = 1;
            InitScrollViewer();
            InitImg();
        }

        public Visualization(string path) : this()
        {
            LoadData(path);
            UpdateImg();
        }


        private void InitScrollViewer()
        {
            ScrollViewer = new ScrollViewer();
            ScrollViewer.Margin = new System.Windows.Thickness(0);
            ScrollViewer.PanningMode = PanningMode.Both;
            ScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            ScrollViewer.Background = Backgrounds[0];
            ScrollViewer.PreviewKeyDown += ScrollViewer_PreviewKeyDown;
            ScrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            ScrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            //Grid.Column = "2" Grid.Row = "1" 
        }
        private void InitImg()
        {
            Img = new Image();
            Img.Margin = new System.Windows.Thickness(0);
            Img.Width = Double.NaN;
            Img.Height= Double.NaN;

            Img.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetBitmapScalingMode(Img, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(Img, EdgeMode.Aliased);
            Img.MinHeight = 200;
            Img.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            Img.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            Img.IsHitTestVisible = false;
            ScrollViewer.Content = Img;
        }

        public void LoadData(string path)
        {
            using (System.IO.FileStream fs = System.IO.File.Open(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                int numBytesToRead = Convert.ToInt32(fs.Length);
                Data = new byte[(numBytesToRead)];
                fs.Read(Data, 0, numBytesToRead);
            }
            
        }

        private BitmapSource MakeBitmap()
        {
            byte[] subarray = new byte[Data.Length - VisOffset];
            
            if (VisOffset < 0) VisOffset = 0;
            Array.ConstrainedCopy(Data, (int)VisOffset, subarray, 0, Data.Length - (int)VisOffset);
            subarray = PaddedSubrray(subarray);
            int stride = GetStride(Width, PixelFormat.BitsPerPixel);

            int pixels = subarray.Length / PixelFormat.BitsPerPixel * 8;
            PaddedWidth = stride * 8 / PixelFormat.BitsPerPixel;
            Debug.WriteLine(PaddedWidth);
            try
            {
                BitmapSource bitmapSource = BitmapSource.Create(PaddedWidth, pixels / PaddedWidth, 10, 10, PixelFormat,
                    BitmapPalettes.WebPalette, subarray, stride);
                return bitmapSource;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private byte[] PaddedSubrray(byte[] input)
        {
            int w = Width;
            int bpp = PixelFormat.BitsPerPixel;
            int W = w * bpp / 8; //width in bytes
            int stride = GetStride(w, bpp);
            





            if (W != stride)
            {
                if (bpp == 8 || bpp == 16) //padding in integer amount of bytes
                {
                    int pixels = input.Length / bpp * 8;
                    int h = pixels / w;
                    byte[] padded = new byte[stride * h];
                    int row_bytes = 0;

                    row_bytes = stride;
                    for (int r = 0; r < h; r++)
                    {
                        byte[] row = new byte[row_bytes];
                        Array.Copy(input, r * W, row, 0, W);
                        Array.Copy(row, 0, padded, r * row_bytes, row_bytes);
                    }
                    return padded;
                }
                else if (bpp < 8) // bit padding
                {
                    BitArray input_bits = new BitArray(input);
                    int pixels = input.Length / bpp;
                    int h = pixels / w;
                    BitArray padded_bits = new BitArray(stride * 8 * h);
                    W = w * bpp;
                    int stride_bits = stride * 8;
                    BitArray row = new BitArray(stride_bits);
                    byte[] padded = new byte[padded_bits.Length / 8];

                    for (int r = 0; r < h; r++)
                    {
                        for (int i = 0; i < stride_bits; i++)
                        {
                            if (i < W)
                            {
                                int I_in = r * W + i;
                                int I_out = r * stride_bits + i;
                                row[i] = input_bits[I_in];
                                //padded_bits[I_out] = input_bits[I_in];
                            }

                        }
                        BitArray wor = new BitArray(stride_bits);
                        for (int B = 0; B < stride_bits / 8; B++)
                        {
                            for (int u = 0; u < 8; u++)
                            {
                                wor[8 * B + u] = row[8 * B + 7 - u];
                            }

                        }

                        byte[] row_bytes = new byte[stride_bits / 8];
                        wor.CopyTo(row_bytes, 0);
                            //row.CopyTo(row_bytes, 0);
                        row_bytes.CopyTo(padded, r * stride);
                    }
                    //byte[] padded = new byte[padded_bits.Length / 8];
                    //padded_bits.CopyTo(padded, 0);
                    return padded;
                }
            }
            return input;



        }
        private int GetStride(int w, int bpp)
        {
            if (bpp == 24) return w * 3;
            else return ((((w * bpp) - 1) / 32) + 1) * 4;
        }

        public bool SetWidth(int w)
        {
            var oldWidth = Width;
            try
            {
                Width = w;
                UpdateImg();
                return true;
            }
            catch (Exception x)
            {
                Width = oldWidth;
                Debug.WriteLine(x.ToString() + " @ SetWidth(" + w.ToString() + ")");
                return false;
            }
        }
        public bool SetPixel(PixelFormat pf)
        {
            try
            {
                PixelFormat = pf;
                UpdateImg();
                return true;
            }
            catch (Exception x)
            {
                Debug.WriteLine(x.ToString() + " @ SetPixel(" + pf.ToString() + ")");
                return false;
            }
        }
        public void UpdateImg()
        {
            Img.Source = MakeBitmap();
            Img.Width = PaddedWidth * Scale;
            if (ImageUpdated != null)
            {
                ImageUpdated(this, e);
            }
        }

        public void SwitchBackground(int i)
        {
            ScrollViewer.Background = Backgrounds[i];
        }

        private void ScrollViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) || (Keyboard.Modifiers == ModifierKeys.Shift))
            {
                ScrollOffset = ScrollViewer.VerticalOffset / ScrollViewer.ScrollableHeight;
                Debug.WriteLine("offset = " + ScrollOffset);
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Delta > 0)
                {
                    Scale *= 1.2;
                    Img.Width = Width * Scale;
                    Img.Height *= Scale;
                }
                else if (e.Delta < 0)
                {
                    Scale /= 1.2;
                    if (Scale < 1) Scale = 1;
                    Img.Width = Width * Scale;
                    Img.Height /= Scale;
                }

            }
            else if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                (sender as ScrollViewer).ScrollToVerticalOffset((sender as ScrollViewer).ContentVerticalOffset);

                (sender as ScrollViewer).ScrollToHorizontalOffset((sender as ScrollViewer).ContentHorizontalOffset - e.Delta);
            }
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if ((Keyboard.Modifiers == ModifierKeys.Control) || (Keyboard.Modifiers == ModifierKeys.Shift))
                try
                {
                    ScrollViewer.ScrollToVerticalOffset(ScrollOffset * ScrollViewer.ScrollableHeight);
                }
                catch (Exception x)
                {
                    Debug.WriteLine(x);
                }
        }
    }
}
