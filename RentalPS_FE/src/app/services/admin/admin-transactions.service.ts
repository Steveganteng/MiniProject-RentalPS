import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminRental {
  id: number;
  rentalCode: string;
  userId: number;
  consoleUnitId: number;
  startTime: string | null;
  endTime: string | null;
  durationHour: number;
  consoleRate: number;
  totalAmount: number;
  statusRental: string | number;
}

export interface AdminPayment {
  id: number;
  rentalId: number;
  paymentCode: string;
  amount: number;
  paymentMethod: string | number;
  paymentStatus: string | number;
  paidAt: string | null;
}

export interface AdminRentalRoom {
  id: number;
  rentalId: number;
  roomId: number;
  name: string;
  additionalHourlyRate: number;
  subTotal: number;
}

export interface AdminRentalStick {
  id: number;
  rentalId: number;
  stickId: number;
  hourlyRate: number;
  subTotal: number;
}

export interface AdminConsoleUnit {
  id: number;
  consoleTypeId: number;
  consoleTypeName: string;
  roomId: number | null;
  roomNumber: string | null;
  unitCode: string;
  statusConsole: string;
}

export interface AdminConsoleType {
  id: number;
  name: string;
  hourlyRate: number;
  isActive: boolean;
}

export interface AdminRoom {
  id: number;
  roomTypeId: number;
  roomNumber: string;
  description: string;
  statusRoom: string;
}

export interface AdminRoomType {
  id: number;
  name: string;
  additionalHourlyRate: number;
  description: string;
  isActive: boolean;
}

export interface AdminStick {
  id: number;
  stickTypeId: number;
  stickCode: string;
  statusStick: string;
}

export interface AdminStickType {
  id: number;
  consoleTypeId: number;
  name: string;
  additionalHourlyRate: number;
  isActive: boolean;
}

export interface AdminTransactionData {
  rentals: AdminRental[];
  payments: AdminPayment[];
  rentalRooms: AdminRentalRoom[];
  rentalSticks: AdminRentalStick[];
  consoleUnits: AdminConsoleUnit[];
  consoleTypes: AdminConsoleType[];
  rooms: AdminRoom[];
  roomTypes: AdminRoomType[];
  sticks: AdminStick[];
  stickTypes: AdminStickType[];
}

@Injectable({ providedIn: 'root' })
export class AdminTransactionsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getTransactionData(): Observable<AdminTransactionData> {
    return forkJoin({
      rentals: this.http.get<AdminRental[]>(`${this.baseUrl}/rentals`),
      payments: this.http.get<AdminPayment[]>(`${this.baseUrl}/payments`),
      rentalRooms: this.http.get<AdminRentalRoom[]>(`${this.baseUrl}/rental-rooms`),
      rentalSticks: this.http.get<AdminRentalStick[]>(`${this.baseUrl}/rental-sticks`),
      consoleUnits: this.http.get<AdminConsoleUnit[]>(`${this.baseUrl}/console-unit`),
      consoleTypes: this.http.get<AdminConsoleType[]>(`${this.baseUrl}/console-types`),
      rooms: this.http.get<AdminRoom[]>(`${this.baseUrl}/rooms`),
      roomTypes: this.http.get<AdminRoomType[]>(`${this.baseUrl}/room-types`),
      sticks: this.http.get<AdminStick[]>(`${this.baseUrl}/sticks`),
      stickTypes: this.http.get<AdminStickType[]>(`${this.baseUrl}/stick-types`)
    });
  }

  updateRentalStatus(id: number, statusRental: 'Active' | 'Completed' | 'Cancelled'): Observable<AdminRental> {
    return this.http.put<AdminRental>(`${this.baseUrl}/rentals/${id}/status`, { statusRental });
  }

  updatePaymentStatus(id: number, paymentStatus: 'Success' | 'Failed'): Observable<AdminPayment> {
    return this.http.put<AdminPayment>(`${this.baseUrl}/payments/${id}/status`, { paymentStatus });
  }
}
