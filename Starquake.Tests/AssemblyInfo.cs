// ---------------------------------------------------------
// Copyright (c) 2026 Damien C Green. All rights reserved.
// ---------------------------------------------------------
// -
using Xunit;

// CollisionDetection holds its intermediate values in static fields. Until that changes,
// nothing here may run two tests at once.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
