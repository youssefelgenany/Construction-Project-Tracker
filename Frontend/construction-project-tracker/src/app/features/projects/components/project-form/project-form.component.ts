import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  output
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { CreateProject } from '../../../../core/models/project';

function endAfterStartValidator(control: AbstractControl): ValidationErrors | null {
  const startDate = control.get('startDate')?.value;
  const endDate = control.get('endDate')?.value;

  if (startDate && endDate && endDate <= startDate) {
    return { endBeforeStart: true };
  }

  return null;
}

@Component({
  selector: 'app-project-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './project-form.component.html',
  styleUrl: './project-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectFormComponent {
  private readonly fb = inject(FormBuilder);

  readonly initialValue = input<CreateProject | null>(null);
  readonly submitLabel = input('Save Project');
  readonly isSubmitting = input(false);
  readonly formSubmit = output<CreateProject>();
  readonly cancelClick = output<void>();

  readonly form = this.fb.group(
    {
      name: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.required, Validators.maxLength(2000)]],
      budget: ['', [Validators.required, Validators.min(0.01)]],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required]
    },
    { validators: endAfterStartValidator }
  );
  readonly descriptionMaxLength = 2000;

  constructor() {
    effect(() => {
      const value = this.initialValue();
      if (value) {
        this.form.patchValue({
          name: value.name,
          description: value.description,
          budget: String(value.budget),
          startDate: this.toDateInputValue(value.startDate),
          endDate: this.toDateInputValue(value.endDate)
        });
      }
    });

    effect(() => {
      if (this.isSubmitting()) {
        this.form.disable({ emitEvent: false });
      } else {
        this.form.enable({ emitEvent: false });
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.formSubmit.emit({
      name: raw.name!.trim(),
      description: raw.description!.trim(),
      budget: Number(raw.budget),
      startDate: raw.startDate!,
      endDate: raw.endDate!
    });
  }

  onCancel(): void {
    this.cancelClick.emit();
  }

  hasError(controlName: string, errorCode: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.touched && control.hasError(errorCode);
  }

  hasFormError(errorCode: string): boolean {
    return this.form.touched && this.form.hasError(errorCode);
  }

  private toDateInputValue(value: string): string {
    return value ? value.substring(0, 10) : '';
  }
}
