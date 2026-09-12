// Göreli yol: tarayıcı bunu, sayfayı servis eden origin'in "/api" yoluna çevirir.
// - Geliştirmede: ng serve proxy'si /api -> localhost:5223'e iletir.
// - Canlıda: Nginx /api -> api container'ına iletir.
export const API_URL = '/api';
