using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace FaceRecognition
{
    public partial class MainWindow : Window
    {
        private VideoCapture? _capture;
        private bool _isCameraRunning = false;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Mat frame1 = new Mat();
        private CascadeClassifier? _faceCascade;
        private Mat? _savedFace;  // Store saved face

        public MainWindow()
        {
            InitializeComponent();
        }

        public void StartButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();

            _capture = new VideoCapture("rtsp://admin:Subodh@123@192.168.1.64:554");

            frame1 = new Mat();

            // Start each camera on a separate thread
            Task.Run(() => StartCameraStream(_capture, frame1, CameraFeed, cancellationTokenSource.Token));
        }
        private void StartCameraStream(VideoCapture capture, Mat frame, System.Windows.Controls.Image imageControl, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                capture.Read(frame);

                if (!frame.IsEmpty)
                {
                    var bitmap = BitmapSourceConverter(frame);

                    // Update the UI on the dispatcher thread
                    Dispatcher.Invoke(() =>
                    {
                        imageControl.Source = bitmap;

                    });
                }

                Thread.Sleep(35); // Sleep for 33ms (approx. 30 FPS)
            }
        }

        private Mat DetectAndExtractFace(Mat frame)
        {
            var grayFrame = new Mat();
            CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);

            var faces = _faceCascade.DetectMultiScale(grayFrame, 1.1, 4, new System.Drawing.Size(50, 50));
            if (faces.Length > 0)
            {
                var face = new Mat(frame, faces[0]);
                return face;
            }
            return null;
        }

        private bool CompareFaces(Mat savedFace, Mat currentFace)
        {
            if (savedFace == null || currentFace == null)
                return false;

            // Example using a basic comparison (pixel-wise similarity)
            // For actual use, consider implementing Dlib or similar
            return CvInvoke.Norm(savedFace, currentFace, NormType.L2) < 1000;
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                Mat frame = new Mat();
                _capture.Retrieve(frame);

                if (!frame.IsEmpty)
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filePath = $"D:\\CapturedFrames\\frame_{timestamp}.jpg";

                    Directory.CreateDirectory("D:\\CapturedFrames"); // Ensure directory exists
                    frame.Save(filePath); // Save the captured frame

                    MessageBox.Show($"Image captured and saved to {filePath}");
                }
                else
                {
                    MessageBox.Show("Failed to capture image.");
                }
            }
            else
            {
                MessageBox.Show("Camera is not running.");
            }
        }

        private BitmapSource BitmapSourceConverter(Mat mat)
        {
            return BitmapSource.Create(mat.Width, mat.Height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null,
                mat.DataPointer, mat.Step * mat.Height, mat.Step);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isCameraRunning && _capture != null)
            {
                _capture.Stop();
                _capture.Dispose();
                CameraFeed.Source = null;
                _isCameraRunning = false;
                MessageBox.Show("Camera stopped.");
            }
        }

        
    }
}