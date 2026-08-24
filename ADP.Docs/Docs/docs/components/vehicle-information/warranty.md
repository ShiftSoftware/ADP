## Standard Warranty

Almost all authorized vehicles come with a standard warranty that is usually activated from the date of sale to the end customer (Invoice Date).

!!! note
	The Warranty Activation depends on the **End Customer's** Invoice Date.
	This should not to be confused with any of the following or other invoice dates of a vehicle:    
		<ul>
			<li>**Manufacturer to Distributor** Invoice Date.</li>
			<li>**Distributor to Dealer** Invoice Date.</li>
			<li>**Dealer To Sub-Dealer or Other Dealer** Invoice Date.</li>
			<li>**Dealer (Or Sub-Dealer) to 3rd Party Partners (Brokers)** Inovice Date.</li>
		</ul>

    Additionally, there are cases where the warranty activation date does not exactly match the invoice date. For example, there may be a delay in delivering the vehcile to the end customer.

## Extended Warranty

Coverage that runs on after the standard warranty ends. It reaches the lookup from two
independent sources, and `VehicleWarrantyDTO.ExtendedWarranties` lists both together:

- **Purchased coverage** — extended warranty packages the customer bought, stored against the
  vehicle. These carry their own start and end dates.
- **Earned coverage** — awarded by a `LookupOptions.ExtendedWarrantyDefinitions` definition when
  the vehicle satisfies the conditions the definition declares. Coverage begins at the end of the
  standard warranty and runs for the configured duration.

!!! note "The flat fields describe purchased coverage only"
    `HasExtendedWarranty`, `ExtendedWarrantyStartDate` and `ExtendedWarrantyEndDate` are older
    output describing the latest-ending **stored** entry, and only while it is still running.
    Earned coverage never reaches them — a host configuring definitions reads
    `ExtendedWarranties`.

### Earning coverage from service history

A definition is gated by the same declarative condition grammar service items use — see
[Eligibility](../services/claimable-items.md#eligibility) for the full contract — so a coverage
awarded for keeping up with scheduled servicing is written the same way. Definitions are opt-in and
fail closed: one with no conditions, an unusable duration or no provider awards nothing.

!!! warning "Match the milestone, not the suffix"
    A condition can compare package codes as **text** (`Exact`, `EndsWith`) or read the scheduled
    service out of them and compare it as a **number** (`Milestone`). Prefer `Milestone` for
    anything about a service the customer has had.

    A network that appends a spec or variant token writes the same 60,000 km service as
    `MODEL 60KS3`, which a `EndsWith` rule looking for `" 60K"` does not match. The vehicle then
    reads as one that never had the service, the coverage is silently withheld, and nothing
    reports a problem — the only signal is a customer asking why. `Milestone` matching reads the
    milestone out of the code and takes a `qualifier` decision about the variant token explicitly,
    so a new spec suffix does not quietly change who qualifies.

    Leave `program` off when any programme's 60,000 km service should count; name programmes only
    when the coverage genuinely belongs to one.

```json
{
  "id": "EW-SERVICE-REWARD",
  "name": "Service Reward Coverage",
  "providerCompanyID": 901,
  "brandIDs": [1],
  "activeFor": 1,
  "activeForDurationType": "Years",
  "eligibilityConditions": [
    {
      "field": "serviceHistory.laborLines.packageCode",
      "operator": "ContainsAll",
      "valueMatch": "Milestone",
      "qualifier": { "selection": "Any" },
      "values": ["60000"],
      "scope": { "selection": "All" }
    }
  ]
}
```

`scope.selection: "All"` asks whether the vehicle has *ever* had the service, which is what a
reward for reaching a milestone is about. `Latest` asks whether it was the most recent visit, so
the coverage would be withdrawn the next time the customer comes in.

Reading a milestone at all depends on this deployment having declared how its codes are written
(`ServiceMilestoneOptions.Conventions`). ADP ships none, so a deployment that declares none can
match no milestone condition anywhere — for coverage or for service items.

### Scoping coverage to a brand

`brandIDs` names the brands a definition is offered to. A vehicle of any other brand is not one
that failed the rule — the rule was never written about it, so its service history is never read
against the definition at all. Omit `brandIDs` and every brand is awarded, which is what a
definition did before it could be scoped; an empty list awards none.

Brand sits on the definition rather than in `eligibilityConditions` for the same reason service
items keep their own `BrandIDs` separate from theirs: it is a fact about the vehicle, not a
predicate over its history. A programme that runs on one brand and not another is therefore **two
definitions**, each carrying its own conditions and its own duration — not one definition whose
conditions have to keep restating which brand they mean. A second brand joining later needs no
change to the first brand's rule.

## Free Service Start Date

The **Free Service Start Date** anchors when free service items become eligible.
It is normally derived in this priority order: service activation record →
sale warranty activation date → sale invoice date → broker invoice date.

### De Facto Service Start Date

Some vehicles reach the dealer through a broker who has not yet inserted an
invoice. In the UI lookup (where `IgnoreBrokerStock=true`) the dealer can still
claim against the vehicle — a customer can't be turned away for a missing
broker invoice. In the bulk lookup (where `IgnoreBrokerStock=false`, used by
the parquet export and other financial projections) that same vehicle would
otherwise produce no service items at all, because there is no anchor date
to evaluate eligibility against.

The **De Facto Service Start Date** closes that gap. It is the earliest
non-deleted [Item Claim](#) date for the vehicle, exposed on
`VehicleWarrantyDTO.DeFactoServiceStartDate`. When the regular fallback chain
would otherwise leave `FreeServiceStartDate` null, this value becomes the
effective start date so downstream items project as if activation had
occurred — the act of claiming is itself evidence the vehicle has been
serviced. The field is always exposed when any non-deleted claim exists, so
consumers can see "this vehicle has been claimed against starting YYYY-MM-DD"
regardless of whether it ended up driving the effective start date.

`FreeServiceItemDateShift` overrides still win — an operator-applied shift
date takes precedence over the de facto fallback.

