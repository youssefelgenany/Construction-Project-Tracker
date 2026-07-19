import { MatDialogConfig } from '@angular/material/dialog';

export const PREMIUM_DIALOG_PANEL_CLASS = 'ds-premium-dialog-panel';

export function premiumDialogConfig(
  width: string,
  overrides: MatDialogConfig = {}
): MatDialogConfig {
  const extraPanelClasses = Array.isArray(overrides.panelClass)
    ? overrides.panelClass
    : overrides.panelClass
      ? [overrides.panelClass]
      : [];

  const { panelClass: _panelClass, ...rest } = overrides;

  return {
    maxWidth: '96vw',
    maxHeight: '90vh',
    autoFocus: false,
    restoreFocus: true,
    width,
    panelClass: [PREMIUM_DIALOG_PANEL_CLASS, ...extraPanelClasses],
    ...rest
  };
}
