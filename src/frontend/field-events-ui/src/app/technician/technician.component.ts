import { Component } from '@angular/core';

// Skeleton — technician workflow not yet implemented.
// Intended to show events assigned to the currently logged-in technician,
// allow status changes, and display push notification opt-in.
@Component({
  selector: 'app-technician',
  standalone: true,
  imports: [],
  template: `
    <div style="padding:16px">
      <h2>Technician Panel</h2>
      <p>This view is a skeleton placeholder. The full workflow is not implemented.</p>
      <p>Planned features: assigned event list, status change, push notification opt-in.</p>
    </div>
  `
})
export class TechnicianComponent {}
