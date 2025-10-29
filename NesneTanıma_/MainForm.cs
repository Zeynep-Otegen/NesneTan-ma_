using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NesneTanıma_
{
    public partial class MainForm : Form
    {
        private VideoCapture capture;
        private bool isCapturing = false;
        private Mat frame;
        private CascadeClassifier cascadeClassifier;
        private bool classifierLoaded = false; // yükleme kontrolü

        // UI Controls
        private PictureBox pictureBox;
        private Button btnStart;
        private Button btnStop;
        private Button btnLoadClassifier;
        private Label lblStatus;

        public MainForm()
        {
            InitializeComponent();
            InitializeCamera();
        }

        private void InitializeComponent()
        {
            // PictureBox
            this.pictureBox = new PictureBox();
            this.pictureBox.Size = new Size(640, 480);
            this.pictureBox.Location = new Point(10, 10);
            this.pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox.BorderStyle = BorderStyle.Fixed3D;

            // Buttons
            this.btnStart = new Button();
            this.btnStart.Text = "Başlat";
            this.btnStart.Location = new Point(10, 500);
            this.btnStart.Size = new Size(80, 30);
            this.btnStart.Click += BtnStart_Click;

            this.btnStop = new Button();
            this.btnStop.Text = "Durdur";
            this.btnStop.Location = new Point(100, 500);
            this.btnStop.Size = new Size(80, 30);
            this.btnStop.Click += BtnStop_Click;

            this.btnLoadClassifier = new Button();
            this.btnLoadClassifier.Text = "Sınıflandırıcı Yükle";
            this.btnLoadClassifier.Location = new Point(190, 500);
            this.btnLoadClassifier.Size = new Size(120, 30);
            this.btnLoadClassifier.Click += BtnLoadClassifier_Click;

            // Label
            this.lblStatus = new Label();
            this.lblStatus.Text = "Kamera hazır - Sınıflandırıcı yükleyin";
            this.lblStatus.Location = new Point(10, 540);
            this.lblStatus.Size = new Size(400, 20);
            this.lblStatus.ForeColor = Color.Blue;

            // Form
            this.Text = "OpenCV Nesne Tanıma - C# WinForms";
            this.ClientSize = new Size(660, 570);
            this.Controls.AddRange(new Control[] {
                pictureBox, btnStart, btnStop, btnLoadClassifier, lblStatus
            });
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeCamera()
        {
            try
            {
                capture = new VideoCapture(0); // Varsayılan kamera
                frame = new Mat();

                // Kamerayı test et
                capture.Read(frame);
                if (frame.IsEmpty)
                {
                    lblStatus.Text = "Kamera bulunamadı!";
                    return;
                }

                lblStatus.Text = "Kamera başlatıldı - Sınıflandırıcı yükleyin";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kamera hatası: {ex.Message}", "Hata",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoadClassifier_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                openFileDialog.Title = "OpenCV Sınıflandırıcı XML dosyasını seçin";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string path = openFileDialog.FileName;
                        if (!File.Exists(path))
                        {
                            MessageBox.Show("Seçilen dosya bulunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            classifierLoaded = false;
                            return;
                        }

                        // Var olanı temizle
                        cascadeClassifier?.Dispose();
                        string xmlPath = Path.GetFullPath(path);
                        if (!File.Exists(xmlPath))
                        {
                            MessageBox.Show("XML bulunamadı!");
                            return;
                        }
                        cascadeClassifier = new CascadeClassifier(xmlPath);



                        // Emgu/OpenCV sarmalayıcısının "empty" durumunu kontrol etmeye çalış
                        bool isEmpty = false;
                        try
                        {
                            var t = cascadeClassifier.GetType();
                            var mi = t.GetMethod("Empty");
                            if (mi != null && mi.ReturnType == typeof(bool) && mi.GetParameters().Length == 0)
                            {
                                isEmpty = (bool)mi.Invoke(cascadeClassifier, null);
                            }
                            else
                            {
                                var pi = t.GetProperty("Empty") ?? t.GetProperty("IsEmpty");
                                if (pi != null && pi.PropertyType == typeof(bool))
                                {
                                    isEmpty = (bool)pi.GetValue(cascadeClassifier);
                                }
                            }
                        }
                        catch
                        {
                            // Reflection başarısız olursa bile devam et; DetectMultiScale çağrısında hata yakalanacak.
                        }

                        if (isEmpty)
                        {
                            cascadeClassifier.Dispose();
                            cascadeClassifier = null;
                            classifierLoaded = false;
                            MessageBox.Show("Sınıflandırıcı yüklenemedi veya dosya geçerli bir cascade içermiyor.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            lblStatus.Text = "Sınıflandırıcı boş veya hatalı.";
                            lblStatus.ForeColor = Color.Red;
                            return;
                        }

                        classifierLoaded = true;
                        lblStatus.Text = $"Sınıflandırıcı yüklendi: {Path.GetFileName(path)}";
                        lblStatus.ForeColor = Color.Green;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Sınıflandırıcı yükleme hatası: {ex.Message}",
                            "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        classifierLoaded = false;
                        lblStatus.Text = "Sınıflandırıcı yüklenemedi.";
                        lblStatus.ForeColor = Color.Red;
                    }
                }
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (cascadeClassifier == null || !classifierLoaded)
            {
                MessageBox.Show("Önce geçerli bir sınıflandırıcı XML dosyası yükleyin!",
                    "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (capture == null)
            {
                MessageBox.Show("Kamera başlatılamadı!",
                    "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            isCapturing = true;
            Application.Idle += ProcessFrame;
            lblStatus.Text = "Nesne tanıma BAŞLATILDI";
            lblStatus.ForeColor = Color.Red;
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            isCapturing = false;
            Application.Idle -= ProcessFrame;
            lblStatus.Text = "Durduruldu - Yeni sınıflandırıcı yükleyebilirsiniz";
            lblStatus.ForeColor = Color.Blue;
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            if (!isCapturing) return;

            capture.Read(frame);
            if (!frame.IsEmpty)
            {
                DetectAndDrawObjects();
            }
        }

        private void DetectAndDrawObjects()
        {
            try
            {
                if (cascadeClassifier == null || !classifierLoaded)
                {
                    lblStatus.Text = "Sınıflandırıcı yüklü değil veya geçersiz.";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }

                using (var grayFrame = new Mat())
                {
                    CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);
                    CvInvoke.EqualizeHist(grayFrame, grayFrame);

                    Rectangle[] objects = cascadeClassifier.DetectMultiScale(
                        grayFrame,
                        scaleFactor: 1.1,
                        minNeighbors: 5,
                        minSize: new Size(30, 30)
                    );

                    using (var resultFrame = frame.Clone())
                    {
                        foreach (Rectangle obj in objects)
                        {
                            CvInvoke.Rectangle(resultFrame, obj, new MCvScalar(0, 255, 0), 2);
                            CvInvoke.PutText(resultFrame, "NESNE", new Point(obj.X, obj.Y - 10),
                                FontFace.HersheySimplex, 0.6, new MCvScalar(0, 255, 0), 2);
                        }

                        // Önceki Bitmap'i serbest bırak (memory leak önlemi)
                        Bitmap newBmp = resultFrame.ToBitmap();
                        var old = pictureBox.Image;
                        pictureBox.Image = newBmp;
                        old?.Dispose();

                        lblStatus.Text = $"Tespit edilen nesne: {objects.Length} adet";
                        lblStatus.ForeColor = Color.Green;
                    }
                }
            }
            catch (Exception ex)
            {
                // OpenCV'nin "!empty" benzeri hatalarını daha anlaşılır göster
                string msg = ex.Message ?? ex.ToString();
                if (msg.IndexOf("empty", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    msg.IndexOf("!empty", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    lblStatus.Text = "İşlem hatası: Sınıflandırıcı boş veya geçersiz. XML dosyasının doğru bir Haar/LBP cascade olduğundan emin olun.";
                }
                else
                {
                    lblStatus.Text = $"İşlem hatası: {msg}";
                }
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Kaynakları temizle
            isCapturing = false;
            Application.Idle -= ProcessFrame;

            capture?.Dispose();
            frame?.Dispose();
            cascadeClassifier?.Dispose();

            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
            }
        }
    }
}