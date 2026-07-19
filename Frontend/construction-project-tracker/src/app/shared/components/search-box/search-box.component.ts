import { Component, effect, input, output } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-search-box',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatIconModule],
  templateUrl: './search-box.component.html',
  styleUrl: './search-box.component.scss'
})
export class SearchBoxComponent {
  readonly placeholder = input('Search...');
  readonly initialValue = input('');
  readonly compact = input(false);
  readonly searchChange = output<string>();

  readonly searchControl = new FormControl('', { nonNullable: true });

  constructor() {
    effect(() => {
      const value = this.initialValue();
      if (this.searchControl.value !== value) {
        this.searchControl.setValue(value, { emitEvent: false });
      }
    });

    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed()
    ).subscribe(value => this.searchChange.emit(value.trim()));
  }
}
