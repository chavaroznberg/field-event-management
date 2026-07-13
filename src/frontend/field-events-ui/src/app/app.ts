import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { Subscription } from 'rxjs';
import { EventApiService } from './services/event-api.service';
import { EventRealtimeService, ConnectionStatus } from './services/event-realtime.service';
import { FieldEvent } from './models/field-event.model';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit, OnDestroy {
  private readonly api = inject(EventApiService);
  private readonly realtime = inject(EventRealtimeService);
  private subs = new Subscription();

  events = signal<FieldEvent[]>([]);
  connectionStatus = signal<ConnectionStatus>('disconnected');
  error = signal<string | null>(null);

  statusLabel = computed(() => {
    const s = this.connectionStatus();
    return s.charAt(0).toUpperCase() + s.slice(1);
  });

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString();
  }

  ngOnInit(): void {
    this.subs.add(
      this.api.getRecent().subscribe({
        next: (evs) => this.events.set(evs),
        error: (err) => this.error.set(`Failed to load events: ${err.message ?? err}`),
      })
    );

    this.subs.add(this.realtime.status$.subscribe((s) => this.connectionStatus.set(s)));

    this.subs.add(
      this.realtime.eventCreated$.subscribe((ev) =>
        this.events.update((current) => [ev, ...current])
      )
    );

    this.realtime.start();
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    this.realtime.stop();
  }
}
