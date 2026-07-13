import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FieldEvent } from '../models/field-event.model';

@Injectable({ providedIn: 'root' })
export class EventApiService {
  private readonly http = inject(HttpClient);

  getRecent(): Observable<FieldEvent[]> {
    return this.http.get<FieldEvent[]>('/api/events');
  }
}
