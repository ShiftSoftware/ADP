It's typical that each company has a dedicated parts database.

## Various Part Databases

- **The Manufacturer:** has a parts database that contains all the genine parts. This dataset is provided by the manufacturer to the distributor and it is updated regularly.
- **The Distributor:** has a parts database that contains all the genuine parts received from the manufacturer plus other 3rd party parts that are provided by other manufacturers or the distributor itself.
- **The Dealers:** have a parts database that contains a subset of the genuine and 3rd party parts received from the distributor plus other parts that are not provided by the distributor.

## Master Data Management **(MDM)**

As a Master Data Management **(MDM)** system, ADP keeps a single **Golden Record (GR)** for each part.   

The GRs are typically created by the distributor by merging part and pricing information from the manufacturer and other 3rd party parts.   

### Dealer Parts Catalog

ADP Regularly pulls dealer parts catalogs and creates Dealer-Specific GRs for parts that are not already available.