import { Component, inject } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';

/** The campaign video on our own video channel. The address is fixed at build time. */
export const PROMO_VIDEO_URL = 'https://www.youtube-nocookie.com/embed/kiosk-spring-2026';

@Component({
  selector: 'app-promo-video',
  template: `
    <h1>About Kiosk</h1>
    <p>Kiosk sells home goods from small makers in Europe.</p>
    <iframe class="promo" [src]="videoUrl" title="Spring campaign video" width="560" height="315" allowfullscreen></iframe>
  `,
})
export class PromoVideo {
  protected readonly videoUrl = inject(DomSanitizer).bypassSecurityTrustResourceUrl(PROMO_VIDEO_URL);
}
