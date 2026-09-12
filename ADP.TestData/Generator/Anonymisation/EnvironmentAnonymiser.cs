using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ADP.TestData.Generator.Anonymisation;

/// <summary>
/// Turns a raw environment file (a <see cref="GeneratorEnvironment"/> in JSON, carrying a real estate's
/// identifiers) into a public one in the identical schema, with every identifier family re-keyed
/// consistently and every name, place and free text made fictional — so the evaluators reach the same
/// verdicts over data that names no one.
/// <para>
/// The file is edited as a JSON DOM, never round-tripped through the model classes: only string values
/// change (and the dictionary keys that are identifiers), so dates keep their exact form, numbers their
/// exact text, enums whatever form they were stored in, nulls stay omitted, and a field this code does not
/// know about comes through untouched apart from the free-text vocabulary.
/// </para>
/// <para>
/// Two walks over the same schema: the first collects every real value per family, the families are then
/// assigned (keyed, unique within a family, never colliding with a real value of the family), and the second
/// walk writes the synthetic values. Composed ids (<c>&lt;VIN&gt;-&lt;campaign&gt;</c>, a line id carrying
/// the invoice number) are rebuilt from the re-keyed parts of their row.
/// </para>
/// </summary>
public sealed class EnvironmentAnonymiser
{
    private readonly Keyed keyed;
    private readonly string environmentName;
    private readonly Dictionary<string, Family> families = new(StringComparer.Ordinal);
    private readonly Family vin, hash, part, campaign, campaignLetters, labor, document, account, broker, sfx, colourCode,
        exteriorColour, interiorColour, distributorName, dealerName, branch, organisation, country, region, locality,
        words, accessory, location, origin;
    private readonly TextRewriter text;
    private readonly Dictionary<string, string> pureMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> vocabularyMap = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> defaultedFields = new(StringComparer.Ordinal);
    private readonly HashSet<string> knownHashes = new(StringComparer.Ordinal);
    private readonly List<string> deferredLocations = new();
    private readonly List<string> fullVariants = new();
    private readonly Stack<RowScope> rows = new();

    private bool rewriting;
    private bool partsAreTPrefixed;
    private readonly List<string> programmeNames = new();
    private long? distributorCompanyId;
    private HashSet<long> intermediaryCompanyIds = new();
    private string? mintedVin;
    private int mintedPanelImages;

    /// <summary>
    /// When set, a paint-thickness panel that stores no images gets two minted image keys in the shape the
    /// stored keys have (<c>Uploads/paintThickness/&lt;VIN&gt;#&lt;date&gt;/[Side_][Position_]Type_N.jpg</c>),
    /// so the generator resolves them to the panel's bundled drawing (plan view and close-up). A demo choice:
    /// a host whose inspections carry no photos would otherwise show a certificate without pictures.
    /// </summary>
    public bool MintPanelImages { get; init; }

    public EnvironmentAnonymiser(Keyed keyed, string environmentName, Vocabulary vocabulary)
    {
        this.keyed = keyed;
        this.environmentName = environmentName;

        Family Derived(string name, bool unique = true, Func<string, string>? normalise = null, CharSet alphabet = CharSet.Default) =>
            Register(new Family(name, (v, a) => keyed.Substitute(name, v, Salt(a), alphabet), normalise, unique));

        Family Pool(string name, string[] pool, bool unique = true) =>
            Register(new Family(name, (v, a) => pool[keyed.Pick(pool.Length, name, v, a.ToString())], v => v.Trim().ToUpperInvariant(), unique));

        vin = Register(new Family("vin", (v, a) => Vin.Derive(keyed, v, a), v => v.ToUpperInvariant()));
        hash = Derived("hash");
        part = Derived("part");
        campaignLetters = Derived("campaign-letters");
        campaign = Register(new Family("campaign", DeriveCampaign));
        labor = Derived("labor");
        document = Derived("document");
        account = Derived("account");
        broker = Register(new Family("broker", (v, a) => keyed.Digits("broker", v + Salt(a), v.Length, nonZeroLead: true)));
        sfx = Derived("sfx");
        colourCode = Derived("colour-code");
        exteriorColour = Pool("colour-exterior", FictionalNames.ExteriorColours);
        interiorColour = Pool("colour-interior", FictionalNames.InteriorColours);
        distributorName = Pool("distributor", FictionalNames.Distributors);
        dealerName = Pool("company", FictionalNames.Dealers);
        branch = Pool("branch", FictionalNames.Places);
        organisation = Pool("organisation", FictionalNames.Organisations);
        country = Pool("country", FictionalNames.Countries);
        region = Pool("region", FictionalNames.Regions);
        locality = Pool("locality", FictionalNames.Places);
        words = Register(new Family("word", (v, a) => FictionalNames.ModelWords[keyed.Pick(FictionalNames.ModelWords.Length, "word", v, a.ToString())], v => v.ToLowerInvariant()));
        accessory = Pool("accessory", FictionalNames.Accessories, unique: false);
        location = Derived("location");
        origin = Derived("origin");

        text = new TextRewriter(keyed, vocabulary, words, name => families.GetValueOrDefault(name));

        foreach (var term in vocabulary.Terms)
            if (term.Mode != VocabularyMode.Regex && !term.Replace.StartsWith('@'))
                vocabularyMap[term.Match] = term.Replace;
    }

    private static string Salt(int attempt) => attempt == 0 ? string.Empty : "#" + attempt;

    private Family Register(Family family)
    {
        families[family.Name] = family;
        return family;
    }

    // --- entry point -------------------------------------------------------------------------------------

    public AnonymisationResult Anonymise(string rawPath, string outputPath, string keyMapPath)
    {
        var root = JsonNode.Parse(File.ReadAllText(rawPath)) as JsonObject
            ?? throw new InvalidOperationException($"'{rawPath}' is not a JSON object.");

        ReadOptions(root);

        rewriting = false;
        Walk(root);
        ResolveDeferredLocations();

        Assign();

        rewriting = true;
        Walk(root);
        AddMintedVehicle(root);

        var writerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            NewLine = "\n",
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(outputPath, root.ToJsonString(writerOptions) + "\n", new UTF8Encoding(false));

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(keyMapPath))!);
        File.WriteAllText(keyMapPath, KeyMapJson(), new UTF8Encoding(false));

        return new AnonymisationResult(
            families.ToDictionary(f => f.Key, f => f.Value.Count),
            pureMap.Count,
            defaultedFields.OrderBy(f => f.Key, StringComparer.Ordinal).ToList(),
            mintedVin!,
            mintedPanelImages);
    }

    private void ReadOptions(JsonObject root)
    {
        var options = root["LookupOptions"] as JsonObject;
        partsAreTPrefixed = string.Equals(Str(options, "PartNumberStorageKeyMode"), nameof(PartNumberStorageKeyMode.TPrefixedDashStripped), StringComparison.Ordinal);
        distributorCompanyId = Long(options?["DistributorCompanyID"]);

        // The programme names the milestone conventions read at the start of a package code: a token that
        // starts with one is re-keyed as a model code whatever else it looks like (a labor code, a part).
        foreach (var convention in Objects(options, "ServiceMilestoneConventions"))
            foreach (Match m in Regex.Matches(Str(convention, "Pattern") ?? string.Empty, @"\(\?<program>([^()]*)\)"))
                programmeNames.AddRange(m.Groups[1].Value.Split('|').Where(n => n.Length > 0 && n.All(char.IsLetterOrDigit)));
        programmeNames.Sort((a, b) => b.Length.CompareTo(a.Length));
        intermediaryCompanyIds = (options?["IntermediaryCompanyIDs"] as JsonArray)?.Select(Long).OfType<long>().ToHashSet() ?? new HashSet<long>();
    }

    private void Assign()
    {
        // Letters of a campaign code are a sub-family of their own, assigned before the codes that embed them.
        campaignLetters.Assign();

        foreach (var family in families.Values)
            if (family != campaignLetters)
                family.Assign();

        // Free text then knows every assigned name, and every campaign code cited verbatim.
        text.AddNames(distributorName.Map.Concat(dealerName.Map).Concat(branch.Map).Concat(organisation.Map)
            .Concat(country.Map).Concat(region.Map).Concat(locality.Map));
        text.AddExactFamily(campaign);
        text.AddExactFamily(labor, v => v.Any(char.IsDigit) && v.Length >= 4);
        text.AddExactFamily(document, v => v.Any(char.IsDigit) && v.Length >= 5);
        text.AddExactFamily(part, v => v.Length >= 8);
    }

    private void ResolveDeferredLocations()
    {
        foreach (var value in deferredLocations)
            ClassifyLocation(value, collect: true);
    }

    private string KeyMapJson()
    {
        var map = new JsonObject
        {
            ["environment"] = environmentName,
            ["generatedAt"] = DateTimeOffset.UtcNow.ToString("O"),
            ["seedFingerprint"] = keyed.Fingerprint,
            ["minted"] = new JsonObject { ["vin"] = mintedVin },
            ["families"] = new JsonObject(families.Select(f => new KeyValuePair<string, JsonNode?>(
                f.Key, new JsonObject(f.Value.Map.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new KeyValuePair<string, JsonNode?>(p.Key, p.Value)))))),
            ["derived"] = new JsonObject(pureMap.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new KeyValuePair<string, JsonNode?>(p.Key, p.Value))),
            ["vocabulary"] = new JsonObject(vocabularyMap.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new KeyValuePair<string, JsonNode?>(p.Key, p.Value))),
        };

        return map.ToJsonString(new JsonSerializerOptions { WriteIndented = true, NewLine = "\n", Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n";
    }

    // --- the walk ----------------------------------------------------------------------------------------

    private void Walk(JsonObject root)
    {
        using (Row(root, "root"))
        {
            LookupOptions(Obj(root, "LookupOptions"));
            RequestOptions(Obj(root, "RequestOptions"));

            foreach (var company in Objects(root, "Companies"))
                using (Row(company, "Companies[]"))
                {
                    var id = Long(company["CompanyId"]);
                    var isSupplyChain = id == distributorCompanyId || (id is { } i && intermediaryCompanyIds.Contains(i));
                    ApplyName(company, "CompanyName", isSupplyChain ? distributorName : dealerName);

                    foreach (var b in Objects(company, "Branches"))
                        using (Row(b, "Companies[].Branches[]"))
                            ApplyName(b, "BranchName", branch);
                }

            foreach (var c in Objects(root, "Countries"))
                using (Row(c, "Countries[]"))
                    ApplyName(c, "CountryName", country);

            foreach (var r in Objects(root, "Regions"))
                using (Row(r, "Regions[]"))
                    ApplyRegionName(r, "RegionName");

            RekeyDictionary(root, "Vehicles", vin, Vehicle);
            foreach (var v in Objects(root, "BrokerInitialVehicles")) BrokerInitialVehicle(v);
            foreach (var v in Objects(root, "BrokerInvoices")) BrokerInvoice(v);
            RekeyDictionaryOfArrays(root, "BrokerStocks", vin, stocks => { foreach (var s in Objects(stocks)) BrokerStock(s); });
            foreach (var b in Objects(root, "Brokers")) Broker(b);
            foreach (var m in Objects(root, "VehicleModels")) VehicleModel(m);
            foreach (var c in Objects(root, "ExteriorColors")) Colour(c, exteriorColour);
            foreach (var c in Objects(root, "InteriorColors")) Colour(c, interiorColour);
            foreach (var c in Objects(root, "Customers")) Customer(c);
            foreach (var s in Objects(root, "ServiceItems")) ServiceItem(s, "ServiceItems[]");
            RekeyDictionary(root, "Parts", part, Part, partKey: true);

            Defaults(root, "root");
        }
    }

    private void LookupOptions(JsonObject? options)
    {
        if (options is null)
            return;

        using var _ = Row(options, "LookupOptions");

        foreach (var convention in Objects(options, "ServiceMilestoneConventions"))
            using (Row(convention, "LookupOptions.ServiceMilestoneConventions[]"))
            {
                Touch("Name");
                ApplyWith(convention, "Pattern", MilestonePattern);
                Defaults(convention, "LookupOptions.ServiceMilestoneConventions[]");
            }

        foreach (var warning in Objects(options, "StandardItemClaimWarnings"))
            Warning(warning);
        Warning(Obj(options, "SkippedItemsClaimWarning"));
        Warning(Obj(options, "UnInvoicedBrokerClaimWarning"));

        foreach (var group in Arrays(options, "SSCInterchangeableLaborCodeGroups"))
            ApplyArray(group, labor);

        if (Obj(options, "DirectEndCustomerSaleAccountNumbersByCompany") is { } direct)
            foreach (var property in direct.ToList())
                if (property.Value is JsonArray numbers)
                    ApplyArrayWith(numbers, AccountNumber);

        if (options["SSCPartStockScope"] is JsonArray scope)
            ApplyArrayWith(scope, StockLocation);

        foreach (var definition in Objects(options, "ExtendedWarrantyDefinitions"))
            using (Row(definition, "LookupOptions.ExtendedWarrantyDefinitions[]"))
            {
                Touch("ID");   // a configuration key, referenced by nothing an estate names
                Text(definition, "Name");
                Conditions(definition);
                Defaults(definition, "LookupOptions.ExtendedWarrantyDefinitions[]");
            }

        Text(options, "ExtendedWarrantyName");
        Text(options, "VehicleInspectionPreClaimVoucherPrintingURL");
        Text(options, "ServiceActivationPreClaimVoucherPrintingURL");

        foreach (var o in Objects(options, "FreeServiceItemValidityOverrides"))
            using (Row(o, "LookupOptions.FreeServiceItemValidityOverrides[]"))
            {
                Apply(o, "VIN", vin);
                Hashes(o);
                Text(o, "Reason");
                RebuildId(o, "id");
                Defaults(o, "LookupOptions.FreeServiceItemValidityOverrides[]");
            }

        Defaults(options, "LookupOptions");
    }

    private void Warning(JsonObject? warning)
    {
        if (warning is null)
            return;

        using var _ = Row(warning, "LookupOptions.*Warning");
        Text(warning, "BodyContent");
        Text(warning, "ConfirmationText");

        // A picture of the required document: a distributor's storage URL becomes the bundled drawing that
        // matches the warning's subject (a QR-coded invoice, or a signed claim form).
        var url = Str(warning, "ImageUrl");
        if (url is not null && !url.StartsWith(DemoAssets.CdnBaseUrl, StringComparison.Ordinal))
        {
            var body = Str(warning, "BodyContent") ?? string.Empty;
            var drawing = body.Contains("QR", StringComparison.OrdinalIgnoreCase) ? "service-invoice-qr.svg" : "signed-claim-document.svg";
            Set(warning, "ImageUrl", DemoAssets.CdnBaseUrl + "documents/" + drawing);
        }
        Touch("ImageUrl");

        Defaults(warning, "LookupOptions.*Warning");
    }

    private void RequestOptions(JsonObject? options)
    {
        if (options is null)
            return;

        using var _ = Row(options, "RequestOptions");
        RekeyDictionary(options, "DistributorStockLookupQuantityByPart", part, _ => { }, partKey: true);
        Defaults(options, "RequestOptions");
    }

    private void Vehicle(JsonObject aggregate)
    {
        using var _ = Row(aggregate, "Vehicles.*");
        Apply(aggregate, "VIN", vin);

        foreach (var e in Objects(aggregate, "VehicleEntries")) VehicleEntry(e);
        foreach (var a in Objects(aggregate, "VehicleServiceActivations")) ServiceActivation(a);
        foreach (var i in Objects(aggregate, "VehicleInspections")) Inspection(i);
        foreach (var c in Objects(aggregate, "CampaignVinEntries")) CampaignVinEntry(c);
        foreach (var o in Objects(aggregate, "InitialOfficialVINs")) InitialOfficialVin(o);
        foreach (var l in Objects(aggregate, "LaborLines")) LaborLine(l);
        foreach (var p in Objects(aggregate, "PartLines")) PartLine(p);
        foreach (var s in Objects(aggregate, "SSCAffectedVINs")) SscRecord(s);
        foreach (var w in Objects(aggregate, "WarrantyClaims")) WarrantyClaim(w);
        foreach (var v in Objects(aggregate, "BrokerInitialVehicles")) BrokerInitialVehicle(v);
        foreach (var v in Objects(aggregate, "BrokerInvoices")) BrokerInvoice(v);
        foreach (var p in Objects(aggregate, "PaidServiceInvoices")) PaidServiceInvoice(p);
        foreach (var c in Objects(aggregate, "ItemClaims")) ItemClaim(c);
        foreach (var x in Objects(aggregate, "FreeServiceItemExcludedVINs")) VinRow(x, "Vehicles.*.FreeServiceItemExcludedVINs[]");
        foreach (var x in Objects(aggregate, "FreeServiceItemDateShifts")) VinRow(x, "Vehicles.*.FreeServiceItemDateShifts[]");
        foreach (var x in Objects(aggregate, "FreeServiceItemValidityOverrides")) VinRow(x, "Vehicles.*.FreeServiceItemValidityOverrides[]", "Reason");
        foreach (var x in Objects(aggregate, "WarrantyDateShifts")) VinRow(x, "Vehicles.*.WarrantyDateShifts[]");
        foreach (var p in Objects(aggregate, "PaintThicknessInspections")) PaintInspection(p);
        foreach (var a in Objects(aggregate, "Accessories")) Accessory(a);
        foreach (var e in Objects(aggregate, "ExtendedWarrantyEntries")) VinRow(e, "Vehicles.*.ExtendedWarrantyEntries[]");

        Defaults(aggregate, "Vehicles.*");
    }

    private void VehicleEntry(JsonObject e)
    {
        using var _ = Row(e, "Vehicles.*.VehicleEntries[]");
        Apply(e, "VIN", vin);
        Hashes(e);
        ApplyWith(e, "ExteriorColorCode", ColourCodeValue);
        ApplyWith(e, "InteriorColorCode", ColourCodeValue);
        ApplyWith(e, "ModelCode", ModelCodeValue);
        ApplyWith(e, "ModelDescription", ModelText);
        ApplyWith(e, "Katashiki", Model);
        ApplyWith(e, "VariantCode", Variant);
        Apply(e, "InvoiceNumber", document);
        Apply(e, "OrderDocumentNumber", document);
        ApplyWith(e, "AccountNumber", AccountNumber);
        ApplyWith(e, "CustomerAccountNumber", AccountNumber);
        ApplyWith(e, "CustomerID", AccountNumber);
        ApplyWith(e, "Location", LocationValue);

        if (Obj(e, "Intermediary") is { } intermediary)
            using (Row(intermediary, "Vehicles.*.VehicleEntries[].Intermediary"))
            {
                Hashes(intermediary);
                Apply(intermediary, "InvoiceNumber", document);
                Defaults(intermediary, "Vehicles.*.VehicleEntries[].Intermediary");
            }

        if (Obj(e, "Distributor") is { } distributor)
            using (Row(distributor, "Vehicles.*.VehicleEntries[].Distributor"))
            {
                Apply(distributor, "InvoiceNumber", document);
                Defaults(distributor, "Vehicles.*.VehicleEntries[].Distributor");
            }

        RebuildId(e, "LineID");
        RebuildId(e, "id");
        Defaults(e, "Vehicles.*.VehicleEntries[]");
    }

    private void ServiceActivation(JsonObject a)
    {
        using var _ = Row(a, "Vehicles.*.VehicleServiceActivations[]");
        Apply(a, "VIN", vin);
        Hashes(a);
        CustomerFields(a);
        RebuildId(a, "id");
        Defaults(a, "Vehicles.*.VehicleServiceActivations[]");
    }

    private void Inspection(JsonObject i)
    {
        using var _ = Row(i, "Vehicles.*.VehicleInspections[]");
        Apply(i, "VIN", vin);
        Hashes(i);
        ApplyWith(i, "Model", ModelText);
        ApplyWith(i, "ModelCode", ModelCodeValue);
        Apply(i, "JobNumber", document);
        ApplyWith(i, "TechnicianName", v => Person(v, NameKind.Full));
        ApplyWith(i, "QualityControlName", v => Person(v, NameKind.Full));
        Text(i, "FrontPhoto");
        Text(i, "RearPhoto");
        CustomerFields(i);
        RebuildId(i, "id");
        Defaults(i, "Vehicles.*.VehicleInspections[]");
    }

    private void CustomerFields(JsonObject o)
    {
        ApplyWith(o, "OrganizationName", v => Organisation(v));
        ApplyWith(o, "CustomerFirstName", v => Person(v, NameKind.First));
        ApplyWith(o, "CustomerMiddleName", v => Person(v, NameKind.Middle));
        ApplyWith(o, "CustomerLastName", v => Person(v, NameKind.Last));
        ApplyWith(o, "CustomerPhone", Phone);
        ApplyWith(o, "CustomerEmail", Email);
    }

    private void CampaignVinEntry(JsonObject c)
    {
        using var _ = Row(c, "Vehicles.*.CampaignVinEntries[]");
        Apply(c, "VIN", vin);
        Hashes(c);
        Text(c, "CampaignUniqueReference");
        RebuildId(c, "id");
        Defaults(c, "Vehicles.*.CampaignVinEntries[]");
    }

    private void InitialOfficialVin(JsonObject o)
    {
        using var _ = Row(o, "Vehicles.*.InitialOfficialVINs[]");
        Apply(o, "VIN", vin);
        Hashes(o);
        ApplyWith(o, "Model", ModelText);
        RebuildId(o, "id");
        Defaults(o, "Vehicles.*.InitialOfficialVINs[]");
    }

    private void LaborLine(JsonObject l)
    {
        using var _ = Row(l, "Vehicles.*.LaborLines[]");
        OrderLine(l);
        Apply(l, "LaborCode", labor);
        Text(l, "ServiceDescription");
        Text(l, "JobDescription");
        RebuildId(l, "LineID");
        RebuildId(l, "id");
        Defaults(l, "Vehicles.*.LaborLines[]");
    }

    private void PartLine(JsonObject p)
    {
        using var _ = Row(p, "Vehicles.*.PartLines[]");
        OrderLine(p);
        ApplyWith(p, "PartNumber", PartNumber);
        ApplyWith(p, "Location", LocationValue);
        RebuildId(p, "LineID");
        RebuildId(p, "id");
        Defaults(p, "Vehicles.*.PartLines[]");
    }

    private void OrderLine(JsonObject l)
    {
        Apply(l, "VIN", vin);
        Hashes(l);
        Apply(l, "InvoiceNumber", document);
        Apply(l, "ParentInvoiceNumber", document);
        Apply(l, "OrderDocumentNumber", document);
        ApplyWith(l, "PackageCode", PackageCode);
        ApplyWith(l, "AccountNumber", AccountNumber);
        ApplyWith(l, "CustomerAccountNumber", AccountNumber);
        ApplyWith(l, "CustomerID", AccountNumber);
        ApplyWith(l, "GoldenCustomerID", AccountNumber);
    }

    private void SscRecord(JsonObject s)
    {
        using var _ = Row(s, "Vehicles.*.SSCAffectedVINs[]");
        Apply(s, "VIN", vin);
        Hashes(s);
        ApplyWith(s, "CampaignCode", CampaignCode);
        Text(s, "Description");

        foreach (var labour in Objects(s, "Labors"))
            using (Row(labour, "Vehicles.*.SSCAffectedVINs[].Labors[]"))
            {
                Apply(labour, "LaborCode", labor);
                Defaults(labour, "Vehicles.*.SSCAffectedVINs[].Labors[]");
            }

        if (s["PartNumbers"] is JsonArray partNumbers)
            ApplyArrayWith(partNumbers, PartNumber);

        foreach (var n in new[] { "1", "2", "3" })
        {
            Apply(s, "LaborCode" + n, labor);
            ApplyWith(s, "PartNumber" + n, PartNumber);
        }

        RebuildId(s, "id");
        Defaults(s, "Vehicles.*.SSCAffectedVINs[]");
    }

    private void WarrantyClaim(JsonObject w)
    {
        using var _ = Row(w, "Vehicles.*.WarrantyClaims[]");
        Apply(w, "VIN", vin);
        Hashes(w);
        Text(w, "DistributorComment");
        Apply(w, "LaborOperationNumberMain", labor);
        Apply(w, "ClaimNumber", document);
        Apply(w, "InvoiceNumber", document);
        Apply(w, "DealerClaimNumber", document);
        Apply(w, "RepairOrderNumber", document);
        Text(w, "WarrantyType");

        foreach (var line in Objects(w, "LaborLines"))
            using (Row(line, "Vehicles.*.WarrantyClaims[].LaborLines[]"))
            {
                Apply(line, "LaborCode", labor);
                Defaults(line, "Vehicles.*.WarrantyClaims[].LaborLines[]");
            }

        var id = Str(w, "id");
        if (id is not null && Guid.TryParse(id, out var _guid))
        {
            if (rewriting)
                Set(w, "id", text.KeyedGuid(id));
            Touch("id");
        }
        else
            RebuildId(w, "id");

        Defaults(w, "Vehicles.*.WarrantyClaims[]");
    }

    private void BrokerInitialVehicle(JsonObject v)
    {
        using var _ = Row(v, "BrokerInitialVehicles[]");
        Apply(v, "VIN", vin);
        ApplyNumber(v, "BrokerID", broker);
        RebuildId(v, "id");
        Defaults(v, "BrokerInitialVehicles[]");
    }

    private void BrokerInvoice(JsonObject v)
    {
        using var _ = Row(v, "BrokerInvoices[]");
        Apply(v, "VIN", vin);
        ApplyNumber(v, "InvoiceNumber", document);
        ApplyNumber(v, "BrokerCustomerID", broker);
        ApplyNumber(v, "NonOfficialBrokerCustomerID", broker);
        RebuildId(v, "id");
        Defaults(v, "BrokerInvoices[]");
    }

    private void PaidServiceInvoice(JsonObject p)
    {
        using var _ = Row(p, "Vehicles.*.PaidServiceInvoices[]");
        Apply(p, "VIN", vin);
        Hashes(p);
        // The invoice number is stored three times over (id, integration id, number) and must stay one number.
        ApplyNumber(p, "InvoiceNumber", document);
        Apply(p, "IntegrationID", document);
        Apply(p, "id", document);

        foreach (var line in Objects(p, "Lines"))
            using (Row(line, "Vehicles.*.PaidServiceInvoices[].Lines[]"))
            {
                ApplyWith(line, "PackageCode", PackageCode);
                if (Obj(line, "ServiceItem") is { } item)
                    ServiceItem(item, "Vehicles.*.PaidServiceInvoices[].Lines[].ServiceItem");
                Defaults(line, "Vehicles.*.PaidServiceInvoices[].Lines[]");
            }

        Defaults(p, "Vehicles.*.PaidServiceInvoices[]");
    }

    private void ItemClaim(JsonObject c)
    {
        using var _ = Row(c, "Vehicles.*.ItemClaims[]");
        Apply(c, "VIN", vin);
        Hashes(c);
        ApplyWith(c, "PackageCode", PackageCode);
        Apply(c, "JobNumber", document);
        Apply(c, "InvoiceNumber", document);
        ApplyWith(c, "QRCode", Opaque);
        RebuildId(c, "id");
        Defaults(c, "Vehicles.*.ItemClaims[]");
    }

    private void VinRow(JsonObject o, string path, string? textField = null)
    {
        using var _ = Row(o, path);
        Apply(o, "VIN", vin);
        Hashes(o);
        if (textField is not null)
            Text(o, textField);
        RebuildId(o, "id");
        Defaults(o, path);
    }

    private void PaintInspection(JsonObject p)
    {
        using var _ = Row(p, "Vehicles.*.PaintThicknessInspections[]");
        var realVin = Str(p, "VIN");
        Apply(p, "VIN", vin);
        Hashes(p);
        ApplyWith(p, "ModelDescription", ModelText);
        ApplyWith(p, "ColorCode", v => Regex.IsMatch(v.Trim(), "^[A-Z0-9]{1,4}$") ? ColourCodeValue(v) : ColourName(v, exteriorColour));

        foreach (var panel in Objects(p, "Panels"))
            using (Row(panel, "Vehicles.*.PaintThicknessInspections[].Panels[]"))
            {
                // An image key names the VIN in its path; the panel it names is what the drawing follows.
                if (panel["Images"] is JsonArray images)
                    ApplyArrayWith(images, key => rewriting && realVin is not null ? key.Replace(realVin, vin.Get(realVin), StringComparison.OrdinalIgnoreCase) : key);
                if (MintPanelImages && rewriting && realVin is not null && (panel["Images"] is not JsonArray { Count: > 0 }))
                    panel["Images"] = MintedPanelImages(vin.Get(realVin), Str(p, "InspectionDate"), panel);
                Defaults(panel, "Vehicles.*.PaintThicknessInspections[].Panels[]");
            }

        // Inspection ids are numeric text the certificate serial resolver parses; they stay. The source
        // is a label an estate may name its own inspection programme in, so the vocabulary applies.
        Touch("id");
        Text(p, "Source");
        Defaults(p, "Vehicles.*.PaintThicknessInspections[]");
    }

    /// <summary>
    /// Two keys per panel — the odd index is the plan view, the even one the close-up (see
    /// <see cref="DemoAssets.PaintPanelImageUrl"/>) — under the synthetic VIN and the inspection's date, the way
    /// inspection tools name their uploads (<c>Left_Front_Fender_1.jpg</c>, <c>Hood_2.jpg</c>). Deterministic:
    /// a function of the row alone, so regenerated environments stay byte-identical.
    /// </summary>
    private JsonArray MintedPanelImages(string syntheticVin, string? inspectionDate, JsonObject panel)
    {
        var stamp = DateTime.TryParse(inspectionDate, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var date)
            ? date.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture)
            : "undated";
        var stem = string.Join("_", new[] { Str(panel, "PanelSide"), Str(panel, "PanelPosition"), Str(panel, "PanelType") }
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => Regex.Replace(t!, "(?<=[a-z])(?=[A-Z])", "_")));   // TailGate → Tail_Gate
        mintedPanelImages += 2;

        return new JsonArray(
            JsonValue.Create($"Uploads/paintThickness/{syntheticVin}#{stamp}/{stem}_1.jpg"),
            JsonValue.Create($"Uploads/paintThickness/{syntheticVin}#{stamp}/{stem}_2.jpg"));
    }

    private void Accessory(JsonObject a)
    {
        using var _ = Row(a, "Vehicles.*.Accessories[]");
        var realVin = Str(a, "VIN");
        Apply(a, "VIN", vin);
        Hashes(a);
        ApplyWith(a, "PartNumber", PartNumber);
        ApplyNumber(a, "JobNumber", document);
        ApplyNumber(a, "InvoiceNumber", document);

        var description = Str(a, "PartDescription");
        ApplyWith(a, "PartDescription", v => Place(v.Trim(), accessory, adaptCase: true));

        // The stored image key is rebuilt around the fictional description, so the bundled drawing the
        // generator resolves matches what the row says it is (or is the generic carton).
        if (Str(a, "Image") is { } image)
        {
            if (rewriting && realVin is not null && description is not null)
            {
                var fictional = accessory.Get(description.Trim())!;
                Set(a, "Image", $"Uploads/accessories/{vin.Get(realVin)}/{AccessorySlug(fictional)}.jpg");
                pureMap[image] = Str(a, "Image")!;
            }
            Touch("Image");
        }

        RebuildId(a, "id");
        Defaults(a, "Vehicles.*.Accessories[]");
    }

    private static string AccessorySlug(string description)
    {
        var d = description.ToLowerInvariant();
        foreach (var (keyword, slug) in new[] { ("step", "side-steps"), ("running board", "side-steps"), ("rail", "roof-rails"), ("rack", "roof-rack"), ("roof box", "roof-rack"), ("mat", "floor-mats"), ("mud", "mud-guards"), ("tow", "tow-bar") })
            if (d.Contains(keyword, StringComparison.Ordinal))
                return slug;

        return "accessory";
    }

    private void BrokerStock(JsonObject s)
    {
        using var _ = Row(s, "BrokerStocks.*[]");
        Apply(s, "VIN", vin);
        ApplyNumber(s, "BrokerID", broker);

        if (Obj(s, "Broker") is { } b)
            using (Row(b, "BrokerStocks.*[].Broker"))
            {
                ApplyNumber(b, "ID", broker);
                ApplyWith(b, "Name", v => Organisation(v));
                Hashes(b);
                foreach (var n in Objects(b, "AccountNumbers"))
                    using (Row(n, "BrokerStocks.*[].Broker.AccountNumbers[]"))
                    {
                        ApplyWith(n, "AccountNumber", AccountNumber);
                        Defaults(n, "BrokerStocks.*[].Broker.AccountNumbers[]");
                    }
                foreach (var access in Objects(b, "BrandAccesses"))
                    using (Row(access, "BrokerStocks.*[].Broker.BrandAccesses[]"))
                    {
                        ApplyWith(access, "LocationCode", LocationValue);
                        Defaults(access, "BrokerStocks.*[].Broker.BrandAccesses[]");
                    }
                Defaults(b, "BrokerStocks.*[].Broker");
            }

        if (Obj(s, "Vehicle") is { } v)
            using (Row(v, "BrokerStocks.*[].Vehicle"))
            {
                Apply(v, "VIN", vin);
                ApplyWith(v, "Model", ModelText);
                ApplyWith(v, "VariantDescription", ModelText);
                ApplyWith(v, "VariantCode", Variant);
                ApplyWith(v, "Katashiki", Model);
                foreach (var (name, pool) in new[] { ("ExteriorColor", exteriorColour), ("InteriorColor", interiorColour) })
                    if (Obj(v, name) is { } colour)
                        using (Row(colour, "BrokerStocks.*[].Vehicle." + name))
                        {
                            ApplyWith(colour, "Code", ColourCodeValue);
                            ApplyWith(colour, "Description", d => ColourName(d, pool));
                            Defaults(colour, "BrokerStocks.*[].Vehicle." + name);
                        }
                Defaults(v, "BrokerStocks.*[].Vehicle");
            }

        foreach (var t in Objects(s, "Transfers"))
            using (Row(t, "BrokerStocks.*[].Transfers[]"))
            {
                ApplyNumber(t, "SellerBrokerID", broker);
                ApplyNumber(t, "BuyerBrokerID", broker);
                Defaults(t, "BrokerStocks.*[].Transfers[]");
            }

        foreach (var i in Objects(s, "Invoices"))
            using (Row(i, "BrokerStocks.*[].Invoices[]"))
            {
                ApplyNumber(i, "InvoiceNumber", document);
                ApplyWith(i, "CustomerName", v => Person(v, NameKind.Full));
                ApplyWith(i, "CustomerPhone", Phone);
                Apply(i, "CustomerIDNumber", document);
                ApplyWith(i, "NonOfficialBrokerName", v => Organisation(v));
                ApplyWith(i, "NonOfficialBrokerPhone", Phone);
                Defaults(i, "BrokerStocks.*[].Invoices[]");
            }

        foreach (var e in Objects(s, "VehilceEntries"))
            using (Row(e, "BrokerStocks.*[].VehilceEntries[]"))
            {
                ApplyWith(e, "CustomerAccountNumber", AccountNumber);
                ApplyWith(e, "Location", LocationValue);
                ApplyWith(e, "Region", LocationValue);
                Defaults(e, "BrokerStocks.*[].VehilceEntries[]");
            }

        RebuildId(s, "id");
        Defaults(s, "BrokerStocks.*[]");
    }

    private void Broker(JsonObject b)
    {
        using var _ = Row(b, "Brokers[]");
        ApplyNumber(b, "ID", broker);
        ApplyWith(b, "Name", v => Organisation(v));
        Hashes(b);
        if (b["AccountNumbers"] is JsonArray numbers)
            ApplyArrayWith(numbers, AccountNumber);
        RebuildId(b, "id");
        Defaults(b, "Brokers[]");
    }

    private void VehicleModel(JsonObject m)
    {
        using var _ = Row(m, "VehicleModels[]");
        Hashes(m);
        ApplyWith(m, "BasicModelCode", Model);
        Apply(m, "SFX", sfx);
        ApplyWith(m, "ModelCode", ModelCodeValue);
        ApplyWith(m, "ModelDescription", ModelText);
        ApplyWith(m, "VariantCode", Variant);
        ApplyWith(m, "VariantDescription", ModelText);
        ApplyWith(m, "Katashiki", Model);
        RebuildId(m, "id");
        Defaults(m, "VehicleModels[]");
    }

    private void Colour(JsonObject c, Family pool)
    {
        using var _ = Row(c, "Colors[]");
        Hashes(c);
        ApplyWith(c, "Code", ColourCodeValue);
        ApplyWith(c, "Description", d => ColourName(d, pool));
        RebuildId(c, "id");
        Defaults(c, "Colors[]");
    }

    private void Customer(JsonObject c)
    {
        using var _ = Row(c, "Customers[]");
        Hashes(c);
        ApplyWith(c, "CustomerID", AccountNumber);
        ApplyWith(c, "GoldenCustomerID", AccountNumber);
        ApplyWith(c, "FullName", v => Person(v, NameKind.Full));
        if (c["PhoneNumbers"] is JsonArray phones)
            ApplyArrayWith(phones, Phone);
        if (c["Address"] is JsonArray address)
            ApplyArrayWith(address, AddressLine);
        ApplyWith(c, "DateOfBirth", DateOfBirth);
        Apply(c, "IDNumber", document);
        RebuildId(c, "id");
        Defaults(c, "Customers[]");
    }

    private void ServiceItem(JsonObject s, string path)
    {
        using var _ = Row(s, path);
        foreach (var dictionary in new[] { "Name", "Photo", "PrintoutTitle", "PrintoutDescription", "CampaignName" })
            if (Obj(s, dictionary) is { } localized)
            {
                using (Row(localized, path + "." + dictionary))
                    foreach (var language in localized.Select(p => p.Key).ToList())
                        Text(localized, language);
                Touch(dictionary);
            }

        ApplyWith(s, "PackageCode", PackageCode);
        Text(s, "UniqueReference");
        Text(s, "CampaignUniqueReference");

        foreach (var cost in Objects(s, "ModelCosts"))
            using (Row(cost, path + ".ModelCosts[]"))
            {
                ApplyWith(cost, "Variant", Variant);
                ApplyWith(cost, "Katashiki", Model);
                ApplyWith(cost, "PackageCode", PackageCode);
                Defaults(cost, path + ".ModelCosts[]");
            }

        Conditions(s);
        Touch("id");
        Touch("IntegrationID");
        Defaults(s, path);
    }

    private void Conditions(JsonObject owner)
    {
        foreach (var condition in Objects(owner, "EligibilityConditions"))
            using (Row(condition, "*.EligibilityConditions[]"))
            {
                if (condition["Program"] is JsonArray programmes)
                    ApplyArrayWith(programmes, Model);
                if (condition["Values"] is JsonArray values)
                    ApplyArrayWith(values, PackageCode);
                Touch("Field");
                Defaults(condition, "*.EligibilityConditions[]");
            }
    }

    private void Part(JsonObject aggregate)
    {
        using var _ = Row(aggregate, "Parts.*");

        foreach (var c in Objects(aggregate, "CatalogParts"))
            CatalogPart(c, "Parts.*.CatalogParts[]");

        foreach (var s in Objects(aggregate, "StockParts"))
            using (Row(s, "Parts.*.StockParts[]"))
            {
                ApplyWith(s, "PartNumber", PartNumber);
                Hashes(s);
                ApplyWith(s, "Location", StockLocation);
                if (Obj(s, "CatalogPart") is { } catalogue)
                    CatalogPart(catalogue, "Parts.*.StockParts[].CatalogPart");
                RebuildId(s, "id");
                Defaults(s, "Parts.*.StockParts[]");
            }

        foreach (var d in Objects(aggregate, "CompanyDeadStockParts"))
            using (Row(d, "Parts.*.CompanyDeadStockParts[]"))
            {
                ApplyWith(d, "PartNumber", PartNumber);
                Hashes(d);
                ApplyWith(d, "Location", StockLocation);
                RebuildId(d, "id");
                Defaults(d, "Parts.*.CompanyDeadStockParts[]");
            }

        Defaults(aggregate, "Parts.*");
    }

    private void CatalogPart(JsonObject c, string path)
    {
        using var _ = Row(c, path);
        ApplyWith(c, "PartNumber", PartNumber);
        ApplyWith(c, "ID", PartNumber);
        Text(c, "PartName");
        Text(c, "LocalDescription");
        Text(c, "ProductGroupDescription");
        Apply(c, "Origin", origin);
        ApplyWith(c, "Location", v => v.Length == 0 ? v : StockLocation(v));

        foreach (var supersession in Objects(c, "SupersededTo").Concat(Objects(c, "SupersededFrom")))
            using (Row(supersession, path + ".Superseded*[]"))
            {
                ApplyWith(supersession, "PartNumber", PartNumber);
                Defaults(supersession, path + ".Superseded*[]");
            }

        foreach (var countryData in Objects(c, "CountryData"))
            using (Row(countryData, path + ".CountryData[]"))
            {
                Hashes(countryData);
                foreach (var price in Objects(countryData, "RegionPrices"))
                    using (Row(price, path + ".CountryData[].RegionPrices[]"))
                    {
                        Hashes(price);
                        Defaults(price, path + ".CountryData[].RegionPrices[]");
                    }
                Defaults(countryData, path + ".CountryData[]");
            }

        RebuildId(c, "id");
        Defaults(c, path);
    }

    // --- value transforms --------------------------------------------------------------------------------

    private string DeriveCampaign(string code, int attempt)
    {
        // <year><letters>-<serial>: the year stays (chronology is a shape of the data), the letters map as a
        // sub-family of their own (one distributor's codes share them), the serial is re-keyed per code.
        var m = Regex.Match(code, @"^(\d{2})([A-Z]{2,5})-(\d{2,4})$");
        if (!m.Success)
            return keyed.Substitute("campaign", code, Salt(attempt));

        // The letters may first surface in a comment the vocabulary mapped through this family; they are
        // assigned on the spot then, deterministically, like any other late value.
        var prefixLength = m.Groups[1].Length + m.Groups[2].Length + 1;
        return m.Groups[1].Value + campaignLetters.GetOrAssign(m.Groups[2].Value) + "-" + keyed.Substitute("campaign-serial", code, Salt(attempt))[prefixLength..];
    }

    private string CampaignCode(string code)
    {
        var m = Regex.Match(code, @"^(\d{2})([A-Z]{2,5})-(\d{2,4})$");
        if (!rewriting)
        {
            if (m.Success)
                campaignLetters.Collect(m.Groups[2].Value);
            campaign.Collect(code);
            return code;
        }

        return campaign.Get(code)!;
    }

    private string PartNumber(string value)
    {
        var (lead, core, trail) = CaseShape.Trim(value);
        if (core.Length == 0)
            return value;

        var prefixed = partsAreTPrefixed && core.Length > 1 && core[0] == 'T' && char.IsAsciiDigit(core[1]);
        var body = prefixed ? core[1..] : core;
        var canonical = body.Replace("-", string.Empty);

        // A part field holding words rather than a number (a line for "other works") is text, not a key.
        if (canonical.Count(char.IsAsciiLetterOrDigit) < 2 || canonical.Contains(' '))
            return TextValue(value);

        if (!rewriting)
        {
            part.Collect(canonical);
            return value;
        }

        var synthetic = part.Get(canonical)!;
        var result = new StringBuilder(value.Length);
        result.Append(lead);
        if (prefixed)
            result.Append('T');

        var next = 0;
        foreach (var c in body)
            result.Append(c == '-' ? '-' : synthetic[next++]);

        // The key map carries every written form (dashed, prefixed) beside the canonical core, so a
        // literal in a test or a document translates without knowing the convention.
        var written = result.Append(trail).ToString();
        pureMap.TryAdd(value, written);
        return written;
    }

    private string AccountNumber(string value)
    {
        var (lead, core, trail) = CaseShape.Trim(value);

        // Codes are re-keyed; the odd account field that holds a word or a phrase (a programme name, a
        // cash account) is text. A code carries a digit, or is all capitals, or is dashed.
        var isCode = core.Any(char.IsAsciiDigit) || core.Contains('-') || CaseShape.IsAllUpper(core);
        if (core.Length < 2 || core.Any(char.IsWhiteSpace) || !isCode)
            return TextValue(value);

        if (!rewriting)
        {
            account.Collect(core);
            return value;
        }

        return lead + account.Get(core) + trail;
    }

    private string TextValue(string value)
    {
        if (!rewriting)
        {
            text.Collect(value);
            return value;
        }

        return text.Rewrite(value)!;
    }

    private string LocationValue(string value)
    {
        var (lead, core, trail) = CaseShape.Trim(value);
        if (core.Length == 0)
            return value;

        // A word-shaped location is a place; a code is re-keyed as a code.
        if (Regex.IsMatch(core, @"^\p{L}{4,}$") && !CaseShape.IsAllUpper(core))
            return lead + Place(core, locality) + trail;

        if (!rewriting)
        {
            location.Collect(core);
            return value;
        }

        return lead + location.Get(core) + trail;
    }

    /// <summary>
    /// A stock location is a hash id where a deployment keys stock by region (and so must move with the hash
    /// family and the SSC stock scope), a pair of hashes for dead stock, or a plain code elsewhere. Which of
    /// those a value is can only be told once every hash id has been seen, so the collecting pass defers it.
    /// </summary>
    private string StockLocation(string value)
    {
        if (!rewriting)
        {
            deferredLocations.Add(value);
            return value;
        }

        return ClassifyLocation(value, collect: false);
    }

    private string ClassifyLocation(string value, bool collect)
    {
        if (knownHashes.Contains(value))
            return collect ? Collected(hash, value) : hash.Get(value)!;

        var parts = value.Split('-');
        if (parts.Length > 1 && parts.All(knownHashes.Contains))
            return collect ? value : string.Join('-', parts.Select(p => hash.Get(p)));

        if (collect)
        {
            location.Collect(value);
            return value;
        }

        return location.Get(value)!;
    }

    private static string Collected(Family family, string value)
    {
        family.Collect(value);
        return value;
    }

    private string ColourCodeValue(string value)
    {
        var (lead, core, trail) = CaseShape.Trim(value);
        if (core.Length == 0 || IsPlaceholder(core))
            return value;

        if (!rewriting)
        {
            colourCode.Collect(core);
            return value;
        }

        return lead + colourCode.Get(core) + trail;
    }

    private string ColourName(string value, Family pool)
    {
        var (lead, core, trail) = CaseShape.Trim(value);
        if (core.Length == 0 || IsPlaceholder(core))
            return value;

        return lead + Place(core, pool, adaptCase: true) + trail;
    }

    private static bool IsPlaceholder(string value) =>
        value.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase)
        || value.Equals("N/A", StringComparison.OrdinalIgnoreCase)
        || value.Equals("NA", StringComparison.OrdinalIgnoreCase)
        || value.Equals("NONE", StringComparison.OrdinalIgnoreCase)
        || value.StartsWith("BACKFILL", StringComparison.OrdinalIgnoreCase);

    /// <summary>The prefix-preserving family: katashiki, model codes, programme names, the tokens of a package code.</summary>
    private string Model(string value)
    {
        var (lead, core, trail) = CaseShape.Trim(value);
        if (core.Length == 0 || IsPlaceholder(core))
            return value;

        if (!rewriting)
            return value;

        var synthetic = keyed.SubstitutePrefixPreserving("model", core);
        pureMap.TryAdd(core, synthetic);
        return lead + synthetic + trail;
    }

    private string ModelCodeValue(string value) =>
        Regex.IsMatch(value.Trim(), @"^[A-Z0-9]{3,8}$") ? Model(value) : ModelText(value);

    private static readonly Regex KatashikiShaped = new(@"(?<![A-Z0-9])[A-Z]{2,4}\d{2,3}[A-Z]?-[A-Z]{3,7}\d?(?![A-Z0-9])", RegexOptions.Compiled);
    private static readonly Regex Token = new(@"[\p{L}\p{N}]+(?:\.\d+)?", RegexOptions.Compiled);

    private static readonly HashSet<string> GenericAutomotiveTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "AT", "MT", "CVT", "AMT", "DCT", "HEV", "PHEV", "EV", "BEV", "HYBRID", "DIESEL", "PETROL", "GASOLINE", "GAS",
        "TURBO", "AWD", "4WD", "2WD", "4X4", "4X2", "LHD", "RHD", "SUV", "MPV", "SEDAN", "SED", "SALOON", "HATCH",
        "HATCHBACK", "COUPE", "WAGON", "VAN", "BUS", "MINIBUS", "TRUCK", "PICKUP", "PICK", "UP", "CAB", "DC", "SC",
        "DOUBLE", "SINGLE", "CABIN", "HIGH", "MID", "LOW", "STD", "STANDARD", "BASE", "DLX", "DELUXE", "LIMITED",
        "PLUS", "SPORT", "SPORTS", "GRADE", "GRAD", "SPEC", "MODEL", "YEAR", "SERIES", "SEAT", "SEATS", "SEATER",
        "DOOR", "DOORS", "CYL", "LEATHER", "LTR", "FABRIC", "SUNROOF", "MOONROOF", "MOON", "ROOF", "WIDE", "WITH",
        "AND", "OR", "TWIN", "URBAN", "TOURISM", "VIP", "AUTO", "AUTOMATIC", "MANUAL", "ELECTRIC", "FULL", "SEMI",
        "LONG", "SHORT", "WHEEL", "WHEELBASE", "WB", "KM", "KMS", "MILES", "SERVICE", "SERVICES", "PACKAGE", "PKG",
        "KIT", "FREE", "PAID", "BLACK", "WHITE", "EDITION", "INTERIOR", "EXTERIOR", "RED", "BLUE", "GREY", "GRAY",
        "GREEN", "SILVER", "FRONT", "REAR", "PM", "YM", "TON", "SPEED", "VALVE", "OVERTRAIL", "NEW", "OLD", "PREMIUM",
        "LUXURY", "EXECUTIVE", "COMFORT", "ACTIVE", "STYLE", "DESIGN", "TECH", "ADVANCED", "ELEGANCE", "ULTIMATE",
        "MINI", "MICRO", "COMPACT", "LARGE", "SMALL", "MEDIUM", "HEAVY", "LIGHT", "DUTY", "COMMERCIAL", "PASSENGER",
        "TAXI", "FLEET", "SPECIAL", "LIMO", "AMBULANCE", "ARMOURED", "ARMORED", "CHASSIS", "CARGO", "CREW", "PANEL",
    };

    /// <summary>
    /// Model descriptions and other model text: a katashiki-shaped run maps as a unit (so it matches the
    /// katashiki fields), a mixed letter-digit token as a code, a word that names a model as a coined word,
    /// generic automotive vocabulary, numbers and single letters stay.
    /// </summary>
    private string ModelText(string value)
    {
        if (value.Trim().Length == 0)
            return value;

        var katashikis = new List<string>();
        var masked = KatashikiShaped.Replace(value, m =>
        {
            katashikis.Add(Model(m.Value));
            return "\u0001" + (katashikis.Count - 1) + "\u0001";
        });

        var result = Token.Replace(masked, m =>
        {
            var t = m.Value;
            if (t.Length == 1 || t.All(c => char.IsDigit(c) || c == '.') || GenericAutomotiveTokens.Contains(t))
                return t;

            if (t.All(char.IsLetter))
            {
                if (t.Length <= 4 && CaseShape.IsAllUpper(t))
                    return Model(t);

                if (!rewriting)
                {
                    words.Collect(t);
                    return t;
                }

                return text.Word(t);
            }

            if (Regex.IsMatch(t, @"^\d{1,2}YM$|^\d+(AT|MT|CVT)$|^\d+(L|T|KM|KG|PS|HP)$"))
                return t;

            return Model(t);
        });

        return Regex.Replace(result, "\u0001(\\d+)\u0001", m => katashikis[int.Parse(m.Groups[1].Value)]);
    }

    private static readonly Regex VariantWithYear = new(@"^(?<model>[A-Z0-9]{3,})(?<sfx>[A-Z0-9]{2})(?<year>(?:19|20)\d{2})(?<tail>\d{2})$", RegexOptions.Compiled);
    private static readonly Regex VariantWithColours = new(@"^(?<katashiki>[A-Z0-9]+-[A-Z0-9]+)-(?<sfx>[A-Z0-9]{1,2})-(?<exterior>[A-Z0-9]{1,3})-(?<interior>[A-Z0-9]{1,2})$", RegexOptions.Compiled);

    /// <summary>
    /// Variant codes keep the structure a consumer decodes: <c>&lt;model&gt;&lt;2-char SFX&gt;&lt;4-digit year&gt;&lt;2&gt;</c>
    /// (the model prefix maps with the model-code family, the SFX with the SFX family, the year stays) or
    /// <c>&lt;katashiki&gt;-&lt;SFX&gt;-&lt;exterior&gt;-&lt;interior&gt;</c> (the colour codes map with the colour family).
    /// A partial variant (a model cost's prefix) maps as the prefix of a full variant it matches, so the
    /// prefix match the evaluator makes still holds.
    /// </summary>
    private string Variant(string value)
    {
        var (lead, core, trail) = CaseShape.Trim(value);
        if (core.Length == 0 || IsPlaceholder(core))
            return value;

        if (VariantWithYear.Match(core) is { Success: true } y)
        {
            if (!rewriting)
            {
                fullVariants.Add(core);
                sfx.Collect(y.Groups["sfx"].Value);
                return value;
            }

            return lead + Model(y.Groups["model"].Value) + sfx.Get(y.Groups["sfx"].Value) + y.Groups["year"].Value + y.Groups["tail"].Value + trail;
        }

        if (VariantWithColours.Match(core) is { Success: true } c)
        {
            if (!rewriting)
            {
                fullVariants.Add(core);
                sfx.Collect(c.Groups["sfx"].Value);
                colourCode.Collect(c.Groups["exterior"].Value);
                colourCode.Collect(c.Groups["interior"].Value);
                return value;
            }

            return lead + Model(c.Groups["katashiki"].Value) + "-" + sfx.Get(c.Groups["sfx"].Value) + "-"
                + colourCode.Get(c.Groups["exterior"].Value) + "-" + colourCode.Get(c.Groups["interior"].Value) + trail;
        }

        if (rewriting)
        {
            var full = fullVariants.FirstOrDefault(f => f.StartsWith(core, StringComparison.Ordinal));
            if (full is not null)
                return lead + Variant(full)[..core.Length] + trail;
        }

        return Model(value);
    }

    private static readonly Regex Milestone = new(@"^\d{1,3}K[A-Z0-9]*$", RegexOptions.Compiled);

    /// <summary>
    /// A package (menu) code keeps what the milestone convention reads — the <c>&lt;milestone&gt;K</c> token,
    /// everything after it (the qualifier, given only the free-text vocabulary) and the numbers — and re-keys the rest: a programme name or model
    /// token through the prefix-preserving family (so the convention's alternation, re-keyed the same way,
    /// still reads it), a part number or labor code through its own family.
    /// </summary>
    private string PackageCode(string value)
    {
        if (value.Trim().Length == 0)
            return value;

        var afterMilestone = false;

        return Regex.Replace(value, @"[A-Za-z0-9.]+", m =>
        {
            var t = m.Value;
            if (afterMilestone)
                return TextValue(t);   // the qualifier is read as is; only the free-text vocabulary applies

            if (Milestone.IsMatch(t))
            {
                afterMilestone = true;
                return t;
            }

            if (t.Length == 1 || t.All(c => char.IsDigit(c) || c == '.') || GenericAutomotiveTokens.Contains(t))
                return t;

            if (programmeNames.Any(n => t.StartsWith(n, StringComparison.Ordinal)))
                return Model(t);

            if (labor.Contains(t))
                return rewriting ? labor.Get(t)! : t;

            var canonical = partsAreTPrefixed && t.Length > 1 && t[0] == 'T' && char.IsAsciiDigit(t[1]) ? t[1..] : t;
            if (part.Contains(canonical))
                return PartNumber(t);

            return Model(t);
        });
    }

    private string MilestonePattern(string pattern)
    {
        if (!rewriting)
            return pattern;

        // The programme group is a plain alternation; each name maps through the prefix-preserving family,
        // which keeps a longer name ahead of the shorter one it starts with.
        var rewritten = Regex.Replace(pattern, @"\(\?<program>([^()]*)\)", m =>
            "(?<program>" + string.Join("|", m.Groups[1].Value.Split('|').Select(p => p.All(c => char.IsLetterOrDigit(c) || c == '-') ? Model(p) : p)) + ")");

        if (rewritten == pattern && pattern.Contains("(?<program>", StringComparison.Ordinal))
            Console.WriteLine("  warning: the milestone convention's programme group is not a plain alternation and was left as is; check it against the re-keyed package codes.");

        return rewritten;
    }

    private string Opaque(string value)
    {
        if (!rewriting)
            return value;

        var result = Regex.Replace(value, @"[A-Za-z0-9]{8,}", m =>
            Regex.IsMatch(m.Value, "^[0-9a-f]{16,}$") ? keyed.Hex("opaque", m.Value, m.Value.Length)
            : Regex.IsMatch(m.Value, "^[0-9A-F]{16,}$") ? keyed.Hex("opaque", m.Value, m.Value.Length, upper: true)
            : m.Value.All(char.IsDigit) && m.Value.Length < 8 ? m.Value
            : keyed.Substitute("opaque", m.Value));

        pureMap.TryAdd(value, result);
        return result;
    }

    private string Phone(string value)
    {
        if (!value.Any(char.IsDigit))
            return value;

        if (!rewriting)
            return value;

        var stream = keyed.Stream("phone", value).GetEnumerator();
        var digits = new StringBuilder();
        foreach (var c in value)
        {
            if (!char.IsDigit(c))
            {
                digits.Append(c);
                continue;
            }

            stream.MoveNext();
            digits.Append((char)('0' + stream.Current % 10));
        }

        // A number with a country code gets the one no country has, so no synthetic number can ring anywhere.
        var result = digits.ToString();
        if (result.StartsWith('+'))
        {
            var chars = result.ToCharArray();
            var replaced = 0;
            for (var i = 1; i < chars.Length && replaced < 3; i++)
                if (char.IsDigit(chars[i]))
                {
                    chars[i] = '9';
                    replaced++;
                }
            result = new string(chars);
        }

        pureMap.TryAdd(value, result);
        return result;
    }

    private string Email(string value)
    {
        if (!rewriting || !value.Contains('@'))
            return value;

        var result = "user" + keyed.Digits("email", value, 5, nonZeroLead: true) + "@example.com";
        pureMap.TryAdd(value, result);
        return result;
    }

    private string DateOfBirth(string value)
    {
        if (!rewriting || !DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return value;

        var result = new DateTime(date.Year, 1 + keyed.Pick(12, "dob-month", value), 1 + keyed.Pick(28, "dob-day", value)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        pureMap.TryAdd(value, result);
        return result;
    }

    private string AddressLine(string value)
    {
        var (lead, core, trail) = CaseShape.Trim(value);
        if (core.Length == 0)
            return value;

        if (rewriting)
        {
            if (country.Contains(core))
                return lead + country.Get(core) + trail;
            if (region.Contains(core))
                return lead + region.Get(core) + trail;
        }

        return lead + Place(core, locality) + trail;
    }

    /// <summary>
    /// A pool-drawn replacement. A name (company, place, organisation) keeps the pool's own spelling — an
    /// abbreviation in capitals is not a style to copy; a colour or an accessory follows the field's case.
    /// </summary>
    private string Place(string core, Family pool, bool adaptCase = false)
    {
        if (!rewriting)
        {
            pool.Collect(core);
            return core;
        }

        var fictional = pool.Get(core)!;
        return adaptCase ? CaseShape.Like(fictional, core) : fictional;
    }

    private string Organisation(string value)
    {
        var (lead, core, trail) = CaseShape.Trim(value);
        if (core.Length == 0 || core.StartsWith("Built-in System", StringComparison.OrdinalIgnoreCase))
            return value;

        return lead + Place(core, organisation) + trail;
    }

    private static readonly HashSet<string> OrganisationTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "company", "co", "cars", "auto", "automall", "showroom", "showrooms", "motors", "ministry", "rental",
        "rentals", "mall", "trading", "ltd", "llc", "mchj", "dealer", "group", "electricity", "general",
        "distribution", "store", "center", "centre", "garage", "service", "services", "transport", "logistics",
        "bank", "university", "hospital", "department", "office", "industries", "industrial", "enterprise",
        "enterprises", "holding", "corporation", "corp", "inc", "agency", "authority", "council", "school",
        "hotel", "market", "shop", "fleet", "test", "شركة", "مؤسسة",
    };

    private enum NameKind { Full, First, Middle, Last }

    /// <summary>
    /// A person's name becomes a fictional one of the same script and word count, token by token, so a
    /// customer named twice reads as the same fictional person. A name that is an organisation's (a rental
    /// company recorded as a customer) goes to the organisation pool instead.
    /// </summary>
    private string Person(string value, NameKind kind)
    {
        var (lead, core, trail) = CaseShape.Trim(value);
        if (core.Length == 0)
            return value;

        var tokens = core.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (kind == NameKind.Full && tokens.Any(OrganisationTokens.Contains))
            return Organisation(value);

        if (!rewriting)
            return value;

        var script = Scripts.Of(core);
        var result = new List<string>();

        for (var i = 0; i < tokens.Length; i++)
        {
            var token = tokens[i];
            var position = kind switch
            {
                NameKind.First => NameKind.First,
                NameKind.Last => NameKind.Last,
                NameKind.Middle => NameKind.Middle,
                _ when i == 0 => NameKind.First,
                _ when i == tokens.Length - 1 => NameKind.Last,
                _ => NameKind.Middle,
            };

            var pool = (script, position) switch
            {
                (Script.Arabic, NameKind.Last) => FictionalNames.ArabicLastNames,
                (Script.Arabic, _) => FictionalNames.ArabicFirstNames,
                (Script.Cyrillic, NameKind.Last) => FictionalNames.CyrillicLastNames,
                (Script.Cyrillic, NameKind.Middle) => FictionalNames.CyrillicPatronymics,
                (Script.Cyrillic, _) => FictionalNames.CyrillicFirstNames,
                (_, NameKind.Last) => FictionalNames.LatinLastNames,
                _ => FictionalNames.LatinFirstNames,
            };

            var pick = pool[keyed.Pick(pool.Length, "person", position.ToString(), token.ToLowerInvariant())];
            if (token.Length == 1)
                pick = pick[..1];

            result.Add(CaseShape.Like(pick, token));
        }

        var synthetic = lead + string.Join(' ', result) + trail;
        pureMap.TryAdd(value, synthetic);
        return synthetic;
    }

    // --- rows, ids and the walk helpers ---------------------------------------------------------------------

    private sealed class RowScope : IDisposable
    {
        public RowScope(EnvironmentAnonymiser owner, JsonObject node, string path)
        {
            Owner = owner;
            Node = node;
            Path = path;
        }

        public EnvironmentAnonymiser Owner { get; }
        public JsonObject Node { get; }
        public string Path { get; }
        public HashSet<string> Touched { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, string> Tokens { get; } = new(StringComparer.Ordinal);

        public void Dispose() => Owner.rows.Pop();
    }

    private RowScope Row(JsonObject node, string path)
    {
        var scope = new RowScope(this, node, path);
        rows.Push(scope);
        return scope;
    }

    private RowScope Current => rows.Peek();

    private void Touch(string name) => Current.Touched.Add(name);

    private static JsonObject? Obj(JsonObject? o, string name) => o?[name] as JsonObject;

    private static long? Long(JsonNode? node) => node is JsonValue v && v.TryGetValue<long>(out var n) ? n : null;

    private static IEnumerable<JsonObject> Objects(JsonObject? o, string name) =>
        o?[name] is JsonArray array ? array.OfType<JsonObject>() : Enumerable.Empty<JsonObject>();

    private static IEnumerable<JsonObject> Objects(JsonArray array) => array.OfType<JsonObject>();

    private static IEnumerable<JsonArray> Arrays(JsonObject? o, string name) =>
        o?[name] is JsonArray array ? array.OfType<JsonArray>() : Enumerable.Empty<JsonArray>();

    private static string? Str(JsonObject? o, string name) =>
        o?[name] is JsonValue v && v.TryGetValue<string>(out var s) ? s : null;

    private static void Set(JsonObject o, string name, string value) => o[name] = value;

    /// <summary>A string field of an identifier family.</summary>
    private void Apply(JsonObject o, string name, Family family) => ApplyWith(o, name, v =>
    {
        // Values too short to be an identifier (a "0" parent invoice, a "-" package code) are left alone.
        if (v.Count(char.IsLetterOrDigit) < 2)
            return v;

        if (!rewriting)
        {
            family.Collect(v);
            return v;
        }

        return family.Get(v)!;
    });

    /// <summary>A string field with a transform that collects in the first pass and rewrites in the second.</summary>
    private void ApplyWith(JsonObject o, string name, Func<string, string> transform)
    {
        Touch(name);
        var value = Str(o, name);
        if (value is null)
            return;

        var result = transform(value);
        if (rewriting && result != value)
        {
            Set(o, name, result);
            Current.Tokens[value] = result;
        }
    }

    private void ApplyName(JsonObject o, string name, Family pool) => ApplyWith(o, name, v =>
    {
        var (lead, core, trail) = CaseShape.Trim(v);
        if (core.Length == 0 || core.StartsWith("Built-in System", StringComparison.OrdinalIgnoreCase))
            return v;

        return lead + Place(core, pool) + trail;
    });

    private void ApplyRegionName(JsonObject o, string name) => ApplyWith(o, name, v =>
    {
        var (lead, core, trail) = CaseShape.Trim(v);
        if (core.Length == 0)
            return v;

        // A region named like a country (a price set per country) keeps the country's fictional name.
        if (rewriting && country.Contains(core))
            return lead + country.Get(core) + trail;

        return lead + Place(core, region) + trail;
    });

    private void ApplyArray(JsonArray array, Family family) => ApplyArrayWith(array, v =>
    {
        if (v.Count(char.IsLetterOrDigit) < 2)
            return v;
        if (!rewriting)
        {
            family.Collect(v);
            return v;
        }
        return family.Get(v)!;
    });

    private void ApplyArrayWith(JsonArray array, Func<string, string> transform)
    {
        for (var i = 0; i < array.Count; i++)
        {
            if (array[i] is not JsonValue v || !v.TryGetValue<string>(out var value))
                continue;

            var result = transform(value);
            if (rewriting && result != value)
            {
                array[i] = result;
                Current.Tokens[value] = result;
            }
        }
    }

    /// <summary>A numeric field of an identifier family: re-keyed as its decimal text, written back as a number.</summary>
    private void ApplyNumber(JsonObject o, string name, Family family)
    {
        Touch(name);
        if (o[name] is not JsonValue v)
            return;

        var isNumber = v.TryGetValue<long>(out var number);
        string? digits = isNumber ? number.ToString(CultureInfo.InvariantCulture)
            : v.TryGetValue<string>(out var s) ? s : null;
        if (digits is null || digits.Length < 2)
            return;

        if (!rewriting)
        {
            family.Collect(digits);
            return;
        }

        var synthetic = family.Get(digits)!;
        o[name] = isNumber ? JsonValue.Create(long.Parse(synthetic, CultureInfo.InvariantCulture)) : JsonValue.Create(synthetic);
        Current.Tokens[digits] = synthetic;
    }

    private void Text(JsonObject o, string name) => ApplyWith(o, name, TextValue);

    private void Hashes(JsonObject o)
    {
        foreach (var name in new[] { "CompanyHashID", "BranchHashID", "CountryHashID", "RegionHashID", "BrandHashID" })
            ApplyWith(o, name, v =>
            {
                if (v.Length == 0)
                    return v;
                if (!rewriting)
                {
                    knownHashes.Add(v);
                    hash.Collect(v);
                    return v;
                }
                return hash.Get(v)!;
            });
    }

    /// <summary>
    /// A composed id is rebuilt from the re-keyed values of its own row: a component that carries a separator
    /// (a campaign code, a dashed part number) is replaced as a substring, longest first; the rest token by
    /// token; an opaque token nobody re-keyed (a source system's row hash) is re-keyed as opaque.
    /// </summary>
    private void RebuildId(JsonObject o, string name)
    {
        Touch(name);
        if (!rewriting)
            return;

        var id = Str(o, name);
        if (id is null || id.Length == 0)
            return;

        var tokens = Current.Tokens;
        var result = id;

        foreach (var (real, synthetic) in tokens.Where(t => t.Key.Any(IsSeparator)).OrderByDescending(t => t.Key.Length))
            result = result.Replace(real, synthetic, StringComparison.Ordinal);

        result = Regex.Replace(result, @"[^-|:/#]+", m =>
            tokens.TryGetValue(m.Value, out var synthetic) ? synthetic
            : m.Value.Length >= 24 && m.Value.All(char.IsLetterOrDigit) ? Opaque(m.Value)
            : m.Value);

        if (result != id)
            Set(o, name, result);
    }

    private static bool IsSeparator(char c) => c is '-' or '|' or ':' or '/' or '#';

    /// <summary>
    /// Fields whose string value is a date, an enum name, a status code, a numeric id or a plain
    /// specification attribute: nothing to re-key, nothing a vocabulary should touch.
    /// </summary>
    private static readonly HashSet<string> PlainFields = new(StringComparer.Ordinal)
    {
        "InvoiceDate", "PostDate", "LoadDate", "ExpireDate", "CampaignStartDate", "CampaignEndDate", "ValidFrom", "ValidTo",
        "RepairDate", "RepairCompletionDate", "DeliveryDate", "ProcessDate", "DistributorProcessDate", "DateOfReceipt",
        "NewDate", "StartDate", "EndDate", "WarrantyActivationDate", "ProductionDate", "InspectionDate", "ClaimDate",
        "RecordedDate", "TransferDate", "AccountStartDate", "TerminationDate", "InventoryDate", "LastSoldDate",
        "LastArrivedDate", "LastPurchasedDate", "FirstReceivedDate", "UnlockedOn", "ExpiresAt", "OneKOrFiveKServieDate",
        "FirstServieDate", "Date", "NextServiceDate", "DateOfBirth",
        "InvoiceStatus", "ItemStatus", "OrderStatus", "SaleType", "ClaimStatus", "ManufacturerStatus", "PayCode",
        "PanelType", "PanelSide", "PanelPosition", "ActiveForDurationType", "ProgramRole", "CampaignActivationTrigger",
        "CampaignActivationType", "ValidityMode", "ClaimingMethod", "AttachmentFieldBehavior", "Operator", "ValueMatch",
        "WhenUnmet", "Selection", "Gender", "CustomerGender", "CustomerType", "VehicleServiceHistoryConsistencyLevel",
        "PartNumberStorageKeyMode", "InvoiceCurrency", "Status", "LineStatus",
        "ServiceItemID", "VehicleInspectionID", "CampaignVinEntryID", "IntegrationID", "CompanyIntegrationID",
        "BranchIntegrationID", "ServiceCode", "Department", "ModelYear", "Field", "Key", "ProductGroup", "ProductCode",
        "PNC", "BinType", "HSCode", "Class", "BodyType", "Engine", "Cylinders", "LightHeavyType", "Doors", "Fuel",
        "Transmission", "Side", "EngineType", "TankCap", "Style",
    };

    /// <summary>
    /// Every string the schema walk did not name goes through the free-text vocabulary, and its path is
    /// reported, so a field this code does not know about is neither silently kept nor silently lost.
    /// </summary>
    private void Defaults(JsonObject o, string path)
    {
        foreach (var property in o.ToList())
        {
            if (Current.Touched.Contains(property.Key) || PlainFields.Contains(property.Key))
                continue;

            if (property.Value is JsonValue v && v.TryGetValue<string>(out var value))
            {
                if (!rewriting)
                {
                    defaultedFields[path + "." + property.Key] = defaultedFields.GetValueOrDefault(path + "." + property.Key) + 1;
                    text.Collect(value);
                }
                else
                {
                    var rewritten = text.Rewrite(value)!;
                    if (rewritten != value)
                        o[property.Key] = rewritten;
                }
            }
        }
    }

    /// <summary>Dictionary keys that are identifiers (the Vehicles, BrokerStocks and Parts keys) are re-keyed with their family.</summary>
    private void RekeyDictionary(JsonObject owner, string name, Family family, Action<JsonObject> each, bool partKey = false)
    {
        Touch(name);
        if (owner[name] is not JsonObject dictionary)
            return;

        foreach (var entry in dictionary.ToList())
        {
            if (!rewriting)
            {
                if (partKey)
                    PartNumber(entry.Key);
                else
                    family.Collect(entry.Key);
            }

            if (entry.Value is JsonObject value)
                each(value);
        }

        if (!rewriting)
            return;

        var rekeyed = new JsonObject();
        foreach (var entry in dictionary.ToList())
        {
            var value = entry.Value;
            dictionary.Remove(entry.Key);
            rekeyed[partKey ? PartNumber(entry.Key) : family.Get(entry.Key)!] = value;
        }

        owner[name] = rekeyed;
    }

    private void RekeyDictionaryOfArrays(JsonObject owner, string name, Family family, Action<JsonArray> each)
    {
        Touch(name);
        if (owner[name] is not JsonObject dictionary)
            return;

        foreach (var entry in dictionary.ToList())
        {
            if (!rewriting)
                family.Collect(entry.Key);
            if (entry.Value is JsonArray value)
                each(value);
        }

        if (!rewriting)
            return;

        var rekeyed = new JsonObject();
        foreach (var entry in dictionary.ToList())
        {
            var value = entry.Value;
            dictionary.Remove(entry.Key);
            rekeyed[family.Get(entry.Key)!] = value;
        }

        owner[name] = rekeyed;
    }

    /// <summary>
    /// The unauthorized vehicle: a VIN in no record, generated as the <c>isAuthorized: false</c> shell. Its
    /// aggregate carries every section the other vehicles carry, empty.
    /// </summary>
    private void AddMintedVehicle(JsonObject root)
    {
        if (root["Vehicles"] is not JsonObject vehicles)
            return;

        for (var attempt = 0; ; attempt++)
        {
            var candidate = Vin.Mint(keyed, environmentName, attempt);
            if (!vin.Map.Values.Contains(candidate) && !vin.Contains(candidate))
            {
                mintedVin = candidate;
                break;
            }
        }

        var template = vehicles.Select(p => p.Value).OfType<JsonObject>().FirstOrDefault();
        var shell = new JsonObject();
        if (template is not null)
            foreach (var property in template)
            {
                if (property.Key == "VIN")
                    shell["VIN"] = mintedVin;
                else if (property.Value is JsonArray)
                    shell[property.Key] = new JsonArray();
            }

        vehicles[mintedVin!] = shell;
    }
}

public sealed record AnonymisationResult(
    IReadOnlyDictionary<string, int> FamilyCounts,
    int DerivedCount,
    IReadOnlyList<KeyValuePair<string, int>> DefaultedFields,
    string MintedVin,
    int MintedPanelImages);
