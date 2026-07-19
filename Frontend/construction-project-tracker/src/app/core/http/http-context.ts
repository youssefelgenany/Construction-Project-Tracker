import { HttpContextToken } from '@angular/common/http';

/** Opt-in global overlay loading for exceptional operations (e.g. login). */
export const USE_GLOBAL_LOADING = new HttpContextToken<boolean>(() => false);
