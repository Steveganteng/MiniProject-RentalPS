import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class RentalService {
  private http = inject(HttpClient);
  private baseUrl = 'https://localhost:7054/api';

  // Mengambil semua data master & transaksi secara paralel
  getDashboardData(): Observable<any> {
    return forkJoin({
      consoleTypes: this.http.get<any[]>(`${this.baseUrl}/console-types`),
      consoleUnits: this.http.get<any[]>(`${this.baseUrl}/console-unit`),
      roomTypes: this.http.get<any[]>(`${this.baseUrl}/room-types`),
      rooms: this.http.get<any[]>(`${this.baseUrl}/rooms`),
      stickTypes: this.http.get<any[]>(`${this.baseUrl}/stick-types`),
      sticks: this.http.get<any[]>(`${this.baseUrl}/sticks`),
      rentals: this.http.get<any[]>(`${this.baseUrl}/rentals`),
      rentalRooms: this.http.get<any[]>(`${this.baseUrl}/rental-rooms`),
      rentalSticks: this.http.get<any[]>(`${this.baseUrl}/rental-sticks`),
      payments: this.http.get<any[]>(`${this.baseUrl}/payments`)
    });
  }

  createRental(payload: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/rentals`, payload);
  }

  createRentalRoom(payload: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/rental-rooms`, payload);
  }

  createRentalStick(payload: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/rental-sticks`, payload);
  }

  createPayment(payload: any): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/payments`, payload);
  }
}