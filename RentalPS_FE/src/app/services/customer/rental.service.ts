import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ConsoleType {
  id: number;
  name: string;
  hourlyRate: number;
  isActive: boolean;
}

export interface ConsoleUnit {
  id: number;
  consoleTypeId: number;
  consoleTypeName: string;
  roomId: number | null;
  roomNumber: string | null;
  unitCode: string;
  statusConsole: string;
}

export interface RoomType {
  id: number;
  name: string;
  additionalHourlyRate: number;
  description: string;
  isActive: boolean;
}

export interface Room {
  id: number;
  roomTypeId: number;
  roomNumber: string;
  description: string;
  statusRoom: string;
}

export interface StickType {
  id: number;
  consoleTypeId: number;
  name: string;
  additionalHourlyRate: number;
  isActive: boolean;
}

export interface Stick {
  id: number;
  stickTypeId: number;
  stickCode: string;
  statusStick: string;
}

export interface Rental {
  id: number;
  rentalCode: string;
  userId: number;
  consoleUnitId: number;
  startTime: string | null;
  endTime: string | null;
  durationHour: number;
  consoleRate: number;
  totalAmount: number;
  statusRental: string;
}

export interface RentalRoom {
  id: number;
  rentalId: number;
  roomId: number;
  name: string;
  additionalHourlyRate: number;
  subTotal: number;
}

export interface RentalStick {
  id: number;
  rentalId: number;
  stickId: number;
  hourlyRate: number;
  subTotal: number;
}

export interface Payment {
  id: number;
  rentalId: number;
  paymentCode: string;
  amount: number;
  paymentMethod: string;
  paymentStatus: string;
  paidAt: string | null;
}

export interface CustomerDashboardData {
  consoleTypes: ConsoleType[];
  consoleUnits: ConsoleUnit[];
  roomTypes: RoomType[];
  rooms: Room[];
  stickTypes: StickType[];
  sticks: Stick[];
  rentals: Rental[];
  rentalRooms: RentalRoom[];
  rentalSticks: RentalStick[];
  payments: Payment[];
}

export interface CreateRentalRequest {
  consoleUnitId: number;
  startTime: string;
  endTime: string;
}

export interface CreateRentalRoomRequest {
  rentalId: number;
  roomId: number;
}

export interface CreateRentalStickRequest {
  rentalId: number;
  stickId: number;
}

export interface CreatePaymentRequest {
  rentalId: number;
  amount: number;
  paymentMethod: string;
}

@Injectable({
  providedIn: 'root'
})
export class RentalService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getDashboardData(): Observable<CustomerDashboardData> {
    return forkJoin({
      consoleTypes: this.http.get<ConsoleType[]>(`${this.baseUrl}/console-types`),
      consoleUnits: this.http.get<ConsoleUnit[]>(`${this.baseUrl}/console-unit`),
      roomTypes: this.http.get<RoomType[]>(`${this.baseUrl}/room-types`),
      rooms: this.http.get<Room[]>(`${this.baseUrl}/rooms`),
      stickTypes: this.http.get<StickType[]>(`${this.baseUrl}/stick-types`),
      sticks: this.http.get<Stick[]>(`${this.baseUrl}/sticks`),
      rentals: this.http.get<Rental[]>(`${this.baseUrl}/rentals`),
      rentalRooms: this.http.get<RentalRoom[]>(`${this.baseUrl}/rental-rooms`),
      rentalSticks: this.http.get<RentalStick[]>(`${this.baseUrl}/rental-sticks`),
      payments: this.http.get<Payment[]>(`${this.baseUrl}/payments`)
    });
  }

  createRental(request: CreateRentalRequest): Observable<Rental> {
    return this.http.post<Rental>(`${this.baseUrl}/rentals`, request);
  }

  createRentalRoom(request: CreateRentalRoomRequest): Observable<RentalRoom> {
    return this.http.post<RentalRoom>(`${this.baseUrl}/rental-rooms`, request);
  }

  createRentalStick(request: CreateRentalStickRequest): Observable<RentalStick> {
    return this.http.post<RentalStick>(`${this.baseUrl}/rental-sticks`, request);
  }

  createPayment(request: CreatePaymentRequest): Observable<Payment> {
    return this.http.post<Payment>(`${this.baseUrl}/payments`, request);
  }
}
