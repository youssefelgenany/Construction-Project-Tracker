import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize, map, switchMap, tap } from 'rxjs';
import { toSignal } from '@angular/core/rxjs-interop';
import { ProjectService } from '../../../core/services/project.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ProjectStatus } from '../../../core/enums/project-status';
import { RiskLevel } from '../../../core/enums/risk-level';
import { ProjectRisk } from '../../../core/models/project-risk.model';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../../shared/components/search-box/search-box.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { NoResultsComponent } from '../../../shared/components/no-results/no-results.component';
import { premiumDialogConfig } from '../../../shared/dialogs/premium-dialog.config';
import { ConfirmationDialogComponent } from '../../../shared/dialogs/confirmation-dialog/confirmation-dialog.component';
import { ConfirmationDialogData } from '../../../shared/dialogs/confirmation-dialog/confirmation-dialog-data';
import { ProjectStatusChipComponent } from '../components/project-status-chip/project-status-chip.component';
import { ProjectCardComponent } from '../components/project-card/project-card.component';
import {
  PROJECT_SORT_FIELDS,
  ProjectSortField,
  getProjectStatusLabel
} from '../projects.utils';
import { ProjectDialogComponent } from '../dialogs/project-dialog/project-dialog.component';
import { ProjectDialogData } from '../dialogs/project-dialog-data';
import { RiskChipComponent } from '../../../shared/components/risk-chip/risk-chip.component';

interface SortOption {
  field: ProjectSortField;
  label: string;
}

@Component({
  selector: 'app-project-list',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatCardModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTooltipModule,
    PageHeaderComponent,
    SearchBoxComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    NoResultsComponent,
    ProjectStatusChipComponent,
    ProjectCardComponent,
    RiskChipComponent
  ],
  templateUrl: './project-list.component.html',
  styleUrl: './project-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectListComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly authService = inject(AuthService);
  private readonly notificationService = inject(NotificationService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly isAdmin = this.authService.isAdmin;
  readonly isMobile = toSignal(
    this.breakpointObserver.observe([Breakpoints.Handset, Breakpoints.TabletPortrait]).pipe(
      map(result => result.matches)
    ),
    { initialValue: false }
  );

  readonly projects = signal<ProjectRisk[]>([]);
  readonly isLoading = signal(false);
  readonly isRefreshing = signal(false);
  readonly totalCount = signal(0);
  readonly searchTerm = signal('');
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);
  readonly sortActive = signal<ProjectSortField>('name');
  readonly sortDirection = signal<'asc' | 'desc'>('asc');
  readonly filterRiskLevel = signal<RiskLevel | null>(null);
  readonly filterStatus = signal<ProjectStatus | null>(null);
  readonly deletingProjectId = signal<number | null>(null);

  readonly RiskLevel = RiskLevel;
  readonly ProjectStatus = ProjectStatus;
  readonly riskOptions = [RiskLevel.None, RiskLevel.Low, RiskLevel.Medium, RiskLevel.High, RiskLevel.Critical];
  readonly statusOptions = [ProjectStatus.NotStarted, ProjectStatus.InProgress, ProjectStatus.Completed];
  readonly sortOptions: SortOption[] = [
    { field: 'name', label: 'Name' },
    { field: 'risk', label: 'Risk Level' },
    { field: 'overdue', label: 'Overdue Tasks' },
    { field: 'budget', label: 'Budget' },
    { field: 'startDate', label: 'Start Date' },
    { field: 'endDate', label: 'End Date' }
  ];

  readonly displayedProjects = computed(() => {
    const status = this.filterStatus();
    const items = this.projects();

    if (status === null) {
      return items;
    }

    return items.filter(project => project.status === status);
  });

  readonly hasActiveFilters = computed(
    () => !!this.searchTerm() || this.filterRiskLevel() !== null || this.filterStatus() !== null
  );

  readonly displayedColumns = [
    'name',
    'risk',
    'overdueTasks',
    'budget',
    'startDate',
    'endDate',
    'status',
    'progress',
    'actions'
  ];

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(
        tap(params => {
          this.searchTerm.set(params.get('search') ?? '');
          this.pageIndex.set(Math.max(0, Number(params.get('page') ?? '1') - 1));
          this.pageSize.set(Number(params.get('pageSize') ?? '10') || 10);

          const sortBy = (params.get('sortBy') ?? 'name') as ProjectSortField;
          this.sortActive.set(
            Object.prototype.hasOwnProperty.call(PROJECT_SORT_FIELDS, sortBy) ? sortBy : 'name'
          );
          this.sortDirection.set(params.get('descending') === 'true' ? 'desc' : 'asc');
          this.filterRiskLevel.set(this.toNullableRiskLevel(params.get('riskLevel')));
          this.filterStatus.set(this.toNullableStatus(params.get('status')));
        }),
        switchMap(() => this.fetchProjects()),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: result => {
          this.projects.set(result.items);
          this.totalCount.set(result.totalCount);
        }
      });
  }

  onSearchChange(term: string): void {
    this.updateQueryParams({ search: term || null, page: 1 });
  }

  onSortChange(sort: Sort): void {
    if (!sort.direction) {
      this.updateQueryParams({ sortBy: 'name', descending: 'false', page: 1 });
      return;
    }

    const active = (sort.active || 'name') as ProjectSortField;
    const direction = sort.direction === 'desc' ? 'desc' : 'asc';
    this.updateQueryParams({
      sortBy: PROJECT_SORT_FIELDS[active] ? active : 'name',
      descending: direction === 'desc' ? 'true' : 'false',
      page: 1
    });
  }

  onSortFilterChange(field: ProjectSortField): void {
    this.updateQueryParams({
      sortBy: field,
      descending: this.sortDirection() === 'desc' ? 'true' : 'false',
      page: 1
    });
  }

  onSortDirectionToggle(): void {
    const nextDirection = this.sortDirection() === 'asc' ? 'desc' : 'asc';
    this.updateQueryParams({
      sortBy: this.sortActive(),
      descending: nextDirection === 'desc' ? 'true' : 'false',
      page: 1
    });
  }

  onPageChange(event: PageEvent): void {
    this.updateQueryParams({
      page: event.pageIndex + 1,
      pageSize: event.pageSize
    });
  }

  onRiskFilterChange(value: RiskLevel | null): void {
    this.updateQueryParams({ riskLevel: value === null ? null : value, page: 1 });
  }

  onStatusFilterChange(value: ProjectStatus | null): void {
    this.updateQueryParams({ status: value === null ? null : value, page: 1 });
  }

  viewProject(project: ProjectRisk): void {
    void this.router.navigate(['/projects', project.id]);
  }

  editProject(project: ProjectRisk): void {
    const data: ProjectDialogData = { projectId: project.id };

    this.dialog
      .open(ProjectDialogComponent, premiumDialogConfig('900px', { disableClose: true, data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.refreshCurrentPage();
        }
      });
  }

  openCreateProjectDialog(): void {
    this.dialog
      .open(ProjectDialogComponent, premiumDialogConfig('900px', { disableClose: true, data: {} as ProjectDialogData }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.refreshCurrentPage();
        }
      });
  }

  deleteProject(project: ProjectRisk): void {
    const data: ConfirmationDialogData = {
      title: 'Delete Project',
      message: `Are you sure you want to delete "${project.name}"? This action cannot be undone.`,
      confirmText: 'Delete',
      cancelText: 'Cancel'
    };

    this.dialog
      .open(ConfirmationDialogComponent, premiumDialogConfig('520px', { data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) {
          return;
        }

        this.deletingProjectId.set(project.id);

        this.projectService
          .delete(project.id)
          .pipe(finalize(() => this.deletingProjectId.set(null)))
          .subscribe({
            next: () => {
              this.notificationService.success('Project deleted successfully.');
              this.loadProjects();
            }
          });
      });
  }

  trackByProjectId(_: number, project: ProjectRisk): number {
    return project.id;
  }

  projectHealthTone(project: ProjectRisk): string {
    switch (project.riskLevel) {
      case RiskLevel.Critical:
        return 'tone-danger';
      case RiskLevel.High:
      case RiskLevel.Medium:
        return 'tone-warning';
      case RiskLevel.Low:
        return 'tone-primary';
      default:
        return 'tone-neutral';
    }
  }

  getRiskFilterLabel(level: RiskLevel): string {
    switch (level) {
      case RiskLevel.None:
        return 'Healthy';
      case RiskLevel.Low:
        return 'Low';
      case RiskLevel.Medium:
        return 'Medium';
      case RiskLevel.High:
        return 'High';
      case RiskLevel.Critical:
        return 'Critical';
      default:
        return 'All';
    }
  }

  getStatusFilterLabel(status: ProjectStatus): string {
    return getProjectStatusLabel(status);
  }

  getSortLabel(field: ProjectSortField): string {
    return this.sortOptions.find(option => option.field === field)?.label ?? 'Name';
  }

  isDeleting(projectId: number): boolean {
    return this.deletingProjectId() === projectId;
  }

  private refreshCurrentPage(): void {
    this.fetchProjects()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.projects.set(result.items);
          this.totalCount.set(result.totalCount);
        }
      });
  }

  private loadProjects(): void {
    this.fetchProjects().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.projects.set(result.items);
        this.totalCount.set(result.totalCount);
      }
    });
  }

  private fetchProjects() {
    const hasExistingData = this.projects().length > 0;

    if (hasExistingData) {
      this.isRefreshing.set(true);
    } else {
      this.isLoading.set(true);
    }

    return this.projectService
      .getRiskProjects({
        search: this.searchTerm() || undefined,
        sortBy: PROJECT_SORT_FIELDS[this.sortActive()],
        descending: this.sortDirection() === 'desc',
        riskLevel: this.filterRiskLevel(),
        pageNumber: this.pageIndex() + 1,
        pageSize: this.pageSize()
      })
      .pipe(
        finalize(() => {
          this.isLoading.set(false);
          this.isRefreshing.set(false);
        })
      );
  }

  private updateQueryParams(params: Record<string, string | number | null>): void {
    const current = this.route.snapshot.queryParamMap;
    void this.router.navigate([], {
      relativeTo: this.route,
      replaceUrl: true,
      queryParams: {
        search: current.get('search'),
        page: current.get('page') ?? 1,
        pageSize: current.get('pageSize') ?? 10,
        sortBy: current.get('sortBy') ?? 'name',
        descending: current.get('descending') ?? 'false',
        riskLevel: current.get('riskLevel'),
        status: current.get('status'),
        ...params
      }
    });
  }

  private toNullableRiskLevel(value: string | null): RiskLevel | null {
    if (value === null || value === '') {
      return null;
    }

    const parsed = Number(value);
    return Number.isNaN(parsed) ? null : (parsed as RiskLevel);
  }

  private toNullableStatus(value: string | null): ProjectStatus | null {
    if (value === null || value === '') {
      return null;
    }

    const parsed = Number(value);
    return Number.isNaN(parsed) ? null : (parsed as ProjectStatus);
  }
}
