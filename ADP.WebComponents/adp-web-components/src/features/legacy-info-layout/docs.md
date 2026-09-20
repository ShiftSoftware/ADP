## Legacy info layout

A verbatim copy of the vehicle-info-layout wrapper as it was before the vehicle-lookup family moved to
the design language's card (2026-09-20). Only the part-lookup components use it, so that they stay
pixel-identical until their own redesign; it is deleted with that redesign. Do not add features here.

```typescript
  // #region Legacy info layout prop

  @Prop() coreOnly: boolean = false;

  // #endregion
```
