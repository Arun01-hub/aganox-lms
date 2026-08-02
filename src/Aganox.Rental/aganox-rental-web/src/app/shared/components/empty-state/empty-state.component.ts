import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  template: `
    <div class="empty">
      <div class="empty__icon" aria-hidden="true">{{ icon() }}</div>
      <h3>{{ title() }}</h3>
      <p>{{ message() }}</p>
      @if (actionLabel()) {
        <ng-content />
      }
    </div>
  `,
  styles: `
    .empty {
      text-align: center;
      padding: 3rem 1.5rem;
      background: var(--surface);
      border: 1px dashed var(--border);
      border-radius: var(--radius-lg);
    }

    .empty__icon {
      font-size: 2.5rem;
      margin-bottom: 0.75rem;
    }

    h3 {
      margin: 0 0 0.5rem;
      font-size: 1.25rem;
    }

    p {
      margin: 0 auto 1.25rem;
      max-width: 28rem;
      color: var(--text-muted);
      line-height: 1.5;
    }
  `
})
export class EmptyStateComponent {
  readonly icon = input('🏠');
  readonly title = input('Nothing here yet');
  readonly message = input('');
  readonly actionLabel = input('');
}
