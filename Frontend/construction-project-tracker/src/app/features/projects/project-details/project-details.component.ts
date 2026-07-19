import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  OnInit,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDialog } from '@angular/material/dialog';
import { AssignmentService } from '../../../core/services/assignment.service';
import { ProjectService } from '../../../core/services/project.service';
import { AuthService } from '../../../core/services/auth.service';
import { ProjectDetails } from '../../../core/models/project';
import { ProjectDelayPrediction } from '../../../core/models/project-delay-prediction.model';
import { ProjectAssignedEngineer } from '../../../core/models/project-assigned-engineer';
import { BackLinkComponent } from '../../../shared/components/back-link/back-link.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { LoadingSpinnerComponent } from '../../../shared/components/loading-spinner/loading-spinner.component';
import { ProjectDetailsGeneralComponent } from '../components/project-details-general/project-details-general.component';
import { ProjectEngineersTabComponent } from '../components/project-engineers-tab/project-engineers-tab.component';
import { ProjectTasksTabComponent } from '../components/project-tasks-tab/project-tasks-tab.component';
import { ProjectDocumentsTabComponent } from '../components/project-documents-tab/project-documents-tab.component';
import { ProjectTimelineTabComponent } from '../components/project-timeline-tab/project-timeline-tab.component';
import { ProjectStatusChipComponent } from '../components/project-status-chip/project-status-chip.component';
import { ProjectDelayPredictionCardComponent } from '../components/project-delay-prediction-card/project-delay-prediction-card.component';
import { ProjectDialogComponent } from '../dialogs/project-dialog/project-dialog.component';
import { ProjectDialogData } from '../dialogs/project-dialog-data';
import { premiumDialogConfig } from '../../../shared/dialogs/premium-dialog.config';
import { DeadlineExtensionService } from '../../../core/services/deadline-extension.service';
import {
  DeadlineExtensionRequest,
  ExtensionRequestStatus
} from '../../../core/models/deadline-extension.model';
import { ProjectStatus } from '../../../core/enums/project-status';
import { RequestDeadlineExtensionDialogComponent } from '../../deadline-extensions/dialogs/request-deadline-extension-dialog.component';
import { AdminExtendDeadlineDialogComponent } from '../../deadline-extensions/dialogs/admin-extend-deadline-dialog.component';

@Component({
  selector: 'app-project-details',
  standalone: true,
  imports: [
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    PageHeaderComponent,
    BackLinkComponent,
    LoadingSpinnerComponent,
    ProjectStatusChipComponent,
    ProjectDetailsGeneralComponent,
    ProjectEngineersTabComponent,
    ProjectTasksTabComponent,
    ProjectDocumentsTabComponent,
    ProjectTimelineTabComponent,
    ProjectDelayPredictionCardComponent
  ],
  templateUrl: './project-details.component.html',
  styleUrl: './project-details.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectDetailsComponent implements OnInit {
  private readonly projectService = inject(ProjectService);
  private readonly assignmentService = inject(AssignmentService);
  private readonly deadlineExtensionService = inject(DeadlineExtensionService);
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  readonly isAdmin = this.authService.isAdmin;
  readonly isLoading = signal(true);
  readonly predictionLoading = signal(false);
  readonly project = signal<ProjectDetails | null>(null);
  readonly delayPrediction = signal<ProjectDelayPrediction | null>(null);
  readonly assignedEngineers = signal<ProjectAssignedEngineer[]>([]);
  readonly latestExtension = signal<DeadlineExtensionRequest | null>(null);
  selectedTabIndex = 0;
  readonly ExtensionRequestStatus = ExtensionRequestStatus;
  readonly ProjectStatus = ProjectStatus;

  readonly canRequestProjectExtension = computed(() => {
    const details = this.project();
    if (!details || this.isAdmin() || details.status === ProjectStatus.Completed) {
      return false;
    }
    return this.latestExtension()?.status !== ExtensionRequestStatus.Pending;
  });

  readonly canAdminExtendProjectDeadline = computed(() => {
    const details = this.project();
    return !!details && this.isAdmin() && details.status !== ProjectStatus.Completed;
  });

  readonly engineerNames = computed(() =>
    this.assignedEngineers()
      .map(engineer => engineer.fullName)
      .slice(0, 4)
      .join(', ')
  );

  readonly extraEngineerCount = computed(() => Math.max(0, this.assignedEngineers().length - 4));

  readonly daysRemaining = computed(() => {
    const details = this.project();
    if (!details) {
      return null;
    }

    const end = new Date(details.endDate);
    const today = new Date();
    const startOfToday = new Date(today.getFullYear(), today.getMonth(), today.getDate());
    const startOfEnd = new Date(end.getFullYear(), end.getMonth(), end.getDate());
    return Math.ceil((startOfEnd.getTime() - startOfToday.getTime()) / 86_400_000);
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      void this.router.navigate(['/projects']);
      return;
    }

    this.loadProject(id);
  }

  onAssignmentChanged(): void {
    this.refreshProjectSummary();
  }

  onTasksChanged(): void {
    this.refreshProjectSummary();
  }

  onDocumentsChanged(): void {
    this.refreshProjectSummary();
  }

  openEditProjectDialog(): void {
    const details = this.project();
    if (!details) {
      return;
    }

    const data: ProjectDialogData = { projectId: details.id };

    this.dialog
      .open(ProjectDialogComponent, premiumDialogConfig('900px', { disableClose: true, data }))
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.loadProject(details.id);
        }
      });
  }

  openRequestProjectExtensionDialog(): void {
    const details = this.project();
    if (!details || !this.canRequestProjectExtension()) {
      return;
    }

    this.dialog
      .open(
        RequestDeadlineExtensionDialogComponent,
        premiumDialogConfig('640px', {
          data: {
            target: 'project',
            entityId: details.id,
            entityTitle: details.name,
            currentDeadline: details.endDate
          }
        })
      )
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.loadExtension(details.id);
        }
      });
  }

  openAdminExtendProjectDeadlineDialog(): void {
    const details = this.project();
    if (!details || !this.canAdminExtendProjectDeadline()) {
      return;
    }

    this.dialog
      .open(
        AdminExtendDeadlineDialogComponent,
        premiumDialogConfig('640px', {
          data: {
            target: 'project',
            entityId: details.id,
            entityTitle: details.name,
            currentDeadline: details.endDate
          }
        })
      )
      .afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(saved => {
        if (saved) {
          this.loadProject(details.id);
        }
      });
  }

  private refreshProjectSummary(): void {
    const id = this.project()?.id;
    if (!id) {
      return;
    }

    this.projectService
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: project => this.project.set(project)
      });

    this.loadAssignments(id);
    this.loadDelayPrediction(id);
  }

  private loadProject(id: number): void {
    this.isLoading.set(true);

    this.projectService
      .getById(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: project => {
          this.project.set(project);
          this.loadAssignments(project.id);
          this.loadDelayPrediction(project.id);
          this.loadExtension(project.id);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
          void this.router.navigate(['/projects']);
        }
      });
  }

  private loadExtension(projectId: number): void {
    this.deadlineExtensionService
      .getLatestProjectRequest(projectId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: request => this.latestExtension.set(request),
        error: () => this.latestExtension.set(null)
      });
  }

  private loadDelayPrediction(projectId: number): void {
    this.predictionLoading.set(true);

    this.projectService
      .getDelayPrediction(projectId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: prediction => {
          this.delayPrediction.set(prediction);
          this.predictionLoading.set(false);
        },
        error: () => {
          this.delayPrediction.set(null);
          this.predictionLoading.set(false);
        }
      });
  }

  private loadAssignments(projectId: number): void {
    this.assignmentService
      .getByProject(projectId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: assignments => this.assignedEngineers.set(assignments),
        error: () => this.assignedEngineers.set([])
      });
  }
}
