using MahApps.Metro.Controls;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.IO.Ports;
using System.Media;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using YoloBackend.Scorer;
using YoloBackend.Scorer.Models;
using static System.Formats.Asn1.AsnWriter;


namespace UAVDefender
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public struct MatSurface
        {
            public int w, h;
            public string backend;
        }

        MatSurface surface;
        OpenCvSharp.VideoCapture? CameraCapture;

        public bool usingCamera;

        public int CAMNUM;

        public PerformanceCounter RAMCounter = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName),
                                  CPUCounter = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);

        YoloScorer<UAVModel> scorer = new YoloScorer<UAVModel>();

        bool played = false, hwAvaliable = false;

        SoundPlayer player;

        string backend, SerialPortName;

        SerialPort serialPort = new();

        public MainWindow(int camNum, string InferenceBackend)
        {
            InitializeComponent();

            player = new SoundPlayer();
            player.SoundLocation = ".\\ding.wav";
            player.Load();
            
            CAMNUM = camNum;
            backend = InferenceBackend;
            /*
            try
            {
                foreach (var portName in SerialPort.GetPortNames())
                {
                    SerialPort port = new();
                    port.PortName = portName;
                    port.Open();
                    port.BaudRate = 115200;
                    port.Write("U");
                    if (port.ReadByte() == 126)
                    {
                        hwAvaliable = true;
                        SerialPortName = portName;
                        MessageBox.Show(portName);
                        port.Close();
                        break;
                    }
                    port.Close();
                }

                serialPort.PortName = SerialPortName;
                serialPort.BaudRate = 115200;
                serialPort.Open();
            }
            catch (Exception)
            {

                
            }
            */

            this.Closing += Window_Closing;

            usingCamera = !(camNum == 0);
            InitOpenCVSharp();
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += ChangeTimeText;
            dispatcherTimer.Tick += DetectAndDraw;

            try
            {
                dispatcherTimer.Start();
            }
            catch (Exception e)
            {
                this.ErrorText.Text = e.ToString();
            }
        }

        void ChangeTimeText(object? sender, EventArgs e)
        {
            System.DateTime time = System.DateTime.Now;
            this.TimeText.Text = time.ToString("HH:mm:ss");
        }

        void InitOpenCVSharp()
        {
            var mat = new OpenCvSharp.Mat();
            if(usingCamera) 
            {

                try
                {
                    CameraCapture = new(CAMNUM - 1);
                    CameraCapture.Read(mat);
                    var ImageStream = mat.ToMemoryStream();
                }
                catch (Exception)
                {
                    if (MessageBox.Show("Camera not available.\nUse built in test video instead?", "ERROR", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        CameraCapture = new("video\\video.mp4");
                    }
                    else
                    {
                        MessageBox.Show("Invalid input stream.\nApplication will exit now.", "ERROR");
                        Application.Current.Shutdown();
                    }
                }
            }
            else CameraCapture = new("video\\video.mp4");

            CameraCapture.Read(mat);
            
            if(backend == "ROCm(Not supported)")
            {
                MessageBox.Show("ROCm does not support Windows yet.\nCurrently your inference backend will be forced to DirectML.");
                backend = "DirectML(Recommended)";
            }

            try
            {
                scorer = new YoloScorer<UAVModel>(".\\models\\uav.onnx", backend);
                surface.w = mat.Width; surface.h = mat.Height; surface.backend = scorer.backendType;
            }
            catch (Exception)
            {
            }
        }

        void DetectAndDraw(object? sender, EventArgs e)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            try
            {
                //Postprocess Image From OpenCVSharp
                OpenCvSharp.Mat mat = new OpenCvSharp.Mat();
                CameraCapture.Read(mat);
                var ImageStream = mat.ToMemoryStream();

                //Detect UAVs in ImageStream

                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(ImageStream);
                {
                    {
                        var predictions = scorer.Predict(image);
                        double maxscore = 0;

                        if (predictions.Count != 0)
                        {

                            foreach (var prediction in predictions) // draw predictions
                            {
                                var score = Math.Round(prediction.Score, 2);
                                maxscore = Math.Max(maxscore, score);

                                var (x, y) = ((prediction.Rectangle.Left + prediction.Rectangle.Right)/2, (prediction.Rectangle.Top + prediction.Rectangle.Bottom)/2);
                                if (hwAvaliable && serialPort.IsOpen)
                                {
                                    if(x > image.Width/2 + 10)
                                    {
                                        serialPort.Write("D");
                                    }
                                    if(x < image.Width / 2 - 10)
                                    {
                                        serialPort.Write("A");
                                    }
                                    if(y > image.Height / 2 + 10)
                                    {
                                        serialPort.Write("W");
                                    }
                                    if( y < image.Height / 2 - 10)
                                    {
                                        serialPort.Write("S");
                                    }
                                }


                                var p1 = new OpenCvSharp.Point(prediction.Rectangle.X, prediction.Rectangle.Y);
                                var p2 = new OpenCvSharp.Point(prediction.Rectangle.X + prediction.Rectangle.Width, prediction.Rectangle.Y + prediction.Rectangle.Height);

                                Cv2.Rectangle(mat, p1, p2, new Scalar(0, 0, 255), 3);
                                Cv2.PutText(mat, "UAV:" + score.ToString(), new OpenCvSharp.Point(prediction.Rectangle.X, prediction.Rectangle.Y - 4), HersheyFonts.HersheyPlain, 1, new Scalar(0, 0, 255), 2);
                            }

                            if(!played)
                            {
                                if (maxscore >= 0.3)
                                {
                                    player.Play();
                                    played = true;
                                    WarningZone.Visibility = Visibility.Visible;
                                    this.Background = Brushes.Red;
                                }
                                else
                                {
                                    played = false;
                                    WarningZone.Visibility = Visibility.Hidden;
                                    this.Background = Brushes.Black;
                                }
                            }

                        }
                        else
                        {
                            played = false;
                            WarningZone.Visibility = Visibility.Hidden;
                            this.Background = Brushes.Black;
                        }
                    }
                }

                stopWatch.Stop();
                this.DisplayImage.Source = BitmapSourceConverter.ToBitmapSource(mat);
                this.DisplayInfo.Text = "Resolution:" + surface.w.ToString() + "x" + surface.h.ToString() + "\nRAM:" + RAMCounter.NextValue() / 1024 + "KB\nCPU:" + (int)CPUCounter.NextValue() + "\nFrame time:" + stopWatch.ElapsedMilliseconds + "ms\n";
                if(!hwAvaliable)
                {
                    //this.DisplayInfo.Text += "UAV Defender跟瞄硬件不可用";
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Input stream was cut off.\nApplication will exit now.","UAVDefender");
                Application.Current.Shutdown();
            }

            GC.Collect();
        }

        void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if(MessageBox.Show("Are you sure to exit UAVDefender?", "UAVDefender", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                Application.Current.Shutdown();
            else e.Cancel = true;
            serialPort.Close();
        }
    }
}