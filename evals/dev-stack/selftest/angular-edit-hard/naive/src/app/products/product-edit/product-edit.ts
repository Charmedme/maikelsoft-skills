import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { ProductEditService } from './product-edit.service';

@Component({
  selector: 'app-product-edit',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './product-edit.html',
  styles: ['form { width: 640px; } input { width: 100%; }'],
})
export class ProductEdit implements OnInit {
  id = '';
  etag = '';
  error = '';
  serverErrors: Record<string, string> = {};

  form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    code: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    price: new FormControl<number | null>(null, [Validators.required]),
    availableFrom: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private service: ProductEditService,
  ) {}

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id') ?? '';
    this.load();
  }

  load(): void {
    this.service.load(this.id).subscribe({
      next: (response) => {
        this.etag = response.headers.get('ETag') ?? '';
        if (response.body) {
          this.form.patchValue(response.body);
        }
      },
      error: () => undefined,
    });
  }

  save(): void {
    const value = this.form.getRawValue();
    const body = {
      ...value,
      price: value.price ?? 0,
      availableFrom: new Date(value.availableFrom + 'T00:00:00').toISOString(),
    };
    this.error = '';
    this.serverErrors = {};
    this.service.save(this.id, body, this.etag).subscribe({
      next: () => this.router.navigate(['/products']),
      error: (err) => {
        if (err.status === 412) {
          this.error = 'This product was changed by someone else.';
          this.load();
        } else if (err.status === 400) {
          for (const key of Object.keys(this.form.controls)) {
            if (err.error?.errors?.[key]) {
              this.serverErrors[key] = err.error.errors[key][0];
            }
          }
          this.error = 'Please check the form.';
        }
      },
    });
  }
}
