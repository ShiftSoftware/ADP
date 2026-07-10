namespace ShiftSoftware.ADP.ClaimableItems.Shared;

// Standard ISO-3779 VIN check-digit validation. Module-local copy of the generic helper (the original host application
// keeps its own Services.Shared.VINValidation, used by other host DTOs). Pure/self-contained.
public static class VINValidation
{
    public static bool IsValidVIN(string vin)
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
}
