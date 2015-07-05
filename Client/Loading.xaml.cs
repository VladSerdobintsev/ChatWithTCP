using System.Windows;

namespace Client
{
    /// <summary>
    /// Логика взаимодействия для Loading.xaml
    /// </summary>
    public partial class Loading : Window
    {
        public Loading(Window parent)
        {
            InitializeComponent();
            Owner = parent;
            /*timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += (s, e) =>
            {
                int count=3-loadingTextBlock.Text.Replace("Загрузка","").Length;
                if (leftRight)
                {
                    if (count < 3)
                        loadingTextBlock.Text = loadingTextBlock.Text.Remove(loadingTextBlock.Text.Length - 1, 1);
                    if (3 - loadingTextBlock.Text.Replace("Загрузка", "").Length == 3)
                    {
                        leftRight = false;
                    }
                }
                else
                {
                    if (count > 0)
                        loadingTextBlock.Text += ".";
                    if (3 - loadingTextBlock.Text.Replace("Загрузка", "").Length == 0)
                    {
                        leftRight = true;
                    }
                }
                
            };
            timer.Start();
            _source = GetSource();
            loadingImage.Source = _source;
            ImageAnimator.Animate(_bitmap, OnFrameChanged);*/
        }
        /*bool leftRight=false;
        DispatcherTimer timer = new DispatcherTimer();
        Bitmap _bitmap; BitmapSource _source;
        private void OnFrameChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                    new Action(FrameUpdatedCallback));
        }
        private void FrameUpdatedCallback()
        {
            ImageAnimator.UpdateFrames();
            if (_source != null)
                _source.Freeze();
            _source = GetSource();
            loadingImage.Source = _source;  
            InvalidateVisual();
        }
        private BitmapSource GetSource()
        {
            if (_bitmap == null)
            {
                _bitmap = new Bitmap("loading.gif");
            }
            IntPtr handle = IntPtr.Zero;
            handle = _bitmap.GetHbitmap();
            return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }*/
    }
}
