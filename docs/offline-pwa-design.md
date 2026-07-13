# Offline / PWA Design

High-level design for making the Field Event Management System work offline for technicians in the field.

## Service Worker

Angular's `@angular/service-worker` package (driven by `ngsw-config.json`) handles:

- **App shell caching** — the compiled JS/CSS bundles are cached on first load so the technician UI loads instantly even without a network connection.
- **API response caching** — recently fetched event lists can be served from cache with a stale-while-revalidate strategy.
- **Background sync** — the browser's Background Sync API (`SyncManager`) allows the service worker to retry queued commands when connectivity is restored, even if the browser tab is closed.

## IndexedDB

When the technician performs actions offline (status change, add comment), those actions are written to a local IndexedDB store (`pending_commands` object store) before the UI updates optimistically. Keys: `{ id: uuid, type: 'ChangeStatus'|'AddComment', payload: {...}, createdAt: ISO }`.

## Local Command Queue

A `OfflineCommandQueue` service wraps IndexedDB writes. Pseudo-flow:

1. User taps "Mark In-Progress" with no network.
2. `OfflineCommandQueue.enqueue({ type: 'ChangeStatus', eventId, newStatus: 'InProgress' })`.
3. UI shows the event as InProgress optimistically.
4. Service worker registers a Background Sync tag `'field-events-sync'`.

## Synchronisation After Reconnection

When the device regains connectivity, the service worker fires the `sync` event. The sync handler:

1. Reads all entries from `pending_commands` in CreatedAt order.
2. Posts each command to the backend (`PATCH /api/events/{id}/status`, `POST /api/events/{id}/comments`).
3. On `200`/`201`, deletes the entry from IndexedDB.
4. On `409 Conflict`, triggers conflict resolution (see below).
5. On transient network error (`5xx`, timeout), leaves the entry in the queue — Background Sync retries automatically with exponential backoff.

## RowVersion / Version Conflicts

The backend `FieldEvent` entity carries a SQL Server `rowversion` column (`RowVersion byte[]`). Every mutating request from the technician should include the last-known `rowversion` as an `If-Match` or `X-Row-Version` header. The backend validates it before applying changes.

If another user modified the event between the offline action and the sync:
- The backend returns `409 Conflict` with the current server state in the response body.
- The sync handler records the conflict for the technician to resolve.

## Conflict Resolution Options

| Strategy | Description | When to use |
|---|---|---|
| **Server wins** | Discard the offline command; reload server state. | Low-stakes status progression (no data loss). |
| **Client wins** | Re-apply the offline command on top of the current server state (if still valid per state machine). | Technician's intent is authoritative. |
| **Manual merge** | Prompt the technician to choose which version to keep. | Comments or assignments where both changes carry information. |

The state machine already guards invalid transitions, so "client wins" is safe for status changes as long as the transition remains valid from the server's current state. For comment additions, client wins is always safe (additive, no conflict).
