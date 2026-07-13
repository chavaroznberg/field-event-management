import { Injectable } from '@angular/core';
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { FieldEvent } from '../models/field-event.model';

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

@Injectable({ providedIn: 'root' })
export class EventRealtimeService {
  private readonly connection: HubConnection;

  readonly eventCreated$ = new Subject<FieldEvent>();
  readonly status$ = new Subject<ConnectionStatus>();

  constructor() {
    this.connection = new HubConnectionBuilder()
      .withUrl('/hubs/dispatcher')
      .withAutomaticReconnect()
      .build();

    this.connection.on('EventCreated', (ev: FieldEvent) => this.eventCreated$.next(ev));
    this.connection.onreconnecting(() => this.status$.next('reconnecting'));
    this.connection.onreconnected(() => this.status$.next('connected'));
    this.connection.onclose(() => this.status$.next('disconnected'));
  }

  start(): Promise<void> {
    this.status$.next('connecting');
    return this.connection.start()
      .then(() => this.status$.next('connected'))
      .catch(() => this.status$.next('disconnected'));
  }

  stop(): Promise<void> {
    return this.connection.stop();
  }
}
