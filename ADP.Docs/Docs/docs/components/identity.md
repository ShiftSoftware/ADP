# Identity System
The Identity System is a centralized platform designed to manage and streamline the organizational structure of the distributor and its dealers.  

It offers comprehensive features to manage companies, company branches (including their departments and services), users, permissions, and roles.

Here is a video recording showing the Identity Dashboard:

<iframe width="720" height="460" src="https://www.youtube.com/embed/0opKUD7u_Tk?si=W3WmVXLLDEJ7nvcc" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>

The following are the fundamental sections of the Identity System:

## Countries
The countries where the distributor and its partners operate.

## Regions
The regions where the distributor operates. These could represent countries, states, or provinces.

## Cities
The cities where the distributor, its dealers, and customers are located.

## Brands
The car brands that the distributor is authorized to sell. For example, below are the available brands of Toyota Motor Corporation:

- Toyota
- Lexus
- Hino
- Daihatsu

## Departments
The facilities that a given dealer branch may have. Below are the available departments:

| Name                       | ID                  |
|----------------------------|---------------------|
| Showroom                  | showroom            |
| Parts Shop                | parts-shop          |
| Service Center            | service-center      |
| Body Shop                 | body-shop           |
| Quick Service Center      | quick-service-center|

## Services
The services that a given dealer branch may provide are described below:

| Name                       | ID                  |
|----------------------------|---------------------|
| New Vehicle Sale           | new-vehicle-sale    |
| Installment Sales          | installment-sales   |
| Used Car Sales             | used-car-sales      |
| Test Drive                 | test-drive          |
| Parts Counter Sale         | parts-counter-sale  |
| Auto Repair & Maintenance  | auto-repair-and-maintenance|
| Body & Paint               | body-and-paint      |

!!! note
    Services are associated with departments as follows:

    | Department              | Services                                                                                                   |
    |-------------------------|-----------------------------------------------------------------------------------------------------------|
    | Showroom                | **New Vehicle Sale**, **Installment Sales**, **Used Car Sales**, **Test Drive**                           |
    | Parts Shop              | **Parts Counter Sale**                                                                                    |
    | Service Center          | **Auto Repair & Maintenance**                                                                             |
    | Body Shop               | **Body & Paint**                                                                                          |
    | Quick Service Center    | **Auto Repair & Maintenance**                                                                             |

## Companies
The companies within the distributor network, including the distributor itself, TMC, dealers, suppliers, and other companies.

## Company Branches
Every company must have at least one branch. A branch is a physical facility that may contain one or more departments providing one or more services.

## Access Trees
An Access Tree is a grouping of actions (permissions). One or more Access Trees are assigned to a user.

## Users
A user belongs to a company branch and may have one or more Access Trees.

## Teams
A team is a grouping of users. A team may contain one or more users.
