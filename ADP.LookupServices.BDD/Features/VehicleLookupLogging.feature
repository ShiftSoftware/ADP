Feature: Vehicle lookup logging
	A logged lookup is a KPI entry: distributors count SSC lookups from these rows, so a row must mean
	one lookup a person made. A request that asks for an evaluation trace is a diagnostic re-read of a
	lookup that was already made and logged — the web components send the trace as a follow-up request,
	never as the lookup itself — and it is never logged, even when it carries the log flag. It does carry
	the flag, because hosts put the flag on the tab's query string; the flag alone cannot be trusted.
	Hosts drop the flag on traced requests as well; this is the guarantee for every host.

Background:
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall |

Rule: A lookup with the log flag is logged once

Scenario: An SSC lookup is logged
	When "1G1ZC5E17BF283048" is looked up with the SSC log flag
	Then one SSC lookup log entry is written

Scenario: A customer vehicle lookup is logged
	When "1G1ZC5E17BF283048" is looked up with the customer lookup log flag
	Then one customer vehicle lookup log entry is written

Rule: A traced request is a re-read, never a lookup, and is never logged

Scenario: An SSC trace request carrying the SSC log flag is not logged
	When "1G1ZC5E17BF283048" is looked up with the SSC log flag and an SSC trace
	Then no SSC lookup log entry is written

Scenario: A service-item trace request carrying the SSC log flag is not logged
	When "1G1ZC5E17BF283048" is looked up with the SSC log flag and a service-item trace
	Then no SSC lookup log entry is written

Scenario: A traced request carrying the customer lookup log flag is not logged
	When "1G1ZC5E17BF283048" is looked up with the customer lookup log flag and an SSC trace
	Then no customer vehicle lookup log entry is written
