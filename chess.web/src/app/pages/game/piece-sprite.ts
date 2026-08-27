import { Component, computed, effect, ElementRef, inject, input } from '@angular/core';

@Component({
  selector: 'app-piece-sprite',
  template: `<img [src]="src()" [alt]="piece()" draggable="false" />`,
  styleUrl: './piece-sprite.scss',
})
export class PieceSprite {
  readonly piece = input.required<string>();
  readonly transform = input.required<string>();

  protected readonly src = computed(() => `/pieces/${this.piece()}.svg`);

  private readonly host = inject(ElementRef<HTMLElement>).nativeElement;
  private previousTransform: string | null = null;

  constructor() {
    effect(() => {
      const next = this.transform();
      const previous = this.previousTransform;
      this.previousTransform = next;
      this.host.style.transform = next;

      if (previous !== null && previous !== next) {
        this.host.animate([{ transform: previous }, { transform: next }], { duration: 80, easing: 'ease' });
      }
    });
  }
}
