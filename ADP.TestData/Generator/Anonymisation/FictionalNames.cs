namespace ADP.TestData.Generator.Anonymisation;

/// <summary>
/// The pools the anonymiser draws names from. Every entry is invented for these demos: no manufacturer,
/// distributor, dealer, city or person here is a real one, and the model words are coined so a fictional
/// brand's line-up reads as a line-up rather than as a thesaurus. A pool is only ever indexed by a keyed
/// derivation of the real value it replaces, so the same real name always lands on the same fictional
/// one; the pools are deliberately larger than any environment needs so distinct real names keep
/// distinct fictional ones (the assignment retries on a clash).
/// </summary>
public static class FictionalNames
{
    /// <summary>Supply-chain companies that never sell to an end customer: the distributor, an importer.</summary>
    public static readonly string[] Distributors =
    {
        "Meridia Motor Distribution", "Solmara Vehicle Imports", "Northern Automotive Distribution",
        "Central Motors Distribution", "Coastal Vehicle Distribution", "Highland Motor Imports",
        "Vessandria Automotive Imports", "Estaria Motor Distribution",
    };

    /// <summary>Dealers and other companies of the supply chain.</summary>
    public static readonly string[] Dealers =
    {
        "Summit Motors", "Crossroads Automotive", "Harbor Auto", "Northgate Motors", "Eastbridge Cars",
        "Southport Auto Group", "Riverside Motors", "Hillcrest Automotive", "Lakeside Auto", "Oakridge Motors",
        "Millbrook Cars", "Stonebridge Auto", "Bayview Motors", "Meadowvale Automotive", "Kingsway Motors",
        "Parkside Auto", "Fairmont Motors", "Ashford Auto Group", "Brookfield Cars", "Clearwater Motors",
        "Silverton Auto", "Thornbury Motors", "Valemont Cars", "Willowdale Auto", "Greystone Motors",
        "Copperfield Automotive", "Ivybridge Cars", "Linden Park Motors", "Marlow Auto", "Foxhollow Motors",
    };

    /// <summary>Branch and showroom names: places, not companies, so a branch reads as a location of its dealer.</summary>
    public static readonly string[] Places =
    {
        "Northgate", "Eastbridge", "Southport", "Westhaven", "Riverside", "Hillcrest", "Lakeside", "Oakridge",
        "Millbrook", "Stonebridge", "Bayview", "Meadowvale", "Kingsway", "Parkside", "Fairmont", "Ashford",
        "Brookfield", "Clearwater", "Dunmore", "Elmwood", "Fernhill", "Glenmore", "Harrowgate", "Ironbridge",
        "Juniper Falls", "Kestrel Bay", "Larkspur", "Maplewood", "Newhaven", "Orchard Hill", "Pinecrest",
        "Quarry Lane", "Rosewood", "Silverton", "Thornbury", "Upton", "Valemont", "Willowdale", "Yarrow Point",
        "Ambergate", "Birchwood", "Copperfield", "Dovecote", "Eversley", "Foxhollow", "Greystone", "Hazelmere",
        "Ivybridge", "Kingfisher Row", "Linden Park", "Marlow Street", "Old Mill Road", "Harbour Lane",
        "Station Road", "Market Square", "Cedar Avenue", "Summit Drive", "Crossroads", "Wharf Street",
        "Beacon Hill", "Tannery Row", "Mill Lane", "Bridge Street", "Chapel Green", "Waterside",
    };

    /// <summary>Brokers, rental companies, fleet operators, public bodies — any organisation that is a customer or a trade partner.</summary>
    public static readonly string[] Organisations =
    {
        "Summit Trading", "Crossroads Showroom", "Harbor Cars", "Atlas Fleet Services", "Beacon Car Rental",
        "Cornerstone Motors", "Delta Auto Showroom", "Evergreen Transport", "Frontier Cars", "Granite Logistics",
        "Horizon Rentals", "Ironwood Trading", "Juniper Motor Park", "Keystone Showroom", "Lantern Cars",
        "Meridian Fleet", "Nimbus Motors", "Orbit Car Rental", "Pioneer Auto", "Quartz Trading",
        "Ridgeline Showroom", "Sterling Cars", "Tidewater Motors", "Union Fleet Services", "Vantage Auto",
        "Westwind Trading", "Zenith Car Rental", "Amber Motor Court", "Bluewater Cars", "Cobalt Showroom",
        "Driftwood Motors", "Ember Fleet", "Falcon Trading", "Glacier Auto", "Harvest Transport",
        "Indigo Motors", "Jade Showroom", "Kite Car Rental", "Lumen Auto", "Mosaic Trading",
        "Regional Power Utility", "Public Works Department", "Northern Electricity Board",
        "Interior Services Office", "City Transport Authority", "Coastal Water Company", "Civic Housing Trust",
    };

    public static readonly string[] Countries =
    {
        "Meridia", "Solmara", "Vessandria", "Tarquinia", "Orlestia", "Kaldera", "Norvelle", "Estaria",
    };

    public static readonly string[] Regions =
    {
        "Northern Highlands", "Coastal Plain", "Central Valley", "Eastern Marches", "Western Reach",
        "Lake District", "Southern Delta", "Upland Basin", "Riverlands", "Highland Coast", "Great Plateau",
        "Northern Coast",
    };

    public static readonly string[] ExteriorColours =
    {
        "Glacier White", "Obsidian Black", "Harbor Grey Metallic", "Crimson Red Pearl", "Deep Sapphire",
        "Sandstone Beige", "Forest Green Mica", "Titanium Silver", "Amber Bronze", "Arctic Blue",
        "Charcoal Mica", "Ivory Pearl", "Slate Grey", "Copper Sunset", "Midnight Blue Metallic",
        "Pearl White Crystal", "Storm Grey", "Desert Sand", "Ruby Red Mica", "Graphite Black",
        "Moonlight Silver", "Olive Green", "Sunset Orange", "Quartz White", "Ocean Blue Metallic",
        "Platinum Grey", "Ember Red", "Starlight Black Pearl",
    };

    public static readonly string[] InteriorColours =
    {
        "Black Leather", "Beige Fabric", "Grey Cloth", "Tan Leather", "Ivory Fabric", "Dark Brown Leather",
        "Charcoal Fabric", "Saddle Brown", "Stone Grey Leather", "Chestnut", "Ash Grey", "Cream Leather",
        "Graphite Cloth", "Terracotta Leather", "Sand Fabric", "Ebony", "Walnut and Black", "Slate Cloth",
        "Oatmeal Fabric", "Cocoa Leather",
    };

    /// <summary>
    /// Accessory names. A real description is replaced by a whole name rather than word by word: descriptions
    /// carry a distributor's own product vocabulary, and a fictional catalogue reads better than a scrambled
    /// one. The drawing shown for an accessory is chosen from the fictional name (<see cref="DemoAssets"/>).
    /// </summary>
    public static readonly string[] Accessories =
    {
        "Side Steps", "Roof Rails", "Mud Guards", "Floor Mats", "Tow Bar", "Roof Rack", "Door Visors",
        "Bumper Guard", "Cargo Liner", "Wind Deflectors", "Chrome Mirror Covers", "Bed Liner", "Hood Protector",
        "Wheel Locks", "Cargo Net", "First Aid Kit", "Rear Spoiler", "Running Boards", "Sunroof Deflector",
        "Illuminated Sill Plates", "All-Weather Floor Mats", "Dash Camera", "Parking Sensors", "Reverse Camera",
        "Alloy Wheel Set", "Body Side Moulding", "Tailgate Spoiler", "Trunk Organiser", "Seat Covers",
        "Window Tint", "Paint Protection Film", "Roof Box", "Bike Carrier", "Snorkel", "Winch Kit", "Bull Bar",
        "LED Light Bar", "Fog Lamps", "Rear Step", "Fuel Cap Cover", "Warning Triangle", "Fire Extinguisher",
        "Owner's Handbook Case", "Service Booklet", "Fuel Type Sticker", "Warranty Sticker", "Wireless Charger",
        "Phone Holder", "Boot Tray", "Rear Seat Entertainment", "Blind Spot Monitor", "Engine Cover",
        "Exhaust Tips", "Roadside Kit", "Number Plate Holder", "Rubber Mats", "Puddle Lamps", "Scuff Plates",
    };

    /// <summary>
    /// Coined words for model names, one per real word: a fictional brand's line-up. None is a model name of
    /// any manufacturer as far as the authors know; a coincidence with an obscure one would still not be a
    /// reference to the estates these demos are drawn from.
    /// </summary>
    public static readonly string[] ModelWords =
    {
        "Aurelon", "Vantix", "Kestra", "Solvane", "Marlowe", "Tessari", "Corvaro", "Haldane", "Novaris",
        "Pelagos", "Quorin", "Ravelli", "Sarnova", "Talora", "Veltrix", "Wexlar", "Yarrowe", "Zephira",
        "Altaris", "Brennix", "Calvera", "Dorane", "Elmaris", "Farrow", "Galenor", "Hollisar", "Ibarron",
        "Jovane", "Kalderis", "Lorane", "Merrix", "Nolane", "Orrinal", "Pryora", "Quillon", "Rowane",
        "Sabrix", "Torane", "Umbrel", "Valeris", "Wrenna", "Xandris", "Yveris", "Zanova", "Ardene",
        "Bexlar", "Corbane", "Daltris", "Embris", "Fenwar", "Garrone", "Hadlow", "Ingrane", "Jaspera",
        "Kimbral", "Landris", "Milora", "Nyxar", "Osrin", "Paxora", "Quadris", "Rialdo", "Stellaro", "Thanor",
        "Ulrane", "Vespra", "Wildane", "Xavor", "Yardlow", "Zorane", "Avelin", "Borane", "Cendris", "Delmar",
        "Evanix", "Florane", "Gravon", "Helvane", "Irvane", "Jendra", "Korvane", "Lumaris", "Mireno", "Nardis",
        "Ombra", "Perron", "Quenta", "Rustane", "Sylvane", "Tavrel", "Ulvane", "Velmora", "Wystra", "Xerane",
        "Yolanis", "Zendra", "Abrix", "Belmora", "Caldane", "Dovaris", "Ellonar", "Fabris", "Gaveno",
        "Harrow", "Istral", "Jorane", "Kaelis", "Lavoro", "Morane", "Nerix", "Olvane", "Pardis", "Quavel",
        "Ravane", "Selvor", "Tirane", "Ustra", "Vorane", "Welkin", "Xalis", "Yendra", "Zavrel", "Amberon",
        "Bravane", "Cyrane", "Dastrel", "Eldris", "Fennor", "Gorane", "Hestra", "Ilvane", "Jaxor", "Kerrin",
        "Lindrel", "Maxor", "Norane", "Orvel", "Pellar", "Quiris", "Rendra", "Soraine", "Tellis", "Urvane",
        "Vandris", "Wexane", "Xylor", "Yorane", "Zellis",
    };

    public static readonly string[] LatinFirstNames =
    {
        "Adrian", "Bianca", "Caleb", "Dana", "Elias", "Farah", "Gavin", "Hana", "Idris", "Jonah", "Kira", "Leon",
        "Maya", "Nadia", "Omar", "Priya", "Quentin", "Rafael", "Selin", "Tariq", "Uma", "Victor", "Wanda",
        "Xavier", "Yusuf", "Zara", "Amira", "Bruno", "Celine", "Dario", "Elena", "Faris", "Greta", "Hugo",
        "Ines", "Jamal", "Karim", "Layla", "Marco", "Noor", "Owen", "Petra", "Rami", "Sana", "Theo", "Vera",
    };

    public static readonly string[] LatinLastNames =
    {
        "Abbott", "Barlow", "Carver", "Dalton", "Ellison", "Fairbanks", "Grayson", "Holloway", "Ingles",
        "Jarvis", "Keller", "Lindqvist", "Mercer", "Norwood", "Osborne", "Pendleton", "Quimby", "Rutherford",
        "Sandoval", "Thackeray", "Underhill", "Vasquez", "Whitfield", "Xenakis", "Yates", "Zimmer", "Ashworth",
        "Beaumont", "Calloway", "Draper", "Everett", "Fletcher", "Galloway", "Hartwell", "Irving", "Jennings",
        "Kingsley", "Lockhart", "Marchetti", "Nakamura", "Okafor", "Petrov", "Rahimi", "Soriano", "Tanaka",
        "Varga",
    };

    public static readonly string[] ArabicFirstNames =
    {
        "خالد", "سارة", "يوسف", "ليلى", "عمر", "نور", "طارق", "هدى", "زيد", "مريم", "كريم", "رنا",
        "سامر", "دينا", "ماجد", "لينا", "باسل", "ريم", "فادي", "سلمى",
    };

    public static readonly string[] ArabicLastNames =
    {
        "الحداد", "النجار", "السالم", "الخطيب", "العبدالله", "الرشيد", "الصالح", "المنصور", "البكري",
        "الزهراني", "القاسم", "الهاشمي", "الفارس", "الشمري", "العامري", "الجابر", "الناصر", "السويدي",
        "الغامدي", "الحسن",
    };

    public static readonly string[] CyrillicFirstNames =
    {
        "Алексей", "Дмитрий", "Сергей", "Иван", "Максим", "Андрей", "Николай", "Павел", "Анна", "Мария",
        "Елена", "Ольга", "Татьяна", "Наталья", "Ирина", "Светлана", "Тимур", "Рустам", "Азиз", "Фаррух",
        "Дилшод", "Бахтиёр", "Санжар", "Гульнара", "Нигора", "Дилноза", "Камила", "Жасмин", "Эмир", "Лола",
    };

    public static readonly string[] CyrillicLastNames =
    {
        "Иванов", "Петров", "Смирнов", "Кузнецов", "Попов", "Васильев", "Соколов", "Морозов", "Волков",
        "Новиков", "Каримов", "Рахимов", "Юсупов", "Абдуллаев", "Назаров", "Исмаилов", "Хасанов", "Мирзаев",
        "Турсунов", "Салимов", "Ахмедова", "Ковалёва", "Лебедева", "Орлова",
    };

    public static readonly string[] CyrillicPatronymics =
    {
        "Алексеевич", "Дмитриевич", "Сергеевич", "Ивановна", "Петровна", "Рустамович", "Азизовна",
        "Тимуровна", "Бахтиёрович", "Фаррухович", "Николаевна", "Максимович",
    };

    /// <summary>
    /// World manufacturer identifiers for the fictional brand's VINs. The two-letter prefixes are ones the
    /// public WMI tables the authors checked do not list as assigned, and a third character of <c>9</c> marks
    /// a small-series manufacturer under ISO 3779, so no decoder resolves these to a real make; change the
    /// pool here if a decoder ever does (every VIN re-keys, like a seed change). Each real WMI maps to one
    /// of them by a keyed choice, which keeps "built at several plants" as a visible shape of the data.
    /// </summary>
    public static readonly string[] WorldManufacturerIdentifiers =
    {
        "ZS9", "ZT9", "ZU9", "ZV9", "ZW9", "ZS8", "ZT8", "ZU8", "ZV8", "ZW8",
    };
}
