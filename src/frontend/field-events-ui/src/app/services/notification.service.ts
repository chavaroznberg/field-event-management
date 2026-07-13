import { Injectable } from '@angular/core';

// Stub — real implementation requires:
//   - A registered service worker (ngsw-config.json + @angular/service-worker)
//   - VAPID public key from the backend
//   - PushManager.subscribe() call to obtain a PushSubscription
//   - Sending the subscription to the backend (POST /api/subscriptions)
@Injectable({ providedIn: 'root' })
export class NotificationService {
  requestPermission(): Promise<NotificationPermission> {
    console.warn('[NotificationService] Web Push not implemented');
    return Promise.resolve('denied' as NotificationPermission);
  }

  subscribe(): Promise<void> {
    console.warn('[NotificationService] Push subscription not implemented');
    return Promise.resolve();
  }

  isSupported(): boolean {
    return 'Notification' in window && 'serviceWorker' in navigator && 'PushManager' in window;
  }
}
