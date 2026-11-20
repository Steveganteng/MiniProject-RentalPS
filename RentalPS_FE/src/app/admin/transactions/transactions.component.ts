import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../services/auth/auth.service';
import {
  AdminPayment,
  AdminRental,
  AdminTransactionData,
  AdminTransactionsService
} from '../../services/admin/admin-transactions.service';

 type TransactionTab = 'rentals' | 'payments';
 type RentalStatusAction = 'Active' | 'Completed' | 'Cancelled';
 type PaymentStatusAction = 'Success' | 'Failed';

@Component({
  selector: 'app-admin-transactions',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './transactions.component.html',
  styleUrl: './transactions.component.css'
})
export class TransactionsComponent implements OnInit {
  private readonly transactionsService = inject(AdminTransactionsService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  activeTab: TransactionTab = 'rentals';
  data: AdminTransactionData | null = null;
  selectedRentalId: number | null = null;
  isLoading = false;
  isSaving = false;
  savingLabel = '';
  pageError = '';
  successMessage = '';
  searchTerm = '';
  statusFilter = 'all';

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.isLoading = true;
    this.pageError = '';
    this.transactionsService.getTransactionData()
      .pipe(finalize(() => this.isLoading = false))
      .subscribe({
        next: (data: AdminTransactionData) => {
          this.data = data;
          if (this.selectedRentalId !== null && !data.rentals.some(item => item.id === this.selectedRentalId)) {
            this.selectedRentalId = null;
          }
        },
        error: (error: unknown) => {
          this.pageError = this.readError(error,
            'Data transaksi gagal dimuat. Pastikan API aktif dan akun yang digunakan memiliki role Admin.');
        }
      });
  }

  get rentals(): AdminRental[] {
    return [...(this.data?.rentals ?? [])].sort((a, b) => b.id - a.id);
  }

  get payments(): AdminPayment[] {
    return [...(this.data?.payments ?? [])].sort((a, b) => b.id - a.id);
  }

  get filteredRentals(): AdminRental[] {
    const term = this.searchTerm.trim().toLowerCase();
    return this.rentals.filter(item => {
      const matchesTerm = !term || item.rentalCode.toLowerCase().includes(term)
        || String(item.userId).includes(term)
        || String(item.id).includes(term);
      const matchesStatus = this.statusFilter === 'all'
        || this.normalizeStatus(item.statusRental) === this.normalizeStatus(this.statusFilter);
      return matchesTerm && matchesStatus;
    });
  }

  get filteredPayments(): AdminPayment[] {
    const term = this.searchTerm.trim().toLowerCase();
    return this.payments.filter(item => {
      const rental = this.findRental(item.rentalId);
      const matchesTerm = !term || item.paymentCode.toLowerCase().includes(term)
        || (rental?.rentalCode ?? '').toLowerCase().includes(term)
        || String(item.rentalId).includes(term);
      const matchesStatus = this.statusFilter === 'all'
        || this.normalizeStatus(item.paymentStatus) === this.normalizeStatus(this.statusFilter);
      return matchesTerm && matchesStatus;
    });
  }

  countRentalStatus(status: string): number {
    return this.rentals.filter(item => this.getRentalStatusLabel(item.statusRental) === status).length;
  }

  countPaymentStatus(status: string): number {
    return this.payments.filter(item => this.getPaymentStatusLabel(item.paymentStatus) === status).length;
  }

  get selectedRental(): AdminRental | null {
    return this.selectedRentalId === null ? null : this.findRental(this.selectedRentalId) ?? null;
  }

  get selectedRentalRooms() {
    return (this.data?.rentalRooms ?? []).filter(item => item.rentalId === this.selectedRentalId);
  }

  get selectedRentalSticks() {
    return (this.data?.rentalSticks ?? []).filter(item => item.rentalId === this.selectedRentalId);
  }

  get selectedRentalPayments(): AdminPayment[] {
    return this.payments.filter(item => item.rentalId === this.selectedRentalId);
  }

  selectTab(tab: TransactionTab): void {
    this.activeTab = tab;
    this.searchTerm = '';
    this.statusFilter = 'all';
    this.successMessage = '';
    this.pageError = '';
  }

  showRentalDetail(rental: AdminRental): void {
    this.selectedRentalId = this.selectedRentalId === rental.id ? null : rental.id;
  }

  openRentalFromPayment(payment: AdminPayment): void {
    this.selectedRentalId = payment.rentalId;
    this.activeTab = 'rentals';
  }

  updateRentalStatus(rental: AdminRental, status: RentalStatusAction): void {
    const descriptions: Record<RentalStatusAction, string> = {
      Active: 'mengaktifkan rental dan menandai Console/Stick yang disewa sebagai In Use',
      Completed: 'menyelesaikan rental dan mengembalikan inventaris yang digunakan menjadi Available',
      Cancelled: 'membatalkan rental yang masih Pending'
    };
    if (!window.confirm(`Anda yakin ingin ${descriptions[status]}?`)) return;

    this.isSaving = true;
    this.savingLabel = `Memperbarui status rental ${rental.rentalCode}...`;
    this.pageError = '';
    this.successMessage = '';

    this.transactionsService.updateRentalStatus(rental.id, status)
      .pipe(finalize(() => {
        this.isSaving = false;
        this.savingLabel = '';
      }))
      .subscribe({
        next: () => {
          this.successMessage = `Status rental ${rental.rentalCode} berhasil diubah menjadi ${status}.`;
          this.selectedRentalId = rental.id;
          this.loadData();
        },
        error: (error: unknown) => {
          this.pageError = this.readError(error, `Status rental ${rental.rentalCode} gagal diperbarui.`);
        }
      });
  }

  updatePaymentStatus(payment: AdminPayment, status: PaymentStatusAction): void {
    const message = status === 'Success'
      ? `Konfirmasi pembayaran ${payment.paymentCode} sebagai SUCCESS?`
      : `Tandai pembayaran ${payment.paymentCode} sebagai FAILED?`;
    if (!window.confirm(message)) return;

    this.isSaving = true;
    this.savingLabel = `Memproses pembayaran ${payment.paymentCode}...`;
    this.pageError = '';
    this.successMessage = '';

    this.transactionsService.updatePaymentStatus(payment.id, status)
      .pipe(finalize(() => {
        this.isSaving = false;
        this.savingLabel = '';
      }))
      .subscribe({
        next: () => {
          this.successMessage = `Pembayaran ${payment.paymentCode} berhasil diubah menjadi ${status}.`;
          this.loadData();
        },
        error: (error: unknown) => {
          this.pageError = this.readError(error, `Pembayaran ${payment.paymentCode} gagal diperbarui.`);
        }
      });
  }

  findRental(id: number): AdminRental | undefined {
    return this.data?.rentals.find(item => item.id === id);
  }

  getConsoleUnitLabel(id: number): string {
    const unit = this.data?.consoleUnits.find(item => item.id === id);
    if (!unit) return `Unit #${id}`;
    return `${unit.unitCode} — ${unit.consoleTypeName || this.getConsoleTypeName(unit.consoleTypeId)}`;
  }

  getConsoleTypeName(id: number): string {
    return this.data?.consoleTypes.find(item => item.id === id)?.name ?? `Tipe #${id}`;
  }

  getRoomLabel(id: number): string {
    const room = this.data?.rooms.find(item => item.id === id);
    if (!room) return `Room #${id}`;
    const type = this.data?.roomTypes.find(item => item.id === room.roomTypeId);
    return `${room.roomNumber}${type ? ` — ${type.name}` : ''}`;
  }

  getStickLabel(id: number): string {
    const stick = this.data?.sticks.find(item => item.id === id);
    if (!stick) return `Stick #${id}`;
    const type = this.data?.stickTypes.find(item => item.id === stick.stickTypeId);
    const generation = type ? this.getConsoleTypeName(type.consoleTypeId) : '';
    return `${stick.stickCode}${type ? ` — ${type.name}` : ''}${generation ? ` (${generation})` : ''}`;
  }

  getRentalStatusLabel(status: string | number): string {
    const normalized = this.normalizeStatus(status);
    if (normalized === '0') return 'Pending';
    if (normalized === '1') return 'Active';
    if (normalized === '2') return 'Completed';
    if (normalized === '3') return 'Cancelled';
    if (normalized === 'pending') return 'Pending';
    if (normalized === 'active') return 'Active';
    if (normalized === 'completed') return 'Completed';
    if (normalized === 'cancelled') return 'Cancelled';
    return String(status);
  }

  getPaymentStatusLabel(status: string | number): string {
    const normalized = this.normalizeStatus(status);
    if (normalized === '0' || normalized === 'pending') return 'Pending';
    if (normalized === '1' || normalized === 'success') return 'Success';
    if (normalized === '2' || normalized === 'failed') return 'Failed';
    return String(status);
  }

  getPaymentMethodLabel(method: string | number): string {
    const normalized = this.normalizeStatus(method);
    if (normalized === '0' || normalized === 'cash') return 'Cash';
    if (normalized === '1' || normalized === 'qris') return 'QRIS';
    if (normalized === '2' || normalized === 'transfer') return 'Transfer';
    return String(method);
  }

  rentalStatusClass(status: string | number): string {
    switch (this.getRentalStatusLabel(status)) {
      case 'Pending': return 'text-bg-warning';
      case 'Active': return 'text-bg-success';
      case 'Completed': return 'text-bg-secondary';
      case 'Cancelled': return 'text-bg-danger';
      default: return 'text-bg-light';
    }
  }

  paymentStatusClass(status: string | number): string {
    switch (this.getPaymentStatusLabel(status)) {
      case 'Pending': return 'text-bg-warning';
      case 'Success': return 'text-bg-success';
      case 'Failed': return 'text-bg-danger';
      default: return 'text-bg-light';
    }
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('id-ID', {
      style: 'currency', currency: 'IDR', maximumFractionDigits: 0
    }).format(amount ?? 0);
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }

  private normalizeStatus(value: string | number | null | undefined): string {
    return String(value ?? '').replace(/[\s_-]/g, '').toLowerCase();
  }

  private readError(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null) {
      const candidate = error as { error?: { message?: string } | string; message?: string };
      if (typeof candidate.error === 'object' && candidate.error?.message) return candidate.error.message;
      if (typeof candidate.error === 'string' && candidate.error) return candidate.error;
      if (candidate.message) return candidate.message;
    }
    return fallback;
  }
}
