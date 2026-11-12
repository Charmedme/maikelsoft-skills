import { httpResource } from '@angular/common/http';
import { Service, computed } from '@angular/core';

export interface CurrentUser {
  name: string;
  roles: string[];
}

@Service()
export class AuthService {
  readonly user = httpResource<CurrentUser>(() => '/api/me');

  readonly canChangePrices = computed(() => this.user.hasValue() && this.user.value().roles.includes('pricing'));
}
