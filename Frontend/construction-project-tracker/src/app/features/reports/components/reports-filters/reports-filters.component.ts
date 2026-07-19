import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  output,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { EngineerService } from '../../../../core/services/engineer.service';
import { ProjectService } from '../../../../core/services/project.service';
import { ReportFilters } from '../../../../core/models/report-filters';
import { ProjectStatus } from '../../../../core/enums/project-status';
import { RiskLevel } from '../../../../core/enums/risk-level';
import { Engineer } from '../../../../core/models/engineer';
import { Project } from '../../../../core/models/project';

@Component({
  selector: 'app-reports-filters',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatIconModule
  ],
  templateUrl: './reports-filters.component.html',
  styleUrl: './reports-filters.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsFiltersComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly engineerService = inject(EngineerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly filtersChange = output<ReportFilters>();

  readonly projects = signal<Project[]>([]);
  readonly engineers = signal<Engineer[]>([]);

  readonly statusOptions = [
    { value: null as ProjectStatus | null, label: 'All' },
    { value: ProjectStatus.NotStarted, label: 'Not Started' },
    { value: ProjectStatus.InProgress, label: 'In Progress' },
    { value: ProjectStatus.Completed, label: 'Completed' }
  ];

  readonly riskOptions = [
    { value: null as RiskLevel | null, label: 'All' },
    { value: RiskLevel.None, label: 'Healthy' },
    { value: RiskLevel.Low, label: 'Low' },
    { value: RiskLevel.Medium, label: 'Medium' },
    { value: RiskLevel.High, label: 'High' },
    { value: RiskLevel.Critical, label: 'Critical' }
  ];

  readonly filterForm = new FormGroup({
    startDate: new FormControl<Date | null>(null),
    endDate: new FormControl<Date | null>(null),
    projectId: new FormControl<number | null>(null),
    engineerId: new FormControl<number | null>(null),
    status: new FormControl<ProjectStatus | null>(null),
    riskLevel: new FormControl<RiskLevel | null>(null)
  });

  ngOnInit(): void {
    this.loadOptions();
  }

  applyFilters(): void {
    this.filtersChange.emit(this.buildFilters());
  }

  resetFilters(): void {
    this.filterForm.reset({
      startDate: null,
      endDate: null,
      projectId: null,
      engineerId: null,
      status: null,
      riskLevel: null
    });
    this.filtersChange.emit({});
  }

  private buildFilters(): ReportFilters {
    const value = this.filterForm.getRawValue();
    return {
      startDate: value.startDate ? this.toDateString(value.startDate) : null,
      endDate: value.endDate ? this.toDateString(value.endDate) : null,
      projectId: value.projectId,
      engineerId: value.engineerId,
      status: value.status,
      riskLevel: value.riskLevel
    };
  }

  private toDateString(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }

  private loadOptions(): void {
    this.projectService
      .getAll({ pageNumber: 1, pageSize: 100 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => this.projects.set(result.items));

    this.engineerService
      .getAll({ pageNumber: 1, pageSize: 100 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => this.engineers.set(result.items));
  }
}
