# Knowledge packs

Knowledge packs keep large and frequently updated reference data outside the Engine's diagnostic logic.

## Suitable data

- Windows and Setup error codes
- Vendor and device identifiers
- PCI, USB, ACPI, and UEFI references
- Network OUI vendor data
- Driver and support locations
- Application and package metadata
- Curated support links and diagnostic scopes

## Pack contract

A pack should identify its format, schema version, source, source revision, generation time, and content hash. The Engine validates those fields before using the data. Unknown or invalid packs remain unavailable rather than silently falling back to guesses.

## Generators

Generators belong in the repository when they are required to reproduce a distributed pack. They should:

1. Download only from named authoritative sources.
2. Record source revisions and licenses.
3. Validate input before transformation.
4. Normalize data deterministically.
5. Write a manifest and content hash.
6. Produce the same output from the same source input.

Large generated databases and temporary downloads do not belong in the Engine source folder. The repository can contain small fixtures for parser and schema tests.

## Updates

The desktop About surface can offer an Update databases action. An update should download to a temporary location, validate the manifest and hash, then replace the active pack atomically. A failed update must leave the prior valid pack available.

## Separation from diagnostics

Knowledge data can explain an identifier or suggest a next diagnostic scope. It cannot convert a historical code into a current fault, choose a driver automatically, or authorize a repair. Current machine evidence remains the deciding input.
