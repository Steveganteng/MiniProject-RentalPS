import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth/auth.service';
import {
  AdminRental,
  DashboardService
} from '../../services/dashboard.service';

interface SummaryCard {
  title: string;
  value: number | string;
  description: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  summaryCards: SummaryCard[] = [
    { title: 'Unit Konsol', value: '—', description: 'Total unit terdaftar' },
    { title: 'Ruangan', value: '—', description: 'Total ruangan terdaftar' },
    { title: 'Stik', value: '—', description: 'Total stik terdaftar' },
    { title: 'Rental Berjalan', value: '—', description: 'Rental berstatus Active' }
  ];

  recentRentals: AdminRental[] = [];
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadDashboard();
  }

  private normalizeStatus(value: string | number | null | undefined): string {
    return String(value ?? '').replace(/[\s_-]/g, '').toLowerCase();
  }

  loadDashboard(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.dashboardService.getDashboardData()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (data) => {
          this.summaryCards = [
            {
              title: 'Unit Konsol',
              value: data.consoleUnits.length,
              description: `${data.consoleUnits.filter(item => this.normalizeStatus(item.statusConsole) === 'available').length} tersedia`
            },
            {
              title: 'Ruangan',
              value: data.rooms.length,
              description: `${data.rooms.filter(item => this.normalizeStatus(item.statusRoom) === 'available').length} tersedia`
            },
            {
              title: 'Stik',
              value: data.sticks.length,
              description: `${data.sticks.filter(item => this.normalizeStatus(item.statusStick) === 'available').length} tersedia`
            },
            {
              title: 'Rental Berjalan',
              value: data.rentals.filter(item => this.normalizeStatus(item.statusRental) === 'active').length,
              description: `${data.rentals.filter(item => this.normalizeStatus(item.statusRental) === 'pending').length} menunggu konfirmasi`
            }
          ];

          this.recentRentals = [...data.rentals]
            .sort((a, b) => b.id - a.id)
            .slice(0, 5);
        },
        error: (error) => {
          this.errorMessage =
            error?.error?.message ||
            'Data dashboard gagal dimuat. Pastikan akun memiliki role Admin dan API berjalan.';
        }
      });
  }

  getStatusClass(status: string): string {
    switch (this.normalizeStatus(status)) {
      case 'active': return 'text-bg-success';
      case 'pending': return 'text-bg-warning';
      case 'completed': return 'text-bg-secondary';
      case 'cancelled': return 'text-bg-danger';
      default: return 'text-bg-light';
    }
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
