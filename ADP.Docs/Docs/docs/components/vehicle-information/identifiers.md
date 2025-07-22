This section provides a top view of a vehicle, it's related information and processes.  

# Vehicle Information   

Every single vehicle is indentified by a unique ID called a **VIN** (Vehicle Indentification Number).   
Alongside the VIN, there are other information that identify a group of vehicles, the imporant identifiers are listed below

## Priamry Identifier
### VIN

#### Overview

VIN (Vehicle Identifiction Number) is a 17 digit string that's used to uniquely identify a vehicle (Example: **MR0AX8CDXP4446478**).  
VINs consists of 3 main components:   

1. **WMI** (3 Characters) which is the World manufacturer identifier. (Example: **MR0**)
2. **VDS** (6 Characters, the 6th character is the checksum digit) which is the Vehicle Descriptor Section. (Example: **AX8CDX**)
3. **VIS** (8 Characters) which is the Vehicle Indicator Section. (Example: **P4446478**)

#### Validation
The 9th digit of a VIN is **Check digit**. Using a simple mathmatical formula, you can validate most VINs more precisely than simply checking if the string is 17 digit.
More information can be found on this [Wiki Article](https://en.wikibooks.org/wiki/Vehicle_Identification_Numbers_(VIN_codes)/Check_digit).

??? note "VIN Validation Code Example"
    === "C#"

        ``` c#
        public static bool ValidateVin(string vin)
        {
            if (string.IsNullOrWhiteSpace(vin) || vin.Length != 17)
                return false;
            
            var TransliterationTable = new Dictionary<char, int>();

            TransliterationTable['0'] = 0;
            TransliterationTable['1'] = 1;
            TransliterationTable['2'] = 2;
            TransliterationTable['3'] = 3;
            TransliterationTable['4'] = 4;
            TransliterationTable['5'] = 5;
            TransliterationTable['6'] = 6;
            TransliterationTable['7'] = 7;
            TransliterationTable['8'] = 8;
            TransliterationTable['9'] = 9;
            TransliterationTable['A'] = 1;
            TransliterationTable['B'] = 2;
            TransliterationTable['C'] = 3;
            TransliterationTable['D'] = 4;
            TransliterationTable['E'] = 5;
            TransliterationTable['F'] = 6;
            TransliterationTable['G'] = 7;
            TransliterationTable['H'] = 8;
            TransliterationTable['J'] = 1;
            TransliterationTable['K'] = 2;
            TransliterationTable['L'] = 3;
            TransliterationTable['M'] = 4;
            TransliterationTable['N'] = 5;
            TransliterationTable['P'] = 7;
            TransliterationTable['R'] = 9;
            TransliterationTable['S'] = 2;
            TransliterationTable['T'] = 3;
            TransliterationTable['U'] = 4;
            TransliterationTable['V'] = 5;
            TransliterationTable['W'] = 6;
            TransliterationTable['X'] = 7;
            TransliterationTable['Y'] = 8;
            TransliterationTable['Z'] = 9;

            var WeightTable = new int[] { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

            var sum = 0;

            var valid = true;

            for (var i = 0; i < vin.Length; i++)
            {
                var character = vin[i];

                if (!TransliterationTable.Keys.Contains(character))
                {
                    valid = false;
                    break;
                }

                var value = TransliterationTable[character];

                var weight = WeightTable[i];

                var product = value * weight;

                sum = sum + product;
            }

            var reminder = (sum % 11);

            var reminderString = reminder.ToString();

            if (reminder == 10)
                reminderString = "X";

            if (vin.Substring(8, 1) != reminderString)
            {
                valid = false;
            }

            return valid;
        }
        ```

    === "TypeScript"

        ``` ts
        public static validateVin(vin:string): boolean {
            var TransliterationTable = {
                '0': 0,
                '1': 1,
                '2': 2,
                '3': 3,
                '4': 4,
                '5': 5,
                '6': 6,
                '7': 7,
                '8': 8,
                '9': 9,

                'A': 1,
                'B': 2,
                'C': 3,
                'D': 4,
                'E': 5,
                'F': 6,
                'G': 7,
                'H': 8,

                'J': 1,
                'K': 2,
                'L': 3,
                'M': 4,
                'N': 5,
                'P': 7,
                'R': 9,

                'S': 2,
                'T': 3,
                'U': 4,
                'V': 5,
                'W': 6,
                'X': 7,
                'Y': 8,
                'Z': 9
            };

            var WeightTable = [8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2];

            var sum = 0;

            var valid = true;

            for (var i = 0; i < vin.length; i++) {

                var char = vin[i].toUpperCase();

                var value = TransliterationTable[char];

                if (value === undefined) {
                    valid = false;
                    break;
                }

                var weight = WeightTable[i];

                var product = value * weight;

                sum = sum + product;
            }

            var reminder = (sum % 11).toString();

            if (reminder === '10')
                reminder = 'X';

            if (vin[8] != reminder) {
                valid = false;
            }

            return valid;
        }
        ```

Note: You can use [https://www.randomvin.com](https://www.randomvin.com) to generate random valid VINs for test purposes.

## VIN Decoder
??? note "VIN Decoder Package"
    The following package is used to detect, validate and decode VINs.
    [https://github.com/ShiftSoftware/ADP.VINDecode](https://github.com/ShiftSoftware/ADP.VINDecode)

    it provides SDKs for the following platforms/languages

    - dotnet (C#)
    - TypeScript
    - Flutter (Not Implemented yet)
    - Android (Not Implemented yet)
    - iOS (Not Implemented yet)

## Non Primary Vehicle Information

### Variant
A Variant is used to identify a group of vehicles (Example: **11371HB202301**).  
Variants consist of 4 main components.

Starting from the end of the code

1. **01**: The last two digits: Always 01 and it's ignored.
1. **Year Model**: The last 6 digits, excluding the constant **01** (Example: **2023**).
2. **SFX**: The last 8 digits, excluding Year Model and the Constant (Example: **HB**).
3. **Model Code**: What remains after excluding SFX, Year Model, and the constant. (Example: **11371**).

### Katashiki
A Katashiki is also used to identify a group of vehicles. But it's broader than variant (For example it does not change based on Year Model). (Example: **TGN121L-DTTHKV**).  

Katashiki Codes consist of two components

1. **Service Model Code:** The first component after seperating by a hyphen, and removing the **L** (Example: **TGN121**). 							
2. The Second component is not used individually

!!! note
    * The **"L"** character in the first portion of Katashiki may not always be the last character. We remove it regardless of it's position. (Example: For **F800LE-GQMFG**, the Service Model Code is: **F800E**).
	* Some Katashiki codes don't include an **L** character in the first portion. (Example: For **SH2PEUA-DSW**, the Service Model Code is: **SH2PEUA**)

### Color
Every vehicle has a Color Code that identifies the Exterior Color of a car (Example: **040**).


### Trim
Every vehicle has a Trim Code that identifies the Interios Color of a car (Example: **42**).   

### Brand (Franchise)
An Automotive manufacturer company may produce vehicles under multiple brands. For example, as of 2022, the **Toyota Motor Corporation** produces vehicles under four brands: **Daihatsu**, **Hino**, **Lexus** and the namesake **Toyota**.  