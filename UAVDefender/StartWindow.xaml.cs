using MahApps.Metro.Controls;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Speech.Recognition;

namespace UAVDefender
{
    /// <summary>
    /// StartWindow.xaml 的交互逻辑
    /// </summary>
    /// 
    public partial class StartWindow : MetroWindow
    {
        int camNum = 0, selectedIndex = -1, selectedBackend = 0;
        List<string> camList = new(255);
        List<string> InferenceBackend = new();

        DispatcherTimer Timer = new DispatcherTimer();
        SpeechRecognitionEngine recognitionEngine = new SpeechRecognitionEngine();
        public StartWindow()
        {
            //LoadingAnimation.Visibility = Visibility.Hidden;
            InitializeComponent();
            CheckOS();
            this.Closing += Window_Closing;

            this.LoadingAnimation.Visibility = Visibility.Hidden;
            this.InteractiveZones.Visibility = Visibility.Visible;

            Choices choices = new Choices();
            choices.Add(new string[] {"Start", "Test", "启动", "测试" });
            recognitionEngine.LoadGrammar(new Grammar(choices));
            recognitionEngine.SpeechRecognized += SpeechRecognized;
            recognitionEngine.SetInputToDefaultAudioDevice();
            recognitionEngine.RecognizeAsync();
        }

        private void SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            if (e != null && e.Result.Confidence >= 0.5)
            {
                switch (e.Result.Text) 
                {
                    case "Start":
                        {
                            this.LoadingAnimation.Visibility = Visibility.Visible;
                            this.InteractiveZones.Visibility = Visibility.Hidden;
                            //MessageBox.Show(selectedIndex.ToString());
                            MainWindow mainWindow = new(selectedIndex, InferenceBackend[selectedBackend]);

                            if (selectedIndex == 0) mainWindow.usingCamera = false;
                            else mainWindow.usingCamera = true;

                            mainWindow.Show();
                            break;
                        }
                    case "Test":
                        {

                            MessageBox.Show("System test complete");
                            break;
                        }
                }
                recognitionEngine.RecognizeAsync();
            }
        }

        void CheckOS() 
        {

            this.cmbCameras.Items.Clear();  
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Camera'");

            camList.Add("Built in test video");
            foreach (var device in searcher.Get())
            {
                //MessageBox.Show(this.cmbCameras.Items.ToString());
                camList.Add($"{device["Caption"]}");
               
                camNum = camNum + 1;
            }

            cmbCameras.ItemsSource = camList;

            InferenceBackend.Add("CPU");
            try
            {
                var GPU = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                bool isGPUAvaliable = false;
                foreach (var device in GPU.Get()) 
                {
                    var MO = (ManagementObject)device;
                    if (MO["PNPDeviceID"].ToString().Contains("VEN_1002") && !InferenceBackend.Contains("ROCm(不支持)"))
                    {
                        InferenceBackend.Add("ROCm(Not supported)");
                        isGPUAvaliable = true;
                    }
                    if (MO["PNPDeviceID"].ToString().Contains("VEN_10DE") && !InferenceBackend.Contains("CUDA"))
                    {
                        InferenceBackend.Add("CUDA");
                        isGPUAvaliable = true;
                    }
                    if (MO["PNPDeviceID"].ToString().Contains("VEN_8086"))
                    {
                        isGPUAvaliable = true;
                    }
                }
                if (isGPUAvaliable) InferenceBackend.Add("DirectML(Recommended)");

                this.cmbBackend.ItemsSource = InferenceBackend;
            }
            catch (Exception)
            {
                
            }
        }

        private void cmbCameras_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedIndex = camList.IndexOf(e.AddedItems[0].ToString());
            //MessageBox.Show(selectedIndex.ToString());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.LoadingAnimation.Visibility = Visibility.Visible;
            this.InteractiveZones.Visibility = Visibility.Hidden;
            //MessageBox.Show(selectedIndex.ToString());
            MainWindow mainWindow = new(selectedIndex, InferenceBackend[selectedBackend]);

            if(selectedIndex == 0) mainWindow.usingCamera = false;
            else mainWindow.usingCamera = true;

            mainWindow.Show();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            new SVGViewer().Show();
            GC.Collect();
        }

        private void cmbBackend_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedBackend = InferenceBackend.IndexOf(e.AddedItems[0].ToString());
        }

        void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();

        }

    }
}
