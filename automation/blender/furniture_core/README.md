# OrdaX Furniture Generation Core

This directory is the reusable generation layer extracted from the bed work.

## Principle

A model is not accepted because it merely looks plausible. Every asset follows:

reference -> family profile -> isolated parts -> dimensions/anchors -> contact rules -> per-part validation -> review -> master assembly -> pairwise validation -> artifact

The current bed is the first reference implementation, not a one-off script.

## Core responsibilities

- interpret a furniture family and variant;
- bind all proportions to a declared real-world reference dimension;
- generate or import major parts separately;
- assign stable `ordax_*` identities;
- forbid mesh intersections by default;
- define explicit structural joints where overlap is physically intentional;
- handle cloth/soft parts with collision-aware workflows;
- validate parts before master assembly;
- keep external AI providers optional and non-authoritative.

## External providers

External tools such as Higgsfield may create a reference, blockout or candidate 3D mesh. They never bypass OrdaX validation. Imported geometry must be normalized to meters, tagged, checked against the family profile, checked for intersections and reviewed before it can enter a master assembly.

## Bed family

See `families/bed.family.json`. A future request such as a house-style bed uses the same family contract but activates a different structural variant rather than copying the current bed literally.
