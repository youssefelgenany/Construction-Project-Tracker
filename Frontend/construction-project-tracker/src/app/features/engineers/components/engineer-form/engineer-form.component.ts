import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  output
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { CreateEngineer, UpdateEngineer } from '../../../../core/models/engineer';

export type EngineerFormSubmit =
  | { mode: 'create'; value: CreateEngineer }
  | { mode: 'edit'; value: UpdateEngineer };

@Component({
  selector: 'app-engineer-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSlideToggleModule
  ],
  templateUrl: './engineer-form.component.html',
  styleUrl: './engineer-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EngineerFormComponent {
  private readonly fb = inject(FormBuilder);

  readonly isEditMode = input(false);
  readonly submitLabel = input('Save Engineer');
  readonly isSubmitting = input(false);
  readonly createValue = input<CreateEngineer | null>(null);
  readonly editValue = input<UpdateEngineer | null>(null);
  readonly formSubmit = output<EngineerFormSubmit>();
  readonly cancelClick = output<void>();

  readonly form = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(50)]],
    position: ['', [Validators.required, Validators.maxLength(100)]],
    hireDate: ['', Validators.required],
    isActive: [true]
  });

  constructor() {
    effect(() => {
      const passwordControl = this.form.get('password');
      if (!passwordControl) {
        return;
      }

      if (this.isEditMode()) {
        passwordControl.clearValidators();
        passwordControl.setValue('');
      } else {
        passwordControl.setValidators([Validators.required, Validators.minLength(6)]);
      }
      passwordControl.updateValueAndValidity({ emitEvent: false });
    });

    effect(() => {
      if (this.isEditMode()) {
        const value = this.editValue();
        if (value) {
          this.form.patchValue({
            fullName: value.fullName,
            email: value.email,
            phoneNumber: value.phoneNumber,
            position: value.position,
            hireDate: this.toDateInputValue(value.hireDate),
            isActive: value.isActive
          });
        }
        return;
      }

      const value = this.createValue();
      if (value) {
        this.form.patchValue({
          fullName: value.fullName,
          email: value.email,
          phoneNumber: value.phoneNumber,
          position: value.position,
          hireDate: this.toDateInputValue(value.hireDate)
        });
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    if (this.isEditMode()) {
      this.formSubmit.emit({
        mode: 'edit',
        value: {
          fullName: raw.fullName!.trim(),
          email: raw.email!.trim(),
          phoneNumber: raw.phoneNumber!.trim(),
          position: raw.position!.trim(),
          hireDate: raw.hireDate!,
          isActive: !!raw.isActive
        }
      });
      return;
    }

    this.formSubmit.emit({
      mode: 'create',
      value: {
        fullName: raw.fullName!.trim(),
        email: raw.email!.trim(),
        password: raw.password!,
        phoneNumber: raw.phoneNumber!.trim(),
        position: raw.position!.trim(),
        hireDate: raw.hireDate!
      }
    });
  }

  onCancel(): void {
    this.cancelClick.emit();
  }

  hasError(controlName: string, errorCode: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.touched && control.hasError(errorCode);
  }

  private toDateInputValue(value: string): string {
    return value ? value.substring(0, 10) : '';
  }
}
