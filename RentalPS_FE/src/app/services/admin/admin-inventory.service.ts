import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ConsoleTypeItem {
  id: number;
  name: string;
  hourlyRate: number;
  isActive: boolean;
}

export interface ConsoleUnitItem {
  id: number;
  consoleTypeId: number;
  consoleTypeName: string;
  roomId: number | null;
  roomNumber: string | null;
  unitCode: string;
  statusConsole: string;
}

export interface RoomTypeItem {
  id: number;
  name: string;
  additionalHourlyRate: number;
  description: string;
  isActive: boolean;
}

export interface RoomItem {
  id: number;
  roomTypeId: number;
  roomNumber: string;
  description: string;
  statusRoom: string;
}

export interface StickTypeItem {
  id: number;
  consoleTypeId: number;
  name: string;
  additionalHourlyRate: number;
  isActive: boolean;
}

export interface StickItem {
  id: number;
  stickTypeId: number;
  stickCode: string;
  statusStick: string;
}

@Injectable({ providedIn: 'root' })
export class AdminInventoryService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl.replace(/\/$/, '');

  getConsoleTypes(): Observable<ConsoleTypeItem[]> {
    return this.http.get<unknown>(`${this.baseUrl}/console-types`).pipe(
      map(response => this.unwrapArray(response).map(row => this.normalizeConsoleType(row)))
    );
  }

  createConsoleType(payload: Omit<ConsoleTypeItem, 'id'>): Observable<ConsoleTypeItem> {
    return this.http.post<unknown>(`${this.baseUrl}/console-types`, payload).pipe(
      map(response => this.normalizeConsoleType(response))
    );
  }

  updateConsoleType(id: number, payload: Omit<ConsoleTypeItem, 'id'>): Observable<ConsoleTypeItem> {
    return this.http.put<unknown>(`${this.baseUrl}/console-types/${id}`, payload).pipe(
      map(response => this.normalizeConsoleType(response))
    );
  }

  deactivateConsoleType(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/console-types/${id}`);
  }

  getConsoleUnits(): Observable<ConsoleUnitItem[]> {
    return this.http.get<unknown>(`${this.baseUrl}/console-unit`).pipe(
      map(response => this.unwrapArray(response) as ConsoleUnitItem[])
    );
  }

  createConsoleUnit(payload: Omit<ConsoleUnitItem, 'id' | 'consoleTypeName' | 'roomNumber'>): Observable<ConsoleUnitItem> {
    return this.http.post<ConsoleUnitItem>(`${this.baseUrl}/console-unit`, payload);
  }

  updateConsoleUnit(id: number, payload: Omit<ConsoleUnitItem, 'id' | 'consoleTypeName' | 'roomNumber'>): Observable<ConsoleUnitItem> {
    return this.http.put<ConsoleUnitItem>(`${this.baseUrl}/console-unit/${id}`, payload);
  }

  deactivateConsoleUnit(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/console-unit/${id}`);
  }

  getRoomTypes(): Observable<RoomTypeItem[]> {
    return this.http.get<unknown>(`${this.baseUrl}/room-types`).pipe(
      map(response => this.unwrapArray(response).map(row => this.normalizeRoomType(row)))
    );
  }

  createRoomType(payload: Omit<RoomTypeItem, 'id'>): Observable<RoomTypeItem> {
    return this.http.post<unknown>(`${this.baseUrl}/room-types`, payload).pipe(
      map(response => this.normalizeRoomType(response))
    );
  }

  updateRoomType(id: number, payload: Omit<RoomTypeItem, 'id'>): Observable<RoomTypeItem> {
    return this.http.put<unknown>(`${this.baseUrl}/room-types/${id}`, payload).pipe(
      map(response => this.normalizeRoomType(response))
    );
  }

  deactivateRoomType(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/room-types/${id}`);
  }

  getRooms(): Observable<RoomItem[]> {
    return this.http.get<unknown>(`${this.baseUrl}/rooms`).pipe(
      map(response => this.unwrapArray(response) as RoomItem[])
    );
  }

  createRoom(payload: Omit<RoomItem, 'id'>): Observable<RoomItem> {
    return this.http.post<RoomItem>(`${this.baseUrl}/rooms`, payload);
  }

  updateRoom(id: number, payload: Omit<RoomItem, 'id'>): Observable<RoomItem> {
    return this.http.put<RoomItem>(`${this.baseUrl}/rooms/${id}`, payload);
  }

  deactivateRoom(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/rooms/${id}`);
  }

  getStickTypes(): Observable<StickTypeItem[]> {
    return this.http.get<unknown>(`${this.baseUrl}/stick-types`).pipe(
      map(response => this.unwrapArray(response).map(row => this.normalizeStickType(row)))
    );
  }

  createStickType(payload: Omit<StickTypeItem, 'id'>): Observable<StickTypeItem> {
    return this.http.post<unknown>(`${this.baseUrl}/stick-types`, payload).pipe(
      map(response => this.normalizeStickType(response))
    );
  }

  updateStickType(id: number, payload: Omit<StickTypeItem, 'id'>): Observable<StickTypeItem> {
    return this.http.put<unknown>(`${this.baseUrl}/stick-types/${id}`, payload).pipe(
      map(response => this.normalizeStickType(response))
    );
  }

  deactivateStickType(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/stick-types/${id}`);
  }

  getSticks(): Observable<StickItem[]> {
    return this.http.get<unknown>(`${this.baseUrl}/sticks`).pipe(
      map(response => this.unwrapArray(response) as StickItem[])
    );
  }

  createStick(payload: Omit<StickItem, 'id'>): Observable<StickItem> {
    return this.http.post<StickItem>(`${this.baseUrl}/sticks`, payload);
  }

  updateStick(id: number, payload: Omit<StickItem, 'id'>): Observable<StickItem> {
    return this.http.put<StickItem>(`${this.baseUrl}/sticks/${id}`, payload);
  }

  deactivateStick(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/sticks/${id}`);
  }

  private normalizeConsoleType(value: unknown): ConsoleTypeItem {
    const row = this.asRecord(value);
    const rawRate = this.readRate(row, 'console');
    if (rawRate === undefined) {
      console.warn('[AdminInventory] Response ConsoleType tidak memiliki field tarif. Periksa Network > GET /api/console-types:', value);
    }

    return {
      id: this.toNumber(this.readField(row, ['id', 'Id'])),
      name: this.toText(this.readField(row, ['name', 'Name'])),
      hourlyRate: this.toNumber(rawRate),
      isActive: this.toBoolean(this.readField(row, ['isActive', 'IsActive', 'is_active']))
    };
  }

  private normalizeRoomType(value: unknown): RoomTypeItem {
    const row = this.asRecord(value);
    const rawRate = this.readRate(row, 'additional');
    if (rawRate === undefined) {
      console.warn('[AdminInventory] Response RoomType tidak memiliki field tarif. Periksa Network > GET /api/room-types:', value);
    }

    return {
      id: this.toNumber(this.readField(row, ['id', 'Id'])),
      name: this.toText(this.readField(row, ['name', 'Name'])),
      additionalHourlyRate: this.toNumber(rawRate),
      description: this.toText(this.readField(row, ['description', 'Description'])),
      isActive: this.toBoolean(this.readField(row, ['isActive', 'IsActive', 'is_active']))
    };
  }

  private normalizeStickType(value: unknown): StickTypeItem {
    const row = this.asRecord(value);
    const rawRate = this.readRate(row, 'additional');
    if (rawRate === undefined) {
      console.warn('[AdminInventory] Response StickType tidak memiliki field tarif. Periksa Network > GET /api/stick-types:', value);
    }

    return {
      id: this.toNumber(this.readField(row, ['id', 'Id'])),
      consoleTypeId: this.toNumber(this.readField(row, ['consoleTypeId', 'ConsoleTypeId', 'console_type_id'])),
      name: this.toText(this.readField(row, ['name', 'Name'])),
      additionalHourlyRate: this.toNumber(rawRate),
      isActive: this.toBoolean(this.readField(row, ['isActive', 'IsActive', 'is_active']))
    };
  }

  private readRate(row: Record<string, unknown>, kind: 'console' | 'additional'): unknown {
    const candidates = kind === 'console'
      ? [
          'hourlyRate', 'HourlyRate', 'hourly_rate', 'baseHourlyRate', 'BaseHourlyRate',
          'basePrice', 'BasePrice', 'basePricePerHour', 'ratePerHour', 'pricePerHour',
          'hourlyPrice', 'price', 'rate', 'tarifPerJam'
        ]
      : [
          'additionalHourlyRate', 'AdditionalHourlyRate', 'additional_hourly_rate',
          'additionalRate', 'AdditionalRate', 'additionalPricePerHour',
          'hourlyRate', 'HourlyRate', 'hourly_rate', 'basePrice', 'ratePerHour',
          'pricePerHour', 'hourlyPrice', 'price', 'rate', 'tarifPerJam'
        ];

    const direct = this.readField(row, candidates);
    if (direct !== undefined) return direct;

    // Fallback untuk nama kolom tarif yang berbeda tetapi masih mengandung rate/price/tarif.
    const fallback = Object.entries(row).find(([key, value]) => {
      const normalized = this.normalizeKey(key);
      return value !== undefined && value !== null &&
        (normalized.includes('rate') || normalized.includes('price') || normalized.includes('tarif'));
    });
    return fallback?.[1];
  }

  private unwrapArray(value: unknown, depth = 0): unknown[] {
    if (Array.isArray(value)) return value;
    if (depth >= 4) return [];

    const row = this.asRecord(value);
    const nested = this.readField(row, ['data', 'items', 'results', 'value', 'result']);
    return nested === undefined ? [] : this.unwrapArray(nested, depth + 1);
  }

  private asRecord(value: unknown): Record<string, unknown> {
    return typeof value === 'object' && value !== null
      ? value as Record<string, unknown>
      : {};
  }

  private normalizeKey(value: string): string {
    return value.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
  }

  private readField(row: Record<string, unknown>, keys: string[]): unknown {
    for (const key of keys) {
      if (row[key] !== undefined && row[key] !== null) return row[key];
    }

    const wanted = new Set(keys.map(key => this.normalizeKey(key)));
    const match = Object.entries(row).find(([key, value]) =>
      wanted.has(this.normalizeKey(key)) && value !== undefined && value !== null
    );
    return match?.[1];
  }

  private toNumber(value: unknown): number {
    if (typeof value === 'number' && Number.isFinite(value)) return value;
    if (typeof value === 'string') {
      const cleaned = value.trim().replace(/\s/g, '');
      if (!cleaned) return 0;
      const parsed = Number(cleaned);
      if (Number.isFinite(parsed)) return parsed;

      // Mendukung angka yang dikirim sebagai string dengan format lokal, mis. 3.000,50.
      const localized = cleaned.includes(',')
        ? cleaned.replace(/\./g, '').replace(',', '.')
        : cleaned;
      const localizedNumber = Number(localized);
      if (Number.isFinite(localizedNumber)) return localizedNumber;
    }
    return 0;
  }

  private toText(value: unknown): string {
    return value === undefined || value === null ? '' : String(value);
  }

  private toBoolean(value: unknown): boolean {
    return value === true || value === 1 || value === 'true' || value === 'True';
  }
}
