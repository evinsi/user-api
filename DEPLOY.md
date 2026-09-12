# UserApi — Sunucuya Yayınlama Rehberi

Bu uygulamayı (UI + API + Mongo + Redis) internete açık bir sunucuda yayınlamak için adım adım rehber. Domain olmadan, önce sadece **sunucu IP'si** ile.

## Genel bakış

```
İnternet
   │  http://SUNUCU_IP  (port 80)
   ▼
┌─────────────── SUNUCU (Docker) ───────────────┐
│  ui (Nginx)                                    │
│    ├─ Angular statik dosyaları servis eder     │
│    └─ /api → api container'ına yönlendirir      │
│         │                                      │
│         ▼                                      │
│  api (.NET)  →  mongo   +   redis              │
│  (dışarı kapalı — sadece iç ağ)                │
└────────────────────────────────────────────────┘
```
Dışarı açık tek kapı: **ui, port 80**. Mongo/Redis/API yalnızca iç ağda.

---

## 0. Ön hazırlık: kodu bir git deposuna koy
Sunucuya kodu çekmenin en kolay yolu git. Projeyi GitHub'a (özel repo) gönder:

```bash
cd /Users/macbookair/Desktop/adisyo-projeler/user-api
git init
git add .
git commit -m "İlk sürüm"
# GitHub'da boş bir repo aç, sonra:
git remote add origin https://github.com/KULLANICI/user-api.git
git push -u origin main
```
> `.gitignore` sayesinde `.env`, `node_modules`, `bin/obj` gitmez. `.env`'i asla göndermiyoruz.

---

## 1. Sunucu (VPS) oluştur
Bir sağlayıcıdan **Ubuntu 24.04** sunucu al (en küçük paket yeter — 1-2 GB RAM):
- **Hetzner** (~€4/ay, en ucuz) · **DigitalOcean** (~$6/ay, dökümanları çok iyi)

Kurulumda **SSH key** eklersen şifresiz ve güvenli bağlanırsın. Sunucunun bir **public IP**'si olacak (örn. `1.2.3.4`).

---

## 2. Sunucuya bağlan (SSH)

```bash
ssh root@SUNUCU_IP
```
İlk bağlantıda parmak izini "yes" ile onayla.

---

## 3. Docker'ı kur
Ubuntu'da resmi kurulum scripti en kolayı:

```bash
curl -fsSL https://get.docker.com | sh
```
Kontrol:
```bash
docker --version
docker compose version
```

---

## 4. Kodu sunucuya al

```bash
git clone https://github.com/KULLANICI/user-api.git
cd user-api
```

---

## 5. Secret'ları ayarla (.env)
Şablonu kopyalayıp **güçlü** değerlerle doldur:

```bash
cp .env.example .env
nano .env
```
`.env` içeriği (örnek — kendi güçlü değerlerini üret):
```
MONGO_USERNAME=admin
MONGO_PASSWORD=Cok-Guclu-Bir-Sifre-123!
JWT_KEY=uzun-rastgele-en-az-32-karakter-gizli-anahtar-abc123
```
> Güçlü değer üretmek için: `openssl rand -base64 32`
> `nano`'da kaydet: `Ctrl+O`, `Enter`, sonra çık: `Ctrl+X`.

---

## 6. Uygulamayı başlat

```bash
docker compose -f docker-compose.prod.yml up -d --build
```
İlk derleme birkaç dakika sürer (imajları indirip UI/API'yi derler). Durum:
```bash
docker compose -f docker-compose.prod.yml ps
```

---

## 7. Firewall — port 80'i aç
```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw enable
```

---

## 8. Aç ve ilk kullanıcıyı oluştur
Tarayıcıda:
```
http://SUNUCU_IP
```
Login ekranı gelir. Veritabanı boş olduğu için önce bir kullanıcı kaydet (API register endpoint'i UI'da "Ekle" ile de olur; ya da terminalden):

```bash
curl -X POST http://SUNUCU_IP/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"name":"Admin","email":"admin@example.com","password":"1234"}'
```
Sonra bu bilgilerle giriş yap. 🎉 Uygulama internette yayında.

---

## 9. (Sonra) Domain + HTTPS
Bir domain alıp DNS'inde bir **A kaydı** oluştur: `app.alanadin.com → SUNUCU_IP`.
HTTPS için en kolay yol **Caddy** (Let's Encrypt sertifikasını otomatik alır). `docker-compose.prod.yml`'a ekle:

```yaml
  caddy:
    image: caddy:2
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
      - caddy-data:/data
    depends_on:
      - ui
```
`ui` servisinden `ports` satırını **kaldır** (artık dışarı Caddy bakıyor). Volumes'a `caddy-data:` ekle. Proje köküne `Caddyfile`:
```
app.alanadin.com {
    reverse_proxy ui:80
}
```
Sonra tekrar `docker compose -f docker-compose.prod.yml up -d`. Caddy sertifikayı otomatik alır; `https://app.alanadin.com` 🔒 hazır.

---

## Operasyon komutları

```bash
# Logları izle
docker compose -f docker-compose.prod.yml logs -f

# Yeniden başlat
docker compose -f docker-compose.prod.yml restart

# Kod güncelledikten sonra (git pull sonrası) yeniden derle
git pull
docker compose -f docker-compose.prod.yml up -d --build

# Durdur
docker compose -f docker-compose.prod.yml down

# Durdur + verileri de sil (DİKKAT: DB gider)
docker compose -f docker-compose.prod.yml down -v
```
