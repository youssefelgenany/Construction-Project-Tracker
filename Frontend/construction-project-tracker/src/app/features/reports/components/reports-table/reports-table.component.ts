import {
  ChangeDetectionStrategy,
  Component,
  input,
  output
} from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ReportProjectRow } from '../../../../core/models/report-project-row';
import { SearchBoxComponent } from '../../../../shared/components/search-box/search-box.component';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state.component';
import { NoResultsComponent } from '../../../../shared/components/no-results/no-results.component';

@Component({
  selector: 'app-reports-table',
  standalone: true,
  imports: [
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatProgressBarModule,
    SearchBoxComponent,
    EmptyStateComponent,
    NoResultsComponent
  ],
  templateUrl: './reports-table.component.html',
  styleUrl: './reports-table.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReportsTableComponent {
  readonly rows = input.required<ReportProjectRow[]>();
  readonly totalCount = input(0);
  readonly pageSize = input(10);
  readonly pageIndex = input(0);
  readonly isLoading = input(false);

  readonly searchChange = output<string>();
  readonly sortChange = output<Sort>();
  readonly pageChange = output<PageEvent>();

  readonly columns = [
    'projectName',
    'manager',
    'progressPercentage',
    'openTasks',
    'completedTasks',
    'overdueTasks',
    'documentsCount',
    'engineersAssigned'
  ] as const;

  onSearch(term: string): void {
    this.searchChange.emit(term);
  }

  onSort(sort: Sort): void {
    this.sortChange.emit(sort);
  }

  onPage(event: PageEvent): void {
    this.pageChange.emit(event);
  }

  managerLabel(manager: string | null): string {
    return manager?.trim() ? manager : 'N/A';
  }
}
