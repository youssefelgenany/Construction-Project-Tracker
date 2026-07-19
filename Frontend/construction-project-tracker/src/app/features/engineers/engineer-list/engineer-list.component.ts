import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize, map, switchMap, tap } from 'rxjs';
import { EngineerService } from '../../../core/services/engineer.service';
import { EngineerWorkload } from '../../../core/models/engineer-workload.model';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { SearchBoxComponent } from '../../../shared/components/search-box/search-box.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { EmptyStateComponent } from '../../../shared/components/empty-state/empty-state.component';
import { NoResultsComponent } from '../../../shared/components/no-results/no-results.component';
import { EngineerCardComponent } from '../components/engineer-card/engineer-card.component';
import { WorkloadChipComponent } from '../../../shared/components/workload-chip/workload-chip.component';
import {
  CAPACITY_FILTER_OPTIONS,
  CapacityFilter,
  ENGINEER_LIST_SORT_OPTIONS,
  EngineerListSortField,
  capacityToApiLevel,
  getEngineerInitials
} from '../engineers.utils';

@Component({
  selector: 'app-engineer-list',
  standalone: true,
  imports: [
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatSelectModule,
    MatTableModule,
    MatProgressBarModule,
    MatTooltipModule,
    PageHeaderComponent,
    SearchBoxComponent,
    LoadingSpinnerComponent,
    EmptyStateComponent,
    NoResultsComponent,
    EngineerCardComponent,
    WorkloadChipComponent
  ],
  templateUrl: './engineer-list.component.html',
  styleUrl: './engineer-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EngineerListComponent implements OnInit {
  private readonly engineerService = inject(EngineerService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly breakpointObserver = inject(BreakpointObserver);

  readonly isMobile = toSignal(
    this.breakpointObserver.observe([Breakpoints.Handset, Breakpoints.TabletPortrait]).pipe(
      map(result => result.matches)
    ),
    { initialValue: false }
  );

  readonly engineers = signal<EngineerWorkload[]>([]);
  readonly isLoading = signal(false);
  readonly isRefreshing = signal(false);
  readonly totalCount = signal(0);
  readonly searchTerm = signal('');
  readonly capacityFilter = signal<CapacityFilter>('all');
  readonly sortActive = signal<EngineerListSortField>('name');
  readonly sortDirection = signal<'asc' | 'desc'>('asc');
  readonly pageIndex = signal(0);
  readonly pageSize = signal(10);

  readonly capacityFilterOptions = CAPACITY_FILTER_OPTIONS;
  readonly sortOptions = ENGINEER_LIST_SORT_OPTIONS;

  readonly displayedColumns = ['engineer', 'performance', 'workload', 'availability', 'actions'];

  readonly hasActiveFilters = computed(
    () => !!this.searchTerm() || this.capacityFilter() !== 'all'
  );

  readonly getInitials = getEngineerInitials;

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(
        tap(params => {
          this.searchTerm.set(params.get('search') ?? '');
          this.capacityFilter.set(this.toCapacityFilter(params.get('capacity') ?? params.get('workload')));
          this.sortActive.set(this.toSortField(params.get('sortBy')));
          this.sortDirection.set(params.get('descending') === 'true' ? 'desc' : 'asc');
          this.pageIndex.set(Math.max(0, Number(params.get('page') ?? '1') - 1));
          this.pageSize.set(Number(params.get('pageSize') ?? '10') || 10);
        }),
        switchMap(() => this.fetchEngineers()),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: result => {
          this.engineers.set(result.items);
          this.totalCount.set(result.totalCount);
        }
      });
  }

  onSearchChange(term: string): void {
    this.updateQueryParams({ search: term || null, page: 1 });
  }

  onCapacityFilterChange(filter: CapacityFilter): void {
    this.updateQueryParams({ capacity: filter === 'all' ? null : filter, page: 1 });
  }

  onSortFilterChange(field: EngineerListSortField): void {
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

  viewEngineer(engineer: EngineerWorkload | { engineerId: number }): void {
    void this.router.navigate(['/engineers', engineer.engineerId]);
  }

  trackByEngineerId(_: number, engineer: EngineerWorkload): number {
    return engineer.engineerId;
  }

  workloadPercent(engineer: EngineerWorkload): number {
    return Math.min(100, Math.round((engineer.activeTasks / 15) * 100));
  }

  private fetchEngineers() {
    const hasExistingData = this.engineers().length > 0;
    if (hasExistingData) {
      this.isRefreshing.set(true);
    } else {
      this.isLoading.set(true);
    }

    const capacity = this.capacityFilter();

    return this.engineerService
      .getWorkload({
        search: this.searchTerm() || undefined,
        workloadLevel: capacityToApiLevel(capacity),
        pageNumber: 1,
        pageSize: 100
      })
      .pipe(
        map(result => {
          let items = [...result.items];

          if (capacity === 'busy') {
            items = items.filter(item => item.overdueTasks === 0);
          } else if (capacity === 'overloaded') {
            items = items.filter(item => item.overdueTasks > 0);
          }

          items = this.sortItems(items);

          const totalCount = items.length;
          const start = this.pageIndex() * this.pageSize();
          const paged = items.slice(start, start + this.pageSize());

          return { items: paged, totalCount };
        }),
        finalize(() => {
          this.isLoading.set(false);
          this.isRefreshing.set(false);
        })
      );
  }

  private sortItems(items: EngineerWorkload[]): EngineerWorkload[] {
    const field = this.sortActive();
    const descending = this.sortDirection() === 'desc';
    const direction = descending ? -1 : 1;

    return [...items].sort((a, b) => {
      let comparison = 0;

      switch (field) {
        case 'activeTasks':
          comparison = a.activeTasks - b.activeTasks;
          break;
        case 'completedTasks':
          comparison = a.completedTasks - b.completedTasks;
          break;
        case 'performance':
          comparison = a.averageProgress - b.averageProgress;
          break;
        case 'hireDate':
          comparison = new Date(a.hireDate).getTime() - new Date(b.hireDate).getTime();
          break;
        case 'name':
        default:
          comparison = a.engineerName.localeCompare(b.engineerName);
          break;
      }

      return comparison * direction;
    });
  }

  private updateQueryParams(params: Record<string, string | number | null>): void {
    const current = this.route.snapshot.queryParamMap;

    void this.router.navigate([], {
      relativeTo: this.route,
      replaceUrl: true,
      queryParams: {
        search: current.get('search'),
        capacity: current.get('capacity') ?? current.get('workload'),
        sortBy: current.get('sortBy') ?? 'name',
        descending: current.get('descending') ?? 'false',
        page: current.get('page') ?? 1,
        pageSize: current.get('pageSize') ?? 10,
        ...params
      }
    });
  }

  private toCapacityFilter(value: string | null): CapacityFilter {
    const valid = CAPACITY_FILTER_OPTIONS.some(option => option.value === value);
    if (valid) {
      return value as CapacityFilter;
    }

    switch (value) {
      case 'low':
        return 'available';
      case 'medium':
        return 'balanced';
      case 'high':
        return 'busy';
      default:
        return 'all';
    }
  }

  private toSortField(value: string | null): EngineerListSortField {
    const valid = ENGINEER_LIST_SORT_OPTIONS.some(option => option.field === value);
    return valid ? (value as EngineerListSortField) : 'name';
  }
}
