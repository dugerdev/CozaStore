# GitHub'a Yükleme Adımları

## 1. GitHub'da Repository Oluştur
1. https://github.com adresine git
2. Sağ üstteki "+" butonuna tıkla → "New repository"
3. Repository adını gir: `CozaStore`
4. Public veya Private seç
5. **"Initialize this repository with a README" seçeneğini İŞARETLEME!**
6. "Create repository" butonuna tıkla

## 2. Terminal Komutları

GitHub'dan aldığın URL'yi kullanarak şu komutları çalıştır:

```bash
# Remote repository ekle (GitHub URL'ini buraya yapıştır)
git remote add origin https://github.com/KULLANICI_ADI/CozaStore.git

# Branch'i main olarak değiştir (GitHub'ın varsayılan branch'i)
git branch -M main

# GitHub'a yükle
git push -u origin main
```

## 3. Visual Studio / VS Code Git Changes Ekranından

### Visual Studio:
1. **Git Changes** sekmesine git
2. **"..." (üç nokta)** menüsüne tıkla
3. **"Remote"** > **"Add Remote"** seç
4. **Name:** `origin`
5. **URL:** GitHub'dan aldığın URL'yi yapıştır
6. **"Add"** butonuna tıkla
7. **"Sync"** veya **"Push"** butonuna tıkla

### VS Code:
1. **Source Control** sekmesine git (Ctrl+Shift+G)
2. **"..." (üç nokta)** menüsüne tıkla
3. **"Remote"** > **"Add Remote"** seç
4. **Name:** `origin`
5. **URL:** GitHub'dan aldığın URL'yi yapıştır
6. **"..."** menüsünden **"Push"** seç

## 4. Sonraki Değişiklikler İçin

Her değişiklikten sonra:

1. **Git Changes** ekranında değişiklikleri gör
2. **Commit message** yaz (örn: "Add detailed comments to entities")
3. **"Commit"** butonuna tıkla
4. **"Sync"** veya **"Push"** butonuna tıkla

VEYA terminal'de:
```bash
git add .
git commit -m "Commit mesajı buraya"
git push
```

## Önemli Notlar

- İlk push'tan sonra GitHub'da tüm dosyalarını göreceksin
- `.gitignore` dosyası sayesinde gereksiz dosyalar (bin, obj, .vs) yüklenmeyecek
- Her commit'ten sonra `git push` yaparak GitHub'ı güncelleyebilirsin

