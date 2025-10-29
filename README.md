# NesneTanıma_
  OpenCV ile Nesne Tanıma (C# - EmguCV)

Zip dosyasını açıp sadece NesneTanıma_ yazan dosya ile açabilirsiniz.Eğer yüklediğiniz yerin dosya yolunda türkçe karakter varsa seçilen .xml dosyaları bulunamaz
bu yüzden dosya yolunda türkçe karakterin olmadığı yere koyun.
İndir: https://github.com/Zeynep-Otegen/NesneTan-ma_/releases/tag/NesneTan%C4%B1ma

Bu proje, **EmguCV** kütüphanesi kullanılarak geliştirilen basit bir **gerçek zamanlı nesne tanıma uygulamasıdır**.  
Program, bilgisayar kamerasından alınan görüntüler üzerinde **Haar Cascade** sınıflandırıcılarını kullanarak belirli nesneleri tespit eder.

---

##  Özellikler

- Bilgisayar kamerasını otomatik başlatır.  
- Kullanıcı, bir **Haar Cascade (.xml)** dosyası seçerek istediği nesneyi tanımlayabilir.  
  (Örneğin: yüz, göz, gülümseme vb.)  
- Gerçek zamanlı olarak tespit edilen nesnelerin etrafına **yeşil dikdörtgen** çizilir.  
- Tespit edilen nesne sayısı **ekranda gösterilir**.  
- Basit ve kullanıcı dostu **WinForms** arayüzü.

---
##  Kullanılan Teknolojiler

 Bileşen - Açıklama 

 **C# (.NET Framework 4.7.2)** - Geliştirme dili 
 **Emgu.CV 4.12** - OpenCV’nin .NET sarmalayıcısı 
 **Emgu.CV.runtime.windows** - OpenCV çekirdek DLL’lerini içerir 
 **Haar Cascade XML** - Nesne tespiti için önceden eğitilmiş sınıflandırıcı 

## Geliştirilebilecek Özellikler
-Renk Tanıma: OpenCV’nin inRange() veya CvInvoke.CvtColor() fonksiyonlarıyla belirli renk aralıklarını algılamak.
-Nesne Adı Yazdırma: Tespit edilen nesnelerin üzerine, tanımlayıcı isim (örneğin “Yüz”, “Göz”, “Gülümseme”) yazmak.
-Ekran Görüntüsü Kaydetme: Tespit anında yakalanan görüntüyü otomatik kaydetmek.
-Farklı Modeller (DNN, YOLO, SSD): Daha gelişmiş nesne tanıma modelleriyle entegrasyon.



