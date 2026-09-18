# Knowledge packs

KillerBench reference catalogs are independent of executable repair recipes. A match explains a code in a specific product context. It does not establish a current diagnosis, authorize a command, or prove that a driver package is appropriate.

## Current format

Schema 1 is UTF-8 JSON with case-sensitive property names:

```json
{
  "SchemaVersion": 1,
  "Id": "example-reference",
  "Revision": 1,
  "PublishedOn": "2026-09-12",
  "License": "Synthetic example only",
  "Entries": [
    {
      "Context": "example-product",
      "Code": "5",
      "Title": "Example code",
      "Explanation": "Synthetic entry, not a real error definition.",
      "SourceUrl": "https://example.com/reference",
      "VerifiedOn": "2026-09-12",
      "AppliesTo": "Example product only"
    }
  ]
}
```

Context and code form one key. Lookup trims their outer whitespace and ignores case. It does not infer numeric equivalence: `5`, `0x5`, and `0x80070005` are different keys. Define aliases explicitly with their applicable context rather than treating every numeric code as a Win32 error.

Packs are limited to 32,000,000 bytes and 100,000 entries. Split larger collections into independently versioned packs. Duplicate contextual keys and duplicate JSON fields are rejected. Source URLs must use HTTPS. Verification dates cannot follow the pack publication date. These checks validate structure, not the truth or authenticity of a source.

## Content collection

1. Identify the owning product, subsystem, supported versions, and original documentation.
2. Confirm permission to redistribute the intended material. A public page is not itself a redistribution license. Retain license notices and attribution required by each source.
3. Write a concise explanation with explicit applicability and a direct source URL. Record the actual review date.
4. Keep current evidence requirements and repair actions in reviewed executable recipes. Reference packs contain no executable instructions consumed by the engine.
5. Review changed and removed entries before assigning a new monotonically increasing revision. Never publish different bytes under an existing revision.

The standalone builder can normalize reviewed schema files, convert reviewed error-code or vendor-reference CSV into schema 1 packs, import user-supplied IEEE MA-L, MA-M and MA-S CSV snapshots, import retained UEFI PNP and ACPI vendor registries, import retained Bluetooth SIG company identifiers, import retained pci.ids vendor and device assignments, import retained usb.ids vendor and product assignments, and convert reviewed driver-reference CSV into a validated schema 2 pack. UEFI and Bluetooth packs have typed Engine lookups and direct CLI commands. On Windows the builder can also generate review CSV files from defined messages in installed Win32, NTSTATUS and HRESULT message tables. Generated content depends on the retained source, installed OS build and language and remains outside a published pack until its content and redistribution terms are reviewed. Importers fingerprint their inputs and never fetch, sign, publish or authorize content. See [generator tooling](Tools/Knowledge/README.md).

Schema 2 also allows a `Driver` object on each entry: `HardwareId`, `Architecture` (`x86`, `x64`, or `arm64`), `WindowsProduct` (`client` or `server`), positive inclusive `MinimumBuild` and `MaximumBuild`, `Vendor`, `DriverVersion`, and HTTPS `DownloadUrl`. Each record declares one exact hardware ID and one OS scope. Schema 1 rejects driver metadata. Existing schema 1 code references remain supported.

Driver lookup requires exact hardware-ID equality (ignoring case), matching architecture and product, and an in-range build. It never shortens subsystem IDs or uses name/vendor similarity. Multiple matching references are retained without selecting a winner. This is a reference filter, not Windows driver ranking: INF applicability, OS editions, package signatures, OEM restrictions and current download contents are not verified. A match cannot authorize installation. Windows uses hardware and compatible IDs as described in [Microsoft's device identification documentation](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/device-identification-strings).

## Trust and updates

The implemented verifier accepts detached P-256 ECDSA signatures using SHA-256 and fixed 64-byte IEEE P1363 encoding. It verifies the exact payload bytes before parsing. Public keys use SubjectPublicKeyInfo encoding and must come from a trusted application configuration, never from the downloaded pack itself.

The caller supplies the expected pack ID and minimum acceptable revision. Selection prefers a newer verified external pack over the verified bundle. Invalid, older, or conflicting same-revision external packs fall back with a reason.

`KnowledgeActivation` and the `knowledge-activate` CLI command verify a signed local candidate, store it as an immutable versioned container and atomically replace a small active pointer. A minimum revision and the active revision prevent rollback. Identical revisions are idempotent, conflicting bytes are rejected and prior containers remain available. `knowledge-active` verifies the selected container again before returning it. `knowledge-recover` scans retained containers and atomically selects the newest version that passes signature, identity, revision, file-name and payload-hash checks. These APIs do not provision publisher keys or revoke compromised keys.

`KnowledgeUpdates` supplies the application-facing orchestration boundary for one database or an ordered batch. The host injects the candidate source and trusted key. Progress distinguishes inspection, retained-version recovery, retrieval, verification, activation, current data and failure. A damaged active version is automatically recovered before candidate processing. A rejected candidate reports retained installed metadata when it remains verifiable, and one failed catalog does not prevent another catalog from being checked.

The supplied HTTP candidate source accepts only configured HTTPS payload and detached-signature URLs without embedded credentials or fragments. It bounds streamed payload and signature bytes even when Content-Length is absent. The host remains responsible for HTTP policy, production endpoints and trusted keys.

`KnowledgeUpdateConfiguration` reads a strict local publisher configuration and creates the update requests and HTTPS endpoints used by the Engine. Configuration is limited to 32 unique lowercase pack identities. Unknown or duplicate JSON fields, unsafe identities, invalid URLs, invalid P-256 public keys and unsupported revisions are rejected before storage or network access. Each pack receives its own directory beneath the selected storage root.

```json
{
  "schemaVersion": 1,
  "databases": [
    {
      "packId": "example-reference",
      "minimumRevision": 1,
      "payloadUrl": "https://updates.example/reference.json",
      "signatureUrl": "https://updates.example/reference.sig",
      "trustedPublicKey": "BASE64_SUBJECT_PUBLIC_KEY_INFO"
    }
  ]
}
```

`knowledge-status CONFIG_JSON STORAGE_ROOT` verifies every configured active pack without downloading or changing data. Missing and unverifiable packs are reported independently. `knowledge-update CONFIG_JSON STORAGE_ROOT` retrieves and activates each signed candidate in configuration order. It returns a review exit when any pack fails while continuing to check the remaining configured packs. Configuration establishes the trust root, so production applications must ship a reviewed configuration through a trusted application release rather than downloading keys or endpoints from an untrusted source.

Local app imports currently perform structural validation only and label results as unverified reference data. The About card inspects app-configured active packs without network access and enables Update databases only when a valid `knowledge-updates.json` publisher configuration is installed beside KillerBench. Verified active packs join the current session after an update. Local imports remain separate and are never promoted into trusted active data.

## Using a local pack

CLI: `killerbench-cli knowledge PACK_PATH CONTEXT CODE`

Pack inventory: `killerbench-cli knowledge-info PACK_PATH`. The Engine and CLI return validated pack metadata plus deterministic per-context entry and driver-reference counts. Inventory does not sample entries, verify factual accuracy or establish publisher trust.

Log references: `killerbench-cli knowledge-log PACK_PATH CONTEXT LOG_PATH`. The shared Engine analyzes bounded text and attaches references to observed eight-digit hexadecimal codes in the selected context. Unmatched codes, occurrence counts, original evidence lines and truncation flags remain in the report. Context is explicitly supplied, not inferred from a filename. Decimal codes and numeric aliases are not inferred. This does not run repairs or convert reference matches into findings.

Driver references: `killerbench-cli driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`. Quote hardware IDs in the shell. No downloads occur. There is not yet a production driver catalog or automatic device-to-catalog UI.

MAC vendor references: `killerbench-cli mac-vendor PACK_PATH MAC_ADDRESS`. The Engine accepts 12 hexadecimal digits, six colon- or hyphen-separated octets, or three dot-separated groups. It checks 36-bit, 28-bit and then 24-bit IEEE assignments and returns the longest prefix present in the validated local pack. It makes no network request. Multicast and locally administered addresses do not receive vendor claims because they do not reliably identify one registry assignment. A match identifies the registered assignment owner; it does not prove the current device manufacturer.

PCI vendor references: `killerbench-cli pci-vendor PACK_PATH PCI_HARDWARE_ID`. Quote the hardware ID in the shell. The Engine requires one complete `PCI\VEN_hhhh&DEV_hhhh` identity, permits bounded suffix fields, and rejects repeated vendor or device fields. It returns exact vendor and device assignments plus pack provenance without a network request. A registry assignment does not prove the manufacturer of the complete product, establish driver compatibility or authorize a download or installation.

USB vendor references: `killerbench-cli usb-vendor PACK_PATH USB_HARDWARE_ID`. Quote the hardware ID in the shell. The Engine requires one complete `USB\VID_hhhh&PID_hhhh` identity, permits bounded suffix fields, and rejects repeated vendor or product fields. It returns exact vendor and product assignments plus pack provenance without a network request. A registry assignment does not prove the manufacturer of the complete product, establish driver compatibility or authorize a download or installation.

UEFI vendor references: `killerbench-cli uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`. The Engine requires one exact three-letter PNP ID or four-character ACPI ID and keeps the two registries separate. It returns the exact assignment plus pack provenance without a network request. A registry assignment does not prove the manufacturer of the complete product, establish driver compatibility or authorize a download or installation.

Bluetooth company references: `killerbench-cli bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`. The Engine accepts one unsigned 16-bit decimal or `0x`-prefixed identifier and normalizes it to four hexadecimal digits. It returns the exact Bluetooth SIG assignment plus pack provenance without inspecting radio traffic or making a network request. An assignment does not prove the manufacturer of the complete product, establish driver compatibility or authorize a download or installation.

In the app, expand Advanced tools, enter the product context and code, then choose Open pack and look up code. Results include pack provenance and applicability in the console. No production catalog is bundled yet.

## Validation still required

The About card follows KillerScan's inline progress and result pattern. The Engine, CLI and desktop provide strict publisher configuration, bounded HTTPS retrieval, ordered progress, installed revision, entry count, activation time and automatic retained-version recovery. With no installed configuration, the action stays disabled and explains that production updates are not configured. Rendered interaction still requires validation after reviewed production endpoints and trusted publisher keys are installed.

- Licensed, reviewed production content with reproducible source inventories.
- Representative production-pack memory and performance measurements. A local synthetic check loaded 100,000 entries (18,189,008 bytes) in 1,119 ms and looked up all entries in 32 ms on the development machine. This is one observed run, not a performance guarantee; driver-heavy packs and other machines remain unmeasured. Regression checks also reject excess byte/entry counts and duplicate JSON fields.
- Reviewed production endpoints and trusted-key provisioning plus revocation policy.
- Reviewed driver catalog content, device-to-catalog UI, and official package compatibility/signature verification.
- Reliable product-context identification for automatic suggestions. Engine/CLI log reference attachment currently requires an explicit context.
