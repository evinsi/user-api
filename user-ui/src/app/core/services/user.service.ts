import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { API_URL } from '../constants';
import { User } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);

  getAll() {
    return this.http.get<User[]>(`${API_URL}/users`);
  }

  // Yeni kullanıcı eklemek = onu sisteme kaydetmek (şifresiyle) → register endpoint'i
  create(name: string, email: string, password: string) {
    return this.http.post(`${API_URL}/auth/register`, { name, email, password });
  }

  update(id: string, name: string, email: string) {
    return this.http.put(`${API_URL}/users/${id}`, { name, email });
  }

  delete(id: string) {
    return this.http.delete(`${API_URL}/users/${id}`);
  }
}
