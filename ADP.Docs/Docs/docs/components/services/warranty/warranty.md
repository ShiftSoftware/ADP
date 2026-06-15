# Warranty System

The warranty system manages **manufacturer warranty claims** from end to end — from the moment a dealer performs a warranty repair, through review and settlement by the distributor, all the way to reimbursement by the manufacturer. It is the hub that connects the three parties of an authorized distribution network.

```mermaid
flowchart LR
    D["<b>Dealer</b><br/>Performs the warranty<br/>repair and submits a claim"]
    S["<b>Distributor</b><br/>Reviews, certifies, invoices,<br/>and forwards claims"]
    M["<b>Manufacturer</b><br/>Settles claims through<br/>its warranty system"]
    D -->|"submits claim"| S
    S -->|"reimburses dealer"| D
    S -->|"exports claims"| M
    M -->|"Payment Advice"| S
    classDef dealer fill:#fef3c7,color:#78350f,stroke:#f59e0b,stroke-width:2px
    classDef dist fill:#e0e7ff,color:#1e3a8a,stroke:#6366f1,stroke-width:2px
    classDef mfr fill:#ede9fe,color:#4c1d95,stroke:#8b5cf6,stroke-width:2px
    class D dealer
    class S dist
    class M mfr
```

The system runs two settlement legs back to back: **dealer ↔ distributor** (the distributor reimburses the dealer for approved repairs) and **distributor ↔ manufacturer** (the manufacturer reimburses the distributor once the claims are processed on its side).

!!! info "The parties"
    - **Customer** — the vehicle owner, who receives the warranty repair at no charge.
    - **Dealer** — the authorized service point that carried out the repair and is owed its cost.
    - **Distributor** — owns the warranty program, reviews every claim, and settles with both sides.
    - **Manufacturer** — receives claims through its own warranty system and reimburses the distributor via a **Payment Advice**.

!!! note "Not to be confused with Claimable Items"
    This module covers **manufacturer warranty claims** — repairs against the vehicle's defect warranty, settled in labor / parts / sublet amounts and ultimately reimbursed by the manufacturer. The separate [Claimable Items](../claimable-items.md) module covers **free and paid services** (the standard maintenance bundle, extended warranty, promotions) that the *distributor* reimburses. The two share vocabulary ("claim", "certificate", "invoice") but are distinct pipelines.

---

## The Claim Lifecycle at a Glance

Every warranty claim travels the same six stages. They map one-to-one onto the two settlement legs above.

```mermaid
flowchart LR
    subgraph LEG1["Dealer ↔ Distributor"]
      direction LR
      S1["<b>1 · Submission</b><br/>Dealer files the claim"]
      S2["<b>2 · Review</b><br/>Distributor accepts / rejects"]
      S3["<b>3 · Certificate</b><br/>Claims certified per dealer"]
      S4["<b>4 · Invoice</b><br/>Certificate is invoiced"]
    end
    subgraph LEG2["Distributor ↔ Manufacturer"]
      direction LR
      S5["<b>5 · Export</b><br/>Upload to manufacturer"]
      S6["<b>6 · Reconcile</b><br/>Payment Advice received"]
    end
    S1 --> S2 --> S3 --> S4 --> S5 --> S6
    classDef dealer fill:#fef3c7,color:#78350f,stroke:#f59e0b
    classDef dist fill:#e0e7ff,color:#1e3a8a,stroke:#6366f1
    classDef mfr fill:#ede9fe,color:#4c1d95,stroke:#8b5cf6
    class S1 dealer
    class S2,S3,S4 dist
    class S5,S6 mfr
```

| # | Stage | Driven by | What happens | Claim status after |
|---|-------|-----------|--------------|--------------------|
| 1 | **Dealer Claim Submission** | Dealer | Repair captured as a claim (labor, parts, sublet) and submitted | `Draft` → `Pending` |
| 2 | **Review by Distributor** | Distributor | Claim accepted, bounced back for correction, or rejected | `Accepted` / `Error` / `Rejected` |
| 3 | **Dealer Certificate** | Distributor | Accepted claims grouped onto a per-dealer certificate | `Certified` |
| 4 | **Dealer Invoice** | Distributor | The certificate is invoiced for settlement | `Invoiced` |
| 5 | **Export & Upload to Manufacturer** | Distributor | Claims exported as a manufacturer CSV + invoice | Manufacturer status → `Exported` |
| 6 | **Reconciliation** | Distributor | The manufacturer's Payment Advice imported and matched | Manufacturer status → `Paid` / `Rejected` |

---

## Two Status Tracks

A claim carries **two independent statuses** at all times. Keeping them separate is what lets the dealer↔distributor leg settle before the manufacturer has even seen the claim.

```mermaid
flowchart TB
    C(["A Warranty Claim"])
    C --> A["<b>Claim Status</b><br/>The internal dealer ↔ distributor<br/>lifecycle (stages 1–4)"]
    C --> B["<b>Manufacturer Status</b><br/>The manufacturer-side<br/>lifecycle (stages 5–6)"]
    classDef root fill:#4f46e5,color:#fff,stroke:#4338ca,stroke-width:2px
    classDef facet fill:#f1f5f9,color:#0f172a,stroke:#cbd5e1
    class C root
    class A,B facet
```

### Claim Status — the internal lifecycle

```mermaid
stateDiagram-v2
    direction LR
    [*] --> Draft: dealer creates
    Draft --> Pending: Submit to Distributor
    Pending --> Accepted: distributor accepts
    Pending --> Error: distributor returns with error
    Pending --> Rejected: distributor rejects permanently
    Error --> Pending: dealer corrects and resubmits
    Accepted --> Certified: grouped onto a Certificate
    Certified --> Invoiced: Certificate invoiced
    Rejected --> [*]
    Invoiced --> [*]
```

Only **Draft** and **Error** claims are editable by the dealer; once **Certified** or **Invoiced**, a claim is locked. (A distributor-created claim skips `Draft` and starts at `Pending`.)

### Manufacturer Status — the manufacturer lifecycle

```mermaid
stateDiagram-v2
    direction LR
    [*] --> NA: not yet exported
    NA --> Exported: uploaded to manufacturer
    Exported --> Paid: Payment Advice — settled
    Exported --> Rejected: Payment Advice — nothing settled
    Paid --> Exported: settlement reversed
    Rejected --> Exported: settlement reversed
    Paid --> [*]
```

!!! note "Defined but currently unused"
    The manufacturer track also defines **Downloaded** and **On Hold** states for completeness with the manufacturer's system, but the current flow never sets them — claims move straight from `Exported` to `Paid` or `Rejected`.

---

## Stage 1 · Dealer Claim Submission

The dealer captures a completed warranty repair as a claim. Saving creates it as a **Draft** with an auto-generated claim number; **Submit to Distributor** then advances it to **Pending**.

### Anatomy of a claim

A claim is a header (the vehicle, the defect, the dates) plus three kinds of repeating cost lines. The totals roll up automatically from the lines.

```mermaid
flowchart TB
    H["<b>Claim Header</b><br/>VIN · Warranty Type · Brand · Dates ·<br/>Odometer · Repair Order · Defect (Condition / Cause / Remedy)"]
    H --> L["<b>Labor Lines</b><br/>Operation no. + hours<br/>(one flagged as the main operation)"]
    H --> P["<b>Part Lines</b><br/>Part no. + qty + price<br/>(one flagged as the failed part)"]
    H --> S["<b>Sublet Lines</b><br/>Sublet type + invoice + amount"]
    L --> T["<b>Total Claim Amount</b><br/>Labor + Parts + Sublet"]
    P --> T
    S --> T
    classDef head fill:#e0e7ff,color:#1e3a8a,stroke:#6366f1,stroke-width:2px
    classDef line fill:#fef3c7,color:#78350f,stroke:#f59e0b
    classDef total fill:#dcfce7,color:#14532f,stroke:#16a34a,stroke-width:2px
    class H head
    class L,P,S line
    class T total
```

- **Labor** is valued as *hours × labor rate*. Hours auto-fill from the flat-rate catalog by operation code and model year.
- **Parts** are valued as *price × quantity*, with prices resolved from the part-lookup service. Parts are further weighted by the parts reimbursement rate (**PRR**) on the distributor side.
- **Sublet** captures outsourced work (paint, towing, electrical, etc.) as flat amounts.

### Claim categories

Each claim declares a **warranty type** — the category the manufacturer settles it under. A typical configuration covers:

| Category | Covers |
|----------|--------|
| **Vehicle Warranty** | The standard defect warranty on the vehicle |
| **Service Parts Warranty** | Parts replaced during a workshop repair |
| **Counter-Sales Parts Warranty** | Parts sold over the counter |
| **Accessory Warranty** | Fitted accessories |
| **Counter-Sales Accessory Warranty** | Accessories sold over the counter |
| **Campaign** | Recall / field-action work |

The exact set is configurable per distributor. The form adapts to the chosen type — a counter-sales (non-vehicle) type hides the VIN and repair fields, a parts-warranty type captures the previous repair, an accessory type captures the install date and mileage, and a campaign type resolves its campaign code automatically from the labor operations. An orthogonal **Operation Type** (General, Paint, Noise, Rain, Campaign, Dealer / Distributor Stock Disposal) classifies the nature of the work.

### Claim numbering

```mermaid
flowchart LR
    A["Claim saved"] --> B["<b>Claim Number</b><br/>the canonical reference<br/>used everywhere downstream"]
    A --> C["<b>Dealer Claim Number</b><br/>the dealer's own<br/>running sequence"]
    classDef a fill:#e0e7ff,color:#1e3a8a,stroke:#6366f1
    classDef b fill:#dcfce7,color:#14532f,stroke:#16a34a
    class A a
    class B,C b
```

The **Claim Number** is generated from the claim year, a per-dealer code, and a running sequence. It is immutable once assigned and is the key used everywhere downstream — on the certificate, in the manufacturer export, and when matching the Payment Advice.

!!! info "One delivery date per VIN"
    A vehicle's **Delivery Date** is treated as a property of the VIN, not the individual claim — all claims for the same VIN must agree on it. Once any claim for the VIN reaches a *verified* state (Certified, Invoiced, or Manufacturer Paid), the date is locked and propagation can never overwrite it. Editing the date on an unverified claim offers to align its siblings. See the [Warranty Dates](../../../generated/Features/WarrantyDates.md) behavior specs.

---

## Stage 2 · Review by Distributor

The distributor works the queue of **Pending** claims. Review actions are taken on a selection of claims at once and drive the claim status.

```mermaid
flowchart LR
    P["<b>Pending</b><br/>claim awaiting review"]
    P -->|"Accept"| A["<b>Accepted</b><br/>eligible to be certified"]
    P -->|"Error"| E["<b>Error</b><br/>returned to dealer<br/>with a message, editable again"]
    P -->|"Reject"| R["<b>Rejected</b><br/>permanently closed"]
    E -.->|"corrected and resubmitted"| P
    classDef pend fill:#e0e7ff,color:#1e3a8a,stroke:#6366f1
    classDef ok fill:#dcfce7,color:#14532f,stroke:#16a34a
    classDef warn fill:#fef3c7,color:#78350f,stroke:#f59e0b
    classDef bad fill:#fee2e2,color:#7f1d1d,stroke:#ef4444
    class P pend
    class A ok
    class E warn
    class R bad
```

- **Accept** — the claim is sound and ready to be certified.
- **Error** — a correctable problem; the claim is bounced back to the dealer with an **error message** and becomes editable again for resubmission.
- **Reject** — the claim is permanently declined.

While reviewing, the distributor can also adjust the settlement: per-claim **labor / parts / sublet adjustment percentages**, the distributor's own line amounts (distributor-adjusted hours and purchase prices), the parts reimbursement rate, and the settlement-currency exchange rates used later in the export.

---

## Stage 3 · Dealer Certificate

The distributor groups **Accepted** claims into a **Certificate** — the document that certifies a dealer's claims for a billing period and moves them to **Certified**.

```mermaid
flowchart LR
    subgraph DEALER["One dealer · one period"]
      C1["Accepted claim"]
      C2["Accepted claim"]
      C3["Accepted claim"]
    end
    C1 --> CERT
    C2 --> CERT
    C3 --> CERT
    CERT["<b>Warranty Certificate</b><br/>per-dealer no. + distributor no.<br/>period start → end"]
    CERT --> X["all claims → <b>Certified</b>"]
    classDef acc fill:#dcfce7,color:#14532f,stroke:#16a34a
    classDef cert fill:#e0e7ff,color:#1e3a8a,stroke:#6366f1,stroke-width:2px
    class C1,C2,C3,X acc
    class CERT cert
```

**Grouping rule:** every claim on a certificate must belong to the **same dealer**, and the certificate covers a **period** (defaulting to the previous calendar month). A certificate carries two numbers: a per-dealer running **Certificate Number**, and a **distributor-wide number** that runs per year. Certifying locks the underlying claims; **removing the certificate** reverts them to `Accepted`.

---

## Stage 4 · Dealer Invoice

An **Invoice is a Certificate that has been given an invoice date.** It is the same document — invoicing is the act of stamping an invoice date on an existing certificate, which advances its claims from **Certified** to **Invoiced**.

```mermaid
flowchart LR
    A["<b>Certificate</b><br/>no invoice date<br/>claims: Certified"] -->|"set invoice date"| B["<b>Invoice</b><br/>invoice date stamped<br/>claims: Invoiced"]
    B -.->|"delete invoice"| A
    classDef cert fill:#e0e7ff,color:#1e3a8a,stroke:#6366f1
    classDef inv fill:#dcfce7,color:#14532f,stroke:#16a34a,stroke-width:2px
    class A cert
    class B inv
```

| | **Certificate** | **Invoice** |
|---|------------------|-------------|
| State | No invoice date | Invoice date set |
| Meaning | Claims certified for the period | Claims billed for settlement |
| Claim status it sets | `Accepted` → `Certified` | `Certified` → `Invoiced` |
| Number | Certificate Number + distributor-wide number | Reuses the same number |
| Editable | Yes, until invoiced | No (system-generated) |
| Reversible | Delete → claims back to `Accepted` | Delete → claims back to `Certified` |

Both documents are per-dealer and per-period, and both can be printed. Their totals (labor, parts, sublet, grand total) are summed from the linked claims in local currency.

---

## Stage 5 · Export & Upload to Manufacturer

To bill the manufacturer, the distributor exports a batch of claims to a **CSV in the manufacturer's format** and uploads it to the manufacturer's system. The distributor supplies the **manufacturer invoice number** and the **settlement-currency exchange rates** (parts reimbursement rate, plus labor / parts / sublet rates) at export time.

```mermaid
flowchart LR
    SEL["Selected claims<br/>(Certified / Invoiced)"] --> GEN["<b>Generate Export CSV</b><br/>+ Manufacturer Invoice"]
    GEN --> CSV[("warranty-claims-<br/>{timestamp}.csv")]
    GEN --> ST["each claim → <b>Exported</b><br/>+ invoice no. stamped"]
    CSV --> CR["Uploaded to the<br/><b>Manufacturer's system</b>"]
    classDef sel fill:#e0e7ff,color:#1e3a8a,stroke:#6366f1
    classDef gen fill:#ede9fe,color:#4c1d95,stroke:#8b5cf6,stroke-width:2px
    classDef out fill:#f1f5f9,color:#0f172a,stroke:#cbd5e1
    classDef ok fill:#dcfce7,color:#14532f,stroke:#16a34a
    class SEL sel
    class GEN gen
    class CSV,CR out
    class ST ok
```

- The export produces a wide, fixed-layout **flat CSV** in the manufacturer's exact column order. Because a single claim can have many lines, it is **paginated** across CSV rows (a fixed number of labor, sublet, and part slots per row), with the header fields repeated on each page.
- A few values are translated for the manufacturer: the brand becomes a numeric code, campaign claims are emitted under the vehicle-warranty type, pre-delivery dates emit a sentinel, and amounts are switched to their settlement-currency equivalents.
- Alongside the CSV, a **manufacturer invoice** (PDF, in the settlement currency, grouped by invoice number) is produced for the same batch.
- Each exported claim's manufacturer status becomes **Exported**. Claims already marked **Paid** cannot be re-exported.

---

## Stage 6 · Reconciliation

After processing the batch, the manufacturer returns a **Payment Advice** — a settlement file stating what it paid (or rejected) per claim. The distributor uploads it as a **Manufacturer Settlement Sheet**, and the system matches each row back to its claim.

```mermaid
flowchart TB
    PA[("Payment Advice<br/>CSV from manufacturer")] --> IMP["<b>Manufacturer Settlement Sheet</b><br/>upload + exchange rate"]
    IMP --> MATCH{"match on<br/>Claim No. + Invoice No."}
    MATCH -->|"amount settled"| PAID["<b>Paid</b><br/>settled labor / parts / sublet<br/>stored, converted to local currency"]
    MATCH -->|"nothing settled"| REJ["<b>Rejected</b><br/>reason code + comment stored"]
    MATCH -->|"no matching claim"| UNK["Unrecognized<br/>(warn and abort, or skip)"]
    classDef src fill:#f1f5f9,color:#0f172a,stroke:#cbd5e1
    classDef imp fill:#ede9fe,color:#4c1d95,stroke:#8b5cf6,stroke-width:2px
    classDef dec fill:#e0e7ff,color:#1e3a8a,stroke:#6366f1
    classDef ok fill:#dcfce7,color:#14532f,stroke:#16a34a
    classDef bad fill:#fee2e2,color:#7f1d1d,stroke:#ef4444
    class PA src
    class IMP imp
    class MATCH dec
    class PAID ok
    class REJ,UNK bad
```

- Only rows the manufacturer marks **Processed** are acted on. Each is matched to a claim on the composite key **Claim Number + Invoice Number**.
- A non-zero settled amount marks the claim **Paid**: the settled labor / parts / sublet amounts (in the settlement currency) are stored and converted to local currency using the sheet's exchange rate. A **partial payment** is simply a lower settled amount on a `Paid` claim — there is no separate status for it.
- A zero settled amount marks the claim **Rejected**, recording the manufacturer's reason code and comment.
- **Unrecognized** claim numbers either abort the whole import with a detailed warning, or are skipped — the operator chooses per upload. A guard also blocks silently overwriting an already-reconciled claim with different amounts.

!!! tip "Reconciliation is reversible"
    Deleting a settlement sheet un-reconciles its claims: the settled amounts are cleared and the claims revert to **Exported**, ready to be reconciled again from a corrected Payment Advice.

---

## Status Reference

### Claim Status

| Value | Display | Meaning |
|------:|---------|---------|
| 0 | **Draft** | Created but not yet submitted |
| 1 | **Pending** | Submitted, awaiting distributor review |
| 2 | **Accepted** | Reviewed and accepted; ready to certify |
| 3 | **Error** | Returned for correction (editable again) |
| 4 | **Rejected** | Permanently rejected |
| 5 | **Certified** | Grouped onto a certificate (locked) |
| 6 | **Invoiced** | Certificate invoiced for settlement (locked) |

### Manufacturer Status

| Value | Display | Meaning |
|------:|---------|---------|
| 0 | **N/A** | Not yet exported |
| 1 | **Exported** | Uploaded to the manufacturer |
| 2 | **Downloaded** | Acknowledged by the manufacturer *(reserved, unused)* |
| 3 | **Paid** | Settled by the manufacturer |
| 4 | **Rejected** | Declined by the manufacturer |
| 5 | **On Hold** | Pending further review *(reserved, unused)* |

---

## Related Reference

- Status enums — [Claim Status](../../../generated/Models/Enums/ClaimStatus.md), [Warranty Manufacturer Claim Status](../../../generated/Models/Enums/WarrantyManufacturerClaimStatus.md).
- Claim model — [`WarrantyClaimModel`](../../../generated/Models/Vehicle/WarrantyClaimModel.md), [`WarrantyClaimLaborLineModel`](../../../generated/Models/Vehicle/WarrantyClaimLaborLineModel.md), [`WarrantyDateShiftModel`](../../../generated/Models/Vehicle/WarrantyDateShiftModel.md).
- Delivery-date behavior — [Warranty Dates BDD specs](../../../generated/Features/WarrantyDates.md).
- The companion free / paid service pipeline — [Claimable Items](../claimable-items.md).
