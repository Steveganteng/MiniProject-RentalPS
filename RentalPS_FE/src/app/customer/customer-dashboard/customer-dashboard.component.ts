import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable, concatMap, defer, finalize, from, of, switchMap, toArray } from 'rxjs';
import { AuthService } from '../../services/auth/auth.service';
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
export class CustomerDashboardComponent implements OnInit {
  private fb = inject(FormBuilder);
  private rentalService = inject(RentalService);
  private authService = inject(AuthService);
  private router = inject(Router);

  userName = 'Customer';

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
  payments: Payment[] = [];

  // State UI
  isLoading = false;
  isSubmitting = false;
  errorMessage = '';
  successMessage = '';
  minimumStartTime = this.getDefaultStartTime();

  bookingForm = this.fb.nonNullable.group({
    consoleUnitId: [0, [Validators.required, Validators.min(1)]],
    roomId: [0],
    stickTypeId: [0],
    stickQuantity: [0, Validators.min(0)],
    startTime: [this.minimumStartTime, Validators.required],
    durationHour: [2, [Validators.required, Validators.min(1), Validators.max(24)]],
    paymentMethod: ['Qris', Validators.required]
  });

  ngOnInit(): void {
    this.extractUserFromToken();
    this.loadAllData();
  }

  private extractUserFromToken(): void {
    const token = this.authService.getToken();
    if (!token) return;
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      this.userName =
        payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
        payload['name'] ||
        'Customer';
    } catch {
      this.userName = 'Customer';
    }
  }

  private getDefaultStartTime(): string {
    const now = new Date();
    now.setHours(now.getHours() + 1);
    now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
    return now.toISOString().slice(0, 16);
  }

  loadAllData(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.rentalService.getDashboardData().subscribe({
      next: (res) => {
        this.consoleTypes = res.consoleTypes;
        this.consoleUnits = res.consoleUnits;
        this.roomTypes = res.roomTypes;
        this.rooms = res.rooms;
        this.stickTypes = res.stickTypes;
        this.sticks = res.sticks;
        this.rentalHistory = res.rentals;
        this.payments = res.payments;

        // Set default pilihan dropdown pertama yang tersedia
        const firstUnit = this.availableUnits[0];
        if (firstUnit) {
          this.bookingForm.patchValue({ consoleUnitId: firstUnit.id });
        }
        const firstRoom = this.availableRooms[0];
        if (firstRoom) {
          this.bookingForm.patchValue({ roomId: firstRoom.id });
        }

        // Cari sewa aktif/pending milik user yang login
        const activeRentals = res.rentals.filter(
          (rental) =>
            ['active', 'pending'].includes(String(rental.statusRental).toLowerCase())
        );

        if (activeRentals.length > 0) {
          this.activeRental = activeRentals[0];
          this.activePayment =
            res.payments.find((payment) => payment.rentalId === this.activeRental?.id) || null;
          this.activeRentalRoom =
            res.rentalRooms.find((rentalRoom) => rentalRoom.rentalId === this.activeRental?.id) || null;
          this.activeRentalSticks = res.rentalSticks.filter(
            (rentalStick) => rentalStick.rentalId === this.activeRental?.id
          );
        } else {
          this.activeRental = null;
          this.activePayment = null;
          this.activeRentalRoom = null;
          this.activeRentalSticks = [];
        }

        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Gagal memuat data dari server API.';
      }
    });
  }

  // --- HELPER FILTER DROPDOWN ---
  get availableUnits(): ConsoleUnit[] {
    return this.consoleUnits.filter((u) => String(u.statusConsole).toLowerCase() === 'available');
  }

  get availableRooms(): Room[] {
    return this.rooms.filter((r) => String(r.statusRoom).toLowerCase() === 'available');
  }

  get availableSticks(): Stick[] {
    return this.sticks.filter((s) => String(s.statusStick).toLowerCase() === 'available');
  }

  get availableStickTypes(): StickType[] {
    const unitId = Number(this.bookingForm.controls.consoleUnitId.value);
    const unit = this.consoleUnits.find((consoleUnit) => consoleUnit.id === unitId);

    if (!unit) return [];

    return this.stickTypes.filter(
      (stickType) =>
        stickType.isActive &&
        stickType.consoleTypeId === unit.consoleTypeId &&
        this.availableSticks.some((stick) => stick.stickTypeId === stickType.id)
    );
  }

  get availableSticksForSelectedType(): Stick[] {
    const stickTypeId = Number(this.bookingForm.controls.stickTypeId.value);
    if (!this.availableStickTypes.some((stickType) => stickType.id === stickTypeId)) {
      return [];
    }
    return this.availableSticks.filter((stick) => stick.stickTypeId === stickTypeId);
  }

  get selectedStickQuantity(): number {
    const quantity = Number(this.bookingForm.controls.stickQuantity.value);
    return Number.isInteger(quantity) && quantity > 0 ? quantity : 0;
  }

  get isStickSelectionInvalid(): boolean {
    const requestedQuantity = Number(this.bookingForm.controls.stickQuantity.value);
    return (
      !Number.isInteger(requestedQuantity) ||
      requestedQuantity < 0 ||
      (requestedQuantity > 0 &&
        !this.availableStickTypes.some(
          (stickType) => stickType.id === Number(this.bookingForm.controls.stickTypeId.value)
        )) ||
      requestedQuantity > this.availableSticksForSelectedType.length
    );
  }

  onConsoleUnitChange(): void {
    this.bookingForm.patchValue({ stickTypeId: 0, stickQuantity: 0 });
  }

  onStickTypeChange(): void {
    this.bookingForm.patchValue({ stickQuantity: 0 });
  }

  // --- HELPER RELASI DATA MASTER ---
  getConsoleType(id: number): ConsoleType | undefined {
    return this.consoleTypes.find((c) => c.id === id);
  }

  getRoomType(id: number): RoomType | undefined {
    return this.roomTypes.find((rt) => rt.id === id);
  }

  getStickType(id: number): StickType | undefined {
    return this.stickTypes.find((st) => st.id === id);
  }

  getUnitLabel(unit: ConsoleUnit): string {
    const type = this.getConsoleType(unit.consoleTypeId);
    return type ? `${unit.unitCode} — ${type.name} (Rp ${type.hourlyRate.toLocaleString('id-ID')}/jam)` : unit.unitCode;
  }

  getRoomLabel(room: Room): string {
    const rt = this.getRoomType(room.roomTypeId);
    const rate = rt ? rt.additionalHourlyRate : 0;
    const typeName = rt ? rt.name : 'Room';
    return `${typeName} ${room.roomNumber} (+Rp ${rate.toLocaleString('id-ID')}/jam)`;
  }

  getStickLabel(stick: Stick): string {
    const st = this.getStickType(stick.stickTypeId);
    const rate = st ? st.additionalHourlyRate : 0;
    const typeName = st ? st.name : 'Stick';
    return `${stick.stickCode} — ${typeName} (+Rp ${rate.toLocaleString('id-ID')}/jam)`;
  }

  getStickCode(stickId: number): string {
    return this.sticks.find((stick) => stick.id === stickId)?.stickCode ?? `Stick #${stickId}`;
  }

  getUnitsByConsoleType(consoleTypeId: number): string {
    const list = this.consoleUnits
      .filter((u) => u.consoleTypeId === consoleTypeId)
      .map((u) => u.unitCode);
    return list.length > 0 ? list.join(', ') : '-';
  }

  getStickTypeByConsoleType(consoleTypeId: number): StickType | undefined {
    return this.stickTypes.find((st) => st.consoleTypeId === consoleTypeId);
  }

  getRoomsByRoomType(roomTypeId: number): string {
    const list = this.rooms
      .filter((r) => r.roomTypeId === roomTypeId)
      .map((r) => r.roomNumber);
    return list.length > 0 ? list.join(', ') : '-';
  }

  getActiveUnitCode(unitId: number): string {
    const unit = this.consoleUnits.find((u) => u.id === unitId);
    if (!unit) return `Unit #${unitId}`;
    const type = this.getConsoleType(unit.consoleTypeId);
    return `${type?.name || ''} — ${unit.unitCode}`;
  }

  getPaymentForRental(rentalId: number): Payment | undefined {
    return this.payments.find((payment) => payment.rentalId === rentalId);
  }

  // --- KALKULASI HARGA REAL-TIME ---
  get selectedConsoleRate(): number {
    const unitId = Number(this.bookingForm.value.consoleUnitId);
    const unit = this.consoleUnits.find((u) => u.id === unitId);
    const type = unit ? this.getConsoleType(unit.consoleTypeId) : null;
    return type ? Number(type.hourlyRate) : 0;
  }

  get selectedRoomRate(): number {
    const roomId = Number(this.bookingForm.value.roomId);
    const room = this.rooms.find((r) => r.id === roomId);
    const rt = room ? this.getRoomType(room.roomTypeId) : null;
    return rt ? Number(rt.additionalHourlyRate) : 0;
  }

  get selectedStickRate(): number {
    return this.selectedStickUnitRate * this.selectedStickQuantity;
  }

  get selectedStickUnitRate(): number {
    const stickTypeId = Number(this.bookingForm.controls.stickTypeId.value);
    const stickType = this.getStickType(stickTypeId);
    return stickType ? Number(stickType.additionalHourlyRate) : 0;
  }

  get duration(): number {
    return Number(this.bookingForm.value.durationHour) || 1;
  }

  private get durationDiscountRate(): number {
    const completedHours = Math.floor(this.duration);
    if (completedHours >= 3 && completedHours <= 4) return 0.1;
    if (completedHours >= 5 && completedHours <= 7) return 0.15;
    if (this.duration > 8) return 0.2;
    return 0;
  }

  get consoleSubtotal(): number {
    return this.selectedConsoleRate * this.duration * (1 - this.durationDiscountRate);
  }

  get roomSubtotal(): number {
    const roomId = Number(this.bookingForm.value.roomId);
    const room = this.rooms.find((availableRoom) => availableRoom.id === roomId);
    const roomType = room ? this.getRoomType(room.roomTypeId) : undefined;
    const discount = roomType?.name.toLowerCase().includes('vip')
      ? this.durationDiscountRate
      : 0;
    return this.selectedRoomRate * this.duration * (1 - discount);
  }

  get stickSubtotal(): number {
    return this.selectedStickRate * this.duration;
  }

  get totalAmount(): number {
    return this.consoleSubtotal + this.roomSubtotal + this.stickSubtotal;
  }

  // --- SUBMIT BOOKING ---
  onSubmitBooking(): void {
    if (this.bookingForm.invalid || this.isSubmitting || this.isStickSelectionInvalid) return;

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formVal = this.bookingForm.getRawValue();
    const start = new Date(formVal.startTime);
    const end = new Date(start.getTime() + this.duration * 60 * 60 * 1000);
    const unitId = Number(formVal.consoleUnitId);
    const roomId = Number(formVal.roomId);
    const stickIds = this.availableSticksForSelectedType
      .slice(0, this.selectedStickQuantity)
      .map((stick) => stick.id);

    const rentalPayload = {
      consoleUnitId: unitId,
      startTime: start.toISOString(),
      endTime: end.toISOString()
    };
    let createdRentalId: number | null = null;
    let bookingStage = 'membuat rental';

    this.rentalService
      .createRental(rentalPayload)
      .pipe(
        switchMap((createdRental) => {
          const rentalId = createdRental.id;
          createdRentalId = rentalId;

          const roomReq: Observable<RentalRoom | null> = roomId
            ? defer(() => {
                bookingStage = 'menambahkan ruangan';
                return this.rentalService.createRentalRoom({ rentalId, roomId });
              })
            : of(null);
          const addSticks = from(stickIds).pipe(
            concatMap((stickId: number) => {
              bookingStage = 'menambahkan stik';
              return this.rentalService.createRentalStick({ rentalId, stickId });
            }),
            toArray()
          );

          return roomReq.pipe(
            switchMap(() => addSticks),
            switchMap(() => {
              bookingStage = 'mengambil total rental terbaru';
              return this.rentalService.getRentalById(rentalId);
            }),
            switchMap((updatedRental: Rental) => {
              bookingStage = 'membuat payment';
              return this.rentalService.createPayment({
                  rentalId,
                  amount: updatedRental.totalAmount,
                  paymentMethod: formVal.paymentMethod
                });
            })
          );
        })
      )
      .pipe(finalize(() => (this.isSubmitting = false)))
      .subscribe({
        next: (payment) => {
          this.successMessage =
            `Booking berhasil dibuat. Payment ${payment.paymentCode} ` +
            `berstatus ${payment.paymentStatus} dengan jumlah Rp ${payment.amount.toLocaleString('id-ID')}.`;
          this.loadAllData();
        },
        error: (error: HttpErrorResponse) => {
          const reason =
            typeof error.error?.message === 'string'
              ? error.error.message
              : error.status === 0
                ? 'Tidak dapat terhubung ke server. Pastikan API sedang berjalan.'
                : 'Periksa kembali jadwal dan ketersediaan fasilitas.';
          this.errorMessage = createdRentalId
            ? `Rental ${createdRentalId} sudah dibuat, tetapi tahap ${bookingStage} gagal. ${reason}`
            : `Gagal ${bookingStage}. ${reason}`;
        }
      });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}