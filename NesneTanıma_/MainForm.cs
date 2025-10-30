using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Face;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace NesneTanıma_
{
    public partial class MainForm : Form
    {
        private VideoCapture capture;
        private bool isCapturing = false;
        private Mat frame;
        private CascadeClassifier cascadeClassifier;
        private bool classifierLoaded = false;

        // Yüz tanıma için yeni değişkenler
        private LBPHFaceRecognizer faceRecognizer;
        private Dictionary<int, string> faceLabels;
        private bool isTrainingMode = false;
        private string trainingDataPath = "face_data";
        private CascadeClassifier faceCascade;

        // UI Controls
        private PictureBox pictureBox;
        private Button btnStart;
        private Button btnStop;
        private Button btnLoadClassifier;
        private Button btnTrainFace;
        private Button btnStartRecognition;
        private Label lblStatus;

        public MainForm()
        {
            InitializeComponent();
            InitializeCamera();
            InitializeFaceRecognition();
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
            this.btnStart.Text = "Nesne Tanıma Başlat";
            this.btnStart.Location = new Point(10, 500);
            this.btnStart.Size = new Size(120, 30);
            this.btnStart.Click += BtnStart_Click;

            this.btnStop = new Button();
            this.btnStop.Text = "Durdur";
            this.btnStop.Location = new Point(140, 500);
            this.btnStop.Size = new Size(80, 30);
            this.btnStop.Click += BtnStop_Click;

            this.btnLoadClassifier = new Button();
            this.btnLoadClassifier.Text = "Sınıflandırıcı Yükle";
            this.btnLoadClassifier.Location = new Point(230, 500);
            this.btnLoadClassifier.Size = new Size(120, 30);
            this.btnLoadClassifier.Click += BtnLoadClassifier_Click;

            this.btnTrainFace = new Button();
            this.btnTrainFace.Text = "Yüz Eğit";
            this.btnTrainFace.Location = new Point(360, 500);
            this.btnTrainFace.Size = new Size(80, 30);
            this.btnTrainFace.Click += BtnTrainFace_Click;

            this.btnStartRecognition = new Button();
            this.btnStartRecognition.Text = "Yüz Tanıma Başlat";
            this.btnStartRecognition.Location = new Point(450, 500);
            this.btnStartRecognition.Size = new Size(120, 30);
            this.btnStartRecognition.Click += BtnStartRecognition_Click;

            // Label
            this.lblStatus = new Label();
            this.lblStatus.Text = "Kamera hazır - Sınıflandırıcı yükleyin";
            this.lblStatus.Location = new Point(10, 540);
            this.lblStatus.Size = new Size(600, 20);
            this.lblStatus.ForeColor = Color.Blue;

            // Form
            this.Text = "OpenCV Nesne ve Yüz Tanıma - C# WinForms";
            this.ClientSize = new Size(660, 570);
            this.Controls.AddRange(new Control[] {
                pictureBox, btnStart, btnStop, btnLoadClassifier,
                btnTrainFace, btnStartRecognition, lblStatus
            });
            this.FormClosing += MainForm_FormClosing;
        }

        private void InitializeCamera()
        {
            try
            {
                capture = new VideoCapture(0);
                frame = new Mat();
                if (!capture.IsOpened)
                {
                    MessageBox.Show("Kamera açılamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    capture = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kamera başlatılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                capture = null;
            }
        }

        private void InitializeFaceRecognition()
        {
            try
            {
                // Face recognizer ve label dictionary her durumda oluşturulsun
                faceRecognizer = new LBPHFaceRecognizer(1, 8, 8, 8, 200);
                faceLabels = new Dictionary<int, string>();

                // Yüz cascade yüklenecekse yükle, yoksa uyar ve devam et (butonlarda kontrol yap)
                string faceCascadePath = "haarcascade_frontalface_default.xml";
                if (!File.Exists(faceCascadePath))
                {
                    MessageBox.Show("Yüz tanıma için cascade dosyası bulunamadı. Lütfen 'haarcascade_frontalface_default.xml' dosyasını uygulama dizinine ekleyin. Yüz tanıma devre dışı kalacaktır.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    faceCascade = null;
                }
                else
                {
                    faceCascade = new CascadeClassifier(faceCascadePath);
                }

                // Eğitim verilerini yükle (model varsa)
                LoadTrainingData();

                lblStatus.Text = "Yüz tanıma sistemi hazır";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yüz tanıma başlatma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                faceCascade = null;
            }
        }

        private void LoadTrainingData()
        {
            try
            {
                if (!Directory.Exists(trainingDataPath))
                {
                    Directory.CreateDirectory(trainingDataPath);
                    return;
                }

                string modelFile = Path.Combine(trainingDataPath, "face_model.xml");
                string labelsFile = Path.Combine(trainingDataPath, "face_labels.txt");

                if (File.Exists(modelFile) && File.Exists(labelsFile) && faceRecognizer != null)
                {
                    // Modeli yükle
                    faceRecognizer.Read(modelFile);

                    // Etiketleri yükle
                    faceLabels.Clear();
                    string[] lines = File.ReadAllLines(labelsFile);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[0], out int id))
                        {
                            string name = parts[1];
                            faceLabels[id] = name;
                        }
                    }

                    lblStatus.Text = $"Yüz tanıma modeli yüklendi: {faceLabels.Count} kişi";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eğitim verisi yükleme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveTrainingData()
        {
            try
            {
                if (!Directory.Exists(trainingDataPath))
                    Directory.CreateDirectory(trainingDataPath);

                string modelFile = Path.Combine(trainingDataPath, "face_model.xml");
                string labelsFile = Path.Combine(trainingDataPath, "face_labels.txt");

                // Modeli kaydet
                faceRecognizer.Write(modelFile);

                // Etiketleri kaydet
                using (StreamWriter writer = new StreamWriter(labelsFile))
                {
                    foreach (var entry in faceLabels)
                    {
                        writer.WriteLine($"{entry.Key}:{entry.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Eğitim verisi kaydetme hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTrainFace_Click(object sender, EventArgs e)
        {
            if (capture == null)
            {
                MessageBox.Show("Kamera başlatılamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (faceCascade == null || faceRecognizer == null)
            {
                MessageBox.Show("Yüz cascade dosyası veya yüz tanıyıcı hazır değil.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string userName = Microsoft.VisualBasic.Interaction.InputBox("Lütfen kişinin adını girin:", "Yüz Eğitimi", "Kullanıcı");
            if (string.IsNullOrEmpty(userName))
                return;

            isTrainingMode = true;
            TrainFace(userName);
        }

        private void TrainFace(string userName)
        {
            try
            {
                List<Mat> trainingImages = new List<Mat>();
                List<int> labels = new List<int>();

                // Yeni kullanıcı ID'si belirle
                int newUserId = faceLabels.Count > 0 ? faceLabels.Keys.Max() + 1 : 1;

                lblStatus.Text = $"Yüz eğitimi başladı: {userName}. Lütfen kameraya bakın...";
                lblStatus.ForeColor = Color.Orange;

                int capturedFaces = 0;
                int maxFaces = 20; // Eğitim için maksimum yüz sayısı

                while (capturedFaces < maxFaces)
                {
                    capture.Read(frame);
                    if (!frame.IsEmpty)
                    {
                        using (var grayFrame = new Mat())
                        {
                            CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);
                            CvInvoke.EqualizeHist(grayFrame, grayFrame);

                            Rectangle[] faces = faceCascade.DetectMultiScale(
                                grayFrame,
                                scaleFactor: 1.1,
                                minNeighbors: 5,
                                minSize: new Size(30, 30)
                            );

                            using (var resultFrame = frame.Clone())
                            {
                                foreach (Rectangle face in faces)
                                {
                                    CvInvoke.Rectangle(resultFrame, face, new MCvScalar(255, 0, 0), 2);
                                    CvInvoke.PutText(resultFrame, $"Eğitiliyor: {userName}",
                                        new Point(face.X, face.Y - 10),
                                        FontFace.HersheySimplex, 0.6, new MCvScalar(255, 0, 0), 2);

                                    // Yüzü eğitim için kaydet
                                    Mat faceROI = new Mat(grayFrame, face);
                                    Mat resizedFace = new Mat();
                                    CvInvoke.Resize(faceROI, resizedFace, new Size(100, 100));

                                    trainingImages.Add(resizedFace);
                                    labels.Add(newUserId);
                                    capturedFaces++;

                                    // faceROI'yi serbest bırak
                                    faceROI.Dispose();
                                }

                                // Görüntüyü göster
                                Bitmap newBmp = resultFrame.ToBitmap();
                                var old = pictureBox.Image;
                                pictureBox.Image = newBmp;
                                old?.Dispose();

                                Application.DoEvents();
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(100); // 100ms bekle
                }

                // Eğitim verilerini kaydet
                if (trainingImages.Count > 0)
                {
                    faceLabels[newUserId] = userName;

                    // Mevcut etiketlere göre Update veya Train karar ver
                    if (faceLabels.Count > 1)
                    {
                        faceRecognizer.Update(trainingImages.ToArray(), labels.ToArray());
                    }
                    else
                    {
                        faceRecognizer.Train(trainingImages.ToArray(), labels.ToArray());
                    }

                    // Eğitim verilerini kaydet
                    SaveTrainingData();

                    lblStatus.Text = $"Yüz eğitimi tamamlandı: {userName} - {capturedFaces} görüntü";
                    lblStatus.ForeColor = Color.Green;
                }

                // Temizlik
                foreach (var img in trainingImages)
                    img.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yüz eğitimi hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isTrainingMode = false;
            }
        }

        private void BtnStartRecognition_Click(object sender, EventArgs e)
        {
            if (faceLabels == null || faceLabels.Count == 0)
            {
                MessageBox.Show("Önce yüz eğitimi yapmalısınız!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (capture == null)
            {
                MessageBox.Show("Kamera başlatılamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (faceCascade == null || faceRecognizer == null)
            {
                MessageBox.Show("Yüz cascade dosyası veya yüz tanıyıcı hazır değil.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isCapturing = true;
            Application.Idle += ProcessFaceRecognition;
            lblStatus.Text = "Yüz tanıma BAŞLATILDI";
            lblStatus.ForeColor = Color.Red;
        }

        private void ProcessFaceRecognition(object sender, EventArgs e)
        {
            if (!isCapturing || isTrainingMode) return;

            capture.Read(frame);
            if (!frame.IsEmpty)
            {
                RecognizeFaces();
            }
        }

        private void RecognizeFaces()
        {
            try
            {
                if (faceCascade == null || faceRecognizer == null)
                {
                    lblStatus.Text = "Yüz tanıma hazır değil.";
                    lblStatus.ForeColor = Color.Red;
                    return;
                }

                using (var grayFrame = new Mat())
                {
                    CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);
                    CvInvoke.EqualizeHist(grayFrame, grayFrame);

                    Rectangle[] faces = faceCascade.DetectMultiScale(
                        grayFrame,
                        scaleFactor: 1.1,
                        minNeighbors: 5,
                        minSize: new Size(30, 30)
                    );

                    using (var resultFrame = frame.Clone())
                    {
                        foreach (Rectangle face in faces)
                        {
                            using (Mat faceROI = new Mat(grayFrame, face))
                            using (Mat resizedFace = new Mat())
                            {
                                CvInvoke.Resize(faceROI, resizedFace, new Size(100, 100));

                                // Yüzü tanı
                                var prediction = faceRecognizer.Predict(resizedFace);
                                string label = "Bilinmeyen";
                                Color color = Color.Red;

                                if (prediction.Label != -1 && faceLabels.ContainsKey(prediction.Label))
                                {
                                    label = faceLabels[prediction.Label];
                                    color = Color.Green;
                                }

                                // Yüzü işaretle ve ismi yaz
                                CvInvoke.Rectangle(resultFrame, face, new MCvScalar(color.B, color.G, color.R), 2);
                                CvInvoke.PutText(resultFrame, label,
                                    new Point(face.X, face.Y - 10),
                                    FontFace.HersheySimplex, 0.6, new MCvScalar(color.B, color.G, color.R), 2);
                            }
                        }

                        // Görüntüyü göster
                        Bitmap newBmp = resultFrame.ToBitmap();
                        var old = pictureBox.Image;
                        pictureBox.Image = newBmp;
                        old?.Dispose();

                        lblStatus.Text = $"Tanınan yüz: {faces.Length} adet";
                        lblStatus.ForeColor = Color.Green;
                    }
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Yüz tanıma hatası: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
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
            Application.Idle -= ProcessFaceRecognition; // Yüz tanımayı durdur
            Application.Idle += ProcessFrame; // Nesne tanımayı başlat
            lblStatus.Text = "Nesne tanıma BAŞLATILDI";
            lblStatus.ForeColor = Color.Red;
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            isCapturing = false;
            Application.Idle -= ProcessFrame;
            Application.Idle -= ProcessFaceRecognition;
            lblStatus.Text = "Durduruldu - Yeni sınıflandırıcı yükleyebilirsiniz";
            lblStatus.ForeColor = Color.Blue;
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            if (!isCapturing || isTrainingMode) return;

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
            isCapturing = false;
            Application.Idle -= ProcessFrame;
            Application.Idle -= ProcessFaceRecognition;

            capture?.Dispose();
            frame?.Dispose();
            cascadeClassifier?.Dispose();
            faceCascade?.Dispose();
            faceRecognizer?.Dispose();

            if (pictureBox.Image != null)
            {
                pictureBox.Image.Dispose();
            }
        }

        // Diğer mevcut metodlar aynı kalacak...
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

                        cascadeClassifier?.Dispose();
                        string xmlPath = Path.GetFullPath(path);
                        if (!File.Exists(xmlPath))
                        {
                            MessageBox.Show("XML bulunamadı!");
                            return;
                        }
                        cascadeClassifier = new CascadeClassifier(xmlPath);

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
    }
}