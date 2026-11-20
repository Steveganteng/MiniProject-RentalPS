import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of, switchMap } from 'rxjs';
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

  bookingForm = this.fb.nonNullable.group({
    consoleUnitId: [0, [Validators.required, Validators.min(1)]],
    roomId: [0],
    stickId: [0],
    startTime: [this.getDefaultStartTime(), Validators.required],
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
    const stickId = Number(this.bookingForm.value.stickId);
    if (!stickId) return 0;
    const stick = this.sticks.find((s) => s.id === stickId);
    const st = stick ? this.getStickType(stick.stickTypeId) : null;
    return st ? Number(st.additionalHourlyRate) : 0;
  }

  get duration(): number {
    return Number(this.bookingForm.value.durationHour) || 1;
  }

  get totalAmount(): number {
    return (this.selectedConsoleRate + this.selectedRoomRate + this.selectedStickRate) * this.duration;
  }

  // --- SUBMIT BOOKING ---
  onSubmitBooking(): void {
    if (this.bookingForm.invalid || this.isSubmitting) return;

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const formVal = this.bookingForm.getRawValue();
    const start = new Date(formVal.startTime);
    const end = new Date(start.getTime() + this.duration * 60 * 60 * 1000);
    const unitId = Number(formVal.consoleUnitId);
    const roomId = Number(formVal.roomId);
    const stickId = Number(formVal.stickId);

    const rentalPayload = {
      consoleUnitId: unitId,
      startTime: start.toISOString(),
      endTime: end.toISOString()
    };

    this.rentalService
      .createRental(rentalPayload)
      .pipe(
        switchMap((createdRental) => {
          const rentalId = createdRental.id;

          const roomReq = roomId
            ? this.rentalService.createRentalRoom({ rentalId, roomId })
            : of(null);

          const stickReq = stickId
            ? this.rentalService.createRentalStick({ rentalId, stickId })
            : of(null);

          const paymentReq = this.rentalService.createPayment({
            rentalId,
            amount: this.totalAmount,
            paymentMethod: formVal.paymentMethod
          });

          return forkJoin([roomReq, stickReq, paymentReq]);
        })
      )
      .subscribe({
        next: () => {
          this.isSubmitting = false;
          this.successMessage = 'Booking berhasil dibuat!';
          this.loadAllData();
        },
        error: (err) => {
          this.isSubmitting = false;
          this.errorMessage = err.error?.message || 'Gagal membuat booking. Periksa kembali inputan atau endpoint backend.';
        }
      });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}