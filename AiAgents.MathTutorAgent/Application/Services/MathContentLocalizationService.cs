using System.Text.RegularExpressions;
using AiAgents.MathTutorAgent.Application.DTOs;

namespace AiAgents.MathTutorAgent.Application.Services;

public class MathContentLocalizationService
{
    private static readonly Dictionary<string, string> TopicTranslationsBs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Addition"] = "Sabiranje",
        ["Subtraction"] = "Oduzimanje",
        ["Multiplication"] = "Množenje",
        ["Division"] = "Dijeljenje",
        ["Fractions"] = "Razlomci",
        ["Decimals & Percentages"] = "Decimale i procenti",
        ["Negative Numbers"] = "Negativni brojevi",
        ["Algebraic Expressions"] = "Algebarski izrazi",
        ["Linear Equations"] = "Linearne jednačine",
        ["Inequalities"] = "Nejednačine",
        ["Systems of Equations"] = "Sistemi jednačina",
        ["Polynomials"] = "Polinomi",
        ["Set Basics"] = "Osnove skupova",
        ["Set Operations"] = "Operacije sa skupovima",
        ["Propositional Logic"] = "Propozicijska logika",
        ["Tautologies & Contradictions"] = "Tautologije i kontradikcije",
        ["Basic Shapes"] = "Osnovni oblici",
        ["Perimeter & Area"] = "Obim i površina",
        ["Triangles"] = "Trouglovi",
        ["Pythagorean Theorem"] = "Pitagorina teorema"
    };

    private static readonly Dictionary<string, string> AreaTranslationsBs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arithmetic"] = "Aritmetika",
        ["PreAlgebra"] = "Predalgebra",
        ["Algebra"] = "Algebra",
        ["Logic"] = "Logika",
        ["SetTheory"] = "Teorija skupova",
        ["Geometry"] = "Geometrija",
        ["Proofs"] = "Dokazi"
    };

    private static readonly Dictionary<string, string> ShapeTranslationsBs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["triangle"] = "trougao",
        ["square"] = "kvadrat",
        ["rectangle"] = "pravougaonik",
        ["parallelogram"] = "paralelogram",
        ["quadrilateral"] = "četverougao",
        ["pentagon"] = "petougao",
        ["hexagon"] = "šesterougao",
        ["heptagon"] = "sedmerougao",
        ["octagon"] = "osmerougao",
        ["nonagon"] = "deveterougao",
        ["decagon"] = "deseterougao",
        ["cube"] = "kocka",
        ["circle"] = "krug"
    };

    private static readonly Dictionary<string, string> ReferenceTitleTranslationsBs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Arithmetic Fundamentals"] = "Osnove aritmetike",
        ["Geometry Essentials"] = "Osnove geometrije"
    };

    private static readonly Dictionary<string, string> ExplanationExactTranslationsBs = new(StringComparer.Ordinal)
    {
        ["A cube has 8 vertices (corners where edges meet). Count the 4 vertices on top and 4 on bottom."] =
            "Kocka ima 8 vrhova (uglova gdje se sastaju ivice). Prebroj 4 vrha gore i 4 dolje.",
        ["Example: Imagine a dice - it has 8 corners where you could put your finger."] =
            "Primjer: Zamisli kockicu za igru - ima 8 uglova gdje možeš staviti prst.",
        ["A triangle has 3 vertices (corners). Each vertex is where two sides meet."] =
            "Trougao ima 3 vrha (ugla). Svaki vrh je mjesto gdje se sijeku dvije stranice.",
        ["Example: Draw a triangle - count the 3 corner points."] =
            "Primjer: Nacrtaj trougao i prebroj 3 ugaone tačke.",
        ["Each angle in a square/rectangle is 90° (right angle). Total sum is 360°."] =
            "Svaki ugao u kvadratu i pravougaoniku je 90° (pravi ugao). Zbir svih uglova je 360°.",
        ["Example: All 4 corners of a piece of paper are 90° angles."] =
            "Primjer: Sva 4 ugla papira su od 90°.",
        ["A square/rectangle has 4 vertices (corners). Each corner is where two sides meet at a right angle."] =
            "Kvadrat i pravougaonik imaju 4 vrha (ugla). Svaki vrh je mjesto gdje se sijeku dvije stranice pod pravim uglom.",
        ["Example: A piece of paper has 4 corners."] =
            "Primjer: Komad papira ima 4 ugla.",
        ["Vertices are the corner points where edges meet. Count each corner point of the shape."] =
            "Vrhovi su ugaone tačke gdje se sastaju ivice. Prebroj svaki vrh oblika.",
        ["Example: A triangle has 3 vertices, a square has 4, a cube has 8."] =
            "Primjer: Trougao ima 3 vrha, kvadrat 4, a kocka 8.",
        ["A triangle has 3 sides. Each side is a straight line connecting two vertices."] =
            "Trougao ima 3 stranice. Svaka stranica je prava linija koja spaja dva vrha.",
        ["Example: Count the edges of a triangle - you'll find 3 straight lines."] =
            "Primjer: Prebroj stranice trougla - ima 3 prave linije.",
        ["A square has 4 equal sides. All sides have the same length."] =
            "Kvadrat ima 4 jednake stranice. Sve stranice su iste dužine.",
        ["Example: A chess board square has 4 equal sides."] =
            "Primjer: Polje na šahovskoj tabli ima 4 jednake stranice.",
        ["A pentagon has 5 sides. Think of the Pentagon building in USA - it has 5 sides."] =
            "Petougao ima 5 stranica. I zgrada Pentagon ima 5 stranica.",
        ["Example: Draw a shape with 5 straight edges - that's a pentagon."] =
            "Primjer: Nacrtaj oblik sa 5 pravih ivica - to je petougao.",
        ["A cube has 6 faces (sides). Each face is a square. Think top, bottom, left, right, front, back."] =
            "Kocka ima 6 strana (ploha). Svaka strana je kvadrat: gore, dolje, lijevo, desno, naprijed i nazad.",
        ["Example: A dice has 6 faces, numbered 1 through 6."] =
            "Primjer: Kockica za igru ima 6 strana, označenih brojevima od 1 do 6.",
        ["Count the straight edges around the perimeter of the shape."] =
            "Prebroj prave ivice duž obima oblika.",
        ["Example: Triangle=3 sides, Square=4 sides, Pentagon=5 sides."] =
            "Primjer: Trougao=3 stranice, kvadrat=4 stranice, petougao=5 stranica.",
        ["The sum of angles in ANY triangle is always 180°. To find a missing angle: subtract the known angles from 180°."] =
            "Zbir uglova u svakom trouglu je uvijek 180°. Nepoznati ugao dobiješ tako što od 180° oduzmeš poznate uglove.",
        ["Example: Triangle with angles 60° and 70°. Missing angle = 180° - 60° - 70° = 50°."] =
            "Primjer: Trougao ima uglove 60° i 70°. Nepoznati ugao = 180° - 60° - 70° = 50°.",
        ["Sum of angles in a polygon = (n-2) × 180° where n is the number of sides."] =
            "Zbir uglova mnogougla je (n-2) × 180°, gdje je n broj stranica.",
        ["Example: For triangle (n=3): (3-2) × 180° = 180°."] =
            "Primjer: Za trougao (n=3): (3-2) × 180° = 180°.",
        ["Addition combines numbers. Start with the first number and count up by the second number."] =
            "Sabiranje spaja brojeve. Kreni od prvog broja i broji unaprijed za drugi broj.",
        ["Example: 5 + 3 = 8. Start at 5, count: 6, 7, 8."] =
            "Primjer: 5 + 3 = 8. Kreni od 5 i broji: 6, 7, 8.",
        ["Subtraction finds the difference. Start with the larger number and count down."] =
            "Oduzimanje daje razliku. Kreni od većeg broja i broji unazad.",
        ["Example: 10 - 4 = 6. Start at 10, count back: 9, 8, 7, 6."] =
            "Primjer: 10 - 4 = 6. Kreni od 10 i broji unazad: 9, 8, 7, 6.",
        ["Multiplication is repeated addition. 3 × 4 means add 3 four times: 3+3+3+3=12."] =
            "Množenje je ponovljeno sabiranje. 3 × 4 znači saberi 3 četiri puta: 3+3+3+3=12.",
        ["Example: 6 × 7 = 42. Think: 6 groups of 7, or add 7 six times."] =
            "Primjer: 6 × 7 = 42. Zamisli 6 grupa po 7 ili saberi 7 šest puta.",
        ["Division splits a number into equal parts. Ask 'how many groups fit?'"] =
            "Dijeljenje raspoređuje broj na jednake dijelove. Pitanje je: 'Koliko grupa stane?'",
        ["Example: 20 ÷ 4 = 5. How many 4s fit in 20? Count: 4, 8, 12, 16, 20 = 5 groups."] =
            "Primjer: 20 ÷ 4 = 5. Koliko puta 4 stane u 20? Broji: 4, 8, 12, 16, 20 = 5 grupa.",
        ["Fractions show parts of a whole. Numerator (top) = how many parts. Denominator (bottom) = total parts. To add fractions, find common denominator first."] =
            "Razlomci prikazuju dijelove cjeline. Brojnik (gore) je broj dijelova, nazivnik (dolje) je ukupan broj dijelova. Za sabiranje razlomaka prvo nađi zajednički nazivnik.",
        ["Example: 1/2 + 1/4 = 2/4 + 1/4 = 3/4 (convert to same denominator)."] =
            "Primjer: 1/2 + 1/4 = 2/4 + 1/4 = 3/4 (prevedi na isti nazivnik).",
        ["Percent means 'per hundred'. To find X% of Y, convert to decimal and multiply: (X/100) × Y."] =
            "Procenat znači 'na sto'. Da nađeš X% od Y, pretvori u decimalni broj i pomnoži: (X/100) × Y.",
        ["Example: 25% of 80 = 0.25 × 80 = 20."] =
            "Primjer: 25% od 80 = 0.25 × 80 = 20.",
        ["To solve equations, isolate the variable using inverse operations. Whatever you do to one side, do to the other."] =
            "Za rješavanje jednačina izoluj nepoznatu koristeći suprotne operacije. Što uradiš na jednoj strani, uradi i na drugoj.",
        ["Example: 2x + 5 = 13 → Subtract 5: 2x = 8 → Divide by 2: x = 4."] =
            "Primjer: 2x + 5 = 13 → Oduzmi 5: 2x = 8 → Podijeli sa 2: x = 4.",
        ["To simplify expressions, combine like terms (terms with the same variable)."] =
            "Za pojednostavljenje izraza spoji slične članove (članove sa istom nepoznatom).",
        ["Example: 2x + 3x = 5x (add coefficients). 5a - 2a = 3a."] =
            "Primjer: 2x + 3x = 5x (saberi koeficijente). 5a - 2a = 3a.",
        ["To expand, use FOIL (First, Outer, Inner, Last). To factor, find common factors or patterns."] =
            "Za razvijanje koristi FOIL (prvi, spoljašnji, unutrašnji, zadnji član). Za faktorisanje traži zajedničke faktore ili obrasce.",
        ["Example: (x+3)(x+2) = x² + 2x + 3x + 6 = x² + 5x + 6."] =
            "Primjer: (x+3)(x+2) = x² + 2x + 3x + 6 = x² + 5x + 6.",
        ["∈ means 'is an element of'. Check if the item is listed in the set."] =
            "∈ znači 'je element skupa'. Provjeri da li je element naveden u skupu.",
        ["Example: Is 3 ∈ {1,2,3}? Yes, 3 is in the list."] =
            "Primjer: Da li je 3 ∈ {1,2,3}? Da, 3 je u skupu.",
        ["Union (∪) combines all elements from both sets, no duplicates."] =
            "Unija (∪) spaja sve elemente iz oba skupa, bez ponavljanja.",
        ["Example: {1,2} ∪ {2,3} = {1,2,3} (2 appears once)."] =
            "Primjer: {1,2} ∪ {2,3} = {1,2,3} (broj 2 se piše jednom).",
        ["Intersection (∩) finds elements common to BOTH sets."] =
            "Presjek (∩) daje elemente koji su zajednički OBA skupa.",
        ["Example: {1,2} ∩ {2,3} = {2} (only 2 is in both)."] =
            "Primjer: {1,2} ∩ {2,3} = {2} (samo je 2 u oba skupa).",
        ["A tautology is always true. Example: p ∨ ¬p (something is true OR false, always true)."] =
            "Tautologija je uvijek tačna. Primjer: p ∨ ¬p (izjava je tačna ili netačna, i izraz je uvijek tačan).",
        ["Example: p ∨ ¬p. If p=True: True ∨ False = True. If p=False: False ∨ True = True. Always true!"] =
            "Primjer: p ∨ ¬p. Ako je p=Tačno: Tačno ∨ Netačno = Tačno. Ako je p=Netačno: Netačno ∨ Tačno = Tačno. Uvijek tačno!",
        ["Pythagorean theorem: a² + b² = c² where c is hypotenuse (longest side) and a,b are legs. Only for RIGHT triangles!"] =
            "Pitagorina teorema: a² + b² = c² gdje je c hipotenuza (najduža stranica), a i b su katete. Važi samo za PRAVOUGLE trouglove!",
        ["Example: Legs 3 and 4 → 3² + 4² = 9 + 16 = 25 → c² = 25 → c = 5."] =
            "Primjer: Katete su 3 i 4 → 3² + 4² = 9 + 16 = 25 → c² = 25 → c = 5.",
        ["Perimeter is the distance around a shape. Add all the sides."] =
            "Obim je dužina linije oko oblika. Saberi sve stranice.",
        ["Example: Rectangle 5×3 → Perimeter = 5+3+5+3 = 16 or 2(5+3) = 16."] =
            "Primjer: Pravougaonik 5×3 → Obim = 5+3+5+3 = 16 ili 2(5+3) = 16.",
        ["Area of rectangle = length × width. Area of square = side × side."] =
            "Površina pravougaonika = dužina × širina. Površina kvadrata = stranica × stranica.",
        ["Example: Rectangle 5×3 → Area = 5 × 3 = 15."] =
            "Primjer: Pravougaonik 5×3 → Površina = 5 × 3 = 15.",
        ["Area of triangle = (base × height) ÷ 2."] =
            "Površina trougla = (osnovica × visina) ÷ 2.",
        ["Example: Base=6, Height=4 → Area = (6×4)÷2 = 12."] =
            "Primjer: Osnovica=6, visina=4 → Površina = (6×4)÷2 = 12.",
        ["Area of circle = πr² where r is radius. Circumference = 2πr."] =
            "Površina kruga = πr², gdje je r poluprečnik. Obim = 2πr.",
        ["Example: Radius=3 → Area = 3.14 × 3² = 3.14 × 9 = 28.26."] =
            "Primjer: Poluprečnik=3 → Površina = 3.14 × 3² = 3.14 × 9 = 28.26.",
        ["Area is the space inside a shape. Use the formula for that specific shape."] =
            "Površina je prostor unutar oblika. Koristi formulu za taj konkretan oblik.",
        ["Example: Rectangle=L×W, Triangle=(B×H)÷2, Circle=πr²."] =
            "Primjer: Pravougaonik=D×Š, trougao=(O×V)÷2, krug=πr².",
        ["Break the problem into smaller steps. Identify what you know, then apply the appropriate method."] =
            "Razdvoji zadatak na manje korake. Prepoznaj šta je poznato, pa primijeni odgovarajuću metodu.",
        ["Example: Read carefully, identify the operation needed, solve step by step."] =
            "Primjer: Pažljivo pročitaj, odredi potrebnu operaciju i riješi korak po korak.",
        ["Addition combines numbers. Start with first number, count up."] =
            "Sabiranje spaja brojeve. Kreni od prvog broja i broji unaprijed.",
        ["Example: 5+3=8. Start at 5, count: 6,7,8."] =
            "Primjer: 5+3=8. Kreni od 5 i broji: 6,7,8.",
        ["Subtraction finds difference. Start with larger number, count down."] =
            "Oduzimanje daje razliku. Kreni od većeg broja i broji unazad.",
        ["Example: 10-4=6. Start at 10, count: 9,8,7,6."] =
            "Primjer: 10-4=6. Kreni od 10 i broji: 9,8,7,6.",
        ["Multiplication is repeated addition."] =
            "Množenje je ponovljeno sabiranje.",
        ["Example: 3×4 = 3+3+3+3 = 12."] =
            "Primjer: 3×4 = 3+3+3+3 = 12.",
        ["Division splits into equal parts."] =
            "Dijeljenje razdvaja na jednake dijelove.",
        ["Example: 20÷4=5. How many 4s in 20? Count: 4,8,12,16,20 = 5 groups."] =
            "Primjer: 20÷4=5. Koliko puta 4 stane u 20? Broji: 4,8,12,16,20 = 5 grupa.",
        ["Fractions show parts of whole. Top=parts you have, Bottom=total parts."] =
            "Razlomci prikazuju dijelove cjeline. Gore je broj dijelova koje imaš, dolje ukupan broj dijelova.",
        ["Example: 1/2 + 1/4 = 2/4 + 1/4 = 3/4."] =
            "Primjer: 1/2 + 1/4 = 2/4 + 1/4 = 3/4.",
        ["Isolate variable using inverse operations."] =
            "Izoluj nepoznatu koristeći suprotne operacije.",
        ["Example: 2x+5=13 → 2x=8 → x=4."] =
            "Primjer: 2x+5=13 → 2x=8 → x=4.",
        ["Know properties: triangles have 3 sides, squares have 4 equal sides."] =
            "Zapamti osobine: trougao ima 3 stranice, kvadrat ima 4 jednake stranice.",
        ["Example: Sum of triangle angles = 180°."] =
            "Primjer: Zbir uglova trougla = 180°.",
        ["In right triangle: a²+b²=c² where c is hypotenuse."] =
            "U pravouglom trouglu: a²+b²=c² gdje je c hipotenuza.",
        ["Example: Legs 3,4 → c²=9+16=25 → c=5."] =
            "Primjer: Katete 3 i 4 → c²=9+16=25 → c=5.",
        ["Review the foundational principles for this concept. Practice similar problems."] =
            "Ponovi osnovne principe ove teme i vježbaj slične zadatke.",
        ["Example: Break problem into steps, solve each carefully."] =
            "Primjer: Podijeli zadatak na korake i pažljivo riješi svaki."
    };

    public string NormalizeLanguage(string? languageCode)
    {
        return string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase) ? "en" : "bs";
    }

    public bool IsBosnian(string? languageCode) => NormalizeLanguage(languageCode) == "bs";

    public string LocalizeTopicName(string? topicName, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(topicName))
        {
            return topicName ?? string.Empty;
        }

        return TopicTranslationsBs.TryGetValue(topicName.Trim(), out var translated)
            ? translated
            : topicName;
    }

    public string LocalizeAreaName(string? areaName, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(areaName))
        {
            return areaName ?? string.Empty;
        }

        return AreaTranslationsBs.TryGetValue(areaName.Trim(), out var translated)
            ? translated
            : areaName;
    }

    public string LocalizeDecision(string? decision, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(decision))
        {
            return decision ?? string.Empty;
        }

        return decision.Trim().ToLowerInvariant() switch
        {
            "advance" => "Napreduj",
            "review" => "Ponovi",
            "remediate" => "Dodatna vježba",
            _ => decision
        };
    }

    public string LocalizeAnswerToken(string? answer, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(answer))
        {
            return answer ?? string.Empty;
        }

        var trimmed = answer.Trim();
        return trimmed.ToLowerInvariant() switch
        {
            "yes" => "Da",
            "no" => "Ne",
            "true" => "Tačno",
            "false" => "Netačno",
            _ => trimmed
        };
    }

    public string LocalizeQuestionText(string? questionText, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(questionText))
        {
            return questionText ?? string.Empty;
        }

        var text = questionText.Trim();

        // High-confidence full-template translations first (prevents mixed EN/BS text).
        text = Regex.Replace(text, @"^What\s+is\s+the\s+sum\s+of\s+angles\s+in\s+a\s+triangle\?$", "Koliki je zbir uglova u trouglu?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+shape\s+has\s+all\s+sides\s+equal\?$", "Koji oblik ima sve stranice jednake?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+the\s+power\s+set\s+of\s+(.+?)\?$", "Koji je partitivni skup od $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Triangle\s+has\s+angles\s+(.+?)\s+and\s+(.+?)\.\s*What\s+is\s+the\s+third\s+angle\?$", "Trougao ima uglove $1 i $2. Koliki je treći ugao?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Right\s+triangle\s+legs\s+(.+?)\s+and\s+(.+?)\.\s*Hypotenuse\?$", "Pravougli trougao ima katete $1 i $2. Kolika je hipotenuza?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Complete\s+the\s+proportion:\s*(.+)$", "Dovrši proporciju: $1", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Evaluate:\s*(.+)$", "Izračunaj: $1", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^If\s+all\s+even\s+numbers\s+are\s+divisible\s+by\s+2\s+and\s+n\s+is\s+even,\s*is\s+n\s+divisible\s+by\s+2\?$", "Ako su svi parni brojevi djeljivi sa 2 i n je paran, da li je n djeljiv sa 2?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^In\s+proof\s+by\s+contradiction,\s*do\s+we\s+assume\s+the\s+opposite\s+of\s+the\s+claim\s+first\?$", "U dokazu kontradikcijom, da li prvo pretpostavljamo suprotno od tvrdnje?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Is\s+p\s*∨\s*¬p\s+always\s+true\?$", "Da li je p ∨ ¬p uvijek tačno?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+True\s*∧\s*False\?$", "Koliko je Tačno ∧ Netačno?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+True\s*∨\s*False\?$", "Koliko je Tačno ∨ Netačno?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+¬False\?$", "Koliko je ¬Netačno?", RegexOptions.IgnoreCase);
        text = Regex.Replace(
            text,
            @"^What\s+is\s+the\s+area\s+of\s+(?:a|an|the)\s+(.+?)\s+base\s*=\s*([0-9.,]+)\s*,\s*height\s*=\s*([0-9.,]+)\?$",
            "Kolika je površina $1 ako su osnovica $2 i visina $3?",
            RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Is\s+(.+?)\?$", "Da li je $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^How\s+many\s+sides\s+does\s+(?:an?\s+|the\s+)?(.+?)\s+have\?$", "Koliko stranica ima $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^How\s+many\s+vertices\s+does\s+(?:an?\s+|the\s+)?(.+?)\s+have\?$", "Koliko vrhova ima $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^What\s+is\s+(.+?)\?$", "Koliko je $1?", RegexOptions.IgnoreCase);

        text = Regex.Replace(text, @"^Perimeter\s+of\s+(.+?)\?$", "Koliki je obim $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Area\s+of\s+(.+?)\?$", "Kolika je površina $1?", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"^Circumference\s+of\s+(.+?)\?$", "Koliki je obim kruga $1?", RegexOptions.IgnoreCase);

        text = text.Replace("Simplify:", "Pojednostavi:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Expand:", "Razvij:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Factor:", "Faktoriši:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Solve:", "Riješi:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Solve for x:", "Riješi po x:", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Increase", "Povećaj", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Distance between", "Udaljenost između", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("Right triangle", "Pravougli trougao", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("hypotenuse", "hipotenuza", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("base", "osnovica", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("height", "visina", StringComparison.OrdinalIgnoreCase);

        text = text.Replace("sum of angles", "zbir uglova", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("all sides equal", "sve stranice jednake", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("third", "treći ugao", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("power set", "partitivni skup", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("What shape has", "Koji oblik ima", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("What type is", "Koji tip je", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("What is the", "Koliko je", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("What is", "Koliko je", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("How many", "Koliko", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("always true", "uvijek tačno", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("true", "tačno", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("false", "netačno", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" and ", " i ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" of ", " od ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" with ", " sa ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" has ", " ima ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace(" have ", " ima ", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("the sum of", "zbir", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("in a", "u", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("in an", "u", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("in the", "u", StringComparison.OrdinalIgnoreCase);

        foreach (var shape in ShapeTranslationsBs)
        {
            text = Regex.Replace(
                text,
                $@"\b{Regex.Escape(shape.Key)}\b",
                shape.Value,
                RegexOptions.IgnoreCase);
        }

        text = Regex.Replace(text, @"\bYes\b", "Da", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bNo\b", "Ne", RegexOptions.IgnoreCase);

        // Basic grammar cleanups for common shapes after Bosnian prepositions.
        text = Regex.Replace(text, @"\bu\s+trougao\b", "u trouglu", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bu\s+kvadrat\b", "u kvadratu", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bu\s+pravougaonik\b", "u pravougaoniku", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bthe\b", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bdoes\b", string.Empty, RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bdo\b", string.Empty, RegexOptions.IgnoreCase);

        // Cleanup after replacements.
        text = Regex.Replace(text, @"\s+", " ");
        text = text.Replace(" ?",
            "?",
            StringComparison.Ordinal);

        return text.Trim();
    }

    public string LocalizeExplanationText(string? text, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(text))
        {
            return text ?? string.Empty;
        }

        var trimmed = text.Trim();
        if (ExplanationExactTranslationsBs.TryGetValue(trimmed, out var exact))
        {
            return exact;
        }

        var value = trimmed;

        var replacements = new (string En, string Bs)[]
        {
            ("A triangle has", "Trougao ima"),
            ("A square has", "Kvadrat ima"),
            ("A cube has", "Kocka ima"),
            ("Count the straight edges", "Prebroj prave ivice"),
            ("Vertices are the corner points", "Vrhovi su ugaone tačke"),
            ("Addition combines numbers", "Sabiranje spaja brojeve"),
            ("Subtraction finds the difference", "Oduzimanje pronalazi razliku"),
            ("Multiplication is repeated addition", "Množenje je ponovljeno sabiranje"),
            ("Division splits a number into equal parts", "Dijeljenje dijeli broj na jednake dijelove"),
            ("Fractions show parts of a whole", "Razlomci predstavljaju dijelove cjeline"),
            ("Percent means 'per hundred'", "Procenat znači 'na sto'"),
            ("To solve equations", "Za rješavanje jednačina"),
            ("To simplify expressions", "Za pojednostavljenje izraza"),
            ("To expand, use FOIL", "Za razvijanje koristi FOIL"),
            ("Pythagorean theorem", "Pitagorina teorema"),
            ("Perimeter is the distance around a shape", "Obim je dužina ivice oko oblika"),
            ("Area is the space inside a shape", "Površina je prostor unutar oblika"),
            ("Break the problem into smaller steps", "Razloži zadatak na manje korake")
        };

        foreach (var replacement in replacements)
        {
            value = value.Replace(replacement.En, replacement.Bs, StringComparison.OrdinalIgnoreCase);
        }

        value = value.Replace("Example:", "Primjer:", StringComparison.OrdinalIgnoreCase);
        value = value.Replace("Page ", "Stranica ", StringComparison.OrdinalIgnoreCase);

        return value;
    }

    public string LocalizeReferenceTitle(string? title, string? languageCode)
    {
        if (!IsBosnian(languageCode) || string.IsNullOrWhiteSpace(title))
        {
            return title ?? string.Empty;
        }

        return ReferenceTitleTranslationsBs.TryGetValue(title.Trim(), out var translated)
            ? translated
            : title;
    }

    public string LocalizeReferenceLocation(int pageNumber, string? languageCode)
    {
        return IsBosnian(languageCode)
            ? $"Stranica {pageNumber}"
            : $"Page {pageNumber}";
    }

    public CrossMathMilestoneDto? LocalizeMilestone(CrossMathMilestoneDto? dto, string? languageCode)
    {
        if (dto == null || !IsBosnian(languageCode))
        {
            return dto;
        }

        var title = dto.Title;
        var subtitle = dto.Subtitle;

        title = title switch
        {
            "Addition Challenge" => "Izazov sabiranja",
            "Subtraction Challenge" => "Izazov oduzimanja",
            "Multiplication Challenge" => "Izazov množenja",
            "Division Challenge" => "Izazov dijeljenja",
            "Chapter Challenge" => "Izazov poglavlja",
            "Grand CrossMath Final" => "Veliko CrossMath finale",
            _ => title
        };

        subtitle = subtitle switch
        {
            "Fill the missing numbers and solve each row equation." => "Popuni nedostajuće brojeve i riješi jednačinu u svakom redu.",
            "Mixed operations from all four chapters." => "Miješane operacije iz sva četiri poglavlja.",
            _ => subtitle
        };

        return new CrossMathMilestoneDto
        {
            ChallengeKey = dto.ChallengeKey,
            Title = title,
            Subtitle = subtitle,
            Mode = dto.Mode,
            Size = dto.Size
        };
    }
}
