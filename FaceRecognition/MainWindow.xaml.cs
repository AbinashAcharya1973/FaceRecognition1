using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;


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
                    try
                    {
                        var bitmap = BitmapSourceConverter(frame);
                        Mat testFrame = new Mat(480, 640, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
                        testFrame.SetTo(new MCvScalar(0, 255, 0)); // Green frame
                        BitmapSource testBitmap = BitmapSourceConverter(testFrame);
                        // Update the UI on the dispatcher thread
                        Dispatcher.Invoke(() =>
                        {
                            //BitmapSource testBitmap = new BitmapImage(new Uri("D:\\CapturedFrames\\frame_20241210_113439.jpg", UriKind.RelativeOrAbsolute));
                            //imageControl.Source = bitmap;
                            if (bitmap != null)
                            {
                                imageControl.Source = testBitmap;
                                Console.WriteLine("OK");
                            }
                            else
                            {
                                Console.WriteLine("BitmapSource conversion failed.");
                            }
                            //this.CameraFeed.Source = bitmap;

                        });
                    }
                    catch (Exception ex) {
                        MessageBox.Show("Error updating the image control:"+ex.Message);
                    }
                
                }
                else
                {
                    if (frame.IsEmpty)
                    {
                        MessageBox.Show("Frame is Blank");
                        Console.WriteLine("Frame is blank.");
                    }

                }

                Thread.Sleep(33); // Sleep for 33ms (approx. 30 FPS)
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
            try
            {
                // Convert Mat to Bitmap if necessary
                if (mat.Depth != Emgu.CV.CvEnum.DepthType.Cv8U || mat.NumberOfChannels != 3)
                {
                    throw new NotSupportedException("Mat format not supported. Ensure 8-bit depth and 3 channels.");
                }

                return BitmapSource.Create(
                    mat.Width, mat.Height,
                    96, 96, PixelFormats.Bgr24,
                    null,
                    mat.DataPointer,
                    mat.Step * mat.Height,
                    mat.Step);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting Mat to BitmapSource: {ex.Message}");
                return null;
            }
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