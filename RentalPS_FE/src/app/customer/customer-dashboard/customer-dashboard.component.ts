import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, OnDestroy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { concatMap, finalize, from, of, switchMap, toArray } from 'rxjs';
import {
  ConsoleType,
  ConsoleUnit,
  Payment,
  Rental,
  RentalRoom,
  RentalService,
  RentalStick,
  Room,
  RoomType,
  Stick,
  StickType
} from '../../services/customer/rental.service';

@Component({
  selector: 'app-customer-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './customer-dashboard.component.html',
  styleUrl: './customer-dashboard.component.css'
})
export class CustomerDashboardComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly rentalService = inject(RentalService);
  consoleTypes: ConsoleType[] = [];
  consoleUnits: ConsoleUnit[] = [];
  roomTypes: RoomType[] = [];
  rooms: Room[] = [];
  stickTypes: StickType[] = [];
  sticks: Stick[] = [];

  activeRental: Rental | null = null;
  activePayment: Payment | null = null;
  activeRentalRoom: RentalRoom | null = null;
  activeRentalSticks: RentalStick[] = [];
  rentalHistory: Rental[] = [];
  rentalRooms: RentalRoom[] = [];
  rentalSticks: RentalStick[] = [];
  payments: Payment[] = [];

  selectedRental: Rental | null = null;
  selectedPayment: Payment | null = null;
  cancellingRentalId: number | null = null;

  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  reminderMessage = '';

  private reminderIntervalId: number | undefined;
  private countdownIntervalId: number | undefined;
  private currentTime = Date.now();
  private shownReminderKeys = new Set<string>();

  bookingForm = this.fb.nonNullable.group({
    consoleUnitId: [0, [Validators.required, Validators.min(1)]],
    // Sesuai alur booking tim: Customer wajib memilih satu tipe/ruangan.
    roomId: [0, [Validators.required, Validators.min(1)]],
    stickQuantity: [0, [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)]],
    startTime: [this.getDefaultStartTime(), Validators.required],
    durationHour: [2, [Validators.required, Validators.min(1), Validators.max(24)]],
    paymentMethod: ['Qris', Validators.required]
  });

  ngOnInit(): void {
    this.loadAllData();
    this.countdownIntervalId = window.setInterval(() => {
      this.currentTime = Date.now();
    }, 1000);
    this.reminderIntervalId = window.setInterval(() => {
      this.checkBookingReminders();
    }, 30000);
  }

  get rentalCountdown(): { label: string; time: string } {
    const rental = this.activeRental;
    if (!rental?.startTime || !rental.endTime) {
      return { label: 'Jadwal sewa belum tersedia', time: '' };
    }

    const start = new Date(rental.startTime).getTime();
    const end = new Date(rental.endTime).getTime();
    if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start) {
      return { label: 'Jadwal sewa belum tersedia', time: '' };
    }
    if (this.currentTime < start) {
      return { label: 'Hitung mundur dimulai sesuai jadwal sewa.', time: '' };
    }
    if (this.currentTime >= end) {
      return { label: 'Waktu sewa sudah habis', time: '00:00:00' };
    }

    const remainingSeconds = Math.ceil((end - this.currentTime) / 1000);
    const hours = Math.floor(remainingSeconds / 3600);
    const minutes = Math.floor(remainingSeconds / 60) % 60;
    const seconds = remainingSeconds % 60;
    const time = [hours, minutes, seconds].map((part) => String(part).padStart(2, '0')).join(':');
    return { label: 'Sisa waktu sewa', time };
  }

  get minStartTime(): string {
    return this.toLocalDateTimeInput(new Date());
  }

  private toLocalDateTimeInput(date: Date): string {
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 16);
  }

  private getDefaultStartTime(): string {
    const start = new Date();
    start.setMinutes(start.getMinutes() + 30);
    start.setSeconds(0, 0);
    return this.toLocalDateTimeInput(start);
  }

  private normalizeStatus(value: string | number | null | undefined): string {
    return String(value ?? '').replace(/[\s_-]/g, '').toLowerCase();
  }

  loadAllData(preserveError = false): void {
    this.isLoading = true;
    if (!preserveError) this.errorMessage = '';

    this.rentalService.getDashboardData()
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (res) => {
          this.consoleTypes = res.consoleTypes ?? [];
          this.consoleUnits = res.consoleUnits ?? [];
          this.roomTypes = res.roomTypes ?? [];
          this.rooms = res.rooms ?? [];
          this.stickTypes = res.stickTypes ?? [];
          this.sticks = res.sticks ?? [];
          this.rentalHistory = res.rentals ?? [];
          this.checkBookingReminders();
          this.rentalRooms = res.rentalRooms ?? [];
          this.rentalSticks = res.rentalSticks ?? [];
          this.payments = res.payments ?? [];
          this.activeRental = [...this.rentalHistory]
            .filter((rental) => ['Active', 'Pending'].includes(this.getRentalStatusLabel(rental.statusRental)))
            .sort((a, b) => b.id - a.id)[0] ?? null;

          if (this.activeRental) {
            this.activePayment = this.getPaymentForRental(this.activeRental.id) ?? null;
            this.activeRentalRoom = this.getRentalRoom(this.activeRental.id) ?? null;
            this.activeRentalSticks = this.getRentalSticks(this.activeRental.id);
          } else {
            this.activePayment = null;
            this.activeRentalRoom = null;
            this.activeRentalSticks = [];
          }

          const firstUnit = this.availableUnits[0];
          if (firstUnit && !this.bookingForm.value.consoleUnitId) {
            this.bookingForm.patchValue({ consoleUnitId: firstUnit.id });
          }

          const firstRoom = this.availableRooms[0];
          if (firstRoom && !this.bookingForm.value.roomId) {
            this.bookingForm.patchValue({ roomId: firstRoom.id });
          }

          this.onConsoleUnitChanged();
        },
        error: () => {
          this.errorMessage = 'Kami belum bisa memuat halaman ini. Coba muat ulang halaman.';
        }
      });
  }

  get availableUnits(): ConsoleUnit[] {
    return this.consoleUnits.filter((unit) => {
      const type = this.getConsoleType(unit.consoleTypeId);
      return this.normalizeStatus(unit.statusConsole) === 'available' &&
        type?.isActive !== false;
    });
  }

  get availableRooms(): Room[] {
    return this.rooms.filter((room) => {
      const type = this.getRoomType(room.roomTypeId);
      return this.normalizeStatus(room.statusRoom) === 'available' &&
        type?.isActive !== false;
    });
  }

  get availableSticks(): Stick[] {
    const selectedUnitId = Number(this.bookingForm.controls.consoleUnitId.value);
    const selectedUnit = this.consoleUnits.find((unit) => unit.id === selectedUnitId);
    if (!selectedUnit) return [];

    return this.sticks.filter((stick) => {
      const stickType = this.getStickType(stick.stickTypeId);
      return this.normalizeStatus(stick.statusStick) === 'available' &&
        stickType?.consoleTypeId === selectedUnit.consoleTypeId &&
        stickType?.isActive !== false;
    });
  }

  onConsoleUnitChanged(): void {
    const quantity = this.bookingForm.controls.stickQuantity;
    const available = this.availableSticks.length;
    if (quantity.value > available) {
      quantity.setValue(available);
    }
  }

  getConsoleType(id: number): ConsoleType | undefined {
    return this.consoleTypes.find((item) => item.id === id);
  }

  getRoomType(id: number): RoomType | undefined {
    return this.roomTypes.find((item) => item.id === id);
  }

  getStickType(id: number): StickType | undefined {
    return this.stickTypes.find((item) => item.id === id);
  }

  getUnitLabel(unit: ConsoleUnit): string {
    const type = this.getConsoleType(unit.consoleTypeId);
    return type
      ? `${unit.unitCode} — ${type.name} (Rp ${this.formatCurrency(type.hourlyRate)}/jam)`
      : `${unit.unitCode} — tipe konsol belum tersedia`;
  }

  getRoomLabel(room: Room): string {
    const type = this.getRoomType(room.roomTypeId);
    if (!type) return `${room.roomNumber} — tipe ruangan belum tersedia`;
    return `${type.name} ${room.roomNumber} (+Rp ${this.formatCurrency(type.additionalHourlyRate)}/jam)`;
  }

  getStickLabel(stick: Stick): string {
    const type = this.getStickType(stick.stickTypeId);
    if (!type) return `${stick.stickCode} — tipe stik belum tersedia`;
    return `${stick.stickCode} — ${type.name} (+Rp ${this.formatCurrency(type.additionalHourlyRate)}/jam)`;
  }

  getUnitsByConsoleType(consoleTypeId: number): string {
    const values = this.consoleUnits
      .filter((unit) => unit.consoleTypeId === consoleTypeId)
      .map((unit) => unit.unitCode);
    return values.length ? values.join(', ') : '-';
  }

  getStickTypeByConsoleType(consoleTypeId: number): StickType | undefined {
    return this.stickTypes.find((item) =>
      item.consoleTypeId === consoleTypeId && item.isActive
    );
  }

  getRoomsByRoomType(roomTypeId: number): string {
    const values = this.rooms
      .filter((room) => room.roomTypeId === roomTypeId)
      .map((room) => room.roomNumber);
    return values.length ? values.join(', ') : '-';
  }

  getActiveUnitCode(unitId: number): string {
    const unit = this.consoleUnits.find((item) => item.id === unitId);
    if (!unit) return 'Konsol belum diketahui';
    return `${this.getConsoleType(unit.consoleTypeId)?.name || ''} — ${unit.unitCode}`;
  }


  getPaymentStatusClass(status: string | number): string {
    switch (this.normalizeStatus(this.getPaymentStatusLabel(status))) {
      case 'success': return 'bg-success';
      case 'pending': return 'bg-warning text-dark';
      case 'failed': return 'bg-danger';
      default: return 'bg-secondary';
    }
  }

  getPaymentForRental(rentalId: number): Payment | undefined {
    return [...this.payments]
      .filter((payment) => payment.rentalId === rentalId)
      .sort((a, b) => b.id - a.id)[0];
  }

  private get selectedUnit(): ConsoleUnit | undefined {
    const id = Number(this.bookingForm.controls.consoleUnitId.value);
    return this.consoleUnits.find((unit) => unit.id === id);
  }

  private get selectedRoom(): Room | undefined {
    const id = Number(this.bookingForm.controls.roomId.value);
    return this.rooms.find((room) => room.id === id);
  }

  get selectedSticks(): Stick[] {
    const quantity = this.bookingForm.controls.stickQuantity.value;
    const available = this.availableSticks;
    return Number.isInteger(quantity) && quantity > 0 && quantity <= available.length
      ? available.slice(0, quantity)
      : [];
  }

  get selectedConsoleRate(): number {
    return Number(this.getConsoleType(this.selectedUnit?.consoleTypeId ?? 0)?.hourlyRate ?? 0);
  }

  get selectedRoomRate(): number {
    return Number(this.getRoomType(this.selectedRoom?.roomTypeId ?? 0)?.additionalHourlyRate ?? 0);
  }

  get selectedStickRate(): number {
    return this.selectedSticks.reduce((total, stick) => {
      const rate = this.getStickType(stick.stickTypeId)?.additionalHourlyRate ?? 0;
      return total + Number(rate);
    }, 0);
  }

  get duration(): number {
    return Number(this.bookingForm.controls.durationHour.value) || 0;
  }

  // Diskon sesuai ketentuan tim: 3-4 jam 10%, 5-7 jam 15%, >8 jam 20%.
  // Tepat 8 jam belum masuk tier diskon berdasarkan ketentuan "lebih dari 8 jam".
  get durationDiscountPercent(): number {
    if (this.duration >= 3 && this.duration <= 4) return 10;
    if (this.duration >= 5 && this.duration <= 7) return 15;
    if (this.duration >= 8) return 20;
    return 0;
  }

  get selectedRoomIsVip(): boolean {
    const roomTypeName = this.getRoomType(this.selectedRoom?.roomTypeId ?? 0)?.name ?? '';
    return roomTypeName.toLowerCase().includes('vip');
  }

  get consoleBaseTotal(): number {
    return this.selectedConsoleRate * this.duration;
  }

  get consoleDiscountAmount(): number {
    return this.consoleBaseTotal * this.durationDiscountPercent / 100;
  }

  get consoleSubtotal(): number {
    return this.consoleBaseTotal - this.consoleDiscountAmount;
  }

  get roomBaseTotal(): number {
    return this.selectedRoomRate * this.duration;
  }

  get roomDiscountPercent(): number {
    return this.selectedRoomIsVip ? this.durationDiscountPercent : 0;
  }

  get roomDiscountAmount(): number {
    return this.roomBaseTotal * this.roomDiscountPercent / 100;
  }

  get roomSubtotal(): number {
    return this.roomBaseTotal - this.roomDiscountAmount;
  }

  get stickSubtotal(): number {
    // Semua stik terpilih dihitung per jam; stik tidak mendapatkan diskon durasi.
    return this.selectedStickRate * this.duration;
  }

  get totalAmount(): number {
    return this.consoleSubtotal + this.roomSubtotal + this.stickSubtotal;
  }

  private formatCurrency(value: number): string {
    return new Intl.NumberFormat('id-ID', { maximumFractionDigits: 0 }).format(value);
  }

  onSubmitBooking(): void {
    if (this.bookingForm.invalid || this.isSubmitting) {
      this.bookingForm.markAllAsTouched();
      return;
    }

    const formValue = this.bookingForm.getRawValue();
    const start = new Date(formValue.startTime);
    if (Number.isNaN(start.getTime()) || start.getTime() <= Date.now()) {
      this.errorMessage = 'Pilih waktu mulai setelah waktu sekarang.';
      return;
    }

    const duration = Number(formValue.durationHour);
    if (!Number.isInteger(duration) || duration < 1 || duration > 24) {
      this.errorMessage = 'Pilih lama bermain antara 1 dan 24 jam, tanpa pecahan.';
      return;
    }

    const unitId = Number(formValue.consoleUnitId);
    const roomId = Number(formValue.roomId);
    const stickIds = this.selectedSticks.map((stick) => stick.id);
    if (!this.availableRooms.some((room) => room.id === roomId)) {
      this.errorMessage = 'Pilih ruangan yang masih tersedia.';
      return;
    }
    if (stickIds.length !== formValue.stickQuantity) {
      this.errorMessage = 'Sesuaikan jumlah stik tambahan dengan stok yang tersedia.';
      return;
    }

    const end = new Date(start.getTime() + duration * 60 * 60 * 1000);
    const rentalPayload = {
      consoleUnitId: unitId,
      startTime: start.toISOString(),
      endTime: end.toISOString()
    };

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    let createdRentalId: number | null = null;

    this.rentalService.createRental(rentalPayload).pipe(
      // Urutkan proses: Rental -> Room -> Stick -> baca total terbaru -> Payment.
      switchMap((createdRental) => {
        createdRentalId = createdRental.id;
        const addRoom$ = this.rentalService.createRentalRoom({
          rentalId: createdRental.id,
          roomId
        });

        return addRoom$.pipe(
          concatMap(() => stickIds.length > 0
            ? from(stickIds).pipe(
                concatMap((stickId) => this.rentalService.createRentalStick({
                  rentalId: createdRental.id,
                  stickId
                })),
                toArray()
              )
            : of([])),
          concatMap(() => this.rentalService.getRental(createdRental.id)),
          concatMap((updatedRental) => this.rentalService.createPayment({
            rentalId: createdRental.id,
            amount: Number(updatedRental.totalAmount),
            paymentMethod: formValue.paymentMethod
          }))
        );
      }),
      finalize(() => (this.isSubmitting = false))
    ).subscribe({
      next: () => {
        this.successMessage = 'Pesanan Anda berhasil dibuat. Pembayaran sedang menunggu konfirmasi.';
        this.bookingForm.patchValue({
          stickQuantity: 0,
          startTime: this.getDefaultStartTime(),
          durationHour: 2
        });
        this.loadAllData();
      },
      error: () => {
        this.errorMessage = createdRentalId !== null
          ? 'Pesanan Anda sudah tercatat. Periksa riwayat sewa untuk melihat rincian pesanan dan pembayaran sebelum memesan lagi.'
          : 'Pesanan belum berhasil dibuat. Periksa pilihan Anda, lalu coba lagi.';
        // Muat ulang agar rental/detail yang mungkin sudah tersimpan sebelum
        // salah satu request berikutnya gagal tetap terlihat pada riwayat.
        this.loadAllData(true);
      }
    });
  }

  getRentalRoom(rentalId: number): RentalRoom | undefined {
    return this.rentalRooms.find((item) => item.rentalId === rentalId);
  }

  getRentalSticks(rentalId: number): RentalStick[] {
    return this.rentalSticks.filter((item) => item.rentalId === rentalId);
  }

  getRentalPayments(rentalId: number): Payment[] {
    return this.payments
      .filter((payment) => payment.rentalId === rentalId)
      .sort((a, b) => b.id - a.id);
  }

  getRentalConsoleSubtotal(rental: Rental): number {
    const roomSubtotal = this.getRentalRoom(rental.id)?.subTotal ?? 0;
    const stickSubtotal = this.getRentalSticks(rental.id)
      .reduce((total, item) => total + Number(item.subTotal || 0), 0);
    return Math.max(0, Number(rental.totalAmount || 0) - roomSubtotal - stickSubtotal);
  }

  isPaymentForCancelledRental(payment: Payment): boolean {
    const rental = this.getRentalById(payment.rentalId);
    return !!rental && this.getRentalStatusLabel(rental.statusRental) === 'Cancelled';
  }

  getRentalById(rentalId: number): Rental | undefined {
    return this.rentalHistory.find((rental) => rental.id === rentalId);
  }

  getRoomNumber(roomId: number): string {
    const room = this.rooms.find((item) => item.id === roomId);
    return room?.roomNumber ?? 'Nomor ruangan belum diketahui';
  }

  getStickInventoryLabel(stickId: number): string {
    const stick = this.sticks.find((item) => item.id === stickId);
    return stick ? this.getStickLabel(stick) : 'Stik tambahan';
  }

  getPaymentMethodLabel(method: string | number): string {
    if (typeof method === 'number') {
      return ['Tunai', 'QRIS', 'Transfer bank'][method] ?? 'Tidak diketahui';
    }
    const normalized = this.normalizeStatus(method);
    if (normalized === 'cash') return 'Tunai';
    if (normalized === 'qris') return 'QRIS';
    if (normalized === 'transfer') return 'Transfer bank';
    return 'Tidak diketahui';
  }

  getRentalStatusLabel(status: string | number): string {
    if (typeof status === 'number') {
      return ['Pending', 'Active', 'Completed', 'Cancelled'][status] ?? 'Tidak diketahui';
    }
    const normalized = this.normalizeStatus(status);
    const labels: Record<string, string> = {
      pending: 'Pending', active: 'Active', completed: 'Completed', cancelled: 'Cancelled'
    };
    return labels[normalized] ?? String(status);
  }

  getPaymentStatusLabel(status: string | number): string {
    if (typeof status === 'number') {
      return ['Pending', 'Success', 'Failed'][status] ?? 'Tidak diketahui';
    }
    const normalized = this.normalizeStatus(status);
    const labels: Record<string, string> = { pending: 'Pending', success: 'Success', failed: 'Failed' };
    return labels[normalized] ?? String(status);
  }

  getRentalStatusText(status: string | number): string {
    const labels: Record<string, string> = {
      Pending: 'Menunggu konfirmasi',
      Active: 'Sedang berlangsung',
      Completed: 'Selesai',
      Cancelled: 'Dibatalkan'
    };
    return labels[this.getRentalStatusLabel(status)] ?? 'Belum diketahui';
  }

  getPaymentStatusText(status: string | number): string {
    const labels: Record<string, string> = {
      Pending: 'Menunggu konfirmasi',
      Success: 'Berhasil',
      Failed: 'Gagal'
    };
    return labels[this.getPaymentStatusLabel(status)] ?? 'Belum diketahui';
  }

  openRentalDetail(rental: Rental): void {
    this.selectedPayment = null;
    this.selectedRental = rental;
  }

  closeRentalDetail(): void {
    this.selectedRental = null;
  }

  openPaymentDetail(payment: Payment): void {
    this.selectedRental = null;
    this.selectedPayment = payment;
  }

  closePaymentDetail(): void {
    this.selectedPayment = null;
  }

  canCancelRental(rental: Rental): boolean {
    const isPending = this.getRentalStatusLabel(rental.statusRental) === 'Pending';
    const hasSuccessfulPayment = this.getRentalPayments(rental.id).some((payment) =>
      this.getPaymentStatusLabel(payment.paymentStatus) === 'Success'
    );
    return isPending && !hasSuccessfulPayment;
  }

  cancelRental(rental: Rental): void {
    if (!this.canCancelRental(rental) || this.cancellingRentalId !== null) return;

    const confirmed = window.confirm(
      `Batalkan pesanan ${rental.rentalCode}? Pembayaran yang sedang menunggu konfirmasi juga tidak bisa diteruskan.`
    );
    if (!confirmed) return;

    this.cancellingRentalId = rental.id;
    this.errorMessage = '';
    this.successMessage = '';

    this.rentalService.cancelRental(rental.id)
      .pipe(finalize(() => (this.cancellingRentalId = null)))
      .subscribe({
        next: () => {
          this.successMessage = `Pesanan ${rental.rentalCode} sudah dibatalkan.`;
          this.selectedRental = null;
          this.loadAllData();
        },
        error: () => {
          this.errorMessage = 'Pesanan belum bisa dibatalkan. Hubungi petugas jika Anda perlu bantuan.';
        }
      });
  }

  private checkBookingReminders(): void {
  if (this.reminderMessage) {
    return;
  }

  const now = Date.now();

  for (const rental of this.rentalHistory) {
    const status = String(rental.statusRental).toLowerCase();

    if (!['pending', 'active'].includes(status)) {
      continue;
    }

    if (!rental.startTime || !rental.endTime) {
      continue;
    }

    const reminders = [
      {
        key: 'start',
        eventTime: new Date(rental.startTime).getTime(),
        message: `Sesi bermain ${rental.rentalCode} dimulai 5 menit lagi.`
      },
      {
        key: 'end',
        eventTime: new Date(rental.endTime).getTime(),
        message: `Sesi bermain ${rental.rentalCode} berakhir 5 menit lagi.`
      }
    ];

    for (const reminder of reminders) {
      const reminderKey = `${rental.id}-${reminder.key}`;
      const reminderTime = reminder.eventTime - 5 * 60 * 1000;

      if (
        Number.isNaN(reminder.eventTime) ||
        now < reminderTime ||
        now >= reminder.eventTime ||
        this.shownReminderKeys.has(reminderKey)
      ) {
        continue;
      }

      this.shownReminderKeys.add(reminderKey);
      this.reminderMessage = reminder.message;
      return;
    }
  }
}

  ngOnDestroy(): void {
    window.clearInterval(this.reminderIntervalId);
    window.clearInterval(this.countdownIntervalId);
  }
}
