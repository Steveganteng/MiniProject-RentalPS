import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ConsoleType,
  RoomType,
  Room,
  ConsoleUnit,
  StickType,
  Stick
} from '../../models/inventory.models';

@Injectable({
  providedIn: 'root'
})
export class InventoryService {
  private readonly apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  getConsoleTypes(): Observable<ConsoleType[]> {
    return this.http.get<ConsoleType[]>(
      `${this.apiUrl}/console-types`
    );
  }

  getRoomTypes(): Observable<RoomType[]> {
    return this.http.get<RoomType[]>(
      `${this.apiUrl}/room-types`
    );
  }

  getRooms(): Observable<Room[]> {
    return this.http.get<Room[]>(
      `${this.apiUrl}/rooms`
    );
  }

  getConsoleUnits(): Observable<ConsoleUnit[]> {
    return this.http.get<ConsoleUnit[]>(
      `${this.apiUrl}/console-unit`
    );
  }

  getStickTypes(): Observable<StickType[]> {
    return this.http.get<StickType[]>(
      `${this.apiUrl}/stick-types`
    );
  }

  getSticks(): Observable<Stick[]> {
    return this.http.get<Stick[]>(
      `${this.apiUrl}/sticks`
    );
  }
}