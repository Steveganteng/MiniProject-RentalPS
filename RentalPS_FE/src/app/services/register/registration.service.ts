import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RegistrationRequest {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface RegistrationResponse {
  id: number;
  name: string;
  email: string;
  role: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class RegistrationService {
  constructor(private http: HttpClient) {}

  register(data: RegistrationRequest): Observable<RegistrationResponse> {
    return this.http.post<RegistrationResponse>(
      `${environment.apiUrl}/Auth/register`,
      data
    );
  }
}
