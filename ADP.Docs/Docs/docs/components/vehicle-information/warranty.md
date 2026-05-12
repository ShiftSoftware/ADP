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
Customers have the option to extend their warranty by buying extended warranty packages.

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

