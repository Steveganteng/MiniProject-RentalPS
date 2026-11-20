import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ConsoleUnitSummary {
  id: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly availableUnitsUrl =
    `${environment.apiUrl}/console-unit`;

  constructor(private http: HttpClient) {}

  getAvailableUnits(): Observable<ConsoleUnitSummary[]> {
    return this.http.get<ConsoleUnitSummary[]>(this.availableUnitsUrl);
  }
}