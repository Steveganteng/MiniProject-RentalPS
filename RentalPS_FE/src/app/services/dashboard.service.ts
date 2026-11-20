import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdminConsoleUnit {
  id: number;
  unitCode: string;
  statusConsole: string;
}

export interface AdminRoom {
  id: number;
  roomNumber: string;
  statusRoom: string;
}

export interface AdminStick {
  id: number;
  stickCode: string;
  statusStick: string;
}

export interface AdminRental {
  id: number;
  rentalCode: string;
  userId: number;
  consoleUnitId: number;
  startTime: string | null;
  endTime: string | null;
  durationHour: number;
  totalAmount: number;
  statusRental: string;
}

export interface AdminDashboardData {
  consoleUnits: AdminConsoleUnit[];
  rooms: AdminRoom[];
  sticks: AdminStick[];
  rentals: AdminRental[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  getDashboardData(): Observable<AdminDashboardData> {
    return forkJoin({
      consoleUnits: this.http.get<AdminConsoleUnit[]>(`${this.baseUrl}/console-unit`),
      rooms: this.http.get<AdminRoom[]>(`${this.baseUrl}/rooms`),
      sticks: this.http.get<AdminStick[]>(`${this.baseUrl}/sticks`),
      rentals: this.http.get<AdminRental[]>(`${this.baseUrl}/rentals`)
    });
  }
}
