import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { User } from '../../core/models/user.model';

@Component({
  selector: 'app-users',
  imports: [FormsModule],
  templateUrl: './users.html',
  styleUrl: './users.scss',
})
export class Users implements OnInit {
  private userService = inject(UserService);
  private auth = inject(AuthService);
  private router = inject(Router);

  users = signal<User[]>([]);
  loading = signal(true);
  error = signal('');

  // Form durumu: editingId doluysa "düzenleme", boşsa "ekleme" modundayız
  editingId = signal<string | null>(null);
  name = '';
  email = '';
  password = '';

  ngOnInit() {
    this.loadUsers();
  }

  loadUsers() {
    this.loading.set(true);
    this.userService.getAll().subscribe({
      next: (data) => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Liste alınamadı.');
        this.loading.set(false);
      },
    });
  }

  save() {
    this.error.set('');
    const id = this.editingId();

    if (id) {
      this.userService.update(id, this.name, this.email).subscribe({
        next: () => {
          this.resetForm();
          this.loadUsers();
        },
        error: () => this.error.set('Güncelleme başarısız.'),
      });
    } else {
      this.userService.create(this.name, this.email, this.password).subscribe({
        next: () => {
          this.resetForm();
          this.loadUsers();
        },
        error: () => this.error.set('Ekleme başarısız (email zaten kayıtlı olabilir).'),
      });
    }
  }

  edit(user: User) {
    this.editingId.set(user.id);
    this.name = user.name;
    this.email = user.email;
    this.password = '';
  }

  remove(user: User) {
    if (!confirm(`${user.name} silinsin mi?`)) {
      return;
    }
    this.userService.delete(user.id).subscribe({
      next: () => this.loadUsers(),
      error: () => this.error.set('Silme başarısız.'),
    });
  }

  resetForm() {
    this.editingId.set(null);
    this.name = '';
    this.email = '';
    this.password = '';
  }

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
