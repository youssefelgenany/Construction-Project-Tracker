import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { TaskPriority } from '../../../../core/enums/task-priority';
import { ProjectAssignedEngineer } from '../../../../core/models/project-assigned-engineer';
import { TaskFormValue } from '../../models/task-form-value';
import { getTaskPriorityLabel } from '../../projects.utils';

@Component({
  selector: 'app-task-form',
  standalone: true,
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './task-form.component.html',
  styleUrl: './task-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TaskFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly engineers = input<ProjectAssignedEngineer[]>([]);
  readonly initialValue = input<TaskFormValue | null>(null);
  readonly isEditMode = input(false);
  readonly isSubmitting = input(false);
  readonly submitLabel = input('Save Task');
  readonly projectStartDate = input<string | null>(null);
  readonly projectEndDate = input<string | null>(null);
  readonly formSubmit = output<TaskFormValue>();
  readonly cancelClick = output<void>();

  readonly priorityOptions = [
    TaskPriority.Low,
    TaskPriority.Medium,
    TaskPriority.High,
    TaskPriority.Critical
  ];
  readonly descriptionMaxLength = 2000;

  readonly form = this.fb.group(
    {
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: ['', [Validators.maxLength(this.descriptionMaxLength)]],
      assignedEngineerId: [null as number | null, Validators.required],
      priority: [null as TaskPriority | null, Validators.required],
      startDate: ['', Validators.required],
      dueDate: ['', Validators.required]
    },
    { validators: [control => this.projectBoundaryValidator(control)] }
  );

  readonly projectDateHint = computed(() => {
    const start = this.projectStartDate();
    const end = this.projectEndDate();
    if (!start || !end) {
      return null;
    }
    return `Must stay within project dates (${start.substring(0, 10)} – ${end.substring(0, 10)}).`;
  });
  readonly descriptionCount = computed(() => this.form.controls.description.value?.length ?? 0);
  readonly dateRangeLabel = computed(() => {
    const start = this.projectStartDate();
    const end = this.projectEndDate();
    if (!start || !end) {
      return null;
    }

    return `${start} → ${end}`;
  });

  constructor() {
    effect(() => {
      const value = this.initialValue();
      if (value) {
        this.form.patchValue({
          title: value.title,
          description: value.description,
          assignedEngineerId: value.assignedEngineerId,
          priority: value.priority,
          startDate: this.toDateInputValue(value.startDate),
          dueDate: this.toDateInputValue(value.dueDate)
        });
      }

      if (this.isEditMode()) {
        this.form.controls.assignedEngineerId.disable();
      } else {
        this.form.controls.assignedEngineerId.enable();
      }
    });

    effect(() => {
      if (this.isSubmitting()) {
        this.form.disable({ emitEvent: false });
      } else {
        this.form.enable({ emitEvent: false });
        if (this.isEditMode()) {
          this.form.controls.assignedEngineerId.disable({ emitEvent: false });
        }
      }
    });

    this.form.controls.startDate.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.form.updateValueAndValidity({ emitEvent: false }));

    this.form.controls.dueDate.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.form.updateValueAndValidity({ emitEvent: false }));
  }

  getPriorityLabel(priority: TaskPriority): string {
    return getTaskPriorityLabel(priority);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    this.formSubmit.emit({
      title: raw.title!.trim(),
      description: raw.description?.trim() ?? '',
      assignedEngineerId: raw.assignedEngineerId!,
      priority: raw.priority!,
      startDate: raw.startDate!,
      dueDate: raw.dueDate!
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

  private projectBoundaryValidator(control: AbstractControl): ValidationErrors | null {
    const start = control.get('startDate')?.value as string | null;
    const due = control.get('dueDate')?.value as string | null;
    if (!start || !due) {
      return null;
    }

    if (start > due) {
      return { dueBeforeStart: true };
    }

    const projectStart = this.projectStartDate()?.substring(0, 10) ?? null;
    const projectEnd = this.projectEndDate()?.substring(0, 10) ?? null;
    if (!projectStart || !projectEnd) {
      return null;
    }

    if (start < projectStart || start > projectEnd || due < projectStart || due > projectEnd) {
      return { outsideProject: true };
    }

    return null;
  }

  private toDateInputValue(value: string): string {
    return value ? value.substring(0, 10) : '';
  }
}
