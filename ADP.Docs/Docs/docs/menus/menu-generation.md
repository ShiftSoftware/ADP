Menu generation has traditionally been a time-consuming and error-prone process, involving the manual compilation of data from various sources into structured Excel files. These files are then imported into the dealer's Dealer Management System (DMS).

The Menu Generation module simplifies this process by identifying and leveraging recurring patterns in maintenance menus. It provides tools for defining reusable data structures, significantly reducing the effort required to create accurate and consistent menus.



## Period Groups

Period Groups are logical groupings of service intervals that share similar service characteristics.  
These groups help streamline replacement item associations and simplify menu creation.

Examples:

- Periods ending with five: 5K, 15K, 25K, etc.
- Twenty-step sequence starting at 10K: 10K, 30K, 50K, etc.

## Periods

Each Period represents a specific maintenance interval and belongs to a Period Group. It is defined with a user-friendly name and description.

Examples:

- 5K: CARRY OUT 5,000 KM SERVICE (Part of Periods ending with five)
- 10K: CARRY OUT 10,000 KM SERVICE (Part of Twenty-step sequence starting at 10K)

## Replacement Items

Replacement Items define the individual components, lubricants, or value-added services that may be included in a menu. 
Each item has a type and can be associated with one or more Period Groups to indicate when it is typically serviced.

Item Types:

- Lubricant: e.g., Rear Traction Motor (E-Trans TE) (4Ltr)
- Component: e.g., Front/Rear Brake Pad
- Value Added: e.g., Brake Cleaner


## Vehicle Model

Vehicle Models are maintained as explicit entities that hold replacement-item defaults and labour settings.

While defining a Vehicle Model, you specify:

- The replacement items applicable to the model.
- The default values, such as specific part numbers where relevant (e.g., HV Battery Filter may only apply to hybrid vehicles).


## Menus

Menus are the final output and the culmination of the data defined in the previous steps.

To create a menu, a user:

- Selects a Vehicle Model.
- Compatible replacement items and their default values are preloaded.
- The user can adjust the items and assign appropriate service periods.
- Once finalized, multiple menus can be exported as Excel files formatted specifically for DMS import, enabling a smooth and accurate upload into dealer systems.

